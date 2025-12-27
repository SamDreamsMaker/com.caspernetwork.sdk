using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Interfaces;
using CasperSDK.Core.Configuration;
using CasperSDK.Models;
using CasperSDK.Services.Deploy;
using CasperSDK.Utilities.Cryptography;

namespace CasperSDK.Services.NFT
{
    /// <summary>
    /// Service for interacting with CEP-78 NFT contracts on Casper Network.
    /// Supports minting, transferring, burning, and querying NFTs.
    /// </summary>
    public class CEP78Service
    {
        private readonly INetworkClient _networkClient;
        private readonly string _chainName;
        private readonly bool _enableLogging;
        private readonly string _contractHash;

        /// <summary>
        /// Creates a new CEP78Service for a specific NFT contract
        /// </summary>
        /// <param name="networkClient">Network client for RPC</param>
        /// <param name="config">Network configuration</param>
        /// <param name="contractHash">CEP-78 contract hash (without 'hash-' prefix)</param>
        public CEP78Service(INetworkClient networkClient, NetworkConfig config, string contractHash)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config?.EnableLogging ?? false;
            _chainName = config?.NetworkType == NetworkType.Mainnet ? "casper" : "casper-test";
            
            // Remove 'hash-' prefix if present
            _contractHash = contractHash?.StartsWith("hash-") == true 
                ? contractHash.Substring(5) 
                : contractHash;
        }

        #region Mint

        /// <summary>
        /// Mints a new NFT with metadata
        /// </summary>
        /// <param name="recipient">Recipient's public key</param>
        /// <param name="tokenMetadata">JSON metadata for the NFT</param>
        /// <param name="senderKeyPair">Sender's key pair (must have mint permission)</param>
        /// <param name="paymentAmount">Payment in motes (default 10 CSPR)</param>
        public async Task<NFTMintResult> MintAsync(
            string recipient,
            NFTMetadata tokenMetadata,
            KeyPair senderKeyPair,
            string paymentAmount = "10000000000")
        {
            if (string.IsNullOrEmpty(recipient))
                throw new ArgumentException("Recipient is required");
            if (tokenMetadata == null)
                throw new ArgumentNullException(nameof(tokenMetadata));
            if (senderKeyPair == null)
                throw new ArgumentNullException(nameof(senderKeyPair));

            try
            {
                if (_enableLogging)
                    Debug.Log($"[CasperSDK] Minting NFT: {tokenMetadata.Name}");

                var metadataJson = Newtonsoft.Json.JsonConvert.SerializeObject(tokenMetadata);

                var args = new RuntimeArg[]
                {
                    new RuntimeArg { Name = "token_owner", Value = CLValueBuilder.PublicKey(recipient) },
                    new RuntimeArg { Name = "token_meta_data", Value = CLValueBuilder.String(metadataJson) }
                };

                var deploy = new DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment(paymentAmount)
                    .SetContractSession(_contractHash, "mint", args)
                    .Build();

                deploy = DeploySigner.SignDeploy(deploy, senderKeyPair);

                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                var result = await _networkClient.SendRequestAsync<Transfer.PutDeployResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    return new NFTMintResult { Success = false, ErrorMessage = "No deploy hash returned" };
                }

                if (_enableLogging)
                    Debug.Log($"[CasperSDK] NFT mint submitted: {result.deploy_hash}");

                return new NFTMintResult
                {
                    Success = true,
                    DeployHash = result.deploy_hash,
                    TokenName = tokenMetadata.Name
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] NFT mint failed: {ex.Message}");
                return new NFTMintResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region Transfer

        /// <summary>
        /// Transfers an NFT to another account
        /// </summary>
        /// <param name="tokenId">Token ID to transfer</param>
        /// <param name="targetPublicKey">Recipient's public key</param>
        /// <param name="senderKeyPair">Current owner's key pair</param>
        /// <param name="paymentAmount">Payment in motes (default 3 CSPR)</param>
        public async Task<NFTTransferResult> TransferAsync(
            ulong tokenId,
            string targetPublicKey,
            KeyPair senderKeyPair,
            string paymentAmount = "3000000000")
        {
            if (string.IsNullOrEmpty(targetPublicKey))
                throw new ArgumentException("Target public key is required");
            if (senderKeyPair == null)
                throw new ArgumentNullException(nameof(senderKeyPair));

            try
            {
                if (_enableLogging)
                    Debug.Log($"[CasperSDK] Transferring NFT #{tokenId}");

                var args = new RuntimeArg[]
                {
                    new RuntimeArg { Name = "token_id", Value = CLValueBuilder.U64(tokenId) },
                    new RuntimeArg { Name = "source_key", Value = CLValueBuilder.PublicKey(senderKeyPair.PublicKeyHex) },
                    new RuntimeArg { Name = "target_key", Value = CLValueBuilder.PublicKey(targetPublicKey) }
                };

                var deploy = new DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment(paymentAmount)
                    .SetContractSession(_contractHash, "transfer", args)
                    .Build();

                deploy = DeploySigner.SignDeploy(deploy, senderKeyPair);

                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                var result = await _networkClient.SendRequestAsync<Transfer.PutDeployResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    return new NFTTransferResult { Success = false, ErrorMessage = "No deploy hash returned" };
                }

                if (_enableLogging)
                    Debug.Log($"[CasperSDK] NFT transfer submitted: {result.deploy_hash}");

                return new NFTTransferResult
                {
                    Success = true,
                    DeployHash = result.deploy_hash,
                    TokenId = tokenId,
                    NewOwner = targetPublicKey
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] NFT transfer failed: {ex.Message}");
                return new NFTTransferResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region Burn

        /// <summary>
        /// Burns (destroys) an NFT
        /// </summary>
        /// <param name="tokenId">Token ID to burn</param>
        /// <param name="senderKeyPair">Owner's key pair</param>
        /// <param name="paymentAmount">Payment in motes (default 3 CSPR)</param>
        public async Task<NFTBurnResult> BurnAsync(
            ulong tokenId,
            KeyPair senderKeyPair,
            string paymentAmount = "3000000000")
        {
            if (senderKeyPair == null)
                throw new ArgumentNullException(nameof(senderKeyPair));

            try
            {
                if (_enableLogging)
                    Debug.Log($"[CasperSDK] Burning NFT #{tokenId}");

                var args = new RuntimeArg[]
                {
                    new RuntimeArg { Name = "token_id", Value = CLValueBuilder.U64(tokenId) }
                };

                var deploy = new DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment(paymentAmount)
                    .SetContractSession(_contractHash, "burn", args)
                    .Build();

                deploy = DeploySigner.SignDeploy(deploy, senderKeyPair);

                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                var result = await _networkClient.SendRequestAsync<Transfer.PutDeployResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    return new NFTBurnResult { Success = false, ErrorMessage = "No deploy hash returned" };
                }

                if (_enableLogging)
                    Debug.Log($"[CasperSDK] NFT burn submitted: {result.deploy_hash}");

                return new NFTBurnResult
                {
                    Success = true,
                    DeployHash = result.deploy_hash,
                    TokenId = tokenId
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] NFT burn failed: {ex.Message}");
                return new NFTBurnResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        #endregion

        #region Query

        /// <summary>
        /// Gets the owner of an NFT by token ID
        /// </summary>
        public async Task<string> GetOwnerAsync(ulong tokenId)
        {
            try
            {
                if (_enableLogging)
                    Debug.Log($"[CasperSDK] Querying owner of NFT #{tokenId}");

                // Query the contract's dictionary
                var dictionaryKey = $"token_owners_{tokenId}";
                var result = await QueryContractDictionary("token_owners", tokenId.ToString());
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Get owner failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the metadata of an NFT by token ID
        /// </summary>
        public async Task<NFTMetadata> GetMetadataAsync(ulong tokenId)
        {
            try
            {
                if (_enableLogging)
                    Debug.Log($"[CasperSDK] Querying metadata of NFT #{tokenId}");

                var metadataJson = await QueryContractDictionary("token_metadata", tokenId.ToString());
                
                if (string.IsNullOrEmpty(metadataJson))
                    return null;

                return Newtonsoft.Json.JsonConvert.DeserializeObject<NFTMetadata>(metadataJson);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Get metadata failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets total supply of NFTs in the collection
        /// </summary>
        public async Task<ulong> GetTotalSupplyAsync()
        {
            try
            {
                var result = await QueryContractNamedKey("number_of_minted_tokens");
                return ulong.TryParse(result, out var supply) ? supply : 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Get total supply failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets collection name
        /// </summary>
        public async Task<string> GetCollectionNameAsync()
        {
            try
            {
                return await QueryContractNamedKey("collection_name");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Get collection name failed: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Private Helpers

        private async Task<string> QueryContractDictionary(string dictionaryName, string itemKey)
        {
            try
            {
                // Get state root hash
                var statusResult = await _networkClient.SendRequestAsync<Models.RPC.StatusResponse>("info_get_status", null);
                var stateRootHash = statusResult?.last_added_block_info?.state_root_hash;

                if (string.IsNullOrEmpty(stateRootHash))
                    throw new Exception("Could not get state root hash");

                var param = new
                {
                    state_root_hash = stateRootHash,
                    dictionary_identifier = new
                    {
                        ContractNamedKey = new
                        {
                            key = $"hash-{_contractHash}",
                            dictionary_name = dictionaryName,
                            dictionary_item_key = itemKey
                        }
                    }
                };

                var result = await _networkClient.SendRequestAsync<Contract.StateQueryResponse>("state_get_dictionary_item", param);
                return result?.stored_value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> QueryContractNamedKey(string keyName)
        {
            try
            {
                var statusResult = await _networkClient.SendRequestAsync<Models.RPC.StatusResponse>("info_get_status", null);
                var stateRootHash = statusResult?.last_added_block_info?.state_root_hash;

                if (string.IsNullOrEmpty(stateRootHash))
                    throw new Exception("Could not get state root hash");

                var param = new
                {
                    state_root_hash = stateRootHash,
                    key = $"hash-{_contractHash}",
                    path = new[] { keyName }
                };

                var result = await _networkClient.SendRequestAsync<Contract.StateQueryResponse>("state_get_item", param);
                return result?.stored_value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private object ConvertToRpcFormat(Models.Deploy deploy)
        {
            return new
            {
                hash = deploy.Hash,
                header = new
                {
                    account = deploy.Header.Account,
                    timestamp = deploy.Header.Timestamp,
                    ttl = $"{deploy.Header.TTL / 60000}m",
                    gas_price = deploy.Header.GasPrice,
                    body_hash = deploy.Header.BodyHash,
                    dependencies = deploy.Header.Dependencies,
                    chain_name = deploy.Header.ChainName
                },
                payment = FormatExecutableItem(deploy.Payment),
                session = FormatExecutableItem(deploy.Session),
                approvals = Array.ConvertAll(deploy.Approvals ?? new DeployApproval[0], 
                    a => new { signer = a.Signer, signature = a.Signature })
            };
        }

        private object FormatExecutableItem(ExecutableDeployItem item)
        {
            if (item == null) return new { };

            switch (item.Type)
            {
                case "ModuleBytes":
                    return new { ModuleBytes = new { module_bytes = item.ModuleBytes ?? "", args = FormatArgs(item.Args) } };
                case "StoredContractByHash":
                    return new { StoredContractByHash = new { hash = item.ContractHash, entry_point = item.EntryPoint, args = FormatArgs(item.Args) } };
                case "Transfer":
                    return new { Transfer = new { args = FormatArgs(item.Args) } };
                default:
                    return new { };
            }
        }

        private object[][] FormatArgs(RuntimeArg[] args)
        {
            if (args == null) return new object[0][];
            var result = new object[args.Length][];
            for (int i = 0; i < args.Length; i++)
            {
                result[i] = new object[] { args[i].Name, new { cl_type = args[i].Value?.CLType, bytes = args[i].Value?.Bytes, parsed = args[i].Value?.Parsed } };
            }
            return result;
        }

        #endregion
    }

    #region Models

    /// <summary>
    /// NFT metadata following CEP-78 standard
    /// </summary>
    [Serializable]
    public class NFTMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
        public NFTAttribute[] Attributes { get; set; }
    }

    [Serializable]
    public class NFTAttribute
    {
        public string TraitType { get; set; }
        public string Value { get; set; }
    }

    [Serializable]
    public class NFTMintResult
    {
        public bool Success { get; set; }
        public string DeployHash { get; set; }
        public string TokenId { get; set; }
        public string TokenName { get; set; }
        public string ErrorMessage { get; set; }
    }

    [Serializable]
    public class NFTTransferResult
    {
        public bool Success { get; set; }
        public string DeployHash { get; set; }
        public ulong TokenId { get; set; }
        public string NewOwner { get; set; }
        public string ErrorMessage { get; set; }
    }

    [Serializable]
    public class NFTBurnResult
    {
        public bool Success { get; set; }
        public string DeployHash { get; set; }
        public ulong TokenId { get; set; }
        public string ErrorMessage { get; set; }
    }

    #endregion
}
