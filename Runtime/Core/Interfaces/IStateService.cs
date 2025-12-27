using System.Threading.Tasks;
using CasperSDK.Models.RPC;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for global state and dictionary queries.
    /// </summary>
    public interface IStateService
    {
        /// <summary>
        /// Query global state by key.
        /// </summary>
        Task<GlobalStateValue> QueryGlobalStateAsync(string key, string stateRootHash = null);

        /// <summary>
        /// Get a dictionary item by its key and seed URef.
        /// </summary>
        Task<DictionaryItem> GetDictionaryItemAsync(string dictionaryKey, string seedUref, string stateRootHash = null);

        /// <summary>
        /// Get a dictionary item by contract named key.
        /// </summary>
        Task<DictionaryItem> GetDictionaryItemByNameAsync(string contractHash, string dictionaryName, string dictionaryKey, string stateRootHash = null);
    }
}
