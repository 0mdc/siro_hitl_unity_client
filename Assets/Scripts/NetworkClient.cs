using System;
using UnityEngine;
using System.Collections.Generic;
using NativeWebSocket;
using Newtonsoft.Json;
using UnityEngine.Assertions;
using System.Collections;
using System.Threading.Tasks;
using System.Web;

public class NetworkClient : IUpdatable
{
    const float CLIENT_STATE_SEND_FREQUENCY = 0.1f;

    const bool IS_HTTPS = false;

    public class FlagObject
    {
        private bool flag = false;

        public void SetFlag()
        {
            flag = true;
        }

        public bool IsSet()
        {
            return flag;
        }
    }

    public int defaultServerPort = 8888;

    List<string> _serverURLs = new List<string>();
    private WebSocket mainWebSocket;
    private int currentServerIndex = 0;
    private int messagesReceivedCount = 0;
    private int frameCount = 0;
    private float _delayReconnect = 0.0f;
    private string _disconnectReason = "";
    int _recentConnectionMessageCount = 0;

    private GfxReplayPlayer _player;
    private ConfigLoader _configLoader;
    private CanvasManager _canvasManager;

    Dictionary<string, string> _connectionParams;
    ClientState _clientState = new ClientState();
    IClientStateProducer[] _clientStateProducers;
    CoroutineContainer _coroutines;
    ServerKeyframeIdHandler _serverKeyframeIdHandler;

    // The client will try to connect until a successful session has started.
    // After the first successful connection, the client stops trying to reconnect.
    bool _tryReconnecting = true;

    // Used to handle data that is only meant to be sent during the first transmission.
    private bool _firstTransmission = true;

    JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
    {
        // Omit null values when serializing objects.
        // E.g. XR-specific fields will be omitted when XR is disabled.
        NullValueHandling = NullValueHandling.Ignore,
        // Skip missing JSON fields when deserializing.
        MissingMemberHandling = MissingMemberHandling.Ignore
    };

    public NetworkClient(
        GfxReplayPlayer player,
        ConfigLoader configLoader,
        IClientStateProducer[] clientStateProducers,
        ServerKeyframeIdHandler serverKeyframeIdHandler,
        CanvasManager canvasManager = null)
    {
        _player = player;
        _configLoader = configLoader;
        _clientStateProducers = clientStateProducers;
        _serverKeyframeIdHandler = serverKeyframeIdHandler;
        _coroutines = CoroutineContainer.Create("NetworkClient");
        _canvasManager = canvasManager;
        
        // Read URL query parameters
        _connectionParams = ConnectionParameters.GetConnectionParameters(Application.absoluteURL);
        var serverHostnameOverride = ConnectionParameters.GetServerHostname(_connectionParams);
        var serverPortRange = ConnectionParameters.GetServerPortRange(_connectionParams);
        var assetHostname = ConnectionParameters.GetAssetHostname(_connectionParams);
        var assetPort = ConnectionParameters.GetAssetPort(_connectionParams);
        var assetPath = ConnectionParameters.GetAssetPath(_connectionParams);

        // Set remote asset server location. See 'HabitatAssetServer'.
        // These static variables are interpreted by the Addressables system the first time it is used.
        if (assetHostname != null)
            HabitatAssetServer.Address = assetHostname;
        if (assetPort != null)
            HabitatAssetServer.Port = assetPort.ToString();
        if (assetPath != null)
            HabitatAssetServer.Path = assetPath;
        HabitatAssetServer.Protocol = IS_HTTPS ? "https" : "http";

        if (serverPortRange == null)
        {
            serverPortRange = (defaultServerPort, defaultServerPort);
        }

        string wsProtocol = IS_HTTPS ? "wss" : "ws";
        // Set up server hostnames and port.
        string[] serverLocations = serverHostnameOverride != null ? 
                                        new[]{serverHostnameOverride} : 
                                        _configLoader.AppConfig.serverLocations;
        Assert.IsTrue(serverLocations.Length > 0);
        foreach (string location in serverLocations)
        {
            if (!location.Contains(":"))
            {
                for (int port = serverPortRange.Value.startPort; port <= serverPortRange.Value.endPort; port++)
                {
                    string adjustedLocation = location + ":" + port;
                    _serverURLs.Add($"{wsProtocol}://{adjustedLocation}");
                }
            }
            else
            {
                _serverURLs.Add($"{wsProtocol}://{location}");
            }
        }
        currentServerIndex = UnityEngine.Random.Range(0, _serverURLs.Count);

        // Start networking.
        _coroutines.StartCoroutine(TryConnectToServers());
        _coroutines.StartCoroutine(LogMessageRate());
        _coroutines.StartCoroutine(SendClientState());
    }

    public void Update()
    {
        foreach (var producer in _clientStateProducers)
        {
            producer.Update();
        }

        if (mainWebSocket != null)
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            mainWebSocket.DispatchMessageQueue();
#endif
        }
        frameCount++;
    }

    private IEnumerator TryConnectToServers()
    {
        while (true)
        {
            if (mainWebSocket == null && _tryReconnecting)
            {
                if (_delayReconnect > 0.0f)
                {
                    Debug.Log($"Delaying reconnect for {_delayReconnect}s");
                    int numWaits = (int)_delayReconnect;
                    for (int i = 0; i < numWaits; i++)
                    {
                        int remainingTime = numWaits - i;
                        string retryDesc = _serverURLs.Count == 1 ? "Retrying" : "Trying another server";
                        SetDisconnectStatus($"{_disconnectReason}\n{retryDesc} in {remainingTime}s...");
                        yield return new WaitForSeconds(1.0f);
                    }
                    SetDisconnectStatus("");
                    _delayReconnect = 0.0f;
                    _disconnectReason = "";
                }

                currentServerIndex++;
                if (currentServerIndex >= _serverURLs.Count)
                {
                    currentServerIndex = 0; // Reset to try again from the beginning.
                }

                if (!_tryReconnecting)
                {
                    yield return new WaitForSeconds(1);
                    continue;
                }

                string url = _serverURLs[currentServerIndex];
                Debug.Log("Attempting to connect to " + url);

                var websocket = new WebSocket(url);
                FlagObject doAbort = new FlagObject();
                // This is an async function that will finish later (but
                // still on the main Unity thread).
                var connectTask = ConnectWebSocket(websocket, url, doAbort);

                // Wait for 2s, then check the result
                yield return new WaitForSeconds(2.0f);

                if (mainWebSocket == null && websocket.State == WebSocketState.Connecting)
                {
                    SetDisconnectStatus("Trying to reach server...");
                    // Wait another 4s
                    yield return new WaitForSeconds(4);
                }

                if (mainWebSocket == null)
                {
                    if (websocket.State == WebSocketState.Connecting)
                    {
                        Debug.LogWarning($"Timeout! Aborting connect to {url}.");
                    }
                    // See OnOpen callback in ConnectWebSocket where we check
                    // this flag and potentially discard the connection.
                    doAbort.SetFlag();

                    if (_disconnectReason == "")
                    {
                        // If there's only one server URL, let's wait 5s (avoid hammering more often than that).
                        // If there's multiple server URLs, let's try them more quickly (wait only 2s).
                        _delayReconnect = _serverURLs.Count == 1 ? 5.0f : 2.0f;
                        _disconnectReason = "Unable to reach server!";
                    }
                }
            }
            else if (mainWebSocket == null && !_tryReconnecting)
            {
                // If disconnected after the session has started, do not reconnect.
                _player.DeleteAllInstancesFromKeyframes();
                SetDisconnectStatus("Disconnected.");
                yield break;
            }
            else
            {
                // If we have an active connection, we simply wait here.
                // Adjust this to check more or less frequently as desired.
                yield return new WaitForSeconds(1);
            }
        }
    }

    private async Task ConnectWebSocket(WebSocket websocket, string url, FlagObject doAbort)
    {
        websocket.OnOpen += () =>
        {
            if (doAbort.IsSet())
            {
                Debug.Log("Discarding connection to " + url);
                websocket.Close();
                return;
            }

            Debug.Log("Connected to: " + url);
            _recentConnectionMessageCount = 0;
            _firstTransmission = true;

            // Reset the server keyframe ID to avoid leaking ID from a previous session
            _serverKeyframeIdHandler.Reset();

            // send connection params
            _connectionParams["isClientReady"] = "1";
            string jsonStr = JsonConvert.SerializeObject(_connectionParams, Formatting.None, _jsonSettings);
            websocket.SendText(jsonStr);
            Debug.Log("Sent message: client ready!");

            mainWebSocket = websocket;
        };

        websocket.OnError += (e) =>
        {
            Debug.LogWarning($"Error connecting to {url}: {e}.");
        };

        websocket.OnClose += (e) =>
        {
            if (websocket == mainWebSocket)
            {
                Debug.Log("Main connection closed!");
                mainWebSocket = null;
                SetDisconnectStatus("");
                const int messageThreshold = 10;
                // delay reconnect after close, to avoid spamming servers and to give other clients a chance to connect
                if (_recentConnectionMessageCount >= messageThreshold)
                {
                    // This was a long-lasting connection. Disconnected most likely due to client idle.
                    _disconnectReason = "Disconnected!";
                    _delayReconnect = 15.0f;
                }
                else
                {
                    // This was a short connection. Disconnected most likely due to server already having a client.
                    _disconnectReason = "Server is busy!";
                    _delayReconnect = _serverURLs.Count == 1 ? 5.0f : 2.0f;
                }
            }
        };

        websocket.OnMessage += (bytes) =>
        {
            if (_recentConnectionMessageCount == 0) {
                // Delete all old instances upon receiving the first keyframe.
                _player.DeleteAllInstancesFromKeyframes();

                // We stop trying to reconnect once a successful two-way connection has been established.
                _tryReconnecting = false;
            }

            string message = System.Text.Encoding.UTF8.GetString(bytes);
            ProcessReceivedKeyframes(message);
            messagesReceivedCount++;
            _recentConnectionMessageCount++;
        };

        await websocket.Connect();
    }

    private bool isConnected()
    {
        return mainWebSocket != null && mainWebSocket.State == WebSocketState.Open;
    }

    private IEnumerator LogMessageRate()
    {
        while (true)
        {
            float duration = 20.0F;
            yield return new WaitForSeconds(duration);

            float fps = frameCount / duration;

            if (isConnected())
            {
                if (messagesReceivedCount > 0 && duration > 0)
                {
                    // Log the count of received messages
                    float messageRate = (float)messagesReceivedCount / duration;
                    Debug.Log($"Message rate: {messageRate.ToString("F1")}, FPS: {fps.ToString("F1")}");
                    _player.SetKeyframeRate(messageRate);
                }
            }
            else
            {
                Debug.Log($"Disconnected, FPS: {fps.ToString("F1")}");
            }

            // Reset the count
            messagesReceivedCount = 0;
            frameCount = 0;
        }
    }

    void ProcessReceivedKeyframes(string message)
    {
        KeyframeWrapper wrapperArray = JsonConvert.DeserializeObject<KeyframeWrapper>(message, _jsonSettings);
        foreach (KeyframeData keyframe in wrapperArray.keyframes)
        {
            _player.ProcessKeyframe(keyframe);
        }
    }

    void UpdateClientState()
    {
        _clientState.isLoading = LoadProgressTracker.Instance.IsLoading;
        
        if (_clientStateProducers == null)
        {
            return;
        }

        foreach (var updater in _clientStateProducers)
        {
            updater.UpdateClientState(ref _clientState);
        }
        
        if (_serverKeyframeIdHandler.recentServerKeyframeId != null)
        {
            _clientState.recentServerKeyframeId = _serverKeyframeIdHandler.recentServerKeyframeId;
        }
    }

    void SetDisconnectStatus(string status)
    {
        if (_canvasManager != null)
        {
            _canvasManager.ClearAllCanvases();
            _canvasManager.CreateOrUpdateUIElement("top_left", new UIElementUpdate
            {
                label=new()
                {
                    uid="disconnect_status",
                    text=status,
                    bold=false,
                    fontSize=24,
                    color=new float[]{1.0f, 1.0f, 1.0f, 1.0f},
                    horizontalAlignment=0,
                }   
            });
        }
    }

    IEnumerator SendClientState()
    {
        var timer = new System.Diagnostics.Stopwatch();
        while (true)
        {
            timer.Reset();
            timer.Start();

            if (isConnected())
            {
                // Update ClientState
                UpdateClientState();

                // Only include connection parameters in the first successful transmission.
                if (_firstTransmission && _connectionParams?.Count > 0)
                {
                    _clientState.connectionParamsDict = _connectionParams;
                }

                // Serialize ClientState to JSON.
                string jsonStr = JsonConvert.SerializeObject(_clientState, Formatting.None, _jsonSettings);

                // Reset the state of IClientStateProducers
                foreach (var updater in _clientStateProducers)
                {
                    updater.OnEndFrame();
                }

                // Send the ClientState to the server.
                Task task = mainWebSocket.SendText(jsonStr);
                yield return new WaitUntil(() => task.IsCompleted);

                // Update state after first successful transmission.
                if (_firstTransmission && task.Status == TaskStatus.RanToCompletion)
                {
                    _firstTransmission = false;
                }
            }

            timer.Stop();
            float elapsed = (float)timer.Elapsed.TotalSeconds;
            float waitTime = CLIENT_STATE_SEND_FREQUENCY - Mathf.Min(elapsed, CLIENT_STATE_SEND_FREQUENCY);
            yield return new WaitForSecondsRealtime(waitTime);
        }
    }

    public void OnDestroy()
    {
        if (mainWebSocket != null)
        {
            mainWebSocket.Close();
            mainWebSocket = null;
        }

        if (_coroutines != null)
        {
            _coroutines.StopAllCoroutines();
        }
    }

    public bool IsConnected() 
    {
        return mainWebSocket != null && mainWebSocket.State == WebSocketState.Open;
    }
}