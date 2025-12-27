using System;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;

namespace CasperSDK.Network.Clients
{
    /// <summary>
    /// Factory for creating network clients based on configuration.
    /// Implements the Factory Method design pattern.
    /// </summary>
    public static class NetworkClientFactory
    {
        /// <summary>
        /// Creates a network client based on network type
        /// </summary>
        /// <param name="networkType">Type of network</param>
        /// <param name="config">Network configuration</param>
        /// <returns>Network client instance</returns>
        public static INetworkClient CreateClient(NetworkType networkType, NetworkConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return networkType switch
            {
                NetworkType.Mainnet => new MainnetClient(config),
                NetworkType.Testnet => new TestnetClient(config),
                NetworkType.Custom => new CustomNetworkClient(config.RpcUrl, config),
                _ => throw new ArgumentException($"Unsupported network type: {networkType}", nameof(networkType))
            };
        }

        /// <summary>
        /// Creates a network client based on configuration
        /// </summary>
        /// <param name="config">Network configuration</param>
        /// <returns>Network client instance</returns>
        public static INetworkClient CreateClient(NetworkConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Use mock client if enabled
            if (config.UseMockNetwork)
            {
                UnityEngine.Debug.LogWarning("[CasperSDK] Using MOCK network client - no real network calls will be made!");
                return new MockNetworkClient(config);
            }

            return CreateClient(config.NetworkType, config);
        }

        /// <summary>
        /// Creates a custom network client with specified endpoint
        /// </summary>
        /// <param name="customEndpoint">Custom RPC endpoint URL</param>
        /// <param name="config">Network configuration</param>
        /// <returns>Custom network client instance</returns>
        public static INetworkClient CreateCustomClient(string customEndpoint, NetworkConfig config)
        {
            if (string.IsNullOrWhiteSpace(customEndpoint))
            {
                throw new ArgumentNullException(nameof(customEndpoint));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return new CustomNetworkClient(customEndpoint, config);
        }
    }
}
