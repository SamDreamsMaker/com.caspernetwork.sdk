using System;

namespace CasperSDK.Models
{
    /// <summary>
    /// Represents a Casper Network Deploy (transaction).
    /// A deploy is the unit of work on the Casper Network.
    /// </summary>
    [Serializable]
    public class Deploy
    {
        /// <summary>
        /// Blake2b-256 hash of the deploy header
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Deploy header containing metadata
        /// </summary>
        public DeployHeader Header { get; set; }

        /// <summary>
        /// Payment execution logic
        /// </summary>
        public ExecutableDeployItem Payment { get; set; }

        /// <summary>
        /// Session execution logic (the actual work)
        /// </summary>
        public ExecutableDeployItem Session { get; set; }

        /// <summary>
        /// Signatures approving the deploy
        /// </summary>
        public DeployApproval[] Approvals { get; set; }
    }

    /// <summary>
    /// Deploy header containing metadata about the deploy
    /// </summary>
    [Serializable]
    public class DeployHeader
    {
        /// <summary>
        /// Public key of the account paying for the deploy
        /// </summary>
        public string Account { get; set; }

        /// <summary>
        /// Timestamp when the deploy was created (ISO 8601)
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Time-to-live in milliseconds
        /// </summary>
        public long TTL { get; set; }

        /// <summary>
        /// Gas price for the deploy
        /// </summary>
        public long GasPrice { get; set; }

        /// <summary>
        /// Blake2b-256 hash of the deploy body
        /// </summary>
        public string BodyHash { get; set; }

        /// <summary>
        /// List of deploy hashes that must be executed before this one
        /// </summary>
        public string[] Dependencies { get; set; }

        /// <summary>
        /// Name of the chain (e.g., "casper-test", "casper")
        /// </summary>
        public string ChainName { get; set; }
    }

    /// <summary>
    /// Executable deploy item (payment or session)
    /// </summary>
    [Serializable]
    public class ExecutableDeployItem
    {
        /// <summary>
        /// Type of executable: ModuleBytes, StoredContractByHash, StoredContractByName, 
        /// StoredVersionedContractByHash, StoredVersionedContractByName, Transfer
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Module bytes (WASM) for ModuleBytes type
        /// </summary>
        public string ModuleBytes { get; set; }

        /// <summary>
        /// Runtime arguments
        /// </summary>
        public RuntimeArg[] Args { get; set; }

        /// <summary>
        /// Contract hash for stored contract types
        /// </summary>
        public string ContractHash { get; set; }

        /// <summary>
        /// Contract name for stored contract by name types
        /// </summary>
        public string ContractName { get; set; }

        /// <summary>
        /// Entry point name to call
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Version for versioned contracts
        /// </summary>
        public uint? Version { get; set; }
    }

    /// <summary>
    /// Runtime argument for contract execution
    /// </summary>
    [Serializable]
    public class RuntimeArg
    {
        /// <summary>
        /// Argument name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// CLValue representing the argument value
        /// </summary>
        public CLValue Value { get; set; }
    }

    /// <summary>
    /// Casper CLValue - the type system for contract arguments
    /// </summary>
    [Serializable]
    public class CLValue
    {
        /// <summary>
        /// CLType tag
        /// </summary>
        public string CLType { get; set; }

        /// <summary>
        /// Serialized bytes (hex)
        /// </summary>
        public string Bytes { get; set; }

        /// <summary>
        /// Parsed value (for display)
        /// </summary>
        public object Parsed { get; set; }
    }

    /// <summary>
    /// Deploy approval containing a signature
    /// </summary>
    [Serializable]
    public class DeployApproval
    {
        /// <summary>
        /// Public key of the signer
        /// </summary>
        public string Signer { get; set; }

        /// <summary>
        /// Signature over the deploy hash
        /// </summary>
        public string Signature { get; set; }
    }

    /// <summary>
    /// Result of deploy execution
    /// </summary>
    [Serializable]
    public class DeployExecutionResult
    {
        public string DeployHash { get; set; }
        public string BlockHash { get; set; }
        public DeployExecutionStatus Status { get; set; }
        public string ErrorMessage { get; set; }
        public string Cost { get; set; }
        public string[] Transfers { get; set; }
    }

    /// <summary>
    /// Deploy execution status
    /// </summary>
    public enum DeployExecutionStatus
    {
        Pending,
        Processing,
        Success,
        Failure
    }
}
