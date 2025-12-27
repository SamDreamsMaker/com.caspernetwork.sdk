using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models.RPC;

namespace CasperSDK.Services.Network
{
    /// <summary>
    /// Service for querying network status and peer information.
    /// </summary>
    public class NetworkInfoService : INetworkInfoService
    {
        private readonly INetworkClient _networkClient;
        private readonly bool _enableLogging;

        public NetworkInfoService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config.EnableLogging;
        }

        /// <summary>
        /// Get the current node status.
        /// </summary>
        public async Task<NodeStatus> GetStatusAsync()
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[CasperSDK] Getting node status");
                }

                var result = await _networkClient.SendRequestAsync<StatusRpcResponse>("info_get_status", null);
                return new NodeStatus
                {
                    ApiVersion = result?.api_version,
                    ChainspecName = result?.chainspec_name,
                    StartingStateRootHash = result?.starting_state_root_hash,
                    Peers = result?.peers,
                    LastAddedBlockInfo = result?.last_added_block_info,
                    BuildVersion = result?.build_version,
                    Uptime = result?.uptime
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get node status: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get list of connected peers.
        /// </summary>
        public async Task<PeerInfo[]> GetPeersAsync()
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[CasperSDK] Getting network peers");
                }

                var result = await _networkClient.SendRequestAsync<PeersResponse>("info_get_peers", null);
                return result?.peers;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get peers: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get the chain specification.
        /// </summary>
        public async Task<ChainspecInfo> GetChainspecAsync()
        {
            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[CasperSDK] Getting chainspec");
                }

                var result = await _networkClient.SendRequestAsync<ChainspecResponse>("info_get_chainspec", null);
                return new ChainspecInfo
                {
                    ApiVersion = result?.api_version,
                    ChainspecBytes = result?.chainspec_bytes
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get chainspec: {ex.Message}");
                throw;
            }
        }
    }
}
