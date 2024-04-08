using System;
using UnityEngine;
using UnityEngine.Assertions;

public class GuiDrawer : IKeyframeMessageConsumer
{
    const float TWO_PI = Mathf.PI * 2.0f;

    private AppConfig _config;
    private Camera _camera;
    private GameObject _container;
    private LineRenderer[] _circlePool;
    private LineRenderer[] _linePool;
    private int _activeCircleCount = 0;
    private int _activeLineCount = 0;

    public GuiDrawer(AppConfig config, Camera camera)
    {
        _config = config;
        _camera = camera;

        _container = new GameObject("GuiDrawer");

        _circlePool = new LineRenderer[_config.highlightPoolSize];
        _linePool = new LineRenderer[_config.highlightPoolSize * 2];

        float _arcSegmentLength = TWO_PI / _config.highlightCircleResolution;

        {
            // Construct a pool of circle line renderers.
            GameObject container = new GameObject("Circles");
            container.transform.parent = _container.transform;
            for (int i = 0; i < _circlePool.Length; ++i)
            {
                GameObject highlight = new GameObject($"Segment {i}");
                var lineRenderer = highlight.AddComponent<LineRenderer>();

                highlight.transform.parent = container.transform;
                lineRenderer.enabled = false;
                lineRenderer.loop = true;
                lineRenderer.useWorldSpace = false;
                lineRenderer.startColor = config.highlightDefaultColor;
                lineRenderer.endColor = config.highlightDefaultColor;
                lineRenderer.startWidth = config.highlightWidth;
                lineRenderer.endWidth = config.highlightWidth;
                lineRenderer.materials = config.highlightMaterials;
                lineRenderer.positionCount = config.highlightCircleResolution;
                for (int j = 0; j < config.highlightCircleResolution; ++j)
                {
                    float xOffset = config.highlightBaseRadius * Mathf.Sin(j * _arcSegmentLength);
                    float yOffset = config.highlightBaseRadius * Mathf.Cos(j * _arcSegmentLength);
                    lineRenderer.SetPosition(j, new Vector3(xOffset, yOffset, 0.0f));
                }
                _circlePool[i] = lineRenderer;
            }
        }

        {
            // Construct a pool of line renderers.
            GameObject container = new GameObject("Lines");
            container.transform.parent = _container.transform;
            for (int i = 0; i < _linePool.Length; ++i)
            {
                GameObject highlight = new GameObject($"Segment {i}");
                var lineRenderer = highlight.AddComponent<LineRenderer>();

                highlight.transform.parent = container.transform;
                lineRenderer.enabled = false;
                lineRenderer.loop = false;
                lineRenderer.useWorldSpace = true;
                lineRenderer.startColor = config.highlightDefaultColor;
                lineRenderer.endColor = config.highlightDefaultColor;
                lineRenderer.startWidth = config.highlightWidth;
                lineRenderer.endWidth = config.highlightWidth;
                lineRenderer.materials = config.highlightMaterials;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.zero);
                _linePool[i] = lineRenderer;
            }
        }
    }

    public void ProcessMessage(Message message)
    {
        // Disable all items in the pools.
        for (int i = 0; i < _activeCircleCount; ++i)
        {
            _circlePool[i].enabled = false;
        }
        for (int i = 0; i < _activeLineCount; ++i)
        {
            _linePool[i].enabled = false;
        }

        // Draw circles
        var circleMessage = message.circles;
        if (circleMessage != null)
        {
            _activeCircleCount = Math.Min(circleMessage.Length, _circlePool.Length);
            for (int i = 0; i < _activeCircleCount; ++i)
            {
                Circle msg = circleMessage[i];
                var lineRenderer = _circlePool[i];
                lineRenderer.enabled = true;

                Color color = _config.highlightDefaultColor;
                if (msg.c != null && msg.c.Length > 0)                
                {
                    Assert.AreEqual(msg.c.Length, 4, $"Invalid highlight color format. Expected 4 ints, got {msg.c.Length}.");
                    color.r = (float)msg.c[0] / 255.0f;
                    color.g = (float)msg.c[1] / 255.0f;
                    color.b = (float)msg.c[2] / 255.0f;
                    color.a = (float)msg.c[3] / 255.0f;
                }
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;

                // Apply translation from message
                Vector3 center = CoordinateSystem.ToUnityVector(msg.t);
                lineRenderer.transform.position = center;

                // Billboarding
                if (msg.b == 1)
                {
                    lineRenderer.transform.LookAt(_camera.transform);
                }
                else
                {
                    var normal = msg.n == null ? Vector3.up : CoordinateSystem.ToUnityVector(msg.n);
                    lineRenderer.transform.LookAt(center + normal);
                }

                // Apply radius from message using scale
                lineRenderer.transform.localScale = circleMessage[i].r * Vector3.one;
            }
        }

        // Draw lines
        var lineMessage = message.lines;
        if (lineMessage != null)
        {
            _activeLineCount = Math.Min(lineMessage.Length, _linePool.Length);
            for (int i = 0; i < _activeLineCount; ++i)
            {
                Line msg = lineMessage[i];
                var lineRenderer = _linePool[i];
                lineRenderer.enabled = true;

                Color color = _config.highlightDefaultColor;
                if (msg.c != null && msg.c.Length > 0)                
                {
                    Assert.AreEqual(msg.c.Length, 4, $"Invalid highlight color format. Expected 4 ints, got {msg.c.Length}.");
                    color.r = (float)msg.c[0] / 255.0f;
                    color.g = (float)msg.c[1] / 255.0f;
                    color.b = (float)msg.c[2] / 255.0f;
                    color.a = (float)msg.c[3] / 255.0f;
                }
                lineRenderer.startColor = color;
                lineRenderer.endColor = color; // TODO: Implement end color.

                // Apply translation from message
                lineRenderer.SetPosition(0, CoordinateSystem.ToUnityVector(msg.a));
                lineRenderer.SetPosition(1, CoordinateSystem.ToUnityVector(msg.b));
            }
        }
    }

    public void Update() {}
}
