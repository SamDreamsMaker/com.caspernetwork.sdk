using System.Threading.Tasks;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Network.RPC;

namespace CasperSDK.Network.Clients
{
    /// <summary>
    /// Base implementation of network client
    /// </summary>
    public class NetworkClient : INetworkClient
    {
        protected readonly JsonRpcClient _rpcClient;
        protected readonly NetworkConfig _config;

        /// <inheritdoc/>
        public string Endpoint { get; }

        /// <inheritdoc/>
        public NetworkType NetworkType { get; }

        /// <summary>
        /// Initializes a new network client
        /// </summary>
        /// <param name="endpoint">RPC endpoint</param>
        /// <param name="networkType">Network type</param>
        /// <param name="config">Network configuration</param>
        public NetworkClient(string endpoint, NetworkType networkType, NetworkConfig config)
        {
            Endpoint = endpoint;
            NetworkType = networkType;
            _config = config;
            _rpcClient = new JsonRpcClient(endpoint, config);
        }

        /// <inheritdoc/>
        public virtual async Task<TResult> SendRequestAsync<TResult>(string method, object parameters = null)
        {
            return await _rpcClient.SendRequestAsync<TResult>(method, parameters);
        }

        /// <inheritdoc/>
        public virtual async Task<bool> TestConnectionAsync()
        {
            return await _rpcClient.TestConnectionAsync();
        }
    }

    /// <summary>
    /// Mainnet-specific network client
    /// </summary>
    public class MainnetClient : NetworkClient
    {
        public MainnetClient(NetworkConfig config)
            : base(GetMainnetUrl(config), NetworkType.Mainnet, config)
        {
        }

        private static string GetMainnetUrl(NetworkConfig config)
        {
            // Use custom URL if provided, otherwise use default
            return config.NetworkType == NetworkType.Custom && !string.IsNullOrEmpty(config.RpcUrl)
                ? config.RpcUrl
                : "http://34.224.191.55:7777/rpc";
        }
    }

    /// <summary>
    /// Testnet-specific network client
    /// </summary>
    public class TestnetClient : NetworkClient
    {
        public TestnetClient(NetworkConfig config)
            : base(GetTestnetUrl(config), NetworkType.Testnet, config)
        {
        }

        private static string GetTestnetUrl(NetworkConfig config)
        {
            // Use the URL from NetworkConfig.RpcUrl which uses the switch statement
            return config.RpcUrl;
        }
    }

    /// <summary>
    /// Custom network client with user-defined endpoint
    /// </summary>
    public class CustomNetworkClient : NetworkClient
    {
        public CustomNetworkClient(string customEndpoint, NetworkConfig config)
            : base(customEndpoint, NetworkType.Custom, config)
        {
        }
    }
}
