using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Network.Clients;
using CasperSDK.Services.Account;
using CasperSDK.Services.Transfer;
using CasperSDK.Utilities.Cryptography;
using CasperSDK.Models;

namespace CasperSDK.Samples
{
    /// <summary>
    /// Controller for the Casper Wallet Demo scene.
    /// Wires up UI elements with SDK functionality.
    /// </summary>
    public class CasperWalletDemoController : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Optional: Create via Right-Click > Create > CasperSDK > Network Config. Leave empty for Testnet defaults.")]
        [SerializeField] private NetworkConfig _networkConfig;

        // UI References (found automatically)
        private TMP_Text _balanceValue;
        private TMP_Text _addressText;  // Changed from InputField to Text (read-only)
        private TMP_Text _transferStatus;
        private TMP_InputField _recipientInput;
        private TMP_InputField _amountInput;
        
        private Button _generateBtn;
        private Button _refreshBtn;
        private Button _copyBtn;
        private Button _exportBtn;
        private Button _importBtn;
        private Button _faucetBtn;
        private Button _sendBtn;

        // Services
        private INetworkClient _networkClient;
        private AccountService _accountService;
        private TransferService _transferService;
        private KeyPair _currentKeyPair;

        private void Awake()
        {
            FindUIElements();
            InitializeServices();
            SetupButtonListeners();
        }

        private void Start()
        {
            // Don't auto-generate - let user choose to Generate or Import
            SetStatus("Click Generate or Import to start", Color.white);
        }

        private void FindUIElements()
        {
            // Find text elements
            _balanceValue = FindComponentInChildren<TMP_Text>("BalanceValue");
            _transferStatus = FindComponentInChildren<TMP_Text>("TransferStatus");
            _addressText = FindComponentInChildren<TMP_Text>("AddressText");
            
            // Find inputs
            _recipientInput = FindComponentInChildren<TMP_InputField>("RecipientInput");
            _amountInput = FindComponentInChildren<TMP_InputField>("AmountInput");
            
            // Find buttons
            _generateBtn = FindComponentInChildren<Button>("GenerateBtn");
            _refreshBtn = FindComponentInChildren<Button>("RefreshBtn");
            _copyBtn = FindComponentInChildren<Button>("CopyBtn");
            _exportBtn = FindComponentInChildren<Button>("ExportBtn");
            _importBtn = FindComponentInChildren<Button>("ImportBtn");
            _faucetBtn = FindComponentInChildren<Button>("FaucetBtn");
            _sendBtn = FindComponentInChildren<Button>("SendBtn");
        }

        private T FindComponentInChildren<T>(string name) where T : Component
        {
            var allComponents = FindObjectsByType<T>(FindObjectsSortMode.None);
            foreach (var comp in allComponents)
            {
                if (comp.gameObject.name == name)
                    return comp;
            }
            return null;
        }

        private void InitializeServices()
        {
            if (_networkConfig == null)
            {
                _networkConfig = ScriptableObject.CreateInstance<NetworkConfig>();
            }
            
            _networkClient = NetworkClientFactory.CreateClient(_networkConfig);
            _accountService = new AccountService(_networkClient, _networkConfig);
            _transferService = new TransferService(_networkClient, _networkConfig);
            
            Debug.Log("[CasperDemo] Services initialized");
        }

        private void SetupButtonListeners()
        {
            _generateBtn?.onClick.AddListener(GenerateNewAccount);
            _refreshBtn?.onClick.AddListener(RefreshBalance);
            _copyBtn?.onClick.AddListener(CopyAddress);
            _exportBtn?.onClick.AddListener(ExportKeys);
            _importBtn?.onClick.AddListener(ImportKeys);
            _faucetBtn?.onClick.AddListener(OpenFaucet);
            _sendBtn?.onClick.AddListener(SendTransaction);
        }

        #region Actions

        private void GenerateNewAccount()
        {
            _currentKeyPair = CasperKeyGenerator.GenerateED25519();
            UpdateAddressDisplay();
            SetBalance("0.00");
            SetStatus("New account generated!", Color.green);
            
            Debug.Log($"[CasperDemo] Generated: {_currentKeyPair.PublicKeyHex}");
        }

        private async void RefreshBalance()
        {
            if (_currentKeyPair == null)
            {
                SetStatus("No account to check!", Color.yellow);
                return;
            }
            
            var address = _currentKeyPair.PublicKeyHex;

            SetStatus("Fetching balance...", Color.white);
            
            try
            {
                var balance = await _accountService.GetBalanceAsync(address);
                var cspr = TransferService.MotesToCspr(balance);
                SetBalance($"{cspr:N2}");
                SetStatus($"Balance updated", Color.green);
            }
            catch (Exception ex)
            {
                SetBalance("0.00");
                SetStatus($"Not found on chain (use faucet first)", Color.yellow);
                Debug.LogWarning($"[CasperDemo] Balance check: {ex.Message}");
            }
        }


        private void CopyAddress()
        {
            if (_currentKeyPair == null) return;
            
            GUIUtility.systemCopyBuffer = _currentKeyPair.PublicKeyHex;
            SetStatus("Address copied to clipboard!", Color.green);
        }

        private void ExportKeys()
        {
            if (_currentKeyPair == null)
            {
                SetStatus("No account to export!", Color.yellow);
                return;
            }

            try
            {
                var path = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    "CasperKeys");
                    
                KeyExporter.ExportToPemFiles(_currentKeyPair, path, "casper_demo");
                SetStatus($"Keys exported to Documents/CasperKeys", Color.green);
                
                #if UNITY_EDITOR || UNITY_STANDALONE_WIN
                System.Diagnostics.Process.Start("explorer.exe", path);
                #endif
            }
            catch (Exception ex)
            {
                SetStatus($"Export failed: {ex.Message}", Color.red);
            }
        }

        private void OpenFaucet()
        {
            Application.OpenURL("https://testnet.cspr.live/tools/faucet");
            SetStatus("Faucet opened. Paste your address there!", Color.cyan);
        }

        private void ImportKeys()
        {
            try
            {
                string pemPath = null;
                
                #if UNITY_EDITOR
                // In Editor: use file picker dialog
                pemPath = UnityEditor.EditorUtility.OpenFilePanel(
                    "Select Casper Secret Key", 
                    System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "CasperKeys"),
                    "pem");
                    
                if (string.IsNullOrEmpty(pemPath))
                {
                    SetStatus("Import cancelled", Color.yellow);
                    return;
                }
                #else
                // In Build: auto-find in CasperKeys folder
                var keysFolder = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
                    "CasperKeys");

                if (!System.IO.Directory.Exists(keysFolder))
                {
                    System.IO.Directory.CreateDirectory(keysFolder);
                    SetStatus("Put your secret_key.pem in Documents/CasperKeys", Color.yellow);
                    #if UNITY_STANDALONE_WIN
                    System.Diagnostics.Process.Start("explorer.exe", keysFolder);
                    #endif
                    return;
                }

                var pemFiles = System.IO.Directory.GetFiles(keysFolder, "*secret*.pem");
                if (pemFiles.Length == 0)
                {
                    SetStatus("No secret_key.pem found in Documents/CasperKeys", Color.yellow);
                    #if UNITY_STANDALONE_WIN
                    System.Diagnostics.Process.Start("explorer.exe", keysFolder);
                    #endif
                    return;
                }
                
                pemPath = pemFiles[0];
                #endif

                var pemContent = System.IO.File.ReadAllText(pemPath);
                
                // Detect algorithm from filename or content
                var algorithm = pemPath.ToLower().Contains("secp") ? KeyAlgorithm.SECP256K1 : KeyAlgorithm.ED25519;
                
                _currentKeyPair = KeyExporter.ImportFromPem(pemContent, algorithm);
                UpdateAddressDisplay();
                SetStatus($"Imported: {System.IO.Path.GetFileName(pemPath)}", Color.green);
                
                Debug.Log($"[CasperDemo] Imported key: {_currentKeyPair.PublicKeyHex}");
                
                // Auto-refresh balance
                RefreshBalance();
            }
            catch (Exception ex)
            {
                SetStatus($"Import failed: {ex.Message}", Color.red);
                Debug.LogError($"[CasperDemo] Import error: {ex}");
            }
        }

        private async void SendTransaction()
        {
            if (_currentKeyPair == null)
            {
                SetStatus("No account!", Color.red);
                return;
            }

            var recipient = _recipientInput?.text?.Trim();
            var amountText = _amountInput?.text?.Trim();

            if (string.IsNullOrEmpty(recipient))
            {
                SetStatus("Enter recipient address", Color.yellow);
                return;
            }

            if (!decimal.TryParse(amountText, out var amount) || amount <= 0)
            {
                SetStatus("Enter valid amount", Color.yellow);
                return;
            }

            SetStatus("Sending transaction...", Color.white);
            _sendBtn.interactable = false;

            try
            {
                var motes = TransferService.CsprToMotes(amount);
                var result = await _transferService.TransferAsync(_currentKeyPair, recipient, motes);

                if (result.Success)
                {
                    SetStatus($"Sent! Hash: {result.DeployHash.Substring(0, 16)}...", Color.green);
                    _recipientInput.text = "";
                    _amountInput.text = "";
                    
                    // Refresh balance after a delay
                    Invoke(nameof(RefreshBalance), 3f);
                }
                else
                {
                    SetStatus($"Failed: {result.ErrorMessage}", Color.red);
                }
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", Color.red);
            }
            finally
            {
                _sendBtn.interactable = true;
            }
        }

        #endregion

        #region UI Helpers

        private void UpdateAddressDisplay()
        {
            if (_addressText != null && _currentKeyPair != null)
            {
                _addressText.text = _currentKeyPair.PublicKeyHex;
            }
        }

        private void SetBalance(string cspr)
        {
            if (_balanceValue != null)
            {
                _balanceValue.text = $"{cspr} CSPR";
            }
        }

        private void SetStatus(string message, Color color)
        {
            if (_transferStatus != null)
            {
                _transferStatus.text = message;
                _transferStatus.color = color;
            }
            
            Debug.Log($"[CasperDemo] {message}");
        }

        #endregion
    }
}
