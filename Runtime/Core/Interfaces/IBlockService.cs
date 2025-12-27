using System.Threading.Tasks;
using CasperSDK.Models.RPC;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for blockchain block queries.
    /// </summary>
    public interface IBlockService
    {
        /// <summary>
        /// Get the latest block from the network.
        /// </summary>
        Task<BlockData> GetLatestBlockAsync();

        /// <summary>
        /// Get a block by its hash.
        /// </summary>
        Task<BlockData> GetBlockByHashAsync(string blockHash);

        /// <summary>
        /// Get a block by its height.
        /// </summary>
        Task<BlockData> GetBlockByHeightAsync(long height);

        /// <summary>
        /// Get the current state root hash.
        /// </summary>
        Task<string> GetStateRootHashAsync();

        /// <summary>
        /// Get state root hash at a specific block height.
        /// </summary>
        Task<string> GetStateRootHashAtHeightAsync(long height);
    }
}
