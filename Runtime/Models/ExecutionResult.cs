namespace CasperSDK.Models
{
    /// <summary>
    /// Execution result of a transaction
    /// </summary>
    public class ExecutionResult
    {
        /// <summary>
        /// Transaction hash
        /// </summary>
        public string TransactionHash { get; set; }

        /// <summary>
        /// Block hash where transaction was executed
        /// </summary>
        public string BlockHash { get; set; }

        /// <summary>
        /// Execution status
        /// </summary>
        public ExecutionStatus Status { get; set; }

        /// <summary>
        /// Error message if execution failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gas cost (legacy, use Cost instead)
        /// </summary>
        public long GasCost { get; set; }

        /// <summary>
        /// Execution cost in motes
        /// </summary>
        public string Cost { get; set; }

        /// <summary>
        /// Execution results
        /// </summary>
        public string[] Transfers { get; set; }
    }

    /// <summary>
    /// Transaction execution status
    /// </summary>
    public enum ExecutionStatus
    {
        NotFound,
        Pending,
        Success,
        Failed
    }
}
