using UnityEngine;
using UnityEditor;
using CasperSDK.Core.Configuration;
using CasperSDK.Utilities.Cryptography;
using CasperSDK.Models;

namespace CasperSDK.Editor
{
    /// <summary>
    /// Editor window for Casper SDK configuration and testing.
    /// Provides tools for network configuration, key management, and testing.
    /// </summary>
    public class CasperSDKSettingsWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private NetworkConfig _selectedConfig;
        private int _selectedNetworkType;
        private string _customRpcUrl = "";
        private bool _enableLogging = true;
        private int _timeoutSeconds = 30;
        private int _maxRetries = 3;

        // Key Generation
        private int _selectedKeyAlgorithm;
        private KeyPair _generatedKeyPair;

        // Testing
        private string _testPublicKey = "";
        private string _connectionTestResult = "";

        private readonly string[] _networkTypeNames = { "Mainnet", "Testnet", "Custom" };
        private readonly string[] _keyAlgorithmNames = { "ED25519", "SECP256K1" };

        [MenuItem("Window/Casper SDK/Settings", false, 1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<CasperSDKSettingsWindow>("Casper SDK");
            window.minSize = new Vector2(400, 500);
        }

        [MenuItem("Window/Casper SDK/Generate Key Pair", false, 1001)]
        public static void ShowKeyGeneratorWindow()
        {
            var window = GetWindow<CasperSDKSettingsWindow>("Casper SDK");
            window.Show();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawNetworkConfiguration();
            EditorGUILayout.Space(10);

            DrawKeyGenerationSection();
            EditorGUILayout.Space(10);

            DrawTestingSection();
            EditorGUILayout.Space(10);

            DrawHelpSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Casper Network SDK", headerStyle, GUILayout.Height(30));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Version: {SDKSettings.Version}", EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawNetworkConfiguration()
        {
            EditorGUILayout.LabelField("Network Configuration", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            _selectedConfig = (NetworkConfig)EditorGUILayout.ObjectField(
                "Configuration Asset",
                _selectedConfig,
                typeof(NetworkConfig),
                false);

            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Create New Configuration:", EditorStyles.miniBoldLabel);

            _selectedNetworkType = EditorGUILayout.Popup("Network", _selectedNetworkType, _networkTypeNames);

            if (_selectedNetworkType == 2) // Custom
            {
                _customRpcUrl = EditorGUILayout.TextField("Custom RPC URL", _customRpcUrl);
            }
            else
            {
                var rpcUrl = _selectedNetworkType == 0 
                    ? SDKSettings.DefaultMainnetRpcUrl
                    : SDKSettings.DefaultTestnetRpcUrl;
                EditorGUILayout.LabelField("RPC URL", rpcUrl);
            }

            _timeoutSeconds = EditorGUILayout.IntSlider("Timeout (seconds)", _timeoutSeconds, 5, 120);
            _maxRetries = EditorGUILayout.IntSlider("Max Retries", _maxRetries, 0, 10);
            _enableLogging = EditorGUILayout.Toggle("Enable Logging", _enableLogging);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Create Configuration Asset"))
            {
                CreateNetworkConfigAsset();
            }
        }

        private void DrawKeyGenerationSection()
        {
            EditorGUILayout.LabelField("Key Generation", EditorStyles.boldLabel);

            _selectedKeyAlgorithm = EditorGUILayout.Popup("Algorithm", _selectedKeyAlgorithm, _keyAlgorithmNames);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Generate Key Pair"))
            {
                GenerateKeyPair();
            }
            if (GUILayout.Button("Copy to Clipboard") && _generatedKeyPair != null)
            {
                CopyKeyPairToClipboard();
            }
            EditorGUILayout.EndHorizontal();

            if (_generatedKeyPair != null)
            {
                EditorGUILayout.Space(5);
                
                var boxStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(10, 10, 10, 10) };
                EditorGUILayout.BeginVertical(boxStyle);

                EditorGUILayout.LabelField("Generated Key Pair:", EditorStyles.boldLabel);
                
                EditorGUILayout.LabelField("Algorithm:", _generatedKeyPair.Algorithm.ToString());
                
                EditorGUILayout.LabelField("Public Key:");
                EditorGUILayout.SelectableLabel(_generatedKeyPair.PublicKeyHex, EditorStyles.textField, GUILayout.Height(20));
                
                EditorGUILayout.LabelField("Private Key (KEEP SECRET!):");
                EditorGUILayout.SelectableLabel(_generatedKeyPair.PrivateKeyHex, EditorStyles.textField, GUILayout.Height(20));
                
                EditorGUILayout.LabelField("Account Hash:");
                EditorGUILayout.SelectableLabel(_generatedKeyPair.AccountHash, EditorStyles.textField, GUILayout.Height(20));

                EditorGUILayout.EndVertical();

                EditorGUILayout.HelpBox(
                    "⚠️ IMPORTANT: Save your private key securely. It cannot be recovered if lost!",
                    MessageType.Warning);
            }
        }

        private void DrawTestingSection()
        {
            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);

            _testPublicKey = EditorGUILayout.TextField("Test Public Key", _testPublicKey);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Validate Public Key"))
            {
                ValidatePublicKey();
            }
            
            if (GUILayout.Button("Test Connection"))
            {
                TestConnection();
            }
            
            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_connectionTestResult))
            {
                EditorGUILayout.HelpBox(_connectionTestResult, 
                    _connectionTestResult.Contains("Success") ? MessageType.Info : MessageType.Error);
            }
        }

        private void DrawHelpSection()
        {
            EditorGUILayout.LabelField("Resources", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Casper Docs"))
            {
                Application.OpenURL("https://docs.casper.network/");
            }
            
            if (GUILayout.Button("Testnet Faucet"))
            {
                Application.OpenURL("https://testnet.cspr.live/tools/faucet");
            }
            
            if (GUILayout.Button("Block Explorer"))
            {
                Application.OpenURL("https://cspr.live/");
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void CreateNetworkConfigAsset()
        {
            var config = ScriptableObject.CreateInstance<NetworkConfig>();
            
            // Use reflection to set the private serialized fields
            var type = typeof(NetworkConfig);
            
            var networkTypeField = type.GetField("_networkType", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            networkTypeField?.SetValue(config, (NetworkType)_selectedNetworkType);
            
            var customRpcField = type.GetField("_customRpcUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            customRpcField?.SetValue(config, _customRpcUrl);
            
            var timeoutField = type.GetField("_requestTimeoutSeconds",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timeoutField?.SetValue(config, _timeoutSeconds);
            
            var retriesField = type.GetField("_maxRetryAttempts",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            retriesField?.SetValue(config, _maxRetries);
            
            var loggingField = type.GetField("_enableLogging",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loggingField?.SetValue(config, _enableLogging);

            // Save the asset
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Network Configuration",
                "CasperNetworkConfig",
                "asset",
                "Please enter a file name to save the configuration.");

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                _selectedConfig = config;
                EditorGUIUtility.PingObject(config);

                Debug.Log($"[CasperSDK] Created network configuration at: {path}");
            }
        }

        private void GenerateKeyPair()
        {
            var algorithm = _selectedKeyAlgorithm == 0 ? KeyAlgorithm.ED25519 : KeyAlgorithm.SECP256K1;
            
            // Use real BouncyCastle cryptography
            _generatedKeyPair = algorithm == KeyAlgorithm.ED25519
                ? CasperKeyGenerator.GenerateED25519()
                : CasperKeyGenerator.GenerateSECP256K1();
            
            Debug.Log($"[CasperSDK] Generated {algorithm} key pair (BouncyCastle)");
        }

        private void CopyKeyPairToClipboard()
        {
            if (_generatedKeyPair == null) return;

            var text = $"Algorithm: {_generatedKeyPair.Algorithm}\n" +
                      $"Public Key: {_generatedKeyPair.PublicKeyHex}\n" +
                      $"Private Key: {_generatedKeyPair.PrivateKeyHex}\n" +
                      $"Account Hash: {_generatedKeyPair.AccountHash}";
            
            GUIUtility.systemCopyBuffer = text;
            Debug.Log("[CasperSDK] Key pair copied to clipboard");
        }

        private void ValidatePublicKey()
        {
            if (string.IsNullOrEmpty(_testPublicKey))
            {
                _connectionTestResult = "Please enter a public key to validate";
                return;
            }

            var isValid = CryptoHelper.ValidatePublicKey(_testPublicKey);
            _connectionTestResult = isValid 
                ? "✓ Public key format is valid" 
                : "✗ Invalid public key format";
        }

        private async void TestConnection()
        {
            if (_selectedConfig == null)
            {
                _connectionTestResult = "Please select or create a network configuration first";
                return;
            }

            _connectionTestResult = "Testing connection...";
            Repaint();

            try
            {
                var client = Network.Clients.NetworkClientFactory.CreateClient(_selectedConfig);
                var success = await client.TestConnectionAsync();
                
                _connectionTestResult = success 
                    ? "✓ Success: Connected to Casper Network"
                    : "✗ Failed: Could not connect to the network";
            }
            catch (System.Exception ex)
            {
                _connectionTestResult = $"✗ Error: {ex.Message}";
            }

            Repaint();
        }
    }
}
