using System.Threading.Tasks;
using CasperSDK.Models.RPC;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for network information queries.
    /// </summary>
    public interface INetworkInfoService
    {
        /// <summary>
        /// Get the current node status.
        /// </summary>
        Task<NodeStatus> GetStatusAsync();

        /// <summary>
        /// Get list of connected peers.
        /// </summary>
        Task<PeerInfo[]> GetPeersAsync();

        /// <summary>
        /// Get the chain specification.
        /// </summary>
        Task<ChainspecInfo> GetChainspecAsync();
    }
}
