using UnityEngine;

namespace CasperSDK.Core.Configuration
{
    /// <summary>
    /// Network configuration for Casper blockchain connection.
    /// Implemented as ScriptableObject for easy configuration in Unity Editor.
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "CasperSDK/Network Configuration", order = 1)]
    public class NetworkConfig : ScriptableObject
    {
        [Header("Network Settings")]
        [Tooltip("Type of network to connect to")]
        [SerializeField] private NetworkType _networkType = NetworkType.Testnet;
        
        [Tooltip("Custom RPC endpoint URL (only used when NetworkType is Custom)")]
        [SerializeField] private string _customRpcUrl = "";
        
        [Header("Connection Settings")]
        [Tooltip("Request timeout in seconds")]
        [SerializeField] [Range(5, 120)] private int _requestTimeoutSeconds = 60;
        
        [Tooltip("Maximum number of retry attempts for failed requests")]
        [SerializeField] [Range(0, 10)] private int _maxRetryAttempts = 3;
        
        [Tooltip("Enable detailed logging for debugging")]
        [SerializeField] private bool _enableLogging = true;
        
        [Header("Mock Mode (for testing without network)")]
        [Tooltip("Use mock network client instead of real RPC calls")]
        [SerializeField] private bool _useMockNetwork = false;

        /// <summary>
        /// Gets the network type
        /// </summary>
        public NetworkType NetworkType => _networkType;

        /// <summary>
        /// Gets the RPC endpoint URL based on network type
        /// </summary>
        public string RpcUrl
        {
            get
            {
                return _networkType switch
                {
                    NetworkType.Mainnet => "http://34.224.191.55:7777/rpc",
                    NetworkType.Testnet => "https://node.testnet.casper.network/rpc",
                    NetworkType.Custom => _customRpcUrl,
                    _ => "https://node.testnet.casper.network/rpc"
                };
            }
        }

        /// <summary>
        /// Gets the request timeout duration
        /// </summary>
        public int RequestTimeoutSeconds => _requestTimeoutSeconds;

        /// <summary>
        /// Gets the maximum retry attempts
        /// </summary>
        public int MaxRetryAttempts => _maxRetryAttempts;

        /// <summary>
        /// Gets whether logging is enabled
        /// </summary>
        public bool EnableLogging => _enableLogging;

        /// <summary>
        /// Gets whether to use mock network client
        /// </summary>
        public bool UseMockNetwork => _useMockNetwork;

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns>True if configuration is valid</returns>
        public bool Validate()
        {
            if (_networkType == NetworkType.Custom && string.IsNullOrWhiteSpace(_customRpcUrl))
            {
                Debug.LogError("Custom RPC URL is required when using Custom network type");
                return false;
            }

            if (_requestTimeoutSeconds <= 0)
            {
                Debug.LogError("Request timeout must be greater than 0");
                return false;
            }

            return true;
        }

        private void OnValidate()
        {
            // Validate on editor changes
            if (_networkType == NetworkType.Custom && string.IsNullOrWhiteSpace(_customRpcUrl))
            {
                Debug.LogWarning("Custom RPC URL should be set when using Custom network type");
            }
        }
    }
}
