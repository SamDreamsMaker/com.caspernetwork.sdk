using UnityEngine;
using CasperSDK.Utilities.Cryptography;
using CasperSDK.Models;

namespace CasperSDK.Examples
{
    /// <summary>
    /// Validation script to verify that generated keys conform to Casper Network specifications.
    /// Attach to a GameObject and run in Play mode to test key generation.
    /// </summary>
    public class KeyValidationTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool _runOnStart = true;
        [SerializeField] private bool _verboseLogging = true;

        private void Start()
        {
            if (_runOnStart)
            {
                RunAllValidationTests();
            }
        }

        [ContextMenu("Run All Validation Tests")]
        public void RunAllValidationTests()
        {
            Debug.Log("==============================================");
            Debug.Log("   CASPER SDK KEY GENERATION VALIDATION");
            Debug.Log("==============================================\n");

            bool allPassed = true;

            allPassed &= TestED25519KeyGeneration();
            allPassed &= TestSECP256K1KeyGeneration();
            allPassed &= TestAccountHashFormat();
            allPassed &= TestKeyConsistency();
            allPassed &= TestPublicKeyFormat();

            Debug.Log("\n==============================================");
            if (allPassed)
            {
                Debug.Log("<color=green>✓ ALL VALIDATION TESTS PASSED</color>");
            }
            else
            {
                Debug.LogError("✗ SOME TESTS FAILED - See details above");
            }
            Debug.Log("==============================================");
        }

        private bool TestED25519KeyGeneration()
        {
            Debug.Log("\n--- Test 1: ED25519 Key Generation ---");
            
            try
            {
                var keyPair = KeyPairGenerator.Generate(KeyAlgorithm.ED25519);
                
                // Validation 1: Public key starts with '01' (ED25519 prefix)
                if (!keyPair.PublicKeyHex.StartsWith("01"))
                {
                    Debug.LogError($"✗ ED25519 public key should start with '01', got: {keyPair.PublicKeyHex.Substring(0, 2)}");
                    return false;
                }
                Debug.Log($"✓ ED25519 prefix: {keyPair.PublicKeyHex.Substring(0, 2)} (expected: 01)");

                // Validation 2: Public key length (01 + 64 hex chars = 66 total for 32-byte key)
                if (keyPair.PublicKeyHex.Length != 66)
                {
                    Debug.LogError($"✗ ED25519 public key length should be 66, got: {keyPair.PublicKeyHex.Length}");
                    return false;
                }
                Debug.Log($"✓ ED25519 public key length: {keyPair.PublicKeyHex.Length} chars (expected: 66)");

                // Validation 3: Private key length (32 bytes = 64 hex chars)
                if (keyPair.PrivateKeyHex.Length != 64)
                {
                    Debug.LogError($"✗ ED25519 private key length should be 64, got: {keyPair.PrivateKeyHex.Length}");
                    return false;
                }
                Debug.Log($"✓ ED25519 private key length: {keyPair.PrivateKeyHex.Length} chars (expected: 64)");

                // Validation 4: Algorithm is correct
                if (keyPair.Algorithm != KeyAlgorithm.ED25519)
                {
                    Debug.LogError($"✗ Algorithm should be ED25519, got: {keyPair.Algorithm}");
                    return false;
                }
                Debug.Log($"✓ Algorithm: {keyPair.Algorithm}");

                if (_verboseLogging)
                {
                    Debug.Log($"  Public Key: {keyPair.PublicKeyHex}");
                    Debug.Log($"  Private Key: {keyPair.PrivateKeyHex.Substring(0, 16)}... (truncated)");
                    Debug.Log($"  Account Hash: {keyPair.AccountHash}");
                }

                Debug.Log("<color=green>✓ ED25519 key generation: PASSED</color>");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ ED25519 test failed with exception: {ex.Message}");
                return false;
            }
        }

        private bool TestSECP256K1KeyGeneration()
        {
            Debug.Log("\n--- Test 2: SECP256K1 Key Generation ---");
            
            try
            {
                var keyPair = KeyPairGenerator.Generate(KeyAlgorithm.SECP256K1);
                
                // Validation 1: Public key starts with '02' (SECP256K1 prefix)
                if (!keyPair.PublicKeyHex.StartsWith("02"))
                {
                    Debug.LogError($"✗ SECP256K1 public key should start with '02', got: {keyPair.PublicKeyHex.Substring(0, 2)}");
                    return false;
                }
                Debug.Log($"✓ SECP256K1 prefix: {keyPair.PublicKeyHex.Substring(0, 2)} (expected: 02)");

                // Validation 2: Public key length (02 + 66 hex chars = 68 total for 33-byte compressed key)
                if (keyPair.PublicKeyHex.Length != 68)
                {
                    Debug.LogError($"✗ SECP256K1 public key length should be 68, got: {keyPair.PublicKeyHex.Length}");
                    return false;
                }
                Debug.Log($"✓ SECP256K1 public key length: {keyPair.PublicKeyHex.Length} chars (expected: 68)");

                // Validation 3: Private key length (32 bytes = 64 hex chars)
                if (keyPair.PrivateKeyHex.Length != 64)
                {
                    Debug.LogError($"✗ SECP256K1 private key length should be 64, got: {keyPair.PrivateKeyHex.Length}");
                    return false;
                }
                Debug.Log($"✓ SECP256K1 private key length: {keyPair.PrivateKeyHex.Length} chars (expected: 64)");

                Debug.Log("<color=green>✓ SECP256K1 key generation: PASSED</color>");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ SECP256K1 test failed with exception: {ex.Message}");
                return false;
            }
        }

        private bool TestAccountHashFormat()
        {
            Debug.Log("\n--- Test 3: Account Hash Format ---");
            
            try
            {
                var keyPair = KeyPairGenerator.Generate(KeyAlgorithm.ED25519);
                
                // Validation 1: Account hash starts with 'account-hash-'
                if (!keyPair.AccountHash.StartsWith("account-hash-"))
                {
                    Debug.LogError($"✗ Account hash should start with 'account-hash-', got: {keyPair.AccountHash.Substring(0, 13)}");
                    return false;
                }
                Debug.Log($"✓ Account hash prefix: account-hash-");

                // Validation 2: Hash part is 64 hex characters (32 bytes)
                var hashPart = keyPair.AccountHash.Substring(13);
                if (hashPart.Length != 64)
                {
                    Debug.LogError($"✗ Account hash hex part should be 64 chars, got: {hashPart.Length}");
                    return false;
                }
                Debug.Log($"✓ Account hash length: {hashPart.Length} hex chars (expected: 64 = 32 bytes)");

                // Validation 3: Hash is valid hex
                try
                {
                    CryptoHelper.HexToBytes(hashPart);
                    Debug.Log($"✓ Account hash is valid hexadecimal");
                }
                catch
                {
                    Debug.LogError($"✗ Account hash contains invalid hex characters");
                    return false;
                }

                // Validation 4: Using validation helper
                if (!CryptoHelper.ValidateAccountHash(keyPair.AccountHash))
                {
                    Debug.LogError($"✗ CryptoHelper.ValidateAccountHash failed");
                    return false;
                }
                Debug.Log($"✓ CryptoHelper.ValidateAccountHash: PASSED");

                if (_verboseLogging)
                {
                    Debug.Log($"  Full Account Hash: {keyPair.AccountHash}");
                }

                Debug.Log("<color=green>✓ Account hash format: PASSED</color>");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ Account hash test failed with exception: {ex.Message}");
                return false;
            }
        }

        private bool TestKeyConsistency()
        {
            Debug.Log("\n--- Test 4: Key Consistency (Same input = Same output) ---");
            
            try
            {
                // Use a fixed private key for testing
                var privateKeyHex = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
                
                var keyPair1 = KeyPairGenerator.Import(privateKeyHex, KeyAlgorithm.ED25519);
                var keyPair2 = KeyPairGenerator.Import(privateKeyHex, KeyAlgorithm.ED25519);

                // Same private key should produce same public key
                if (keyPair1.PublicKeyHex != keyPair2.PublicKeyHex)
                {
                    Debug.LogError($"✗ Same private key produced different public keys!");
                    Debug.LogError($"  Key 1: {keyPair1.PublicKeyHex}");
                    Debug.LogError($"  Key 2: {keyPair2.PublicKeyHex}");
                    return false;
                }
                Debug.Log($"✓ Same private key produces same public key");

                // Same public key should produce same account hash
                if (keyPair1.AccountHash != keyPair2.AccountHash)
                {
                    Debug.LogError($"✗ Same public key produced different account hashes!");
                    return false;
                }
                Debug.Log($"✓ Same public key produces same account hash");

                Debug.Log("<color=green>✓ Key consistency: PASSED</color>");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ Key consistency test failed with exception: {ex.Message}");
                return false;
            }
        }

        private bool TestPublicKeyFormat()
        {
            Debug.Log("\n--- Test 5: Public Key Validation Helper ---");
            
            try
            {
                // Generate valid keys
                var ed25519Key = KeyPairGenerator.Generate(KeyAlgorithm.ED25519);
                var secp256k1Key = KeyPairGenerator.Generate(KeyAlgorithm.SECP256K1);

                // Test valid ED25519
                if (!CryptoHelper.ValidatePublicKey(ed25519Key.PublicKeyHex))
                {
                    Debug.LogError($"✗ Valid ED25519 key failed validation");
                    return false;
                }
                Debug.Log($"✓ Valid ED25519 key passes validation");

                // Test valid SECP256K1
                if (!CryptoHelper.ValidatePublicKey(secp256k1Key.PublicKeyHex))
                {
                    Debug.LogError($"✗ Valid SECP256K1 key failed validation");
                    return false;
                }
                Debug.Log($"✓ Valid SECP256K1 key passes validation");

                // Test invalid keys
                if (CryptoHelper.ValidatePublicKey("invalid"))
                {
                    Debug.LogError($"✗ Invalid key should fail validation");
                    return false;
                }
                Debug.Log($"✓ Invalid key correctly rejected");

                if (CryptoHelper.ValidatePublicKey("03abcd1234"))
                {
                    Debug.LogError($"✗ Key with invalid prefix should fail");
                    return false;
                }
                Debug.Log($"✓ Key with invalid prefix correctly rejected");

                Debug.Log("<color=green>✓ Public key validation: PASSED</color>");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"✗ Public key validation test failed: {ex.Message}");
                return false;
            }
        }
    }
}
