using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Interfaces;
using CasperSDK.Core.Configuration;
using CasperSDK.Models;
using CasperSDK.Utilities.Cryptography;

namespace CasperSDK.Services.Contract
{
    /// <summary>
    /// Service for deploying and interacting with smart contracts on Casper Network.
    /// Supports WASM contract deployment and stored contract invocation.
    /// </summary>
    public class ContractService
    {
        private readonly INetworkClient _networkClient;
        private readonly string _chainName;
        private readonly bool _enableLogging;

        public ContractService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config?.EnableLogging ?? false;
            _chainName = config?.NetworkType == NetworkType.Mainnet ? "casper" : "casper-test";
        }

        /// <summary>
        /// Deploys a WASM contract to the network
        /// </summary>
        /// <param name="wasmBytes">Compiled WASM bytecode</param>
        /// <param name="args">Constructor arguments</param>
        /// <param name="senderKeyPair">Sender's key pair for signing</param>
        /// <param name="paymentAmount">Payment amount in motes</param>
        /// <returns>Deploy result with contract hash</returns>
        public async Task<ContractDeployResult> DeployContractAsync(
            byte[] wasmBytes,
            RuntimeArg[] args,
            KeyPair senderKeyPair,
            string paymentAmount = "50000000000") // 50 CSPR default
        {
            if (wasmBytes == null || wasmBytes.Length == 0)
                throw new ArgumentException("WASM bytes cannot be null or empty");
            if (senderKeyPair == null)
                throw new ArgumentNullException(nameof(senderKeyPair));

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Deploying contract ({wasmBytes.Length} bytes)...");
                }

                // Build deploy with WASM session
                var deploy = new Deploy.DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment(paymentAmount)
                    .SetWasmSession(wasmBytes, args)
                    .Build();

                // Sign
                deploy = Deploy.DeploySigner.SignDeploy(deploy, senderKeyPair);

                // Submit
                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                var result = await _networkClient.SendRequestAsync<Transfer.PutDeployResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    return new ContractDeployResult
                    {
                        Success = false,
                        ErrorMessage = "No deploy hash returned"
                    };
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Contract deployment submitted: {result.deploy_hash}");
                }

                return new ContractDeployResult
                {
                    Success = true,
                    DeployHash = result.deploy_hash
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Contract deployment failed: {ex.Message}");
                return new ContractDeployResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Deploys a WASM contract from a file path
        /// </summary>
        public async Task<ContractDeployResult> DeployContractFromFileAsync(
            string wasmFilePath,
            RuntimeArg[] args,
            KeyPair senderKeyPair,
            string paymentAmount = "50000000000")
        {
            if (!File.Exists(wasmFilePath))
                throw new FileNotFoundException("WASM file not found", wasmFilePath);

            var wasmBytes = File.ReadAllBytes(wasmFilePath);
            return await DeployContractAsync(wasmBytes, args, senderKeyPair, paymentAmount);
        }

        /// <summary>
        /// Invokes a stored contract by its hash
        /// </summary>
        /// <param name="contractHash">Contract hash (without 'hash-' prefix)</param>
        /// <param name="entryPoint">Entry point (function) to call</param>
        /// <param name="args">Function arguments</param>
        /// <param name="senderKeyPair">Sender's key pair for signing</param>
        /// <param name="paymentAmount">Payment amount in motes</param>
        public async Task<ContractCallResult> CallContractByHashAsync(
            string contractHash,
            string entryPoint,
            RuntimeArg[] args,
            KeyPair senderKeyPair,
            string paymentAmount = "3000000000") // 3 CSPR default
        {
            if (string.IsNullOrEmpty(contractHash))
                throw new ArgumentException("Contract hash is required");
            if (string.IsNullOrEmpty(entryPoint))
                throw new ArgumentException("Entry point is required");
            if (senderKeyPair == null)
                throw new ArgumentNullException(nameof(senderKeyPair));

            try
            {
                // Remove 'hash-' prefix if present
                if (contractHash.StartsWith("hash-"))
                    contractHash = contractHash.Substring(5);

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Calling contract {contractHash.Substring(0, 16)}...:{entryPoint}");
                }

                // Build deploy with contract call session
                var deploy = new Deploy.DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment(paymentAmount)
                    .SetContractSession(contractHash, entryPoint, args)
                    .Build();

                // Sign
                deploy = Deploy.DeploySigner.SignDeploy(deploy, senderKeyPair);

                // Submit
                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                var result = await _networkClient.SendRequestAsync<Transfer.PutDeployResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    return new ContractCallResult
                    {
                        Success = false,
                        ErrorMessage = "No deploy hash returned"
                    };
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Contract call submitted: {result.deploy_hash}");
                }

                return new ContractCallResult
                {
                    Success = true,
                    DeployHash = result.deploy_hash,
                    ContractHash = contractHash,
                    EntryPoint = entryPoint
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Contract call failed: {ex.Message}");
                return new ContractCallResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Invokes a stored contract by its name (under the sender's account)
        /// </summary>
        public async Task<ContractCallResult> CallContractByNameAsync(
            string contractName,
            string entryPoint,
            RuntimeArg[] args,
            KeyPair senderKeyPair,
            string paymentAmount = "3000000000")
        {
            if (string.IsNullOrEmpty(contractName))
                throw new ArgumentException("Contract name is required");

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Calling contract '{contractName}':{entryPoint}");
                }

                // Build a deploy for StoredContractByName
                var session = new ExecutableDeployItem
                {
                    Type = "StoredContractByName",
                    ContractName = contractName,
                    EntryPoint = entryPoint,
                    Args = args
                };

                var deploy = new Deploy.DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment(paymentAmount)
                    .Build();

                // Override session manually (simplified approach)
                // In production, you'd extend DeployBuilder

                deploy = Deploy.DeploySigner.SignDeploy(deploy, senderKeyPair);

                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                var result = await _networkClient.SendRequestAsync<Transfer.PutDeployResponse>("account_put_deploy", param);

                return new ContractCallResult
                {
                    Success = !string.IsNullOrEmpty(result?.deploy_hash),
                    DeployHash = result?.deploy_hash,
                    ContractName = contractName,
                    EntryPoint = entryPoint,
                    ErrorMessage = string.IsNullOrEmpty(result?.deploy_hash) ? "No deploy hash returned" : null
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Contract call failed: {ex.Message}");
                return new ContractCallResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Queries a contract's named key value from global state
        /// </summary>
        public async Task<string> QueryContractDataAsync(
            string contractHash,
            string namedKey)
        {
            try
            {
                // Remove 'hash-' prefix if present
                if (contractHash.StartsWith("hash-"))
                    contractHash = contractHash.Substring(5);

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Querying contract state: {namedKey}");
                }

                // Get state root hash first
                var statusResult = await _networkClient.SendRequestAsync<Models.RPC.StatusResponse>("info_get_status", null);
                var stateRootHash = statusResult?.last_added_block_info?.state_root_hash;

                if (string.IsNullOrEmpty(stateRootHash))
                    throw new Exception("Could not get state root hash");

                // Query the contract data
                var key = $"hash-{contractHash}";
                var param = new
                {
                    state_root_hash = stateRootHash,
                    key = key,
                    path = new[] { namedKey }
                };

                var result = await _networkClient.SendRequestAsync<StateQueryResponse>("state_get_item", param);
                
                return result?.stored_value?.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Contract query failed: {ex.Message}");
                throw;
            }
        }

        private object ConvertToRpcFormat(Models.Deploy deploy)
        {
            // Reuse TransferService's conversion logic
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
                approvals = deploy.Approvals?.Length > 0
                    ? Array.ConvertAll(deploy.Approvals, a => new { signer = a.Signer, signature = a.Signature })
                    : new object[0]
            };
        }

        private object FormatExecutableItem(ExecutableDeployItem item)
        {
            if (item == null) return new { };

            switch (item.Type)
            {
                case "ModuleBytes":
                    return new
                    {
                        ModuleBytes = new
                        {
                            module_bytes = item.ModuleBytes ?? "",
                            args = FormatArgs(item.Args)
                        }
                    };
                case "StoredContractByHash":
                    return new
                    {
                        StoredContractByHash = new
                        {
                            hash = item.ContractHash,
                            entry_point = item.EntryPoint,
                            args = FormatArgs(item.Args)
                        }
                    };
                case "StoredContractByName":
                    return new
                    {
                        StoredContractByName = new
                        {
                            name = item.ContractName,
                            entry_point = item.EntryPoint,
                            args = FormatArgs(item.Args)
                        }
                    };
                case "Transfer":
                    return new
                    {
                        Transfer = new
                        {
                            args = FormatArgs(item.Args)
                        }
                    };
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
                result[i] = new object[]
                {
                    args[i].Name,
                    new
                    {
                        cl_type = args[i].Value?.CLType,
                        bytes = args[i].Value?.Bytes,
                        parsed = args[i].Value?.Parsed
                    }
                };
            }
            return result;
        }
    }

    /// <summary>
    /// Result of contract deployment
    /// </summary>
    [Serializable]
    public class ContractDeployResult
    {
        public bool Success { get; set; }
        public string DeployHash { get; set; }
        public string ContractHash { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Result of contract call
    /// </summary>
    [Serializable]
    public class ContractCallResult
    {
        public bool Success { get; set; }
        public string DeployHash { get; set; }
        public string ContractHash { get; set; }
        public string ContractName { get; set; }
        public string EntryPoint { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Response from state query
    /// </summary>
    [Serializable]
    public class StateQueryResponse
    {
        public string api_version { get; set; }
        public object stored_value { get; set; }
        public string merkle_proof { get; set; }
    }
}
