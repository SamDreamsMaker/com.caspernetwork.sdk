using System;
using System.Collections.Generic;
using UnityEngine;

namespace CasperSDK.Unity
{
    /// <summary>
    /// Dispatcher to execute callbacks on Unity's main thread.
    /// Implements Singleton pattern to ensure there's only one dispatcher.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to find existing instance
                    _instance = FindFirstObjectByType<UnityMainThreadDispatcher>();

                    if (_instance == null)
                    {
                        // Create new GameObject with dispatcher
                        var go = new GameObject("CasperSDK_MainThreadDispatcher");
                        _instance = go.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Enqueues an action to be executed on the main thread
        /// </summary>
        /// <param name="action">Action to execute</param>
        public void Enqueue(Action action)
        {
            if (action == null)
            {
                Debug.LogWarning("Attempted to enqueue null action");
                return;
            }

            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }

        /// <summary>
        /// Enqueues an action and provides a callback when completed
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="action">Action to execute that returns a value</param>
        /// <param name="callback">Callback to invoke with the result</param>
        public void Enqueue<T>(Func<T> action, Action<T> callback)
        {
            Enqueue(() =>
            {
                try
                {
                    T result = action();
                    callback?.Invoke(result);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing enqueued action: {ex.Message}");
                }
            });
        }

        private void Update()
        {
            // Execute all queued actions on the main thread
            lock (_lock)
            {
                while (_executionQueue.Count > 0)
                {
                    Action action = _executionQueue.Dequeue();
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error executing action on main thread: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
