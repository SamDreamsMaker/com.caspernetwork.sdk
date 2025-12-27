using System;

namespace CasperSDK.Models.RPC
{
    /// <summary>
    /// Response models for Network Info RPC calls
    /// </summary>

    [Serializable]
    public class StatusRpcResponse
    {
        public string api_version;
        public string chainspec_name;
        public string starting_state_root_hash;
        public PeerInfo[] peers;
        public BlockInfoResponse last_added_block_info;
        public string build_version;
        public string uptime;
    }

    [Serializable]
    public class BlockInfoResponse
    {
        public string hash;
        public string timestamp;
        public int era_id;
        public long height;
        public string state_root_hash;
        public string creator;
    }

    [Serializable]
    public class PeersResponse
    {
        public string api_version;
        public PeerInfo[] peers;
    }

    [Serializable]
    public class PeerInfo
    {
        public string node_id;
        public string address;
        
        public string NodeId => node_id;
        public string Address => address;
    }

    [Serializable]
    public class ChainspecResponse
    {
        public string api_version;
        public ChainspecBytesData chainspec_bytes;
    }

    [Serializable]
    public class ChainspecBytesData
    {
        public string chainspec_bytes;
        public string maybe_genesis_accounts_bytes;
        public string maybe_global_state_bytes;
    }

    /// <summary>
    /// Public model for node status
    /// </summary>
    public class NodeStatus
    {
        public string ApiVersion { get; set; }
        public string ChainspecName { get; set; }
        public string StartingStateRootHash { get; set; }
        public PeerInfo[] Peers { get; set; }
        public BlockInfoResponse LastAddedBlockInfo { get; set; }
        public string BuildVersion { get; set; }
        public string Uptime { get; set; }
    }

    /// <summary>
    /// Public model for chainspec info
    /// </summary>
    public class ChainspecInfo
    {
        public string ApiVersion { get; set; }
        public ChainspecBytesData ChainspecBytes { get; set; }
    }
}
