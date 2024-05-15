using System.Collections;
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

    int DEFAULT_LAYERS = LayerMask.NameToLayer("Default") | LayerMask.NameToLayer("UI");

    public ViewportHandler(Camera mainCamera)
    {
        _mainCamera = mainCamera;
        _viewports.Add(-1, new Viewport()
        {
            camera = mainCamera
        });
    }

    public void ProcessMessage(Message message)
    {
        Dictionary<int, ViewportProperties> viewportUpdates = message.viewports;

        foreach (var pair in _viewports)
        {
            if (pair.Key != -1)
            {
                Camera cam = pair.Value.camera;
                cam.enabled = false;
            }
        }

        if (viewportUpdates == null)
        {
            return;
        }
        foreach (var pair in viewportUpdates)
        {
            int key = pair.Key;
            if (!_viewports.ContainsKey(key))
            {
                // Clone the main camera.
                GameObject container = GameObject.Instantiate(_mainCamera.gameObject);
                container.name = $"Viewport {key}";
                Camera newCamera = container.GetComponent<Camera>();
                newCamera.tag = "Untagged"; // Remove MainCamera tag.
                newCamera.GetComponent<AudioListener>().enabled = false; // Disable audio.
                _viewports[key] = new Viewport()
                {
                    camera = newCamera
                };
            }

            ViewportProperties properties = pair.Value;
            Viewport viewport = _viewports[key];
            Camera camera = viewport.camera;

            if (key != -1)
            {
                camera.enabled = properties.enabled.GetValueOrDefault();
                if (camera.enabled)
                {
                    if (properties.camera?.translation?.Count == 3 && properties.camera?.rotation?.Count == 4) {
                        camera.transform.position = CoordinateSystem.ToUnityVector(properties.camera.translation);
                        camera.transform.rotation = CoordinateSystem.ToUnityQuaternion(properties.camera.rotation);
                    }
                }
            }

            // Rect format: X, Y, Width, Height.
            // The values are in normalized screen coordinates (between 0 and 1).
            if (properties.rect?.Length == 4)
            {
                var rect = properties.rect;
                camera.rect = new Rect(rect[0], rect[1], rect[2], rect[3]);
            }
        }
    }

    void IUpdatable.Update()
    {
        
    }
}
