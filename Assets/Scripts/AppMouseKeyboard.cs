using UnityEngine;

/// <summary>
/// Mouse/Keyboard Habitat client application.
/// Intended to be used from web browsers (WebGL).
/// </summary>
public class AppMouseKeyboard : MonoBehaviour
{
    [Header("Config Defaults")]
    [Tooltip("Config defaults are used directly when running in the Editor. On device, they are used to populate config.txt at Android/data/com.meta.siro_hitl_vr_client/files/. This file persists and can be edited between runs, e.g. by connecting via USB to a laptop.")]
    [SerializeField] private bool _mouseoverForTooltip;  // dummy member so we can add tooltip in Inspector pane

    [Tooltip("Default server locations.")]
    [SerializeField] private string[] _defaultServerLocations = new string[]{ "127.0.01:8888" };

    [Tooltip("If specified, the keyframe will be played upon launching the client.")]
    [SerializeField] TextAsset _testKeyframe;

    [Header("Rendering Configuration")]
    [Tooltip("Main camera.")]
    [SerializeField] private Camera _camera;

    [Tooltip("Global application configuration.")]
    [SerializeField] private AppConfig _appConfig;

    [Tooltip("Icon that is displayed when connection is lost.")]
    [SerializeField] private GameObject _offlineIcon;

    [Tooltip("Container of UI prefab references.")]
    [SerializeField] private UIPrefabs _uiPrefabs;

    [Tooltip("External plugin for rendering outlines.")]
    [SerializeField] private UnityFx.Outline.OutlineLayerCollection _outlineLayers;

    // IKeyframeMessageConsumers
    ServerKeyframeIdHandler _serverKeyframeIdHandler;
    GuiDrawer _guiDrawer;
    LoadingEffectHandler _loadingEffectHandler;
    TextRenderer _textRenderer;
    LoadingScreenOverlay _loadingScreenOverlay;
    ViewportHandler _viewportHandler;

    // IClientStateProducers
    InputTrackerMouse _inputTrackerMouse;
    InputTrackerKeyboard _inputTrackerKeyboard;

    // Producer/Consumers
    UIElementDrawer _uiElementDrawer;
    CanvasManager _canvasManager;

    // Application state
    ConfigLoader _configLoader;
    GfxReplayPlayer _gfxReplayPlayer;
    NetworkClient _networkClient;
    OnlineStatusDisplayHandler _onlineStatusDisplayHandler;
    ReplayFileLoader _replayFileLoader;

    protected void Awake()
    {
        // Initialize Producer/Consumers.
        _uiElementDrawer = new UIElementDrawer(_camera);
        _canvasManager = new CanvasManager(_camera, _uiPrefabs);

        // Initialize IKeyframeMessageConsumers.
        _serverKeyframeIdHandler = new ServerKeyframeIdHandler();
        _guiDrawer = new GuiDrawer(_appConfig, _camera);
        _loadingEffectHandler = new LoadingEffectHandler();
        _textRenderer = new TextRenderer();
        _loadingScreenOverlay = new LoadingScreenOverlay(_camera);
        _viewportHandler = new ViewportHandler(_camera);
        
        var keyframeMessageConsumers = new IKeyframeMessageConsumer[]
        {
            _serverKeyframeIdHandler,
            _guiDrawer,
            _loadingEffectHandler,
            _textRenderer,
            _loadingScreenOverlay,
            _viewportHandler,
            _uiElementDrawer,
            _canvasManager,
        };

        // Initialize IClientStateProducers.
        _inputTrackerMouse = new InputTrackerMouse(_camera);
        _inputTrackerKeyboard = new InputTrackerKeyboard();
        var clientStateProducers = new IClientStateProducer[]
        {
            _inputTrackerMouse,
            _inputTrackerKeyboard,
            _uiElementDrawer,
            _canvasManager,
        };

        // Initialize application state.
        _configLoader = new ConfigLoader(_defaultServerLocations);
        _gfxReplayPlayer = new GfxReplayPlayer(keyframeMessageConsumers, _outlineLayers);
        _networkClient = new NetworkClient(_gfxReplayPlayer, _configLoader, clientStateProducers, _serverKeyframeIdHandler, _canvasManager);
        _onlineStatusDisplayHandler = new OnlineStatusDisplayHandler(_offlineIcon, _camera);
        _replayFileLoader = new ReplayFileLoader(_gfxReplayPlayer, _testKeyframe);
    }

    void Update()
    {
        _gfxReplayPlayer.Update();
        _networkClient.Update();
        _onlineStatusDisplayHandler.Update(_networkClient.IsConnected());
        _replayFileLoader.Update();
    }

    void OnDestroy()
    {
        _networkClient.OnDestroy();
    }
}
