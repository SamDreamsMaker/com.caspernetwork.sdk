namespace CasperSDK.Models
{
    /// <summary>
    /// Represents a Casper account
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Account hash
        /// </summary>
        public string AccountHash { get; set; }

        /// <summary>
        /// Public key
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// Main purse URef
        /// </summary>
        public string MainPurse { get; set; }

        /// <summary>
        /// Account balance in motes
        /// </summary>
        public string Balance { get; set; }

        /// <summary>
        /// Associated keys with weights
        /// </summary>
        public AssociatedKey[] AssociatedKeys { get; set; }

        /// <summary>
        /// Action thresholds
        /// </summary>
        public ActionThresholds ActionThresholds { get; set; }
    }

    /// <summary>
    /// Represents an associated key with weight
    /// </summary>
    public class AssociatedKey
    {
        public string AccountHash { get; set; }
        public byte Weight { get; set; }
    }

    /// <summary>
    /// Action thresholds for account operations
    /// </summary>
    public class ActionThresholds
    {
        public byte Deployment { get; set; }
        public byte KeyManagement { get; set; }
    }
}
