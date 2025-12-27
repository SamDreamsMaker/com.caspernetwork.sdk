using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CasperSDK.Core.Configuration;

namespace CasperSDK.Services.Events
{
    /// <summary>
    /// Server-Sent Events (SSE) client for real-time Casper Network events.
    /// Supports deploy status, block finalization, and other network events.
    /// </summary>
    public class EventStreamingService : IDisposable
    {
        private HttpClient _httpClient;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected;
        private readonly string _baseUrl;
        private readonly bool _enableLogging;

        // Events
        public event Action<DeployEvent> OnDeployAccepted;
        public event Action<DeployEvent> OnDeployProcessed;
        public event Action<DeployEvent> OnDeployExpired;
        public event Action<BlockEvent> OnBlockAdded;
        public event Action<string> OnError;
        public event Action OnConnected;
        public event Action OnDisconnected;

        /// <summary>
        /// Creates a new EventStreamingService
        /// </summary>
        /// <param name="config">Network configuration</param>
        public EventStreamingService(NetworkConfig config)
        {
            _enableLogging = config?.EnableLogging ?? false;
            
            // SSE endpoint is typically on port 9999 for Casper nodes
            var baseRpcUrl = config?.RpcUrl ?? SDKSettings.DefaultTestnetRpcUrl;
            var uri = new Uri(baseRpcUrl);
            _baseUrl = $"http://{uri.Host}:{SDKSettings.DefaultSsePort}/events";

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromHours(24); // Long-lived connection
        }

        /// <summary>
        /// Whether the service is currently connected
        /// </summary>
        public bool IsConnected => _isConnected;

        #region Connection

        /// <summary>
        /// Starts listening to all events
        /// </summary>
        public async Task StartAsync()
        {
            await StartAsync(EventChannel.Main);
        }

        /// <summary>
        /// Starts listening to a specific event channel
        /// </summary>
        public async Task StartAsync(EventChannel channel)
        {
            if (_isConnected)
            {
                Debug.LogWarning("[CasperSDK] Event streaming already connected");
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var channelPath = channel switch
            {
                EventChannel.Main => "/main",
                EventChannel.Deploys => "/deploys",
                EventChannel.Sigs => "/sigs",
                _ => "/main"
            };

            var url = _baseUrl + channelPath;

            if (_enableLogging)
                Debug.Log($"[CasperSDK] Connecting to event stream: {url}");

            try
            {
                await ListenToEventStream(url, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
                Debug.LogError($"[CasperSDK] Event stream error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops listening to events
        /// </summary>
        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _isConnected = false;
            OnDisconnected?.Invoke();

            if (_enableLogging)
                Debug.Log("[CasperSDK] Event stream disconnected");
        }

        #endregion

        #region Event Listening

        private async Task ListenToEventStream(string url, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Accept", "text/event-stream");

            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            _isConnected = true;
            OnConnected?.Invoke();

            if (_enableLogging)
                Debug.Log("[CasperSDK] Event stream connected");

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string eventType = null;
            string eventData = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();

                if (line == null)
                    break;

                if (line.StartsWith("event:"))
                {
                    eventType = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    eventData = line.Substring(5).Trim();
                }
                else if (string.IsNullOrEmpty(line) && !string.IsNullOrEmpty(eventData))
                {
                    // End of event, process it
                    ProcessEvent(eventType, eventData);
                    eventType = null;
                    eventData = null;
                }
            }

            _isConnected = false;
            OnDisconnected?.Invoke();
        }

        private void ProcessEvent(string eventType, string eventData)
        {
            try
            {
                switch (eventType)
                {
                    case "DeployAccepted":
                        var deployAccepted = ParseDeployEvent(eventData);
                        if (deployAccepted != null)
                        {
                            if (_enableLogging)
                                Debug.Log($"[CasperSDK] Deploy accepted: {deployAccepted.DeployHash?.Substring(0, 16)}...");
                            OnDeployAccepted?.Invoke(deployAccepted);
                        }
                        break;

                    case "DeployProcessed":
                        var deployProcessed = ParseDeployEvent(eventData);
                        if (deployProcessed != null)
                        {
                            if (_enableLogging)
                                Debug.Log($"[CasperSDK] Deploy processed: {deployProcessed.DeployHash?.Substring(0, 16)}...");
                            OnDeployProcessed?.Invoke(deployProcessed);
                        }
                        break;

                    case "DeployExpired":
                        var deployExpired = ParseDeployEvent(eventData);
                        if (deployExpired != null)
                        {
                            if (_enableLogging)
                                Debug.Log($"[CasperSDK] Deploy expired: {deployExpired.DeployHash?.Substring(0, 16)}...");
                            OnDeployExpired?.Invoke(deployExpired);
                        }
                        break;

                    case "BlockAdded":
                        var blockAdded = ParseBlockEvent(eventData);
                        if (blockAdded != null)
                        {
                            if (_enableLogging)
                                Debug.Log($"[CasperSDK] Block added: {blockAdded.Height}");
                            OnBlockAdded?.Invoke(blockAdded);
                        }
                        break;

                    case "FinalitySignature":
                        // Finality signatures indicate block finalization
                        break;

                    default:
                        if (_enableLogging)
                            Debug.Log($"[CasperSDK] Unknown event: {eventType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CasperSDK] Error processing event: {ex.Message}");
            }
        }

        private DeployEvent ParseDeployEvent(string json)
        {
            try
            {
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<DeployEventData>(json);
                return new DeployEvent
                {
                    DeployHash = parsed?.DeployAccepted?.deploy_hash ?? parsed?.DeployProcessed?.deploy_hash,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        private BlockEvent ParseBlockEvent(string json)
        {
            try
            {
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<BlockEventData>(json);
                return new BlockEvent
                {
                    BlockHash = parsed?.BlockAdded?.block_hash,
                    Height = parsed?.BlockAdded?.block?.header?.height ?? 0,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Deploy Monitoring

        /// <summary>
        /// Monitors a specific deploy until it's processed or times out
        /// </summary>
        /// <param name="deployHash">Deploy hash to monitor</param>
        /// <param name="timeoutSeconds">Timeout in seconds (default 5 minutes)</param>
        /// <returns>Deploy event when processed, or null on timeout</returns>
        public async Task<DeployEvent> WaitForDeployAsync(string deployHash, int timeoutSeconds = 300)
        {
            var tcs = new TaskCompletionSource<DeployEvent>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

            void OnProcessed(DeployEvent evt)
            {
                if (evt.DeployHash.Equals(deployHash, StringComparison.OrdinalIgnoreCase))
                {
                    tcs.TrySetResult(evt);
                }
            }

            cts.Token.Register(() => tcs.TrySetResult(null));
            OnDeployProcessed += OnProcessed;

            try
            {
                if (!_isConnected)
                    await StartAsync(EventChannel.Deploys);

                return await tcs.Task;
            }
            finally
            {
                OnDeployProcessed -= OnProcessed;
                cts.Dispose();
            }
        }

        #endregion

        public void Dispose()
        {
            Stop();
            _httpClient?.Dispose();
        }
    }

    #region Enums

    /// <summary>
    /// Event channel types
    /// </summary>
    public enum EventChannel
    {
        /// <summary>Main events (blocks, deploys)</summary>
        Main,
        /// <summary>Deploy events only</summary>
        Deploys,
        /// <summary>Signature events</summary>
        Sigs
    }

    #endregion

    #region Event Models

    [Serializable]
    public class DeployEvent
    {
        public string DeployHash { get; set; }
        public string Account { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string Cost { get; set; }
        public DateTime Timestamp { get; set; }
    }

    [Serializable]
    public class BlockEvent
    {
        public string BlockHash { get; set; }
        public long Height { get; set; }
        public string Proposer { get; set; }
        public int DeployCount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Internal parsing models
    [Serializable]
    internal class DeployEventData
    {
        public DeployAcceptedData DeployAccepted { get; set; }
        public DeployProcessedData DeployProcessed { get; set; }
    }

    [Serializable]
    internal class DeployAcceptedData
    {
        public string deploy_hash { get; set; }
    }

    [Serializable]
    internal class DeployProcessedData
    {
        public string deploy_hash { get; set; }
        public string block_hash { get; set; }
    }

    [Serializable]
    internal class BlockEventData
    {
        public BlockAddedData BlockAdded { get; set; }
    }

    [Serializable]
    internal class BlockAddedData
    {
        public string block_hash { get; set; }
        public BlockData block { get; set; }
    }

    [Serializable]
    internal class BlockData
    {
        public BlockHeaderData header { get; set; }
    }

    [Serializable]
    internal class BlockHeaderData
    {
        public long height { get; set; }
    }

    #endregion
}
