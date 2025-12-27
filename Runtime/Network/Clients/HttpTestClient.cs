using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CasperSDK.Core.Configuration;
using CasperSDK.Core.Interfaces;
using Newtonsoft.Json;
using UnityEngine;

namespace CasperSDK.Network.Clients
{
    /// <summary>
    /// Test-friendly network client using HttpClient instead of UnityWebRequest.
    /// This client works properly with async/await in Unity Test Runner
    /// because HttpClient doesn't have Unity's main thread restriction.
    /// 
    /// Use this client for integration tests.
    /// Use MainnetClient/TestnetClient for production runtime code.
    /// </summary>
    public class HttpTestClient : INetworkClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly bool _enableLogging;
        private int _requestIdCounter = 1;

        public string Endpoint => _endpoint;
        public NetworkType NetworkType { get; }

        public HttpTestClient(string endpoint, NetworkType networkType, bool enableLogging = false)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            NetworkType = networkType;
            _enableLogging = enableLogging;

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public HttpTestClient(NetworkConfig config) : this(
            config?.RpcUrl ?? "https://node.testnet.casper.network/rpc",
            config?.NetworkType ?? NetworkType.Testnet,
            config?.EnableLogging ?? false)
        {
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
                    Debug.LogWarning($"[HttpTestClient] Connection failed: {ex.Message}");
                }
                return false;
            }
        }

        public async Task<TResult> SendRequestAsync<TResult>(string method, object parameters)
        {
            // Build JSON-RPC request
            var requestObj = new
            {
                jsonrpc = "2.0",
                id = _requestIdCounter++,
                method = method,
                @params = parameters ?? new object()
            };

            string jsonRequest = JsonConvert.SerializeObject(requestObj);

            if (_enableLogging)
            {
                Debug.Log($"[HttpTestClient] RPC: {method}");
            }

            try
            {
                // Create request with proper Content-Type (no charset that Casper might reject)
                var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
                request.Content = new StringContent(jsonRequest, Encoding.UTF8);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await _httpClient.SendAsync(request);
                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (_enableLogging)
                {
                    var preview = jsonResponse.Length > 150 ? jsonResponse.Substring(0, 150) + "..." : jsonResponse;
                    Debug.Log($"[HttpTestClient] Response ({response.StatusCode}): {preview}");
                }

                if (!response.IsSuccessStatusCode)
                {
                    throw new NetworkException($"HTTP {(int)response.StatusCode}: {jsonResponse}");
                }

                var rpcResponse = JsonConvert.DeserializeObject<JsonRpcResponse<TResult>>(jsonResponse);

                if (rpcResponse?.error != null)
                {
                    throw new RpcException($"RPC error: {rpcResponse.error.message}", rpcResponse.error.code);
                }

                return rpcResponse.result;
            }
            catch (HttpRequestException ex)
            {
                throw new NetworkException($"HTTP error: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new NetworkException("Request timed out", ex);
            }
            catch (NetworkException)
            {
                throw;
            }
            catch (RpcException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new NetworkException($"Unexpected: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        #region Internal Response Models

        [Serializable]
        private class JsonRpcResponse<T>
        {
            public string jsonrpc;
            public int id;
            public T result;
            public JsonRpcError error;
        }

        [Serializable]
        private class JsonRpcError
        {
            public int code;
            public string message;
        }

        #endregion
    }
}
