using System;

namespace CasperSDK.Models.RPC
{
    /// <summary>
    /// Response models for Deploy RPC calls
    /// </summary>

    [Serializable]
    public class DeployResponse
    {
        public string api_version;
        public DeployData deploy;
        public DeployExecutionResultWrapper[] execution_results;
    }

    [Serializable]
    public class DeployData
    {
        public string hash;
        public DeployHeader header;
        public object payment;
        public object session;
        public DeployApproval[] approvals;
        
        public string Hash => hash;
    }

    [Serializable]
    public class DeployHeader
    {
        public string account;
        public string timestamp;
        public string ttl;
        public long gas_price;
        public string body_hash;
        public string[] dependencies;
        public string chain_name;
    }

    [Serializable]
    public class DeployApproval
    {
        public string signer;
        public string signature;
    }

    [Serializable]
    public class DeployExecutionResultWrapper
    {
        public string block_hash;
        public DeployExecutionResultData result;
    }

    [Serializable]
    public class DeployExecutionResultData
    {
        public DeploySuccessResult Success;
        public DeployFailureResult Failure;
    }

    [Serializable]
    public class DeploySuccessResult
    {
        public object effect;
        public object[] transfers;
        public string cost;
    }

    [Serializable]
    public class DeployFailureResult
    {
        public object effect;
        public object[] transfers;
        public string cost;
        public string error_message;
    }

    [Serializable]
    public class DeploySubmitResponse
    {
        public string api_version;
        public string deploy_hash;
    }

    [Serializable]
    public class DeployHashParam
    {
        public string deploy_hash;
    }

    /// <summary>
    /// Public model for deploy info
    /// </summary>
    public class DeployInfo
    {
        public string Hash { get; set; }
        public string Account { get; set; }
        public string Timestamp { get; set; }
        public string ChainName { get; set; }
    }

    /// <summary>
    /// Public model for deploy execution status
    /// </summary>
    public class DeployExecutionStatus
    {
        public DeployStatus Status { get; set; }
        public string BlockHash { get; set; }
        public string Cost { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Deploy status enumeration
    /// </summary>
    public enum DeployStatus
    {
        NotFound,
        Pending,
        Success,
        Failed
    }
}
