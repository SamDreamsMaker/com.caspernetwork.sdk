using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models;
using CasperSDK.Network.Clients;
using CasperSDK.Services.Account;
using CasperSDK.Services.Deploy;
using CasperSDK.Services.Transfer;
using CasperSDK.Utilities.Cryptography;

namespace CasperSDK.Examples
{
    /// <summary>
    /// Demo script showing testnet integration.
    /// Demonstrates account creation, balance queries, and transfer preparation.
    /// </summary>
    public class TestnetDemo : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private NetworkConfig _networkConfig;
        
        [Header("Demo Settings")]
        [SerializeField] private bool _generateNewAccount = true;
        [SerializeField] private string _existingPublicKey = "";

        [Header("Output")]
        [SerializeField] private string _currentPublicKey;
        [SerializeField] private string _currentBalance;
        [SerializeField] private string _lastDeployHash;

        private INetworkClient _networkClient;
        private AccountService _accountService;
        private TransferService _transferService;
        private KeyPair _keyPair;

        private async void Start()
        {
            await InitializeDemoAsync();
        }

        [ContextMenu("Initialize Demo")]
        public async Task InitializeDemoAsync()
        {
            Debug.Log("==============================================");
            Debug.Log("   CASPER TESTNET DEMO");
            Debug.Log("==============================================\n");

            try
            {
                // Step 1: Initialize services
                InitializeServices();

                // Step 2: Create or load account
                if (_generateNewAccount)
                {
                    await GenerateNewAccount();
                }
                else
                {
                    Debug.Log($"[Demo] Using existing public key: {_existingPublicKey}");
                    _currentPublicKey = _existingPublicKey;
                }

                // Step 3: Check balance
                await CheckBalance();

                // Step 4: Show next steps
                ShowNextSteps();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Demo] Error: {ex.Message}");
            }
        }

        private void InitializeServices()
        {
            Debug.Log("[Demo] Initializing Casper SDK services...");

            // Create network client - use config if provided, otherwise create default testnet config
            if (_networkConfig != null)
            {
                _networkClient = NetworkClientFactory.CreateClient(_networkConfig);
            }
            else
            {
                var defaultConfig = ScriptableObject.CreateInstance<NetworkConfig>();
                _networkClient = NetworkClientFactory.CreateClient(defaultConfig);
            }

            // Create config if not provided
            var config = _networkConfig ?? ScriptableObject.CreateInstance<NetworkConfig>();

            // Initialize services
            _accountService = new AccountService(_networkClient, config);
            _transferService = new TransferService(_networkClient, config);

            Debug.Log("[Demo] Services initialized successfully!");
        }

        [ContextMenu("Generate New Account")]
        public async Task GenerateNewAccount()
        {
            Debug.Log("\n[Demo] Generating new ED25519 key pair...");

            _keyPair = CasperKeyGenerator.GenerateED25519();
            _currentPublicKey = _keyPair.PublicKeyHex;

            Debug.Log($"<color=green>✓ Account Generated!</color>");
            Debug.Log($"  Public Key: {_keyPair.PublicKeyHex}");
            Debug.Log($"  Account Hash: {_keyPair.AccountHash}");
            Debug.Log($"  Algorithm: {_keyPair.Algorithm}");
            
            // Warning about private key
            Debug.LogWarning($"  Private Key: {_keyPair.PrivateKeyHex.Substring(0, 16)}... (KEEP SECRET!)");

            await Task.CompletedTask;
        }

        [ContextMenu("Check Balance")]
        public async Task CheckBalance()
        {
            if (string.IsNullOrEmpty(_currentPublicKey))
            {
                Debug.LogError("[Demo] No public key available. Generate an account first.");
                return;
            }

            Debug.Log($"\n[Demo] Checking balance for: {_currentPublicKey.Substring(0, 20)}...");

            try
            {
                var balance = await _accountService.GetBalanceAsync(_currentPublicKey);
                _currentBalance = balance;

                var cspr = TransferService.MotesToCspr(balance);
                Debug.Log($"<color=green>✓ Balance: {cspr} CSPR ({balance} motes)</color>");

                if (cspr == 0)
                {
                    Debug.LogWarning("[Demo] Account has no balance. Get testnet CSPR from the faucet!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Demo] Could not get balance: {ex.Message}");
                Debug.Log("[Demo] This is normal for new accounts that haven't been funded.");
                _currentBalance = "0";
            }
        }

        [ContextMenu("Prepare Test Transfer")]
        public void PrepareTestTransfer()
        {
            if (_keyPair == null)
            {
                Debug.LogError("[Demo] No key pair available. Generate an account first.");
                return;
            }

            Debug.Log("\n[Demo] Preparing test transfer...");

            try
            {
                // Build a test transfer (without submitting)
                var testRecipient = "01" + new string('0', 64); // Dummy recipient
                var testAmount = TransferService.CsprToMotes(1m); // 1 CSPR

                var deploy = new DeployBuilder()
                    .SetChainName("casper-test")
                    .SetSender(_keyPair.PublicKeyHex)
                    .SetStandardPayment("100000000")
                    .SetTransferSession(testRecipient, testAmount)
                    .Build();

                Debug.Log($"<color=green>✓ Transfer Deploy Built!</color>");
                Debug.Log($"  Deploy Hash: {deploy.Hash}");
                Debug.Log($"  Sender: {deploy.Header.Account.Substring(0, 20)}...");
                Debug.Log($"  Amount: 1 CSPR ({testAmount} motes)");

                // Sign it
                deploy = DeploySigner.SignDeploy(deploy, _keyPair);
                Debug.Log($"  Signature: {deploy.Approvals[0].Signature.Substring(0, 20)}...");

                // Verify signature
                var isValid = DeploySigner.Verify(
                    deploy.Hash,
                    deploy.Approvals[0].Signature,
                    _keyPair.PublicKeyHex);
                Debug.Log($"  Signature Valid: {isValid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Demo] Error preparing transfer: {ex.Message}");
            }
        }

        [ContextMenu("Execute Transfer (Requires Balance)")]
        public async void ExecuteTransfer()
        {
            if (_keyPair == null)
            {
                Debug.LogError("[Demo] No key pair available. Generate an account first.");
                return;
            }

            Debug.Log("\n[Demo] EXECUTING REAL TRANSFER!");
            Debug.LogWarning("[Demo] This will send real testnet CSPR!");

            try
            {
                // Transfer 2.5 CSPR to a test account
                var testRecipient = "01" + new string('a', 64); // Test recipient
                var amount = TransferService.CsprToMotes(2.5m);

                var result = await _transferService.TransferAsync(
                    _keyPair,
                    testRecipient,
                    amount);

                if (result.Success)
                {
                    _lastDeployHash = result.DeployHash;
                    Debug.Log($"<color=green>✓ Transfer Submitted!</color>");
                    Debug.Log($"  Deploy Hash: {result.DeployHash}");
                    Debug.Log($"  Check status at: https://testnet.cspr.live/deploy/{result.DeployHash}");
                }
                else
                {
                    Debug.LogError($"[Demo] Transfer failed: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Demo] Transfer failed: {ex.Message}");
            }
        }

        private void ShowNextSteps()
        {
            Debug.Log("\n==============================================");
            Debug.Log("   NEXT STEPS");
            Debug.Log("==============================================");
            Debug.Log("1. Copy your public key from the console");
            Debug.Log("2. Go to: https://testnet.cspr.live/tools/faucet");
            Debug.Log("3. Request testnet CSPR (you'll get 1000 CSPR)");
            Debug.Log("4. Wait ~2 minutes for the transaction");
            Debug.Log("5. Click 'Check Balance' in the context menu");
            Debug.Log("6. Try 'Prepare Test Transfer' to build a transaction");
            Debug.Log("==============================================\n");
        }

        [ContextMenu("Open Testnet Faucet")]
        public void OpenFaucet()
        {
            Application.OpenURL("https://testnet.cspr.live/tools/faucet");
        }

        [ContextMenu("Open Block Explorer")]
        public void OpenBlockExplorer()
        {
            if (!string.IsNullOrEmpty(_lastDeployHash))
            {
                Application.OpenURL($"https://testnet.cspr.live/deploy/{_lastDeployHash}");
            }
            else if (!string.IsNullOrEmpty(_currentPublicKey))
            {
                Application.OpenURL($"https://testnet.cspr.live/account/{_currentPublicKey}");
            }
            else
            {
                Application.OpenURL("https://testnet.cspr.live/");
            }
        }

        [ContextMenu("Copy Public Key")]
        public void CopyPublicKey()
        {
            if (!string.IsNullOrEmpty(_currentPublicKey))
            {
                GUIUtility.systemCopyBuffer = _currentPublicKey;
                Debug.Log("[Demo] Public key copied to clipboard!");
            }
        }

        [ContextMenu("Export Keys to PEM (for Casper Wallet)")]
        public void ExportKeysToPem()
        {
            if (_keyPair == null)
            {
                Debug.LogError("[Demo] No key pair to export. Generate an account first.");
                return;
            }

            // Export to user's Documents folder
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var exportPath = System.IO.Path.Combine(documentsPath, "CasperKeys");
            
            KeyExporter.ExportToPemFiles(_keyPair, exportPath, "casper_testnet");

            Debug.Log("\n==============================================");
            Debug.Log("   KEYS EXPORTED FOR CASPER WALLET");
            Debug.Log("==============================================");
            Debug.Log($"Location: {exportPath}");
            Debug.Log("");
            Debug.Log("To use with Casper Wallet:");
            Debug.Log("1. Install Casper Wallet extension in your browser");
            Debug.Log("2. Open Casper Wallet → Import Account");
            Debug.Log("3. Select 'Upload Keys' and choose 'casper_testnet_secret_key.pem'");
            Debug.Log("4. Go to https://testnet.cspr.live/tools/faucet");
            Debug.Log("5. Connect with Casper Wallet and request CSPR");
            Debug.Log("==============================================\n");

            // Open the folder
            System.Diagnostics.Process.Start("explorer.exe", exportPath);
        }

        [ContextMenu("Import Keys from PEM")]
        public void ImportKeysFromPem()
        {
            var documentsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments);
            var pemPath = System.IO.Path.Combine(documentsPath, "CasperKeys", "casper_testnet_secret_key.pem");

            if (!System.IO.File.Exists(pemPath))
            {
                Debug.LogError($"[Demo] PEM file not found at: {pemPath}");
                return;
            }

            try
            {
                _keyPair = KeyExporter.ImportFromPemFile(pemPath);
                _currentPublicKey = _keyPair.PublicKeyHex;
                
                Debug.Log("[Demo] Keys imported successfully!");
                Debug.Log($"  Public Key: {_keyPair.PublicKeyHex}");
                Debug.Log($"  Account Hash: {_keyPair.AccountHash}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Demo] Import failed: {ex.Message}");
            }
        }
    }
}

