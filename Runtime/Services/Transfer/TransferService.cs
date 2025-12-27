using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Interfaces;
using CasperSDK.Core.Configuration;
using CasperSDK.Models;
using CasperSDK.Services.Deploy;
using CasperSDK.Utilities.Cryptography;

namespace CasperSDK.Services.Transfer
{
    /// <summary>
    /// High-level service for transferring CSPR between accounts.
    /// Simplifies the process of building, signing, and submitting transfer deploys.
    /// </summary>
    public class TransferService
    {
        private readonly INetworkClient _networkClient;
        private readonly string _chainName;
        private readonly bool _enableLogging;

        /// <summary>
        /// Creates a new TransferService
        /// </summary>
        /// <param name="networkClient">Network client for RPC calls</param>
        /// <param name="config">Network configuration</param>
        public TransferService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config?.EnableLogging ?? false;
            _chainName = config?.NetworkType == NetworkType.Mainnet ? "casper" : "casper-test";
        }

        /// <summary>
        /// Transfers CSPR from one account to another
        /// </summary>
        /// <param name="senderKeyPair">Sender's key pair (with private key for signing)</param>
        /// <param name="recipientPublicKey">Recipient's public key</param>
        /// <param name="amountMotes">Amount to transfer in motes (1 CSPR = 1,000,000,000 motes)</param>
        /// <param name="transferId">Optional transfer ID for tracking</param>
        /// <returns>Deploy hash if successful</returns>
        public async Task<TransferResult> TransferAsync(
            KeyPair senderKeyPair,
            string recipientPublicKey,
            string amountMotes,
            ulong? transferId = null)
        {
            if (senderKeyPair == null)
                throw new ArgumentNullException(nameof(senderKeyPair));
            if (string.IsNullOrWhiteSpace(recipientPublicKey))
                throw new ArgumentException("Recipient public key is required");
            if (string.IsNullOrWhiteSpace(amountMotes))
                throw new ArgumentException("Amount is required");

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Initiating transfer of {amountMotes} motes");
                    Debug.Log($"[CasperSDK] From: {senderKeyPair.PublicKeyHex.Substring(0, 20)}...");
                    Debug.Log($"[CasperSDK] To: {recipientPublicKey.Substring(0, 20)}...");
                }

                // Step 1: Build the deploy
                var deploy = new DeployBuilder()
                    .SetChainName(_chainName)
                    .SetSender(senderKeyPair.PublicKeyHex)
                    .SetStandardPayment("100000000") // 0.1 CSPR for transfer fee
                    .SetTransferSession(recipientPublicKey, amountMotes, transferId)
                    .Build();

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Deploy built with hash: {deploy.Hash}");
                }

                // Step 2: Sign the deploy
                deploy = DeploySigner.SignDeploy(deploy, senderKeyPair);

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Deploy signed with {senderKeyPair.Algorithm}");
                }

                // Step 3: Convert to RPC format and submit
                var rpcDeploy = ConvertToRpcFormat(deploy);
                var param = new { deploy = rpcDeploy };
                
                var result = await _networkClient.SendRequestAsync<PutDeployResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    return new TransferResult
                    {
                        Success = false,
                        ErrorMessage = "No deploy hash returned from network"
                    };
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Transfer submitted! Deploy hash: {result.deploy_hash}");
                }

                return new TransferResult
                {
                    Success = true,
                    DeployHash = result.deploy_hash,
                    FromAccount = senderKeyPair.PublicKeyHex,
                    ToAccount = recipientPublicKey,
                    Amount = amountMotes
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Transfer failed: {ex.Message}");
                return new TransferResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Converts CSPR to motes
        /// </summary>
        public static string CsprToMotes(decimal cspr)
        {
            var motes = cspr * 1_000_000_000m;
            return ((long)motes).ToString();
        }

        /// <summary>
        /// Converts motes to CSPR
        /// </summary>
        public static decimal MotesToCspr(string motes)
        {
            if (long.TryParse(motes, out var motesLong))
            {
                return motesLong / 1_000_000_000m;
            }
            return 0;
        }

        /// <summary>
        /// Converts our Deploy model to RPC-compatible format
        /// </summary>
        private object ConvertToRpcFormat(Models.Deploy deploy)
        {
            return new
            {
                hash = deploy.Hash,
                header = new
                {
                    account = deploy.Header.Account,
                    timestamp = deploy.Header.Timestamp,
                    ttl = FormatTTL(deploy.Header.TTL),
                    gas_price = deploy.Header.GasPrice,
                    body_hash = deploy.Header.BodyHash,
                    dependencies = deploy.Header.Dependencies,
                    chain_name = deploy.Header.ChainName
                },
                payment = FormatExecutableItem(deploy.Payment),
                session = FormatExecutableItem(deploy.Session),
                approvals = FormatApprovals(deploy.Approvals)
            };
        }

        private string FormatTTL(long ttlMs)
        {
            // Convert TTL to Casper format (e.g., "30m" for 30 minutes)
            var minutes = ttlMs / 60000;
            return $"{minutes}m";
        }

        private object FormatExecutableItem(ExecutableDeployItem item)
        {
            switch (item.Type)
            {
                case "Transfer":
                    return new
                    {
                        Transfer = new
                        {
                            args = FormatArgs(item.Args)
                        }
                    };

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
                        cl_type = FormatCLType(args[i].Value.CLType),
                        bytes = args[i].Value.Bytes,
                        parsed = args[i].Value.Parsed
                    }
                };
            }
            return result;
        }

        /// <summary>
        /// Formats CLType for RPC - complex types need object format
        /// </summary>
        private object FormatCLType(string clType)
        {
            if (string.IsNullOrEmpty(clType)) return "Unit";
            
            // Handle Option types: "Option(U64)" -> {"Option": "U64"}
            if (clType.StartsWith("Option(") && clType.EndsWith(")"))
            {
                var innerType = clType.Substring(7, clType.Length - 8);
                return new { Option = FormatCLType(innerType) };
            }
            
            // Handle Option without inner type (None): "Option" -> {"Option": "Unit"}
            if (clType == "Option")
            {
                return new { Option = "U64" }; // Default to U64 for transfer id
            }
            
            // Handle List types: "List(U8)" -> {"List": "U8"}
            if (clType.StartsWith("List(") && clType.EndsWith(")"))
            {
                var innerType = clType.Substring(5, clType.Length - 6);
                return new { List = FormatCLType(innerType) };
            }
            
            // Handle Map types: "Map(String,String)" -> {"Map": {"key": "String", "value": "String"}}
            if (clType.StartsWith("Map(") && clType.EndsWith(")"))
            {
                var inner = clType.Substring(4, clType.Length - 5);
                var parts = inner.Split(',');
                if (parts.Length == 2)
                {
                    return new { Map = new { key = FormatCLType(parts[0].Trim()), value = FormatCLType(parts[1].Trim()) } };
                }
            }
            
            // Simple types: U8, U32, U64, U128, U256, U512, String, Bool, etc.
            return clType;
        }

        private object[] FormatApprovals(DeployApproval[] approvals)
        {
            if (approvals == null) return new object[0];

            var result = new object[approvals.Length];
            for (int i = 0; i < approvals.Length; i++)
            {
                result[i] = new
                {
                    signer = approvals[i].Signer,
                    signature = approvals[i].Signature
                };
            }
            return result;
        }
    }

    /// <summary>
    /// Result of a transfer operation
    /// </summary>
    [Serializable]
    public class TransferResult
    {
        public bool Success { get; set; }
        public string DeployHash { get; set; }
        public string FromAccount { get; set; }
        public string ToAccount { get; set; }
        public string Amount { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// RPC response for put_deploy
    /// </summary>
    [Serializable]
    public class PutDeployResponse
    {
        public string deploy_hash { get; set; }
    }
}
