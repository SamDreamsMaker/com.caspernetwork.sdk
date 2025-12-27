namespace CasperSDK.Core.Configuration
{
    /// <summary>
    /// Network type enumeration for Casper blockchain
    /// </summary>
    public enum NetworkType
    {
        /// <summary>
        /// Casper Mainnet - Production network
        /// </summary>
        Mainnet,
        
        /// <summary>
        /// Casper Testnet - Test network for development
        /// </summary>
        Testnet,
        
        /// <summary>
        /// Custom network with user-defined RPC endpoint
        /// </summary>
        Custom
    }
}
