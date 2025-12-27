using System;
using UnityEngine;
using CasperSDK.Models;
using CasperSDK.Utilities.Cryptography;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Crypto.Digests;

namespace CasperSDK.Services.Deploy
{
    /// <summary>
    /// Signs Casper deploys using ED25519 or SECP256K1 algorithms.
    /// Creates approvals that can be added to a deploy for submission.
    /// </summary>
    public static class DeploySigner
    {
        /// <summary>
        /// Signs a deploy and returns the signed deploy with approval added
        /// </summary>
        /// <param name="deploy">The deploy to sign</param>
        /// <param name="keyPair">The key pair to sign with</param>
        /// <returns>Deploy with approval added</returns>
        public static Models.Deploy SignDeploy(Models.Deploy deploy, KeyPair keyPair)
        {
            if (deploy == null)
                throw new ArgumentNullException(nameof(deploy));
            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));
            if (string.IsNullOrEmpty(deploy.Hash))
                throw new ArgumentException("Deploy must have a hash before signing");

            var signature = Sign(deploy.Hash, keyPair);
            var approval = CreateApproval(keyPair.PublicKeyHex, signature);

            // Add approval to deploy
            var approvals = deploy.Approvals ?? Array.Empty<DeployApproval>();
            var newApprovals = new DeployApproval[approvals.Length + 1];
            Array.Copy(approvals, newApprovals, approvals.Length);
            newApprovals[approvals.Length] = approval;

            deploy.Approvals = newApprovals;
            return deploy;
        }

        /// <summary>
        /// Signs a deploy hash with the given key pair
        /// </summary>
        /// <param name="deployHashHex">The deploy hash in hex format</param>
        /// <param name="keyPair">The key pair to sign with</param>
        /// <returns>Signature in hex format with algorithm prefix</returns>
        public static string Sign(string deployHashHex, KeyPair keyPair)
        {
            if (string.IsNullOrEmpty(deployHashHex))
                throw new ArgumentException("Deploy hash cannot be null or empty");
            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));

            var hashBytes = CryptoHelper.HexToBytes(deployHashHex);

            return keyPair.Algorithm switch
            {
                KeyAlgorithm.ED25519 => SignED25519(hashBytes, keyPair.PrivateKeyHex),
                KeyAlgorithm.SECP256K1 => SignSECP256K1(hashBytes, keyPair.PrivateKeyHex),
                _ => throw new ArgumentException($"Unsupported algorithm: {keyPair.Algorithm}")
            };
        }

        /// <summary>
        /// Verifies a signature against a deploy hash
        /// </summary>
        /// <param name="deployHashHex">The deploy hash</param>
        /// <param name="signatureHex">The signature (with algorithm prefix)</param>
        /// <param name="publicKeyHex">The signer's public key (with algorithm prefix)</param>
        /// <returns>True if signature is valid</returns>
        public static bool Verify(string deployHashHex, string signatureHex, string publicKeyHex)
        {
            var hashBytes = CryptoHelper.HexToBytes(deployHashHex);
            
            // Determine algorithm from public key prefix
            var algorithm = publicKeyHex.StartsWith("01") ? KeyAlgorithm.ED25519 : KeyAlgorithm.SECP256K1;

            return algorithm switch
            {
                KeyAlgorithm.ED25519 => VerifyED25519(hashBytes, signatureHex, publicKeyHex),
                KeyAlgorithm.SECP256K1 => VerifySECP256K1(hashBytes, signatureHex, publicKeyHex),
                _ => false
            };
        }

        /// <summary>
        /// Creates a deploy approval from public key and signature
        /// </summary>
        private static DeployApproval CreateApproval(string publicKeyHex, string signatureHex)
        {
            return new DeployApproval
            {
                Signer = publicKeyHex,
                Signature = signatureHex
            };
        }

        #region ED25519 Signing

        private static string SignED25519(byte[] data, string privateKeyHex)
        {
            try
            {
                var privateKeyBytes = CryptoHelper.HexToBytes(privateKeyHex);
                var privateKey = new Ed25519PrivateKeyParameters(privateKeyBytes, 0);

                var signer = new Ed25519Signer();
                signer.Init(true, privateKey);
                signer.BlockUpdate(data, 0, data.Length);

                var signatureBytes = signer.GenerateSignature();
                
                // Casper format: 01 prefix for ED25519 signature
                return "01" + CryptoHelper.BytesToHex(signatureBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] ED25519 signing failed: {ex.Message}");
                throw;
            }
        }

        private static bool VerifyED25519(byte[] data, string signatureHex, string publicKeyHex)
        {
            try
            {
                // Remove algorithm prefix from signature (01)
                var sigHex = signatureHex.StartsWith("01") ? signatureHex.Substring(2) : signatureHex;
                var signatureBytes = CryptoHelper.HexToBytes(sigHex);

                // Remove algorithm prefix from public key (01)
                var keyHex = publicKeyHex.StartsWith("01") ? publicKeyHex.Substring(2) : publicKeyHex;
                var publicKeyBytes = CryptoHelper.HexToBytes(keyHex);

                var publicKey = new Ed25519PublicKeyParameters(publicKeyBytes, 0);

                var verifier = new Ed25519Signer();
                verifier.Init(false, publicKey);
                verifier.BlockUpdate(data, 0, data.Length);

                return verifier.VerifySignature(signatureBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] ED25519 verification failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region SECP256K1 Signing

        private static string SignSECP256K1(byte[] data, string privateKeyHex)
        {
            try
            {
                var privateKeyBytes = CryptoHelper.HexToBytes(privateKeyHex);

                // Get curve parameters
                var curve = ECNamedCurveTable.GetByName("secp256k1");
                var domainParams = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(
                    curve.Curve, curve.G, curve.N, curve.H);

                var d = new BigInteger(1, privateKeyBytes);
                var privateKey = new ECPrivateKeyParameters(d, domainParams);

                // SECP256K1 uses SHA256 for the digest
                var sha256 = new Sha256Digest();
                sha256.BlockUpdate(data, 0, data.Length);
                var hash = new byte[32];
                sha256.DoFinal(hash, 0);

                // Sign with ECDSA
                var signer = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
                signer.Init(true, privateKey);
                var signature = signer.GenerateSignature(hash);

                // Encode signature as R || S (64 bytes)
                var r = signature[0].ToByteArrayUnsigned();
                var s = signature[1].ToByteArrayUnsigned();

                var signatureBytes = new byte[64];
                Buffer.BlockCopy(r, 0, signatureBytes, 32 - r.Length, r.Length);
                Buffer.BlockCopy(s, 0, signatureBytes, 64 - s.Length, s.Length);

                // Casper format: 02 prefix for SECP256K1 signature
                return "02" + CryptoHelper.BytesToHex(signatureBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] SECP256K1 signing failed: {ex.Message}");
                throw;
            }
        }

        private static bool VerifySECP256K1(byte[] data, string signatureHex, string publicKeyHex)
        {
            try
            {
                // Remove algorithm prefix
                var sigHex = signatureHex.StartsWith("02") ? signatureHex.Substring(2) : signatureHex;
                var signatureBytes = CryptoHelper.HexToBytes(sigHex);

                var keyHex = publicKeyHex.StartsWith("02") ? publicKeyHex.Substring(2) : publicKeyHex;
                var publicKeyBytes = CryptoHelper.HexToBytes(keyHex);

                // Get curve parameters
                var curve = ECNamedCurveTable.GetByName("secp256k1");
                var domainParams = new Org.BouncyCastle.Crypto.Parameters.ECDomainParameters(
                    curve.Curve, curve.G, curve.N, curve.H);

                // Decode public key point
                var q = curve.Curve.DecodePoint(publicKeyBytes);
                var publicKey = new ECPublicKeyParameters(q, domainParams);

                // Hash the data with SHA256
                var sha256 = new Sha256Digest();
                sha256.BlockUpdate(data, 0, data.Length);
                var hash = new byte[32];
                sha256.DoFinal(hash, 0);

                // Extract R and S from signature
                var r = new BigInteger(1, signatureBytes, 0, 32);
                var s = new BigInteger(1, signatureBytes, 32, 32);

                // Verify
                var verifier = new Org.BouncyCastle.Crypto.Signers.ECDsaSigner();
                verifier.Init(false, publicKey);

                return verifier.VerifySignature(hash, r, s);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] SECP256K1 verification failed: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
