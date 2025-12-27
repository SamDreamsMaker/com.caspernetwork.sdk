using System;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using CasperSDK.Models;
using CasperSDK.Models.RPC;
using CasperSDK.Unity;

namespace CasperSDK.Services.Account
{
    /// <summary>
    /// Service for account management operations.
    /// Implements Repository pattern for account data access.
    /// </summary>
    public class AccountService : IAccountService
    {
        private readonly INetworkClient _networkClient;
        private readonly NetworkConfig _config;
        private readonly bool _enableLogging;

        /// <summary>
        /// Initializes a new instance of AccountService
        /// </summary>
        public AccountService(INetworkClient networkClient, NetworkConfig config)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _enableLogging = config.EnableLogging;
        }

        /// <inheritdoc/>
        public async Task<Models.Account> GetAccountAsync(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("Public key cannot be null or empty", nameof(publicKey));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting account info for: {publicKey}");
                }

                var param = new AccountInfoParams
                {
                    public_key = publicKey,
                    block_identifier = null
                };

                var result = await _networkClient.SendRequestAsync<AccountInfoRpcResponse>("state_get_account_info", param);

                if (result?.account == null)
                {
                    return null;
                }

                return new Models.Account
                {
                    AccountHash = result.account.account_hash,
                    MainPurse = result.account.main_purse,
                    PublicKey = publicKey
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get account info: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetBalanceAsync(string publicKey)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("Public key cannot be null or empty", nameof(publicKey));
            }

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Getting balance for: {publicKey}");
                }

                // Step 1: Get latest block hash for state root
                var statusResult = await _networkClient.SendRequestAsync<StatusResponse>("info_get_status", null);
                var blockHash = statusResult?.last_added_block_info?.hash;

                if (string.IsNullOrEmpty(blockHash))
                {
                    throw new Exception("Could not get latest block hash");
                }

                // Step 2: Get account info to find main purse
                var accountParam = new AccountInfoParams
                {
                    public_key = publicKey,
                    block_identifier = new AccountBlockIdentifier { Hash = blockHash }
                };

                var accountResult = await _networkClient.SendRequestAsync<AccountInfoRpcResponse>("state_get_account_info", accountParam);

                if (accountResult?.account == null)
                {
                    throw new Exception($"Account not found for public key: {publicKey}");
                }

                var mainPurse = accountResult.account.main_purse;

                if (string.IsNullOrEmpty(mainPurse))
                {
                    throw new Exception("Main purse not found for account");
                }

                // Step 3: Get balance using main purse
                var stateRootHash = statusResult.last_added_block_info?.state_root_hash;
                
                var balanceParam = new BalanceParams
                {
                    state_root_hash = stateRootHash,
                    purse_uref = mainPurse
                };

                var balanceResult = await _networkClient.SendRequestAsync<BalanceRpcResponse>("state_get_balance", balanceParam);

                return balanceResult?.balance_value ?? "0";
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to get balance: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<KeyPair> GenerateKeyPairAsync(KeyAlgorithm algorithm = KeyAlgorithm.ED25519)
        {
            await Task.CompletedTask; // Simulate async

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Generating {algorithm} key pair using BouncyCastle");
                }

                // Use CasperKeyGenerator with real BouncyCastle cryptography
                var keyPair = algorithm == KeyAlgorithm.ED25519
                    ? Utilities.Cryptography.CasperKeyGenerator.GenerateED25519()
                    : Utilities.Cryptography.CasperKeyGenerator.GenerateSECP256K1();

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Key pair generated: {keyPair.PublicKeyHex.Substring(0, 20)}...");
                }

                return keyPair;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to generate key pair: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<KeyPair> ImportAccountAsync(string secretKeyHex, KeyAlgorithm algorithm = KeyAlgorithm.ED25519)
        {
            if (string.IsNullOrWhiteSpace(secretKeyHex))
            {
                throw new ArgumentException("Secret key cannot be null or empty", nameof(secretKeyHex));
            }

            await Task.CompletedTask; // Simulate async

            try
            {
                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Importing {algorithm} account using BouncyCastle");
                }

                // Use CasperKeyGenerator with real BouncyCastle cryptography
                var keyPair = algorithm == KeyAlgorithm.ED25519
                    ? Utilities.Cryptography.CasperKeyGenerator.ImportED25519(secretKeyHex)
                    : Utilities.Cryptography.CasperKeyGenerator.ImportSECP256K1(secretKeyHex);

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Account imported: {keyPair.PublicKeyHex.Substring(0, 20)}...");
                }

                return keyPair;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Failed to import account: {ex.Message}");
                throw;
            }
        }
    }
}

