using System.Threading.Tasks;
using CasperSDK.Models.RPC;

namespace CasperSDK.Core.Interfaces
{
    /// <summary>
    /// Interface for deploy (transaction) operations.
    /// </summary>
    public interface IDeployService
    {
        /// <summary>
        /// Get a deploy by its hash.
        /// </summary>
        Task<DeployInfo> GetDeployAsync(string deployHash);

        /// <summary>
        /// Get deploy execution status.
        /// </summary>
        Task<DeployExecutionStatus> GetDeployStatusAsync(string deployHash);

        /// <summary>
        /// Submit a signed deploy to the network.
        /// </summary>
        Task<string> SubmitDeployAsync(object signedDeploy);
    }
}
