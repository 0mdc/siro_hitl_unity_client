using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages application viewports.
/// Viewports are screen regions rendering with a camera.
/// The default viewport has the ID -1.
/// </summary>
public class ViewportHandler : IKeyframeMessageConsumer
{
    public struct Viewport
    {
        public Camera camera;
    }

    Dictionary<int, Viewport> _viewports = new();

    Camera _mainCamera;

    // TODO: Move to constants.
    public const int FIRST_LAYER_INDEX = 8;
    public const int LAYER_COUNT = 8;
    public const int LAST_LAYER_INDEX = FIRST_LAYER_INDEX + LAYER_COUNT;

    int DEFAULT_LAYERS = LayerMask.GetMask("Default");

    public ViewportHandler(Camera mainCamera)
    {
        _mainCamera = mainCamera;
        _mainCamera.cullingMask = DEFAULT_LAYERS;
        for (int layerIndex = FIRST_LAYER_INDEX; layerIndex < LAST_LAYER_INDEX; ++layerIndex)
        {
            _mainCamera.cullingMask |= 1 << layerIndex;
        }
        _viewports.Add(-1, new Viewport()
        {
            camera = mainCamera
        });
    }

    public void ProcessMessage(Message message)
    {
        Dictionary<int, ViewportProperties> viewportUpdates = message.viewports;

        // Update viewport properties.
        if (viewportUpdates != null)
        {
            foreach (var pair in viewportUpdates)
            {
                UpdateViewportProperties(pair.Key, pair.Value);
            }
        }

        // Disable all cameras except the main viewport camera.
        foreach (var pair in _viewports)
        {
            if (pair.Key != -1)
            {
                Camera cam = pair.Value.camera;
                cam.enabled = false;
            }
        }

        // Update camera transforms.
        // Only enable cameras that were modified.
        Dictionary<int, AbsTransform> cameraUpdates = message.cameras;        
        if (cameraUpdates != null)
        {
            foreach (var pair in cameraUpdates)
            {
                int key = pair.Key;
                AbsTransform cameraUpdate = pair.Value;
                Viewport viewport = _viewports[key];
                Camera camera = viewport.camera;
                camera.enabled = true;
                if (cameraUpdate.translation?.Count == 3 && cameraUpdate?.rotation?.Count == 4) {
                    camera.transform.position = CoordinateSystem.ToUnityVector(cameraUpdate.translation);
                    camera.transform.rotation = CoordinateSystem.ToUnityQuaternion(cameraUpdate.rotation);
                }
            }
        }
    }

    private void UpdateViewportProperties(int key, ViewportProperties properties)
    {
        // If this is a new viewport, create it.
        Viewport viewport;
        if (!_viewports.ContainsKey(key))
        {
            viewport = CreateViewport(key);
        }
        else
        {
            viewport = _viewports[key];
        }

        // Rect format: X, Y, Width, Height.
        // The values are in normalized screen coordinates (between 0 and 1).
        Camera camera = viewport.camera;
        if (properties.rect?.Length == 4)
        {
            var rect = properties.rect;
            camera.rect = new Rect(rect[0], rect[1], rect[2], rect[3]);
        }

        // Set camera visibility layers.
        if (properties.layers != null)
        {
            int mask = DEFAULT_LAYERS;
            foreach (int layer in properties.layers)
            {
                int layerIndex = FIRST_LAYER_INDEX + layer;
                mask |= 1 << layerIndex;
            }
            camera.cullingMask = mask;
        }
    }

    private Viewport CreateViewport(int key)
    {
        // Clone the main camera.
        GameObject container = GameObject.Instantiate(_mainCamera.gameObject);
        container.name = $"Viewport {key}";
        Camera newCamera = container.GetComponent<Camera>();
        newCamera.tag = "Untagged"; // Remove MainCamera tag.
        newCamera.GetComponent<AudioListener>().enabled = false; // Disable audio.
        Viewport newViewport = new()
        {
            camera = newCamera
        };
        _viewports[key] = newViewport;
        return newViewport;
    }

    void IUpdatable.Update()
    {
        
    }
}
