using CasperSDK.Models;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for transaction builder (Builder Pattern)
    /// </summary>
    public interface ITransactionBuilder
    {
        /// <summary>
        /// Sets the sender's public key
        /// </summary>
        /// <param name="publicKey">Sender's public key</param>
        /// <returns>This builder instance for fluent API</returns>
        ITransactionBuilder SetFrom(string publicKey);

        /// <summary>
        /// Sets the recipient's public key
        /// </summary>
        /// <param name="publicKey">Recipient's public key</param>
        /// <returns>This builder instance for fluent API</returns>
        ITransactionBuilder SetTarget(string publicKey);

        /// <summary>
        /// Sets the transfer amount in motes
        /// </summary>
        /// <param name="amount">Amount in motes</param>
        /// <returns>This builder instance for fluent API</returns>
        ITransactionBuilder SetAmount(string amount);

        /// <summary>
        /// Sets the gas price
        /// </summary>
        /// <param name="gasPrice">Gas price</param>
        /// <returns>This builder instance for fluent API</returns>
        ITransactionBuilder SetGasPrice(long gasPrice);

        /// <summary>
        /// Sets the transaction time-to-live in milliseconds
        /// </summary>
        /// <param name="ttlMilliseconds">TTL in milliseconds</param>
        /// <returns>This builder instance for fluent API</returns>
        ITransactionBuilder SetTTL(long ttlMilliseconds);

        /// <summary>
        /// Sets the transfer ID
        /// </summary>
        /// <param name="transferId">Transfer ID</param>
        /// <returns>This builder instance for fluent API</returns>
        ITransactionBuilder SetTransferId(ulong transferId);

        /// <summary>
        /// Builds and validates the transaction
        /// </summary>
        /// <returns>Constructed transaction</returns>
        /// <exception cref="CasperSDKException">Thrown when validation fails</exception>
        Transaction Build();
    }
}
