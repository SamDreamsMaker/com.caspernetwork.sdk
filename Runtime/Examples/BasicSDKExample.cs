using System;
using UnityEngine;
using CasperSDK.Core;
using CasperSDK.Core.Configuration;

namespace CasperSDK.Examples
{
    /// <summary>
    /// Example demonstrating basic SDK usage.
    /// Attach this to a GameObject in your scene to test the SDK.
    /// </summary>
    public class BasicSDKExample : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private NetworkConfig _networkConfig;

        [Header("Test Parameters")]
        [SerializeField] private string _testPublicKey = "01abc123...";

        private async void Start()
        {
            try
            {
                Debug.Log("=== Casper SDK Example ===");

                // Initialize the SDK
                if (_networkConfig == null)
                {
                    Debug.LogWarning("No network config assigned, using defaults");
                    CasperSDKManager.Instance.InitializeWithDefaults();
                }
                else
                {
                    CasperSDKManager.Instance.Initialize(_networkConfig);
                }

                Debug.Log("SDK initialized successfully!");

                // Example 1: Generate a new key pair
                await GenerateKeyPairExample();

                // Example 2: Get account balance
                // await GetBalanceExample();

                // Example 3: Build a transaction
                BuildTransactionExample();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Example failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async System.Threading.Tasks.Task GenerateKeyPairExample()
        {
            Debug.Log("\n--- Generate Key Pair Example ---");

            var accountService = CasperSDKManager.Instance.AccountService;
            var keyPair = await accountService.GenerateKeyPairAsync(Models.KeyAlgorithm.ED25519);

            Debug.Log($"Generated Key Pair:");
            Debug.Log($"  Public Key: {keyPair.PublicKeyHex}");
            Debug.Log($"  Account Hash: {keyPair.AccountHash}");
            Debug.Log($"  Algorithm: {keyPair.Algorithm}");
        }

        private async System.Threading.Tasks.Task GetBalanceExample()
        {
            Debug.Log("\n--- Get Balance Example ---");

            if (string.IsNullOrEmpty(_testPublicKey))
            {
                Debug.LogWarning("Test public key not set, skipping balance example");
                return;
            }

            var accountService = CasperSDKManager.Instance.AccountService;
            var balance = await accountService.GetBalanceAsync(_testPublicKey);

            Debug.Log($"Balance for {_testPublicKey}: {balance} motes");
        }

        private void BuildTransactionExample()
        {
            Debug.Log("\n--- Build Transaction Example ---");

            var transactionService = CasperSDKManager.Instance.TransactionService;

            // Build a transfer transaction
            var transaction = transactionService.CreateTransactionBuilder()
                .SetFrom("01abc123sender...")
                .SetTarget("01def456recipient...")
                .SetAmount("2500000000") // 2.5 CSPR in motes
                .SetGasPrice(1)
                .SetTTL(3600000) // 1 hour
                .Build();

            Debug.Log($"Transaction built:");
            Debug.Log($"  From: {transaction.From}");
            Debug.Log($"  To: {transaction.Target}");
            Debug.Log($"  Amount: {transaction.Amount} motes");
            Debug.Log($"  Gas Price: {transaction.GasPrice}");
            Debug.Log($"  TTL: {transaction.TTL}ms");

            // Note: To actually submit, you would need to sign it first
            // var txHash = await transactionService.SubmitTransactionAsync(transaction);
        }

        private void OnDestroy()
        {
            // Clean up SDK resources
            if (CasperSDKManager.Instance != null)
            {
                CasperSDKManager.Instance.Shutdown();
            }
        }
    }
}
