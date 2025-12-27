# Casper Network Unity SDK

A comprehensive Unity SDK for integrating [Casper Network](https://casper.network/) blockchain functionality into Unity games and applications.

## 🚀 Features

- **Clean Architecture**: Built with SOLID principles and proven design patterns
- **Easy Integration**: Simple, Unity-friendly API
- **Async/Await Support**: Full async support with Unity main thread dispatching
- **Account Management**: Key generation, import, and balance queries
- **Transaction System**: Fluent transaction builder with validation
- **Network Flexibility**: Support for Mainnet, Testnet, and custom networks
- **Error Handling**: Comprehensive exception hierarchy
- **Type Safe**: Strongly typed models and interfaces

## 📋 Requirements

- Unity 2022.3 LTS or higher
- .NET Standard 2.1 or higher
- Windows, macOS, Linux, WebGL, iOS, Android support

## 🎯 Quick Start

### 1. Installation

1. Clone or download this repository
2. Copy the `Assets/CasperSDK` folder to your Unity project's `Assets` folder
3. Import the required DLLs (Casper.Network.SDK) into `Assets/CasperSDK/Plugins/`

### 2. Configuration

Create a network configuration:

1. Right-click in Project window
2. Create > CasperSDK > Network Configuration
3. Configure your network settings (Mainnet/Testnet/Custom)

### 3. Initialize the SDK

```csharp
using CasperSDK.Core;
using CasperSDK.Core.Configuration;

public class MyGame : MonoBehaviour
{
    [SerializeField] private NetworkConfig networkConfig;

    private void Start()
    {
        // Initialize the SDK
        CasperSDKManager.Instance.Initialize(networkConfig);
    }
}
```

### 4. Use the SDK

```csharp
// Generate a new key pair
var accountService = CasperSDKManager.Instance.AccountService;
var keyPair = await accountService.GenerateKeyPairAsync();

// Get account balance
var balance = await accountService.GetBalanceAsync(publicKey);

// Build and submit a transaction
var transactionService = CasperSDKManager.Instance.TransactionService;
var transaction = transactionService.CreateTransactionBuilder()
    .SetFrom(senderPublicKey)
    .SetTarget(recipientPublicKey)
    .SetAmount("2500000000") // 2.5 CSPR in motes
    .Build();

var txHash = await transactionService.SubmitTransactionAsync(transaction);
```

## 🏗️ Architecture

The SDK follows clean architecture principles with the following design patterns:

### Design Patterns Used

- **Singleton Pattern**: `CasperSDKManager` - Single entry point for the SDK
- **Factory Pattern**: `NetworkClientFactory` - Creates appropriate network clients
- **Builder Pattern**: `TransactionBuilder` - Fluent API for transaction construction
- **Repository Pattern**: `AccountService` - Abstraction for account data access
- **Strategy Pattern**: Different network implementations (Mainnet/Testnet)
- **Observer Pattern**: Event system for blockchain events

### Project Structure

```
CasperSDK/
├── Runtime/
│   ├── Core/
│   │   ├── Configuration/       # Network and SDK settings
│   │   ├── Interfaces/          # Service interfaces
│   │   ├── CasperSDKManager.cs  # Main SDK entry point
│   │   └── Exceptions.cs        # Custom exceptions
│   ├── Network/
│   │   ├── RPC/                 # JSON-RPC client
│   │   └── Clients/             # Network client implementations
│   ├── Services/
│   │   ├── Account/             # Account management
│   │   └── Transaction/         # Transaction services
│   ├── Models/                  # Data models
│   ├── Unity/                   # Unity-specific integrations
│   └── Examples/                # Example scripts
├── Editor/                      # Editor tools and utilities
└── Tests/                       # Unit and integration tests
```

## 📚 API Documentation

### CasperSDKManager

Main entry point for the SDK. Implements Singleton pattern.

```csharp
// Initialize
CasperSDKManager.Instance.Initialize(config);

// Access services
var accountService = CasperSDKManager.Instance.AccountService;
var transactionService = CasperSDKManager.Instance.TransactionService;

// Shutdown
CasperSDKManager.Instance.Shutdown();
```

### IAccountService

Manages Casper accounts and keys.

```csharp
Task<Account> GetAccountAsync(string publicKey);
Task<string> GetBalanceAsync(string publicKey);
Task<KeyPair> GenerateKeyPairAsync(KeyAlgorithm algorithm);
Task<KeyPair> ImportAccountAsync(string privateKeyHex, KeyAlgorithm algorithm);
```

### ITransactionService

Handles transaction creation and submission.

```csharp
ITransactionBuilder CreateTransactionBuilder();
Task<string> SubmitTransactionAsync(Transaction transaction);
Task<ExecutionResult> GetTransactionStatusAsync(string transactionHash);
Task<long> EstimateGasAsync(Transaction transaction);
```

### TransactionBuilder

Fluent API for building transactions.

```csharp
var transaction = service.CreateTransactionBuilder()
    .SetFrom(sender)
    .SetTarget(recipient)
    .SetAmount("1000000000")
    .SetGasPrice(1)
    .SetTTL(3600000)
    .Build();
```

## 🎓 Examples

See `Assets/CasperSDK/Runtime/Examples/BasicSDKExample.cs` for a comprehensive example.

## 🧪 Testing

The SDK includes comprehensive unit and integration tests.

### Running Tests

1. Open Unity Test Runner (Window > General > Test Runner)
2. Select "Runtime Tests" or "Editor Tests"
3. Click "Run All" or run individual tests

### Test Categories

- **Unit Tests**: Test individual components with mocked dependencies
- **Integration Tests**: Test against Casper Testnet (requires internet)

## 🔐 Security

⚠️ **IMPORTANT**: Never commit private keys to version control!

- Use Unity's secure storage for private keys in production
- Test with Testnet before deploying to Mainnet
- Always validate user inputs
- Keep dependencies up to date

## 🤝 Contributing

Contributions are welcome! Please:

1. **Fork the source repository** (not this package repo):
   👉 https://github.com/SamDreamsMaker/Casper-Network-Unity-SDK
2. Create a feature branch
3. Follow the existing code style and patterns
4. Add tests for new functionality
5. Submit a pull request to the **source repository**

> ⚠️ **Note**: Do NOT submit PRs to `com.caspernetwork.sdk` - it's auto-generated from the source repo.

## 📄 Code Quality Standards

This SDK follows strict code quality standards:

- ✅ SOLID principles
- ✅ Clean code practices
- ✅ Comprehensive error handling
- ✅ XML documentation for all public APIs
- ✅ Unit tests for all components
- ✅ Consistent naming conventions

## 📝 License

MIT License - see [LICENSE](https://github.com/SamDreamsMaker/Casper-Network-Unity-SDK/blob/main/LICENSE)

## 🔗 Links

- [Casper Network Documentation](https://docs.casper.network/)
- [Casper Network C# SDK](https://github.com/make-software/casper-net-sdk)
- [Unity Documentation](https://docs.unity3d.com/)

## 📧 Support

For issues, questions, or contributions:
- Open an issue on [GitHub](https://github.com/SamDreamsMaker/Casper-Network-Unity-SDK/issues)
- Contact: samdreamsmaker@gmail.com

## 🗺️ Roadmap

- [x] Core infrastructure
- [x] Network layer with JSON-RPC
- [x] Account management
- [x] Transaction builder
- [x] Transaction signing (ED25519 & SECP256K1)
- [x] CSPR transfers
- [x] Key import/export (PEM format)
- [ ] Smart contract deployment
- [ ] Smart contract interaction
- [ ] Event listening (SSE - in progress)
- [ ] WebGL support optimization
- [ ] Sample projects (Wallet ✅, NFT, Token)

---

**Version**: 1.0.0  
**Last Updated**: December 2025  
**Status**: Production Ready

---

**Source Repository**: https://github.com/SamDreamsMaker/Casper-Network-Unity-SDK
