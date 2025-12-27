using System;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models;

namespace CasperSDK.Services.Transaction
{
    /// <summary>
    /// Transaction builder implementing the Builder pattern.
    /// Provides a fluent API for constructing transactions.
    /// </summary>
    public class TransactionBuilder : ITransactionBuilder
    {
        private string _from;
        private string _target;
        private string _amount;
        private long _gasPrice = SDKSettings.DefaultGasPrice;
        private long _ttl = SDKSettings.DefaultTransactionTTL;
        private ulong? _transferId;

        /// <inheritdoc/>
        public ITransactionBuilder SetFrom(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentNullException(nameof(publicKey));
            }
            _from = publicKey;
            return this;
        }

        /// <inheritdoc/>
        public ITransactionBuilder SetTarget(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentNullException(nameof(publicKey));
            }
            _target = publicKey;
            return this;
        }

        /// <inheritdoc/>
        public ITransactionBuilder SetAmount(string amount)
        {
            if (string.IsNullOrWhiteSpace(amount))
            {
                throw new ArgumentNullException(nameof(amount));
            }

            // Validate amount is a valid number
            if (!ulong.TryParse(amount, out _))
            {
                throw new ValidationException("Amount must be a valid positive number");
            }

            _amount = amount;
            return this;
        }

        /// <inheritdoc/>
        public ITransactionBuilder SetGasPrice(long gasPrice)
        {
            if (gasPrice <= 0)
            {
                throw new ValidationException("Gas price must be greater than 0");
            }
            _gasPrice = gasPrice;
            return this;
        }

        /// <inheritdoc/>
        public ITransactionBuilder SetTTL(long ttlMilliseconds)
        {
            if (ttlMilliseconds <= 0)
            {
                throw new ValidationException("TTL must be greater than 0");
            }
            _ttl = ttlMilliseconds;
            return this;
        }

        /// <inheritdoc/>
        public ITransactionBuilder SetTransferId(ulong transferId)
        {
            _transferId = transferId;
            return this;
        }

        /// <inheritdoc/>
        public Models.Transaction Build()
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(_from))
            {
                throw new ValidationException("Sender public key (From) is required");
            }

            if (string.IsNullOrWhiteSpace(_target))
            {
                throw new ValidationException("Recipient public key (Target) is required");
            }

            if (string.IsNullOrWhiteSpace(_amount))
            {
                throw new ValidationException("Amount is required");
            }

            // Create transaction
            var transaction = new Models.Transaction
            {
                From = _from,
                Target = _target,
                Amount = _amount,
                GasPrice = _gasPrice,
                TTL = _ttl,
                TransferId = _transferId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
                ChainName = "casper-test" // TODO: Make this configurable based on network
            };

            return transaction;
        }

        /// <summary>
        /// Resets the builder to default state
        /// </summary>
        /// <returns>This builder instance</returns>
        public ITransactionBuilder Reset()
        {
            _from = null;
            _target = null;
            _amount = null;
            _gasPrice = SDKSettings.DefaultGasPrice;
            _ttl = SDKSettings.DefaultTransactionTTL;
            _transferId = null;
            return this;
        }
    }
}
