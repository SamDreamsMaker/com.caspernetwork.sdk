using System;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Services.Account;
using CasperSDK.Services.Transaction;
using CasperSDK.Services.Block;
using CasperSDK.Services.Network;
using CasperSDK.Services.Deploy;
using CasperSDK.Services.State;
using CasperSDK.Services.Validator;
using CasperSDK.Network.Clients;
using CasperSDK.Unity;

namespace CasperSDK.Core
{
    /// <summary>
    /// Main SDK manager implementing Singleton pattern.
    /// This is the entry point for all Casper SDK operations.
    /// </summary>
    /// <example>
    /// // Initialize the SDK
    /// var config = Resources.Load<NetworkConfig>("CasperSDK/DefaultNetworkConfig");
    /// CasperSDKManager.Instance.Initialize(config);
    /// 
    /// // Use services
    /// var accountService = CasperSDKManager.Instance.AccountService;
    /// var balance = await accountService.GetBalanceAsync(publicKey);
    /// </example>
    public class CasperSDKManager : MonoBehaviour
    {
        private static CasperSDKManager _instance;
        private static readonly object _lock = new object();
        
        private NetworkConfig _config;
        private INetworkClient _networkClient;
        private IAccountService _accountService;
        private ITransactionService _transactionService;
        private IBlockService _blockService;
        private INetworkInfoService _networkInfoService;
        private IDeployService _deployService;
        private IStateService _stateService;
        private IValidatorService _validatorService;
        private bool _isInitialized;

        /// <summary>
        /// Gets the singleton instance of the SDK manager
        /// </summary>
        public static CasperSDKManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            // Try to find existing instance
                            _instance = FindFirstObjectByType<CasperSDKManager>();

                            if (_instance == null)
                            {
                                // Create new GameObject with SDK manager
                                var go = new GameObject("CasperSDKManager");
                                _instance = go.AddComponent<CasperSDKManager>();
                                DontDestroyOnLoad(go);
                                
                                Debug.Log($"[CasperSDK] Created SDK Manager (v{SDKSettings.Version})");
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets whether the SDK is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the current network configuration
        /// </summary>
        public NetworkConfig Configuration => _config;

        /// <summary>
        /// Gets the account service
        /// </summary>
        public IAccountService AccountService
        {
            get
            {
                EnsureInitialized();
                return _accountService;
            }
        }

        /// <summary>
        /// Gets the transaction service
        /// </summary>
        public ITransactionService TransactionService
        {
            get
            {
                EnsureInitialized();
                return _transactionService;
            }
        }

        /// <summary>
        /// Gets the block service for blockchain queries
        /// </summary>
        public IBlockService BlockService
        {
            get
            {
                EnsureInitialized();
                return _blockService;
            }
        }

        /// <summary>
        /// Gets the network info service for node status
        /// </summary>
        public INetworkInfoService NetworkInfoService
        {
            get
            {
                EnsureInitialized();
                return _networkInfoService;
            }
        }

        /// <summary>
        /// Gets the deploy service for transaction queries
        /// </summary>
        public IDeployService DeployService
        {
            get
            {
                EnsureInitialized();
                return _deployService;
            }
        }

        /// <summary>
        /// Gets the state service for global state queries
        /// </summary>
        public IStateService StateService
        {
            get
            {
                EnsureInitialized();
                return _stateService;
            }
        }

        /// <summary>
        /// Gets the validator service for staking info
        /// </summary>
        public IValidatorService ValidatorService
        {
            get
            {
                EnsureInitialized();
                return _validatorService;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Ensure main thread dispatcher is created
                _ = UnityMainThreadDispatcher.Instance;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initializes the SDK with the specified configuration
        /// </summary>
        /// <param name="config">Network configuration</param>
        /// <exception cref="ArgumentNullException">Thrown when config is null</exception>
        /// <exception cref="CasperSDKException">Thrown when initialization fails</exception>
        public void Initialize(NetworkConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config), "Network configuration cannot be null");
            }

            if (!config.Validate())
            {
                throw new ValidationException("Invalid network configuration");
            }

            _config = config;

            try
            {
                // Create network client using factory pattern
                _networkClient = NetworkClientFactory.CreateClient(config);

                // Initialize services with dependency injection
                _accountService = new AccountService(_networkClient, config);
                _transactionService = new TransactionService(_networkClient, config);
                _blockService = new BlockService(_networkClient, config);
                _networkInfoService = new NetworkInfoService(_networkClient, config);
                _deployService = new DeployService(_networkClient, config);
                _stateService = new StateService(_networkClient, config);
                _validatorService = new ValidatorService(_networkClient, config);

                _isInitialized = true;

                if (config.EnableLogging)
                {
                    Debug.Log($"[CasperSDK] SDK initialized successfully");
                    Debug.Log($"[CasperSDK] Network: {config.NetworkType}");
                    Debug.Log($"[CasperSDK] RPC Endpoint: {config.RpcUrl}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to initialize SDK: {ex.Message}");
                throw new CasperSDKException("SDK initialization failed", ex);
            }
        }

        /// <summary>
        /// Initializes the SDK with default configuration from Resources
        /// </summary>
        public void InitializeWithDefaults()
        {
            var defaultConfig = Resources.Load<NetworkConfig>("CasperSDK/DefaultNetworkConfig");
            
            if (defaultConfig == null)
            {
                Debug.LogWarning("[CasperSDK] Default configuration not found, creating testnet config");
                defaultConfig = ScriptableObject.CreateInstance<NetworkConfig>();
            }

            Initialize(defaultConfig);
        }

        /// <summary>
        /// Shuts down the SDK and cleans up resources
        /// </summary>
        public void Shutdown()
        {
            if (_config != null && _config.EnableLogging)
            {
                Debug.Log("[CasperSDK] Shutting down SDK");
            }

            _accountService = null;
            _transactionService = null;
            _networkClient = null;
            _config = null;
            _isInitialized = false;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new CasperSDKException(
                    "SDK is not initialized. Call Initialize() with a NetworkConfig before using any services.");
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Shutdown();
                _instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }
    }
}
