using System;

namespace CasperSDK.Models.RPC
{
    /// <summary>
    /// Response models for Account RPC calls
    /// </summary>

    [Serializable]
    public class AccountInfoRpcResponse
    {
        public string api_version;
        public AccountDataResponse account;
    }

    [Serializable]
    public class AccountDataResponse
    {
        public string account_hash;
        public object[] named_keys;
        public string main_purse;
        public object[] associated_keys;
        public object action_thresholds;
    }

    [Serializable]
    public class BalanceRpcResponse
    {
        public string api_version;
        public string balance_value;
    }

    [Serializable]
    public class AccountInfoParams
    {
        public object public_key;
        public object block_identifier;
    }

    [Serializable]
    public class AccountBlockIdentifier
    {
        public string Hash;
    }

    [Serializable]
    public class BalanceParams
    {
        public string state_root_hash;
        public string purse_uref;
    }

    [Serializable]
    public class StatusResponse
    {
        public string api_version;
        public AccountBlockInfo last_added_block_info;
    }

    [Serializable]
    public class AccountBlockInfo
    {
        public string hash;
        public string timestamp;
        public int era_id;
        public long height;
        public string state_root_hash;
    }
}
