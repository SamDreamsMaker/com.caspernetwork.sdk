using System;

namespace CasperSDK.Models.RPC
{
    /// <summary>
    /// Response models for State RPC calls
    /// </summary>

    [Serializable]
    public class QueryGlobalStateResponse
    {
        public string api_version;
        public object stored_value;
        public string merkle_proof;
    }

    [Serializable]
    public class DictionaryItemResponse
    {
        public string api_version;
        public string dictionary_key;
        public object stored_value;
        public string merkle_proof;
    }

    /// <summary>
    /// Public model for global state value
    /// </summary>
    public class GlobalStateValue
    {
        public string Key { get; set; }
        public object StoredValue { get; set; }
        public string MerkleProof { get; set; }
    }

    /// <summary>
    /// Public model for dictionary item
    /// </summary>
    public class DictionaryItem
    {
        public string Key { get; set; }
        public string DictionaryKey { get; set; }
        public object StoredValue { get; set; }
        public string MerkleProof { get; set; }
    }
}
