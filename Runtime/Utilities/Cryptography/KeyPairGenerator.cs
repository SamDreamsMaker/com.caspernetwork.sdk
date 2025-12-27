using System;
using UnityEngine;
using CasperSDK.Models;

namespace CasperSDK.Utilities.Cryptography
{
    /// <summary>
    /// Key pair generator for Casper Network.
    /// Generates cryptographically secure key pairs for ED25519 and SECP256K1 algorithms.
    /// </summary>
    /// <remarks>
    /// Note: This implementation generates random key pairs for testing.
    /// For production use with real transactions, integrate a proper
    /// cryptographic library like BouncyCastle or NSec for actual
    /// elliptic curve key derivation.
    /// </remarks>
    public static class KeyPairGenerator
    {
        /// <summary>
        /// ED25519 public key prefix as per Casper specification
        /// </summary>
        public const string ED25519_PREFIX = "01";
        
        /// <summary>
        /// SECP256K1 public key prefix as per Casper specification
        /// </summary>
        public const string SECP256K1_PREFIX = "02";

        /// <summary>
        /// ED25519 private key length in bytes
        /// </summary>
        public const int ED25519_PRIVATE_KEY_LENGTH = 32;

        /// <summary>
        /// ED25519 public key length in bytes
        /// </summary>
        public const int ED25519_PUBLIC_KEY_LENGTH = 32;

        /// <summary>
        /// SECP256K1 private key length in bytes
        /// </summary>
        public const int SECP256K1_PRIVATE_KEY_LENGTH = 32;

        /// <summary>
        /// SECP256K1 compressed public key length in bytes
        /// </summary>
        public const int SECP256K1_PUBLIC_KEY_LENGTH = 33;

        /// <summary>
        /// Generates a new key pair for the specified algorithm
        /// </summary>
        /// <param name="algorithm">Key algorithm (ED25519 or SECP256K1)</param>
        /// <returns>Generated key pair</returns>
        public static KeyPair Generate(KeyAlgorithm algorithm = KeyAlgorithm.ED25519)
        {
            try
            {
                return algorithm switch
                {
                    KeyAlgorithm.ED25519 => GenerateED25519KeyPair(),
                    KeyAlgorithm.SECP256K1 => GenerateSECP256K1KeyPair(),
                    _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Key generation failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imports a key pair from a private key hex string
        /// </summary>
        /// <param name="privateKeyHex">Private key in hex format</param>
        /// <param name="algorithm">Key algorithm</param>
        /// <returns>Imported key pair</returns>
        public static KeyPair Import(string privateKeyHex, KeyAlgorithm algorithm = KeyAlgorithm.ED25519)
        {
            if (string.IsNullOrWhiteSpace(privateKeyHex))
            {
                throw new ArgumentException("Private key cannot be null or empty");
            }

            try
            {
                var privateKeyBytes = CryptoHelper.HexToBytes(privateKeyHex);
                
                return algorithm switch
                {
                    KeyAlgorithm.ED25519 => DeriveED25519KeyPair(privateKeyBytes),
                    KeyAlgorithm.SECP256K1 => DeriveSECP256K1KeyPair(privateKeyBytes),
                    _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Key import failed: {ex.Message}");
                throw;
            }
        }

        private static KeyPair GenerateED25519KeyPair()
        {
            // Generate random private key (32 bytes)
            var privateKeyBytes = CryptoHelper.GenerateSecureRandomBytes(ED25519_PRIVATE_KEY_LENGTH);
            return DeriveED25519KeyPair(privateKeyBytes);
        }

        private static KeyPair DeriveED25519KeyPair(byte[] privateKeyBytes)
        {
            // Ensure correct length
            if (privateKeyBytes.Length < ED25519_PRIVATE_KEY_LENGTH)
            {
                throw new ArgumentException($"ED25519 private key must be at least {ED25519_PRIVATE_KEY_LENGTH} bytes");
            }

            // Truncate to correct length if necessary
            var privateKey = new byte[ED25519_PRIVATE_KEY_LENGTH];
            Buffer.BlockCopy(privateKeyBytes, 0, privateKey, 0, ED25519_PRIVATE_KEY_LENGTH);

            // Note: In a real implementation, we would derive the public key using
            // ED25519 elliptic curve multiplication. For now, we simulate this by
            // hashing the private key (this is NOT cryptographically correct for signing).
            // For production, use a proper ED25519 library.
            var publicKeyBytes = CryptoHelper.ComputeSha256(privateKey);
            
            // Take first 32 bytes as public key
            var publicKey = new byte[ED25519_PUBLIC_KEY_LENGTH];
            Buffer.BlockCopy(publicKeyBytes, 0, publicKey, 0, ED25519_PUBLIC_KEY_LENGTH);

            var publicKeyHex = ED25519_PREFIX + CryptoHelper.BytesToHex(publicKey);
            var privateKeyHex = CryptoHelper.BytesToHex(privateKey);
            var accountHash = CryptoHelper.GenerateAccountHash(publicKeyHex);

            return new KeyPair
            {
                PublicKeyHex = publicKeyHex,
                PrivateKeyHex = privateKeyHex,
                Algorithm = KeyAlgorithm.ED25519,
                AccountHash = accountHash
            };
        }

        private static KeyPair GenerateSECP256K1KeyPair()
        {
            // Generate random private key (32 bytes)
            var privateKeyBytes = CryptoHelper.GenerateSecureRandomBytes(SECP256K1_PRIVATE_KEY_LENGTH);
            return DeriveSECP256K1KeyPair(privateKeyBytes);
        }

        private static KeyPair DeriveSECP256K1KeyPair(byte[] privateKeyBytes)
        {
            // Ensure correct length
            if (privateKeyBytes.Length < SECP256K1_PRIVATE_KEY_LENGTH)
            {
                throw new ArgumentException($"SECP256K1 private key must be at least {SECP256K1_PRIVATE_KEY_LENGTH} bytes");
            }

            // Truncate to correct length if necessary
            var privateKey = new byte[SECP256K1_PRIVATE_KEY_LENGTH];
            Buffer.BlockCopy(privateKeyBytes, 0, privateKey, 0, SECP256K1_PRIVATE_KEY_LENGTH);

            // Note: In a real implementation, we would derive the public key using
            // SECP256K1 elliptic curve multiplication. For now, we simulate this.
            // For production, use a proper SECP256K1 library (like BouncyCastle or libsecp256k1).
            var hashBytes = CryptoHelper.ComputeSha256(privateKey);
            
            // Simulate compressed public key (33 bytes)
            var publicKey = new byte[SECP256K1_PUBLIC_KEY_LENGTH];
            publicKey[0] = (byte)(0x02 + (hashBytes[hashBytes.Length - 1] & 0x01)); // Compression flag
            Buffer.BlockCopy(hashBytes, 0, publicKey, 1, ED25519_PUBLIC_KEY_LENGTH);

            var publicKeyHex = SECP256K1_PREFIX + CryptoHelper.BytesToHex(publicKey);
            var privateKeyHex = CryptoHelper.BytesToHex(privateKey);
            var accountHash = CryptoHelper.GenerateAccountHash(publicKeyHex);

            return new KeyPair
            {
                PublicKeyHex = publicKeyHex,
                PrivateKeyHex = privateKeyHex,
                Algorithm = KeyAlgorithm.SECP256K1,
                AccountHash = accountHash
            };
        }

        /// <summary>
        /// Validates that a private key is valid for the given algorithm
        /// </summary>
        public static bool ValidatePrivateKey(string privateKeyHex, KeyAlgorithm algorithm)
        {
            if (string.IsNullOrEmpty(privateKeyHex))
                return false;

            try
            {
                var bytes = CryptoHelper.HexToBytes(privateKeyHex);
                
                return algorithm switch
                {
                    KeyAlgorithm.ED25519 => bytes.Length >= ED25519_PRIVATE_KEY_LENGTH,
                    KeyAlgorithm.SECP256K1 => bytes.Length >= SECP256K1_PRIVATE_KEY_LENGTH,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }
    }
}
