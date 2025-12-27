using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models.RPC;

namespace CasperSDK.Services.Deploy
{
    /// <summary>
    /// Service for querying and submitting deploys (transactions).
    /// </summary>
    public class DeployService : IDeployService
    {
        private readonly INetworkClient _networkClient;
        private readonly bool _enableLogging;

        public DeployService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _enableLogging = config.EnableLogging;
        }

        /// <summary>
        /// Get a deploy by its hash.
        /// </summary>
        public async Task<DeployInfo> GetDeployAsync(string deployHash)
        {
            if (string.IsNullOrWhiteSpace(deployHash))
            {
                throw new ArgumentException("Deploy hash cannot be null or empty", nameof(deployHash));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting deploy: {deployHash}");
                }

                var param = new DeployHashParam { deploy_hash = deployHash };
                var result = await _networkClient.SendRequestAsync<DeployResponse>("info_get_deploy", param);

                if (result?.deploy == null)
                {
                    return null;
                }

                return new DeployInfo
                {
                    Hash = result.deploy.Hash,
                    Account = result.deploy.header?.account,
                    Timestamp = result.deploy.header?.timestamp,
                    ChainName = result.deploy.header?.chain_name
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get deploy: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get deploy execution status.
        /// </summary>
        public async Task<DeployExecutionStatus> GetDeployStatusAsync(string deployHash)
        {
            if (string.IsNullOrWhiteSpace(deployHash))
            {
                throw new ArgumentException("Deploy hash cannot be null or empty", nameof(deployHash));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting deploy status: {deployHash}");
                }

                var param = new DeployHashParam { deploy_hash = deployHash };
                var result = await _networkClient.SendRequestAsync<DeployResponse>("info_get_deploy", param);

                if (result == null)
                {
                    return new DeployExecutionStatus { Status = DeployStatus.NotFound };
                }

                if (result.execution_results == null || result.execution_results.Length == 0)
                {
                    return new DeployExecutionStatus { Status = DeployStatus.Pending };
                }

                var execResult = result.execution_results[0];
                
                if (execResult.result?.Success != null)
                {
                    return new DeployExecutionStatus
                    {
                        Status = DeployStatus.Success,
                        BlockHash = execResult.block_hash,
                        Cost = execResult.result.Success.cost
                    };
                }
                else if (execResult.result?.Failure != null)
                {
                    return new DeployExecutionStatus
                    {
                        Status = DeployStatus.Failed,
                        BlockHash = execResult.block_hash,
                        Cost = execResult.result.Failure.cost,
                        ErrorMessage = execResult.result.Failure.error_message
                    };
                }

                return new DeployExecutionStatus { Status = DeployStatus.Pending };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get deploy status: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Submit a signed deploy to the network.
        /// </summary>
        public async Task<string> SubmitDeployAsync(object deploy)
        {
            if (deploy == null)
            {
                throw new ArgumentNullException(nameof(deploy));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log("[CasperSDK] Submitting deploy");
                }

                var param = new { deploy = deploy };
                var result = await _networkClient.SendRequestAsync<DeploySubmitResponse>("account_put_deploy", param);

                if (string.IsNullOrEmpty(result?.deploy_hash))
                {
                    throw new Exception("No deploy hash returned from submission");
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Deploy submitted: {result.deploy_hash}");
                }

                return result.deploy_hash;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to submit deploy: {ex.Message}");
                throw;
            }
        }
    }
}
