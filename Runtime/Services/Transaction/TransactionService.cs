using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models;
using CasperSDK.Models.RPC;

namespace CasperSDK.Services.Transaction
{
    /// <summary>
    /// Service for transaction operations.
    /// Implements the Service Layer pattern.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly INetworkClient _networkClient;
        private readonly NetworkConfig _config;
        private readonly bool _enableLogging;

        /// <summary>
        /// Initializes a new instance of TransactionService
        /// </summary>
        public TransactionService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _enableLogging = config.EnableLogging;
        }

        /// <inheritdoc/>
        public ITransactionBuilder CreateTransactionBuilder()
        {
            return new TransactionBuilder();
        }

        /// <inheritdoc/>
        public async Task<string> SubmitTransactionAsync(Models.Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Submitting transaction from {transaction.From} to {transaction.Target}");
                }

                // Build the deploy object for submission
                var deploy = BuildDeployFromTransaction(transaction);

                // Submit via account_put_deploy RPC method
                var param = new { deploy = deploy };
                var result = await _networkClient.SendRequestAsync<DeploySubmitResponse>("account_put_deploy", param);

                var deployHash = result?.deploy_hash;

                if (string.IsNullOrEmpty(deployHash))
                {
                    throw new TransactionException("Failed to submit transaction: No deploy hash returned");
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Transaction submitted with hash: {deployHash}");
                }

                return deployHash;
            }
            catch (TransactionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to submit transaction: {ex.Message}");
                throw new TransactionException("Failed to submit transaction", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<ExecutionResult> GetTransactionStatusAsync(string transactionHash)
        {
            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                throw new ArgumentNullException(nameof(transactionHash));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Checking status for transaction: {transactionHash}");
                }

                // Query via info_get_deploy RPC method
                var param = new { deploy_hash = transactionHash };
                var response = await _networkClient.SendRequestAsync<DeployResponse>("info_get_deploy", param);

                if (response == null)
                {
                    return new ExecutionResult
                    {
                        TransactionHash = transactionHash,
                        Status = ExecutionStatus.NotFound
                    };
                }

                // Analyze execution results
                if (response.execution_results == null || response.execution_results.Length == 0)
                {
                    return new ExecutionResult
                    {
                        TransactionHash = transactionHash,
                        Status = ExecutionStatus.Pending
                    };
                }

                var execResult = response.execution_results[0];
                
                if (execResult.result?.Success != null)
                {
                    return new ExecutionResult
                    {
                        TransactionHash = transactionHash,
                        Status = ExecutionStatus.Success,
                        BlockHash = execResult.block_hash,
                        Cost = execResult.result.Success.cost
                    };
                }
                else if (execResult.result?.Failure != null)
                {
                    return new ExecutionResult
                    {
                        TransactionHash = transactionHash,
                        Status = ExecutionStatus.Failed,
                        BlockHash = execResult.block_hash,
                        ErrorMessage = execResult.result.Failure.error_message,
                        Cost = execResult.result.Failure.cost
                    };
                }

                return new ExecutionResult
                {
                    TransactionHash = transactionHash,
                    Status = ExecutionStatus.Pending
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get transaction status: {ex.Message}");
                throw new CasperSDKException("Failed to retrieve transaction status", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<long> EstimateGasAsync(Models.Transaction transaction)
        {
            if (transaction == null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Estimating gas for transaction");
                }

                // Casper uses fixed gas costs based on transaction type
                long estimatedGas;
                
                if (IsNativeTransfer(transaction))
                {
                    // Native CSPR transfers have a fixed cost
                    estimatedGas = SDKSettings.DefaultTransferGas;
                }
                else
                {
                    // Contract calls use default gas limit
                    estimatedGas = SDKSettings.DefaultGasLimit;
                }

                // Query recent blocks to get average gas prices (optional enhancement)
                try
                {
                    await _networkClient.SendRequestAsync<StatusRpcResponse>("info_get_status", null);
                    // Could adjust estimate based on network congestion here
                }
                catch
                {
                    // Ignore - use default estimate
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Estimated gas: {estimatedGas}");
                }

                return estimatedGas;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to estimate gas: {ex.Message}");
                throw new CasperSDKException("Failed to estimate gas", ex);
            }
        }

        #region Private Helpers

        private bool IsNativeTransfer(Models.Transaction transaction)
        {
            return string.IsNullOrEmpty(transaction.ContractHash) &&
                   string.IsNullOrEmpty(transaction.EntryPoint);
        }

        private object BuildDeployFromTransaction(Models.Transaction transaction)
        {
            var timestamp = transaction.Timestamp ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            var ttl = $"{transaction.TTL}ms";
            
            return new
            {
                hash = transaction.DeployHash ?? "",
                header = new
                {
                    account = transaction.From,
                    timestamp = timestamp,
                    ttl = ttl,
                    gas_price = transaction.GasPrice,
                    body_hash = transaction.BodyHash ?? "",
                    dependencies = new string[0],
                    chain_name = transaction.ChainName ?? "casper-test"
                },
                payment = new
                {
                    ModuleBytes = new
                    {
                        module_bytes = "",
                        args = new object[0]
                    }
                },
                session = BuildSessionFromTransaction(transaction),
                approvals = transaction.Approvals ?? new Models.Approval[0]
            };
        }

        private object BuildSessionFromTransaction(Models.Transaction transaction)
        {
            if (IsNativeTransfer(transaction))
            {
                return new
                {
                    Transfer = new
                    {
                        args = new[]
                        {
                            new { name = "amount", value = new { cl_type = "U512", bytes = transaction.Amount } },
                            new { name = "target", value = new { cl_type = "PublicKey", bytes = transaction.Target } },
                            new { name = "id", value = new { cl_type = "Option", bytes = transaction.TransferId?.ToString() ?? "00" } }
                        }
                    }
                };
            }
            else
            {
                return new
                {
                    StoredContractByHash = new
                    {
                        hash = transaction.ContractHash,
                        entry_point = transaction.EntryPoint,
                        args = transaction.Args ?? new object[0]
                    }
                };
            }
        }

        #endregion
    }
}
