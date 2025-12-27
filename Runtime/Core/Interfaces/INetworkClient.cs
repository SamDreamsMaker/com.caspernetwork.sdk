using System.Threading.Tasks;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for network client operations.
    /// Provides abstraction for different network implementations.
    /// </summary>
    public interface INetworkClient
    {
        /// <summary>
        /// Gets the RPC endpoint URL
        /// </summary>
        string Endpoint { get; }

        /// <summary>
        /// Gets the network type
        /// </summary>
        Configuration.NetworkType NetworkType { get; }

        /// <summary>
        /// Sends a JSON-RPC request to the network
        /// </summary>
        /// <typeparam name="TResult">Expected result type</typeparam>
        /// <param name="method">RPC method name</param>
        /// <param name="parameters">Request parameters</param>
        /// <returns>RPC response</returns>
        /// <exception cref="CasperSDKException">Thrown when the request fails</exception>
        Task<TResult> SendRequestAsync<TResult>(string method, object parameters = null);

        /// <summary>
        /// Tests the connection to the network
        /// </summary>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync();
    }
}
