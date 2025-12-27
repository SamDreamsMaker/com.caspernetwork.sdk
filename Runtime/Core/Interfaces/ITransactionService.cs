using System;
using System.Threading.Tasks;
using CasperSDK.Models;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for transaction operations
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>
        /// Creates a new transaction builder
        /// </summary>
        /// <returns>Transaction builder instance</returns>
        ITransactionBuilder CreateTransactionBuilder();

        /// <summary>
        /// Submits a transaction to the network
        /// </summary>
        /// <param name="transaction">Transaction to submit</param>
        /// <returns>Transaction hash</returns>
        /// <exception cref="ArgumentNullException">Thrown when transaction is null</exception>
        /// <exception cref="CasperSDKException">Thrown when submission fails</exception>
        Task<string> SubmitTransactionAsync(Transaction transaction);

        /// <summary>
        /// Gets the execution status of a transaction
        /// </summary>
        /// <param name="transactionHash">Hash of the transaction</param>
        /// <returns>Execution result</returns>
        /// <exception cref="ArgumentNullException">Thrown when transactionHash is null</exception>
        /// <exception cref="CasperSDKException">Thrown when the query fails</exception>
        Task<ExecutionResult> GetTransactionStatusAsync(string transactionHash);

        /// <summary>
        /// Estimates the gas required for a transaction
        /// </summary>
        /// <param name="transaction">Transaction to estimate</param>
        /// <returns>Estimated gas amount</returns>
        /// <exception cref="ArgumentNullException">Thrown when transaction is null</exception>
        /// <exception cref="CasperSDKException">Thrown when estimation fails</exception>
        Task<long> EstimateGasAsync(Transaction transaction);
    }
}
