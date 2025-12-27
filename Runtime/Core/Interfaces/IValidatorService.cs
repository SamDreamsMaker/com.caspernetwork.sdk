using System.Threading.Tasks;
using CasperSDK.Models.RPC;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for validator and staking queries.
    /// </summary>
    public interface IValidatorService
    {
        /// <summary>
        /// Get auction information including all bids and validators.
        /// </summary>
        Task<AuctionInfo> GetAuctionInfoAsync(string blockHash = null);

        /// <summary>
        /// Get list of active validators for the current era.
        /// </summary>
        Task<ValidatorInfo[]> GetValidatorsAsync();

        /// <summary>
        /// Get validator info by public key.
        /// </summary>
        Task<ValidatorBid> GetValidatorByKeyAsync(string publicKey);
    }
}
