using System;
using UnityEngine;
using CasperSDK.Models;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Math.EC;

namespace CasperSDK.Utilities.Cryptography
{
    /// <summary>
    /// Real cryptographic key pair generator using BouncyCastle.
    /// Provides proper ED25519 and SECP256K1 key derivation for Casper Network.
    /// </summary>
    public static class CasperKeyGenerator
    {
        private static readonly SecureRandom SecureRandom = new SecureRandom();

        /// <summary>
        /// Generates a cryptographically secure ED25519 key pair
        /// </summary>
        /// <returns>Valid Casper Network key pair</returns>
        public static KeyPair GenerateED25519()
        {
            try
            {
                // Generate ED25519 key pair using BouncyCastle
                var keyPairGenerator = new Ed25519KeyPairGenerator();
                keyPairGenerator.Init(new Ed25519KeyGenerationParameters(SecureRandom));
                
                var keyPair = keyPairGenerator.GenerateKeyPair();
                
                var privateKey = (Ed25519PrivateKeyParameters)keyPair.Private;
                var publicKey = (Ed25519PublicKeyParameters)keyPair.Public;

                // Get raw bytes
                var privateKeyBytes = privateKey.GetEncoded();
                var publicKeyBytes = publicKey.GetEncoded();

                // Format for Casper: 01 prefix for ED25519
                var publicKeyHex = "01" + CryptoHelper.BytesToHex(publicKeyBytes);
                var privateKeyHex = CryptoHelper.BytesToHex(privateKeyBytes);

                // Generate account hash (using Blake2b)
                var accountHash = GenerateAccountHashBlake2b(publicKeyHex);

                return new KeyPair
                {
                    PublicKeyHex = publicKeyHex,
                    PrivateKeyHex = privateKeyHex,
                    Algorithm = KeyAlgorithm.ED25519,
                    AccountHash = accountHash
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] ED25519 key generation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Generates a cryptographically secure SECP256K1 key pair
        /// </summary>
        /// <returns>Valid Casper Network key pair</returns>
        public static KeyPair GenerateSECP256K1()
        {
            try
            {
                // Get SECP256K1 curve parameters
                var curve = ECNamedCurveTable.GetByName("secp256k1");
                var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

                // Generate key pair
                var keyParams = new ECKeyGenerationParameters(domainParams, SecureRandom);
                var generator = new ECKeyPairGenerator("ECDSA");
                generator.Init(keyParams);

                var keyPair = generator.GenerateKeyPair();

                var privateKey = (ECPrivateKeyParameters)keyPair.Private;
                var publicKey = (ECPublicKeyParameters)keyPair.Public;

                // Get private key bytes (32 bytes)
                var privateKeyBytes = privateKey.D.ToByteArrayUnsigned();
                // Pad to 32 bytes if necessary
                if (privateKeyBytes.Length < 32)
                {
                    var padded = new byte[32];
                    Buffer.BlockCopy(privateKeyBytes, 0, padded, 32 - privateKeyBytes.Length, privateKeyBytes.Length);
                    privateKeyBytes = padded;
                }

                // Get compressed public key (33 bytes)
                var publicKeyPoint = publicKey.Q;
                var publicKeyBytes = publicKeyPoint.GetEncoded(true); // true = compressed

                // Format for Casper: 02 prefix for SECP256K1
                var publicKeyHex = "02" + CryptoHelper.BytesToHex(publicKeyBytes);
                var privateKeyHex = CryptoHelper.BytesToHex(privateKeyBytes);

                // Generate account hash
                var accountHash = GenerateAccountHashBlake2b(publicKeyHex);

                return new KeyPair
                {
                    PublicKeyHex = publicKeyHex,
                    PrivateKeyHex = privateKeyHex,
                    Algorithm = KeyAlgorithm.SECP256K1,
                    AccountHash = accountHash
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] SECP256K1 key generation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imports an ED25519 key pair from a private key
        /// </summary>
        public static KeyPair ImportED25519(string privateKeyHex)
        {
            if (string.IsNullOrEmpty(privateKeyHex))
                throw new ArgumentException("Private key cannot be null or empty");

            try
            {
                var privateKeyBytes = CryptoHelper.HexToBytes(privateKeyHex);
                
                // Create private key parameters
                var privateKey = new Ed25519PrivateKeyParameters(privateKeyBytes, 0);
                
                // Derive public key from private key
                var publicKey = privateKey.GeneratePublicKey();
                var publicKeyBytes = publicKey.GetEncoded();

                var publicKeyHex = "01" + CryptoHelper.BytesToHex(publicKeyBytes);
                var accountHash = GenerateAccountHashBlake2b(publicKeyHex);

                return new KeyPair
                {
                    PublicKeyHex = publicKeyHex,
                    PrivateKeyHex = privateKeyHex,
                    Algorithm = KeyAlgorithm.ED25519,
                    AccountHash = accountHash
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] ED25519 import failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imports a SECP256K1 key pair from a private key
        /// </summary>
        public static KeyPair ImportSECP256K1(string privateKeyHex)
        {
            if (string.IsNullOrEmpty(privateKeyHex))
                throw new ArgumentException("Private key cannot be null or empty");

            try
            {
                var privateKeyBytes = CryptoHelper.HexToBytes(privateKeyHex);
                
                // Get curve parameters
                var curve = ECNamedCurveTable.GetByName("secp256k1");
                var domainParams = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H);

                // Create private key
                var d = new BigInteger(1, privateKeyBytes);
                var privateKey = new ECPrivateKeyParameters(d, domainParams);

                // Derive public key: Q = d * G
                var publicKeyPoint = curve.G.Multiply(d);
                var publicKeyBytes = publicKeyPoint.GetEncoded(true); // compressed

                var publicKeyHex = "02" + CryptoHelper.BytesToHex(publicKeyBytes);
                var accountHash = GenerateAccountHashBlake2b(publicKeyHex);

                return new KeyPair
                {
                    PublicKeyHex = publicKeyHex,
                    PrivateKeyHex = privateKeyHex,
                    Algorithm = KeyAlgorithm.SECP256K1,
                    AccountHash = accountHash
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] SECP256K1 import failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Signs data using ED25519
        /// </summary>
        public static byte[] SignED25519(byte[] data, string privateKeyHex)
        {
            var privateKeyBytes = CryptoHelper.HexToBytes(privateKeyHex);
            var privateKey = new Ed25519PrivateKeyParameters(privateKeyBytes, 0);

            var signer = new Ed25519Signer();
            signer.Init(true, privateKey);
            signer.BlockUpdate(data, 0, data.Length);

            return signer.GenerateSignature();
        }

        /// <summary>
        /// Verifies ED25519 signature
        /// </summary>
        public static bool VerifyED25519(byte[] data, byte[] signature, string publicKeyHex)
        {
            // Remove 01 prefix
            var keyHex = publicKeyHex.StartsWith("01") ? publicKeyHex.Substring(2) : publicKeyHex;
            var publicKeyBytes = CryptoHelper.HexToBytes(keyHex);
            var publicKey = new Ed25519PublicKeyParameters(publicKeyBytes, 0);

            var verifier = new Ed25519Signer();
            verifier.Init(false, publicKey);
            verifier.BlockUpdate(data, 0, data.Length);

            return verifier.VerifySignature(signature);
        }

        /// <summary>
        /// Generates account hash using Blake2b (via BouncyCastle)
        /// </summary>
        private static string GenerateAccountHashBlake2b(string publicKeyHex)
        {
            // Get algorithm prefix
            var prefix = publicKeyHex.Substring(0, 2);
            var algorithmName = prefix == "01" ? "ed25519" : "secp256k1";
            
            // Get public key bytes (without prefix)
            var keyBytes = CryptoHelper.HexToBytes(publicKeyHex.Substring(2));
            
            // Prepare data: algorithm_name + 0x00 + public_key_bytes
            var algorithmBytes = System.Text.Encoding.ASCII.GetBytes(algorithmName);
            var data = new byte[algorithmBytes.Length + 1 + keyBytes.Length];
            Buffer.BlockCopy(algorithmBytes, 0, data, 0, algorithmBytes.Length);
            data[algorithmBytes.Length] = 0x00;
            Buffer.BlockCopy(keyBytes, 0, data, algorithmBytes.Length + 1, keyBytes.Length);

            // Use Blake2b-256
            var blake2b = new Org.BouncyCastle.Crypto.Digests.Blake2bDigest(256);
            blake2b.BlockUpdate(data, 0, data.Length);
            
            var hash = new byte[32];
            blake2b.DoFinal(hash, 0);

            return "account-hash-" + CryptoHelper.BytesToHex(hash);
        }
    }
}
