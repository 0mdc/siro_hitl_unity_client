using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class LoadingScreenOverlay : IKeyframeMessageConsumer
{
    /// <summary>
    /// Overlay, with a progress bar, that shows up when content is being loaded.
    /// </summary>
    class Overlay : MonoBehaviour
    {
        bool _initialized = false;
        Camera _camera = null;
        bool _visible = false;
        float _progress = 0.0f;

        public void Initialize(Camera camera)
        {
            _initialized = true;
            _camera = camera;
        }

        public void UpdateState(bool visible, float progress = 0.0f)
        {
            _visible = visible;
            _progress = progress;
        }

        public bool Visible { get { return _visible; }}

        void OnGUI()
        {
            if (!_visible || !_initialized)
            {
                return;
            }

            int width = _camera.pixelWidth;
            int height = _camera.pixelHeight;
            const int progressBarHeight = 32;
            int progressBarWidth = width / 2;
            int progressBarXOffset = width / 4;
            int progressBarYOffset = (height / 2) - (progressBarHeight / 2);

            GUI.color = Color.white;
            GUILayout.Window(
                7, // TODO
                new Rect(progressBarXOffset,progressBarYOffset,progressBarWidth,progressBarHeight),
                WindowUpdate,
                $"Loading ({_progress:F2})");
        }

        void WindowUpdate(int windowId)
        {
            GUILayout.BeginVertical();
            GUI.color = Color.white;
            GUILayout.HorizontalSlider(_progress, 0.0f, 1.0f);
            GUILayout.EndVertical();
        }
    }

    private Overlay _overlay;
    private LoadProgressTracker _loadProgressTracker;
    private bool _loading = false;
    private bool _changingScene = false;

    public LoadingScreenOverlay(Camera camera)
    {
        _loadProgressTracker = LoadProgressTracker.Instance;
        _overlay = new GameObject("LoadingScreenOverlay").AddComponent<Overlay>();
        _overlay.Initialize(camera);

        _loadProgressTracker.OnLoadStarted += LoadStarted;
        _loadProgressTracker.OnLoadFinished += LoadFinished;
    }

    public void ProcessMessage(Message message)
    {
        if (message.sceneChanged)
        {
            _changingScene = true;
        }
    }

    public void Update()
    {
        // Show the loading bar overlay if the scene has changed, and if loading is active.
        // Avoid showing it for single item loads.
        if (_loading && _changingScene)
        {
            _overlay.UpdateState(visible: true, progress: _loadProgressTracker.EstimateProgress());
        }
        else
        {
            _overlay.UpdateState(visible: false);
        }
    }

    void LoadStarted()
    {
        _loading = true;
    }

    void LoadFinished()
    {
        _loading = false;
        _changingScene = false;
    }
}
