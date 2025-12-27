namespace CasperSDK.Models
{
    /// <summary>
    /// Represents a transaction/deploy
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Transaction hash
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// Deploy hash (when submitted)
        /// </summary>
        public string DeployHash { get; set; }

        /// <summary>
        /// Sender's public key
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Recipient's public key (for transfers)
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Transfer amount in motes
        /// </summary>
        public string Amount { get; set; }

        /// <summary>
        /// Gas price
        /// </summary>
        public long GasPrice { get; set; }

        /// <summary>
        /// Time-to-live in milliseconds
        /// </summary>
        public long TTL { get; set; }

        /// <summary>
        /// Transfer ID (optional)
        /// </summary>
        public ulong? TransferId { get; set; }

        /// <summary>
        /// Transaction timestamp
        /// </summary>
        public string Timestamp { get; set; }

        /// <summary>
        /// Chain name
        /// </summary>
        public string ChainName { get; set; }

        /// <summary>
        /// Body hash for deploy
        /// </summary>
        public string BodyHash { get; set; }

        /// <summary>
        /// Contract hash (for contract calls)
        /// </summary>
        public string ContractHash { get; set; }

        /// <summary>
        /// Entry point (for contract calls)
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Contract call arguments
        /// </summary>
        public object[] Args { get; set; }

        /// <summary>
        /// Approval signatures
        /// </summary>
        public Approval[] Approvals { get; set; }
    }

    /// <summary>
    /// Represents a transaction approval/signature
    /// </summary>
    public class Approval
    {
        public string Signer { get; set; }
        public string Signature { get; set; }
    }
}
