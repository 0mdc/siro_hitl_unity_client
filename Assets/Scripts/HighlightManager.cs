using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HighlightManagerConfig", menuName = "Habitat/HighlightManager.Config", order = 1)]
public class HighlightManagerConfig : ScriptableObject
{    
    [Tooltip("Size of the highlight object pool.\nIf the amount of highlights received from the server is higher that this number, the excess will be discarded.\nThis value cannot be changed during runtime.")]
    public int poolSize = 32;

    [Tooltip("Line segment count composing the highlight circles.\nThis value cannot be changed during runtime.")]
    public int circleResolution = 32;

    [Tooltip("Vertex color of the highlight lines.")]
    public Color highlightColor = Color.white;

    [Tooltip("Materials used to shade the highlight circles.")]
    public Material[] highlightMaterials;

    [Tooltip("Thickness of the highlight circles, in meters.")]
    public float highlightWidth = 0.005f;

    [Tooltip("Base radius of the highlight circles, in meters. It is multiplied by the radius received from server messages.")]
    public float highlightBaseRadius = 1.0f;
}

public class HighlightManager : IKeyframeMessageConsumer
{
    const float TWO_PI = Mathf.PI * 2.0f;

    private HighlightManagerConfig _config;
    private Camera _camera;
    private GameObject _container;
    private LineRenderer[] _highlightPool;
    private int _activeHighlightCount = 0;

    public HighlightManager(HighlightManagerConfig config, Camera camera)
    {
        _config = config;
        _camera = camera;

        _container = new GameObject("HighlightManager");

        _highlightPool = new LineRenderer[_config.poolSize];
        float _arcSegmentLength = TWO_PI / _config.circleResolution;

        // Construct a pool of highlight line renderers
        GameObject container = new GameObject("Highlights");
        container.transform.parent = _container.transform;
        for (int i = 0; i < _highlightPool.Length; ++i)
        {
            GameObject highlight = new GameObject($"Highlight {i}");
            var lineRenderer = highlight.AddComponent<LineRenderer>();

            highlight.transform.parent = container.transform;
            lineRenderer.enabled = false;
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startColor = config.highlightColor;
            lineRenderer.endColor = config.highlightColor;
            lineRenderer.startWidth = config.highlightWidth;
            lineRenderer.endWidth = config.highlightWidth;
            lineRenderer.materials = config.highlightMaterials;
            lineRenderer.positionCount = config.circleResolution;
            for (int j = 0; j < config.circleResolution; ++j)
            {
                float xOffset = config.highlightBaseRadius * Mathf.Sin(j * _arcSegmentLength);
                float yOffset = config.highlightBaseRadius * Mathf.Cos(j * _arcSegmentLength);
                lineRenderer.SetPosition(j, new Vector3(xOffset, yOffset, 0.0f));
            }
            _highlightPool[i] = lineRenderer;
        }
    }

    public void ProcessMessage(Message message)
    {
        // Disable all highlights in the pool
        for (int i = 0; i < _activeHighlightCount; ++i)
        {
            _highlightPool[i].enabled = false;
        }
        // Draw highlights
        var highlightsMessage = message.highlights;
        if (highlightsMessage != null)
        {
            _activeHighlightCount = Math.Min(highlightsMessage.Length, _highlightPool.Length);
            for (int i = 0; i < _activeHighlightCount; ++i)
            {
                var highlight = _highlightPool[i];
                highlight.enabled = true;

                // Apply translation from message
                Vector3 center = CoordinateSystem.ToUnityVector(highlightsMessage[i].t);
                highlight.transform.position = center;

                // Billboarding
                highlight.transform.LookAt(_camera.transform);

                // Apply radius from message using scale
                highlight.transform.localScale = highlightsMessage[i].r * Vector3.one;
            }
        }
    }

    public void Update() {}
}
