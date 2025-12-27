using System;
using Newtonsoft.Json;

namespace CasperSDK.Models.RPC
{
    /// <summary>
    /// Response models for Block RPC calls
    /// </summary>
    
    [Serializable]
    public class BlockResponse
    {
        public string api_version;
        public BlockData block;
        
        public BlockData Block => block;
    }

    [Serializable]
    public class BlockData
    {
        public string hash;
        public BlockHeader header;
        public BlockBody body;
        
        public string Hash => hash;
        public BlockHeader Header => header;
        public BlockBody Body => body;
    }

    [Serializable]
    public class BlockHeader
    {
        public string parent_hash;
        public string state_root_hash;
        public string body_hash;
        public bool random_bit;
        public string accumulated_seed;
        public string era_end;
        public string timestamp;
        public int era_id;
        public long height;
        public string protocol_version;
        
        public string StateRootHash => state_root_hash;
        public long Height => height;
        public string Timestamp => timestamp;
        public int EraId => era_id;
    }

    [Serializable]
    public class BlockBody
    {
        public string proposer;
        public string[] deploy_hashes;
        public string[] transfer_hashes;
        
        public string Proposer => proposer;
        public string[] DeployHashes => deploy_hashes;
        public string[] TransferHashes => transfer_hashes;
    }

    [Serializable]
    public class StateRootHashResponse
    {
        public string api_version;
        public string state_root_hash;
        
        public string StateRootHash => state_root_hash;
    }

    [Serializable]
    public class BlockIdentifierParam
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object block_identifier;
    }

    [Serializable]
    public class BlockHashIdentifier
    {
        public string Hash;
    }

    [Serializable]
    public class BlockHeightIdentifier
    {
        public long Height;
    }
}
