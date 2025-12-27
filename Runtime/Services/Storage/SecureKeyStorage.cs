using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
using CasperSDK.Models;
using CasperSDK.Utilities.Cryptography;

namespace CasperSDK.Services.Storage
{
    /// <summary>
    /// Secure storage for cryptographic keys using AES encryption.
    /// Keys are encrypted before storing in Unity PlayerPrefs.
    /// </summary>
    public class SecureKeyStorage
    {
        private const string STORAGE_PREFIX = "CasperSDK_";
        private const string KEYS_LIST_KEY = STORAGE_PREFIX + "KeysList";
        private const int KEY_SIZE = 256;
        private const int SALT_SIZE = 32;
        private const int ITERATIONS = 100000;

        private byte[] _masterKey;
        private bool _isUnlocked;

        /// <summary>
        /// Whether the storage is currently unlocked
        /// </summary>
        public bool IsUnlocked => _isUnlocked;

        #region Initialization

        /// <summary>
        /// Unlocks the storage with a password
        /// </summary>
        /// <param name="password">User's password</param>
        public void Unlock(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password is required");

            _masterKey = DeriveKey(password, GetOrCreateSalt());
            _isUnlocked = true;

            Debug.Log("[CasperSDK] Secure storage unlocked");
        }

        /// <summary>
        /// Locks the storage and clears the master key from memory
        /// </summary>
        public void Lock()
        {
            if (_masterKey != null)
            {
                Array.Clear(_masterKey, 0, _masterKey.Length);
                _masterKey = null;
            }
            _isUnlocked = false;

            Debug.Log("[CasperSDK] Secure storage locked");
        }

        /// <summary>
        /// Changes the password (re-encrypts all stored keys)
        /// </summary>
        public void ChangePassword(string oldPassword, string newPassword)
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Storage must be unlocked first");

            // Get all keys with old password
            var allKeys = GetAllKeyPairs();

            // Create new master key with new password
            var newSalt = GenerateRandomBytes(SALT_SIZE);
            var newMasterKey = DeriveKey(newPassword, newSalt);

            // Save new salt
            PlayerPrefs.SetString(STORAGE_PREFIX + "Salt", Convert.ToBase64String(newSalt));

            // Re-encrypt all keys
            var oldMasterKey = _masterKey;
            _masterKey = newMasterKey;

            foreach (var key in allKeys)
            {
                SaveKeyPair(key.label, key.keyPair);
            }

            // Clean up old key
            Array.Clear(oldMasterKey, 0, oldMasterKey.Length);

            Debug.Log("[CasperSDK] Password changed successfully");
        }

        #endregion

        #region Key Storage

        /// <summary>
        /// Saves a key pair with a label
        /// </summary>
        /// <param name="label">Friendly name for the key</param>
        /// <param name="keyPair">Key pair to store</param>
        public void SaveKeyPair(string label, KeyPair keyPair)
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Storage must be unlocked first");
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is required");
            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));

            // Serialize key pair
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new StoredKeyPair
            {
                Label = label,
                PublicKeyHex = keyPair.PublicKeyHex,
                PrivateKeyHex = keyPair.PrivateKeyHex,
                AccountHash = keyPair.AccountHash,
                Algorithm = keyPair.Algorithm.ToString(),
                CreatedAt = DateTime.UtcNow.ToString("O")
            });

            // Encrypt
            var encrypted = Encrypt(json);
            var storageKey = GetStorageKey(label);

            // Store
            PlayerPrefs.SetString(storageKey, encrypted);
            AddToKeysList(label);
            PlayerPrefs.Save();

            Debug.Log($"[CasperSDK] Saved key pair: {label}");
        }

        /// <summary>
        /// Loads a key pair by label
        /// </summary>
        public KeyPair LoadKeyPair(string label)
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Storage must be unlocked first");
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is required");

            var storageKey = GetStorageKey(label);
            var encrypted = PlayerPrefs.GetString(storageKey, null);

            if (string.IsNullOrEmpty(encrypted))
                return null;

            try
            {
                var json = Decrypt(encrypted);
                var stored = Newtonsoft.Json.JsonConvert.DeserializeObject<StoredKeyPair>(json);

                return new KeyPair
                {
                    PublicKeyHex = stored.PublicKeyHex,
                    PrivateKeyHex = stored.PrivateKeyHex,
                    AccountHash = stored.AccountHash,
                    Algorithm = Enum.TryParse<KeyAlgorithm>(stored.Algorithm, out var alg) ? alg : KeyAlgorithm.ED25519
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to load key pair: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deletes a key pair by label
        /// </summary>
        public void DeleteKeyPair(string label)
        {
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is required");

            var storageKey = GetStorageKey(label);
            PlayerPrefs.DeleteKey(storageKey);
            RemoveFromKeysList(label);
            PlayerPrefs.Save();

            Debug.Log($"[CasperSDK] Deleted key pair: {label}");
        }

        /// <summary>
        /// Gets all stored key labels
        /// </summary>
        public string[] GetAllLabels()
        {
            var listJson = PlayerPrefs.GetString(KEYS_LIST_KEY, "[]");
            return Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(listJson) ?? Array.Empty<string>();
        }

        /// <summary>
        /// Gets all stored key pairs
        /// </summary>
        public (string label, KeyPair keyPair)[] GetAllKeyPairs()
        {
            if (!_isUnlocked)
                throw new InvalidOperationException("Storage must be unlocked first");

            var labels = GetAllLabels();
            var result = new (string, KeyPair)[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                result[i] = (labels[i], LoadKeyPair(labels[i]));
            }

            return result;
        }

        /// <summary>
        /// Checks if a key pair exists
        /// </summary>
        public bool HasKeyPair(string label)
        {
            var storageKey = GetStorageKey(label);
            return PlayerPrefs.HasKey(storageKey);
        }

        #endregion

        #region Encryption

        private string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.KeySize = KEY_SIZE;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor(_masterKey, aes.IV);
            var plainBytes = Encoding.UTF8.GetBytes(plainText);

            using var ms = new MemoryStream();
            // Write IV first
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plainBytes, 0, plainBytes.Length);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private string Decrypt(string cipherText)
        {
            var cipherBytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.KeySize = KEY_SIZE;

            // Extract IV from the beginning
            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(cipherBytes, 0, iv, 0, iv.Length);

            using var decryptor = aes.CreateDecryptor(_masterKey, iv);

            using var ms = new MemoryStream(cipherBytes, iv.Length, cipherBytes.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cs, Encoding.UTF8);

            return reader.ReadToEnd();
        }

        private byte[] DeriveKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                password,
                salt,
                ITERATIONS,
                HashAlgorithmName.SHA256);

            return pbkdf2.GetBytes(KEY_SIZE / 8);
        }

        private byte[] GetOrCreateSalt()
        {
            var saltKey = STORAGE_PREFIX + "Salt";
            var saltBase64 = PlayerPrefs.GetString(saltKey, null);

            if (string.IsNullOrEmpty(saltBase64))
            {
                var salt = GenerateRandomBytes(SALT_SIZE);
                PlayerPrefs.SetString(saltKey, Convert.ToBase64String(salt));
                PlayerPrefs.Save();
                return salt;
            }

            return Convert.FromBase64String(saltBase64);
        }

        private byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        #endregion

        #region Helpers

        private string GetStorageKey(string label)
        {
            return STORAGE_PREFIX + "Key_" + Convert.ToBase64String(
                Encoding.UTF8.GetBytes(label)).Replace("=", "").Replace("+", "_").Replace("/", "-");
        }

        private void AddToKeysList(string label)
        {
            var labels = new System.Collections.Generic.List<string>(GetAllLabels());
            if (!labels.Contains(label))
            {
                labels.Add(label);
                PlayerPrefs.SetString(KEYS_LIST_KEY, Newtonsoft.Json.JsonConvert.SerializeObject(labels));
            }
        }

        private void RemoveFromKeysList(string label)
        {
            var labels = new System.Collections.Generic.List<string>(GetAllLabels());
            labels.Remove(label);
            PlayerPrefs.SetString(KEYS_LIST_KEY, Newtonsoft.Json.JsonConvert.SerializeObject(labels));
        }

        #endregion
    }

    [Serializable]
    internal class StoredKeyPair
    {
        public string Label { get; set; }
        public string PublicKeyHex { get; set; }
        public string PrivateKeyHex { get; set; }
        public string AccountHash { get; set; }
        public string Algorithm { get; set; }
        public string CreatedAt { get; set; }
    }
}
