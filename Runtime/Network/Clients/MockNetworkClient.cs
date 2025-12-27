using System.Threading.Tasks;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models.RPC;
using UnityEngine;

namespace CasperSDK.Network.Clients
{
    /// <summary>
    /// Mock network client that returns fake data for testing without network access.
    /// Useful for development when RPC endpoints are not accessible.
    /// </summary>
    public class MockNetworkClient : INetworkClient
    {
        private readonly bool _enableLogging;

        public string Endpoint => "mock://localhost";
        public NetworkType NetworkType => NetworkType.Testnet;

        public MockNetworkClient(NetworkConfig config)
        {
            _enableLogging = config?.EnableLogging ?? false;
        }

        public async Task<bool> TestConnectionAsync()
        {
            if (_enableLogging)
            {
                Debug.Log("[MockClient] Test connection (always succeeds)");
            }
            
            await Task.Delay(100);
            return true;
        }

        public async Task<TResult> SendRequestAsync<TResult>(string method, object parameters)
        {
            if (_enableLogging)
            {
                Debug.Log($"[MockClient] Simulating RPC call: {method}");
            }

            // Simulate network delay
            await Task.Delay(500);

            // Return mock data based on the RPC method
            if (method == "state_get_account_info")
            {
                var mockResponse = new AccountInfoRpcResponse
                {
                    account = new AccountDataResponse
                    {
                        main_purse = "uref-0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20-007"
                    }
                };
                
                return (TResult)(object)mockResponse;
            }
            else if (method == "state_get_balance")
            {
                var mockResponse = new BalanceRpcResponse
                {
                    balance_value = "5000000000000" // 5000 CSPR in motes
                };
                
                return (TResult)(object)mockResponse;
            }

            throw new System.NotImplementedException($"Mock not implemented for method: {method}");
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}
