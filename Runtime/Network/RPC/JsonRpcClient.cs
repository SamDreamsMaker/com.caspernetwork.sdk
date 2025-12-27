using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using CasperSDK.Core.Configuration;
using Newtonsoft.Json;

namespace CasperSDK.Network.RPC
{
    /// <summary>
    /// JSON-RPC 2.0 request model
    /// </summary>
    [Serializable]
    internal class JsonRpcRequest
    {
        public string jsonrpc = "2.0";
        public int id;
        public string method;
        public object @params;
    }

    /// <summary>
    /// JSON-RPC 2.0 response model
    /// </summary>
    [Serializable]
    internal class JsonRpcResponse<T>
    {
        public string jsonrpc;
        public int id;
        public T result;
        public JsonRpcError error;
    }

    /// <summary>
    /// JSON-RPC error model
    /// </summary>
    [Serializable]
    internal class JsonRpcError
    {
        public int code;
        public string message;
        public object data;
    }

    /// <summary>
    /// JSON-RPC client for Casper Network using UnityWebRequest.
    /// This client is designed for runtime use in Unity (Play Mode).
    /// 
    /// For unit/integration tests, use HttpTestClient instead.
    /// </summary>
    public class JsonRpcClient
    {
        private readonly string _endpoint;
        private readonly int _timeoutSeconds;
        private readonly int _maxRetries;
        private readonly bool _enableLogging;
        private int _requestIdCounter = 1;

        public JsonRpcClient(string endpoint, NetworkConfig config)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            _endpoint = endpoint;
            _timeoutSeconds = config?.RequestTimeoutSeconds ?? 30;
            _maxRetries = config?.MaxRetryAttempts ?? 3;
            _enableLogging = config?.EnableLogging ?? false;
        }

        public async Task<TResult> SendRequestAsync<TResult>(string method, object parameters = null)
        {
            if (string.IsNullOrWhiteSpace(method))
            {
                throw new ArgumentNullException(nameof(method));
            }

            var request = new JsonRpcRequest
            {
                id = _requestIdCounter++,
                method = method,
                @params = parameters ?? new { }
            };

            Exception lastException = null;
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    return await SendRequestInternalAsync<TResult>(request, attempt);
                }
                catch (NetworkException ex)
                {
                    lastException = ex;
                    
                    if (attempt < _maxRetries)
                    {
                        int delayMs = (int)Math.Pow(2, attempt) * 1000;
                        
                        if (_enableLogging)
                        {
                            Debug.LogWarning($"[CasperSDK] Retry {attempt + 1}/{_maxRetries}: {ex.Message}");
                        }

                        await Task.Delay(delayMs);
                    }
                }
                catch (RpcException)
                {
                    throw;
                }
            }

            throw new NetworkException($"Request failed after {_maxRetries + 1} attempts", lastException);
        }

        private async Task<TResult> SendRequestInternalAsync<TResult>(JsonRpcRequest request, int attemptNumber)
        {
            string jsonRequest = JsonConvert.SerializeObject(request, Formatting.Indented);
            
            if (_enableLogging && attemptNumber == 0)
            {
                Debug.Log($"[CasperSDK] RPC: {request.method} -> {_endpoint}");
                // Log full request for debugging
                if (request.method == "account_put_deploy")
                {
                    Debug.Log($"[CasperSDK] Full request payload:\n{jsonRequest}");
                }
            }

            using (var webRequest = new UnityWebRequest(_endpoint, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequest);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = _timeoutSeconds;

                // Send request and wait (must stay on main thread for Unity)
                var operation = webRequest.SendWebRequest();
                
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (_enableLogging)
                {
                    Debug.Log($"[CasperSDK] Response: {webRequest.result}");
                }

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    throw new NetworkException($"Network error: {webRequest.error}");
                }

                string jsonResponse = webRequest.downloadHandler.text;

                if (_enableLogging)
                {
                    var preview = jsonResponse.Length > 150 ? jsonResponse.Substring(0, 150) + "..." : jsonResponse;
                    Debug.Log($"[CasperSDK] Data: {preview}");
                }

                try
                {
                    var response = JsonConvert.DeserializeObject<JsonRpcResponse<TResult>>(jsonResponse);

                    if (response.error != null)
                    {
                        throw new RpcException($"RPC error: {response.error.message}", response.error.code);
                    }

                    return response.result;
                }
                catch (Exception ex) when (!(ex is RpcException))
                {
                    throw new NetworkException($"Parse error: {ex.Message}", ex);
                }
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                await SendRequestAsync<object>("info_get_status", null);
                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    Debug.LogWarning($"[CasperSDK] Connection test failed: {ex.Message}");
                }
                return false;
            }
        }
    }
}
