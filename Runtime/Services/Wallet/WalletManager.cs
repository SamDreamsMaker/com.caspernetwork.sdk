using System;
using System.Collections.Generic;
using UnityEngine;
using CasperSDK.Models;
using CasperSDK.Utilities.Cryptography;

namespace CasperSDK.Services.Wallet
{
    /// <summary>
    /// Manages multiple wallet accounts.
    /// Provides account switching, naming, and organization features.
    /// </summary>
    public class WalletManager
    {
        private readonly Storage.SecureKeyStorage _storage;
        private readonly List<WalletAccount> _accounts;
        private WalletAccount _activeAccount;
        private readonly bool _enableLogging;

        /// <summary>
        /// Current active account
        /// </summary>
        public WalletAccount ActiveAccount => _activeAccount;

        /// <summary>
        /// All managed accounts
        /// </summary>
        public IReadOnlyList<WalletAccount> Accounts => _accounts;

        /// <summary>
        /// Number of accounts
        /// </summary>
        public int AccountCount => _accounts.Count;

        /// <summary>
        /// Event fired when active account changes
        /// </summary>
        public event Action<WalletAccount> OnActiveAccountChanged;

        /// <summary>
        /// Event fired when account list changes
        /// </summary>
        public event Action OnAccountsChanged;

        public WalletManager(bool enableLogging = true)
        {
            _storage = new Storage.SecureKeyStorage();
            _accounts = new List<WalletAccount>();
            _enableLogging = enableLogging;
        }

        #region Initialization

        /// <summary>
        /// Unlocks the wallet with a password
        /// </summary>
        public void Unlock(string password)
        {
            _storage.Unlock(password);
            LoadAllAccounts();

            if (_accounts.Count > 0)
            {
                SetActiveAccount(_accounts[0].Label);
            }

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Wallet unlocked with {_accounts.Count} accounts");
        }

        /// <summary>
        /// Locks the wallet and clears all accounts from memory
        /// </summary>
        public void Lock()
        {
            _accounts.Clear();
            _activeAccount = null;
            _storage.Lock();
            OnAccountsChanged?.Invoke();

            if (_enableLogging)
                Debug.Log("[CasperSDK] Wallet locked");
        }

        /// <summary>
        /// Whether the wallet is currently unlocked
        /// </summary>
        public bool IsUnlocked => _storage.IsUnlocked;

        #endregion

        #region Account Management

        /// <summary>
        /// Creates a new account with a generated key pair
        /// </summary>
        /// <param name="label">Friendly name for the account</param>
        /// <param name="algorithm">Key algorithm (default ED25519)</param>
        public WalletAccount CreateAccount(string label, KeyAlgorithm algorithm = KeyAlgorithm.ED25519)
        {
            if (!_storage.IsUnlocked)
                throw new InvalidOperationException("Wallet must be unlocked first");
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is required");
            if (AccountExists(label))
                throw new ArgumentException($"Account '{label}' already exists");

            var keyPair = algorithm == KeyAlgorithm.ED25519
                ? CasperKeyGenerator.GenerateED25519()
                : CasperKeyGenerator.GenerateSECP256K1();

            _storage.SaveKeyPair(label, keyPair);

            var account = new WalletAccount
            {
                Label = label,
                PublicKey = keyPair.PublicKeyHex,
                AccountHash = keyPair.AccountHash,
                Algorithm = algorithm,
                KeyPair = keyPair,
                CreatedAt = DateTime.UtcNow
            };

            _accounts.Add(account);
            OnAccountsChanged?.Invoke();

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Created account: {label}");

            return account;
        }

        /// <summary>
        /// Imports an account from a key pair
        /// </summary>
        public WalletAccount ImportAccount(string label, KeyPair keyPair)
        {
            if (!_storage.IsUnlocked)
                throw new InvalidOperationException("Wallet must be unlocked first");
            if (string.IsNullOrEmpty(label))
                throw new ArgumentException("Label is required");
            if (keyPair == null)
                throw new ArgumentNullException(nameof(keyPair));
            if (AccountExists(label))
                throw new ArgumentException($"Account '{label}' already exists");

            _storage.SaveKeyPair(label, keyPair);

            var account = new WalletAccount
            {
                Label = label,
                PublicKey = keyPair.PublicKeyHex,
                AccountHash = keyPair.AccountHash,
                Algorithm = keyPair.Algorithm,
                KeyPair = keyPair,
                CreatedAt = DateTime.UtcNow
            };

            _accounts.Add(account);
            OnAccountsChanged?.Invoke();

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Imported account: {label}");

            return account;
        }

        /// <summary>
        /// Imports an account from a PEM file
        /// </summary>
        public WalletAccount ImportFromPem(string label, string pemFilePath)
        {
            var keyPair = KeyExporter.ImportFromPemFile(pemFilePath);
            return ImportAccount(label, keyPair);
        }

        /// <summary>
        /// Deletes an account
        /// </summary>
        public void DeleteAccount(string label)
        {
            var account = GetAccount(label);
            if (account == null)
                throw new ArgumentException($"Account '{label}' not found");

            _storage.DeleteKeyPair(label);
            _accounts.Remove(account);

            if (_activeAccount?.Label == label)
            {
                _activeAccount = _accounts.Count > 0 ? _accounts[0] : null;
                OnActiveAccountChanged?.Invoke(_activeAccount);
            }

            OnAccountsChanged?.Invoke();

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Deleted account: {label}");
        }

        /// <summary>
        /// Renames an account
        /// </summary>
        public void RenameAccount(string oldLabel, string newLabel)
        {
            var account = GetAccount(oldLabel);
            if (account == null)
                throw new ArgumentException($"Account '{oldLabel}' not found");
            if (AccountExists(newLabel))
                throw new ArgumentException($"Account '{newLabel}' already exists");

            // Save with new label, delete old
            _storage.SaveKeyPair(newLabel, account.KeyPair);
            _storage.DeleteKeyPair(oldLabel);
            account.Label = newLabel;

            OnAccountsChanged?.Invoke();

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Renamed account: {oldLabel} -> {newLabel}");
        }

        /// <summary>
        /// Sets the active account
        /// </summary>
        public void SetActiveAccount(string label)
        {
            var account = GetAccount(label);
            if (account == null)
                throw new ArgumentException($"Account '{label}' not found");

            _activeAccount = account;
            OnActiveAccountChanged?.Invoke(_activeAccount);

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Active account: {label}");
        }

        /// <summary>
        /// Gets an account by label
        /// </summary>
        public WalletAccount GetAccount(string label)
        {
            return _accounts.Find(a => a.Label.Equals(label, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if an account exists
        /// </summary>
        public bool AccountExists(string label)
        {
            return GetAccount(label) != null;
        }

        #endregion

        #region Export

        /// <summary>
        /// Exports an account to PEM files
        /// </summary>
        public void ExportToPem(string label, string directoryPath)
        {
            var account = GetAccount(label);
            if (account == null)
                throw new ArgumentException($"Account '{label}' not found");

            KeyExporter.ExportToPemFiles(account.KeyPair, directoryPath, label.Replace(" ", "_"));

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Exported account to PEM: {label}");
        }

        /// <summary>
        /// Exports all accounts to PEM files
        /// </summary>
        public void ExportAllToPem(string directoryPath)
        {
            foreach (var account in _accounts)
            {
                ExportToPem(account.Label, directoryPath);
            }
        }

        #endregion

        #region Private

        private void LoadAllAccounts()
        {
            _accounts.Clear();
            var allKeyPairs = _storage.GetAllKeyPairs();

            foreach (var (label, keyPair) in allKeyPairs)
            {
                if (keyPair != null)
                {
                    _accounts.Add(new WalletAccount
                    {
                        Label = label,
                        PublicKey = keyPair.PublicKeyHex,
                        AccountHash = keyPair.AccountHash,
                        Algorithm = keyPair.Algorithm,
                        KeyPair = keyPair
                    });
                }
            }

            OnAccountsChanged?.Invoke();
        }

        #endregion
    }

    /// <summary>
    /// Represents a wallet account
    /// </summary>
    [Serializable]
    public class WalletAccount
    {
        public string Label { get; set; }
        public string PublicKey { get; set; }
        public string AccountHash { get; set; }
        public KeyAlgorithm Algorithm { get; set; }
        public KeyPair KeyPair { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Balance { get; set; } // Cached balance

        /// <summary>
        /// Short form of public key for display
        /// </summary>
        public string ShortPublicKey => PublicKey?.Length > 16 
            ? $"{PublicKey.Substring(0, 8)}...{PublicKey.Substring(PublicKey.Length - 8)}" 
            : PublicKey;
    }
}
