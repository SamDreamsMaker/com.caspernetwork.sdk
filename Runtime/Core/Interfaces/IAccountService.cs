using System;
using System.Threading.Tasks;
using CasperSDK.Models;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for account management operations
    /// </summary>
    public interface IAccountService
    {
        /// <summary>
        /// Retrieves account information by public key
        /// </summary>
        /// <param name="publicKey">Public key of the account</param>
        /// <returns>Account information</returns>
        /// <exception cref="ArgumentNullException">Thrown when publicKey is null</exception>
        /// <exception cref="CasperSDKException">Thrown when the operation fails</exception>
        Task<Account> GetAccountAsync(string publicKey);

        /// <summary>
        /// Gets the balance of an account in motes
        /// </summary>
        /// <param name="publicKey">Public key of the account</param>
        /// <returns>Account balance in motes</returns>
        /// <exception cref="ArgumentNullException">Thrown when publicKey is null</exception>
        /// <exception cref="CasperSDKException">Thrown when the operation fails</exception>
        Task<string> GetBalanceAsync(string publicKey);

        /// <summary>
        /// Generates a new key pair for the specified algorithm
        /// </summary>
        /// <param name="algorithm">Key algorithm to use (ED25519 or SECP256K1)</param>
        /// <returns>Generated key pair</returns>
        /// <exception cref="CasperSDKException">Thrown when key generation fails</exception>
        Task<KeyPair> GenerateKeyPairAsync(KeyAlgorithm algorithm = KeyAlgorithm.ED25519);

        /// <summary>
        /// Imports an account from a private key hex string
        /// </summary>
        /// <param name="privateKeyHex">Private key in hexadecimal format</param>
        /// <param name="algorithm">Key algorithm used</param>
        /// <returns>Imported key pair</returns>
        /// <exception cref="ArgumentNullException">Thrown when privateKeyHex is null</exception>
        /// <exception cref="CasperSDKException">Thrown when import fails</exception>
        Task<KeyPair> ImportAccountAsync(string privateKeyHex, KeyAlgorithm algorithm = KeyAlgorithm.ED25519);
    }
}
