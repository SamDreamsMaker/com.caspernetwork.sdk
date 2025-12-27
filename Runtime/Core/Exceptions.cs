using System;

namespace CasperSDK
{
    /// <summary>
    /// Base exception for Casper SDK operations
    /// </summary>
    public class CasperSDKException : Exception
    {
        /// <summary>
        /// Error code associated with this exception
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Initializes a new instance of CasperSDKException
        /// </summary>
        /// <param name="message">Error message</param>
        public CasperSDKException(string message) : base(message)
        {
            ErrorCode = "SDK_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of CasperSDKException with error code
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorCode">Error code</param>
        public CasperSDKException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <summary>
        /// Initializes a new instance of CasperSDKException with inner exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="innerException">Inner exception</param>
        public CasperSDKException(string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = "SDK_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of CasperSDKException with error code and inner exception
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="errorCode">Error code</param>
        /// <param name="innerException">Inner exception</param>
        public CasperSDKException(string message, string errorCode, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown for network-related errors
    /// </summary>
    public class NetworkException : CasperSDKException
    {
        public NetworkException(string message) : base(message, "NETWORK_ERROR") { }
        public NetworkException(string message, Exception innerException) 
            : base(message, "NETWORK_ERROR", innerException) { }
    }

    /// <summary>
    /// Exception thrown for RPC-related errors
    /// </summary>
    public class RpcException : CasperSDKException
    {
        /// <summary>
        /// RPC error code from the server
        /// </summary>
        public int RpcErrorCode { get; }

        public RpcException(string message, int rpcErrorCode) 
            : base(message, $"RPC_ERROR_{rpcErrorCode}")
        {
            RpcErrorCode = rpcErrorCode;
        }

        public RpcException(string message, int rpcErrorCode, Exception innerException) 
            : base(message, $"RPC_ERROR_{rpcErrorCode}", innerException)
        {
            RpcErrorCode = rpcErrorCode;
        }
    }

    /// <summary>
    /// Exception thrown for validation errors
    /// </summary>
    public class ValidationException : CasperSDKException
    {
        public ValidationException(string message) : base(message, "VALIDATION_ERROR") { }
    }

    /// <summary>
    /// Exception thrown for transaction-related errors
    /// </summary>
    public class TransactionException : CasperSDKException
    {
        public TransactionException(string message) : base(message, "TRANSACTION_ERROR") { }
        public TransactionException(string message, Exception innerException) 
            : base(message, "TRANSACTION_ERROR", innerException) { }
    }
}
