using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models.RPC;

namespace CasperSDK.Services.State
{
    /// <summary>
    /// Service for querying global state and dictionary items.
    /// </summary>
    public class StateService : IStateService
    {
        private readonly INetworkClient _networkClient;
        private readonly bool _enableLogging;

        public StateService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config.EnableLogging;
        }

        /// <inheritdoc/>
        public async Task<GlobalStateValue> QueryGlobalStateAsync(string key, string stateRootHash = null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Querying global state for key: {key}");
                }

                object param;
                if (!string.IsNullOrEmpty(stateRootHash))
                {
                    param = new
                    {
                        key = key,
                        path = new string[0],
                        state_identifier = new { StateRootHash = stateRootHash }
                    };
                }
                else
                {
                    param = new
                    {
                        key = key,
                        path = new string[0]
                    };
                }

                var result = await _networkClient.SendRequestAsync<QueryGlobalStateResponse>("query_global_state", param);

                if (result?.stored_value == null)
                {
                    return null;
                }

                return new GlobalStateValue
                {
                    Key = key,
                    StoredValue = result.stored_value,
                    MerkleProof = result.merkle_proof
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to query global state: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DictionaryItem> GetDictionaryItemAsync(string dictionaryKey, string seedUref, string stateRootHash = null)
        {
            if (string.IsNullOrWhiteSpace(dictionaryKey))
            {
                throw new ArgumentException("Dictionary key cannot be null or empty", nameof(dictionaryKey));
            }

            if (string.IsNullOrWhiteSpace(seedUref))
            {
                throw new ArgumentException("Seed URef cannot be null or empty", nameof(seedUref));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting dictionary item: {dictionaryKey}");
                }

                var param = new
                {
                    state_root_hash = stateRootHash,
                    dictionary_identifier = new
                    {
                        URef = new
                        {
                            seed_uref = seedUref,
                            dictionary_item_key = dictionaryKey
                        }
                    }
                };

                var result = await _networkClient.SendRequestAsync<DictionaryItemResponse>("state_get_dictionary_item", param);

                if (result == null)
                {
                    return null;
                }

                return new DictionaryItem
                {
                    Key = dictionaryKey,
                    DictionaryKey = result.dictionary_key,
                    StoredValue = result.stored_value,
                    MerkleProof = result.merkle_proof
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get dictionary item: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DictionaryItem> GetDictionaryItemByNameAsync(string contractHash, string dictionaryName, string dictionaryKey, string stateRootHash = null)
        {
            if (string.IsNullOrWhiteSpace(contractHash))
            {
                throw new ArgumentException("Contract hash cannot be null or empty", nameof(contractHash));
            }

            if (string.IsNullOrWhiteSpace(dictionaryName))
            {
                throw new ArgumentException("Dictionary name cannot be null or empty", nameof(dictionaryName));
            }

            if (string.IsNullOrWhiteSpace(dictionaryKey))
            {
                throw new ArgumentException("Dictionary key cannot be null or empty", nameof(dictionaryKey));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting dictionary item by name: {dictionaryName}/{dictionaryKey}");
                }

                var param = new
                {
                    state_root_hash = stateRootHash,
                    dictionary_identifier = new
                    {
                        ContractNamedKey = new
                        {
                            key = contractHash,
                            dictionary_name = dictionaryName,
                            dictionary_item_key = dictionaryKey
                        }
                    }
                };

                var result = await _networkClient.SendRequestAsync<DictionaryItemResponse>("state_get_dictionary_item", param);

                if (result == null)
                {
                    return null;
                }

                return new DictionaryItem
                {
                    Key = dictionaryKey,
                    DictionaryKey = result.dictionary_key,
                    StoredValue = result.stored_value,
                    MerkleProof = result.merkle_proof
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get dictionary item by name: {ex.Message}");
                throw;
            }
        }
    }
}
