using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CasperSDK.Utilities.Cryptography
{
    /// <summary>
    /// Cryptographic utilities for Casper Network operations.
    /// Provides hashing, key generation, and encoding functions.
    /// </summary>
    public static class CryptoHelper
    {
        /// <summary>
        /// Generates a cryptographically secure random byte array
        /// </summary>
        /// <param name="length">Length of the byte array</param>
        /// <returns>Random bytes</returns>
        public static byte[] GenerateSecureRandomBytes(int length)
        {
            var bytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// Computes Blake2b-256 hash (used by Casper for account hashes)
        /// Note: This is a simplified SHA256 implementation. 
        /// For production, use a proper Blake2b library.
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>Hash bytes</returns>
        public static byte[] ComputeBlake2b256(byte[] data)
        {
            // Note: Casper uses Blake2b-256, but for Unity compatibility
            // we use SHA256 as a placeholder. For production, integrate
            // a proper Blake2b implementation.
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        /// <summary>
        /// Computes SHA256 hash
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <returns>Hash bytes</returns>
        public static byte[] ComputeSha256(byte[] data)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(data);
            }
        }

        /// <summary>
        /// Converts bytes to hexadecimal string
        /// </summary>
        /// <param name="bytes">Bytes to convert</param>
        /// <returns>Hex string (lowercase)</returns>
        public static string BytesToHex(byte[] bytes)
        {
            if (bytes == null) return string.Empty;
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Converts hexadecimal string to bytes
        /// </summary>
        /// <param name="hex">Hex string</param>
        /// <returns>Byte array</returns>
        public static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Array.Empty<byte>();
            
            // Remove 0x prefix if present
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex.Substring(2);
            }

            if (hex.Length % 2 != 0)
            {
                hex = "0" + hex; // Pad with leading zero
            }

            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// Generates an account hash from a public key.
        /// Format: account-hash-{blake2b256(algorithm_prefix + public_key_bytes)}
        /// </summary>
        /// <param name="publicKeyHex">Public key in hex format (with algorithm prefix)</param>
        /// <returns>Account hash string</returns>
        public static string GenerateAccountHash(string publicKeyHex)
        {
            if (string.IsNullOrEmpty(publicKeyHex))
            {
                throw new ArgumentException("Public key cannot be null or empty");
            }

            try
            {
                // Remove algorithm prefix for hash calculation
                var algorithm = publicKeyHex.Substring(0, 2);
                var keyBytes = HexToBytes(publicKeyHex.Substring(2));
                
                // Prepend the algorithm identifier as a string
                var algorithmName = algorithm == "01" ? "ed25519" : "secp256k1";
                var algorithmBytes = Encoding.ASCII.GetBytes(algorithmName);
                
                // Combine: algorithm_name (as string) + 0x00 separator + public_key_bytes
                var combined = new byte[algorithmBytes.Length + 1 + keyBytes.Length];
                Buffer.BlockCopy(algorithmBytes, 0, combined, 0, algorithmBytes.Length);
                combined[algorithmBytes.Length] = 0x00; // Separator
                Buffer.BlockCopy(keyBytes, 0, combined, algorithmBytes.Length + 1, keyBytes.Length);

                // Hash and format
                var hash = ComputeBlake2b256(combined);
                return "account-hash-" + BytesToHex(hash);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to generate account hash: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Validates a public key format
        /// </summary>
        /// <param name="publicKeyHex">Public key hex string</param>
        /// <returns>True if valid format</returns>
        public static bool ValidatePublicKey(string publicKeyHex)
        {
            if (string.IsNullOrEmpty(publicKeyHex))
                return false;

            // Min length: 2 (prefix) + 64 (32 bytes = 64 hex chars)
            if (publicKeyHex.Length < 66)
                return false;

            // Check prefix (01 for ED25519, 02 for SECP256K1)
            var prefix = publicKeyHex.Substring(0, 2);
            if (prefix != "01" && prefix != "02")
                return false;

            // Check if all characters are valid hex
            try
            {
                HexToBytes(publicKeyHex);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a Casper account hash format
        /// </summary>
        /// <param name="accountHash">Account hash string</param>
        /// <returns>True if valid format</returns>
        public static bool ValidateAccountHash(string accountHash)
        {
            if (string.IsNullOrEmpty(accountHash))
                return false;

            if (!accountHash.StartsWith("account-hash-"))
                return false;

            var hashPart = accountHash.Substring(13);
            if (hashPart.Length != 64)
                return false;

            try
            {
                HexToBytes(hashPart);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
