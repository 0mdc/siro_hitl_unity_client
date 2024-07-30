using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : MonoBehaviour
{
    [SerializeField] Canvas _mainCanvas;
    [SerializeField] Canvas _topLeftCanvas;
    [SerializeField] Canvas _topRightCanvas;
    [SerializeField] Canvas _bottomLeftCanvas;
    [SerializeField] Canvas _bottomRightCanvas;
    [SerializeField] Canvas _floatingCanvas;

    Dictionary<string, Canvas> _canvases;
    Dictionary<string, Dictionary<string, GameObject>> _elements;

    void Awake()
    {
    }

    public void Initialize(Camera camera)
    {
        _mainCanvas.worldCamera = camera;

        _canvases = new()
        {
            {"top_left", _topLeftCanvas},
            {"top_right", _topRightCanvas},
            {"bottom_left", _bottomLeftCanvas},
            {"bottom_right", _bottomRightCanvas},
            {"floating", _floatingCanvas},
        };
        _elements = new()
        {
            {"top_left", new()},
            {"top_right", new()},
            {"bottom_left", new()},
            {"bottom_right", new()},
            {"floating", new()},
        };
    }

    public void AddUIElement(string canvasId, string uid, GameObject element)
    {
        // TODO: GetGroupRectTransform...
        Canvas canvas;
        if (!_canvases.TryGetValue(canvasId, out canvas))
        {
            Debug.LogError($"Canvas '{canvasId}' not found.");
            return;
        }

        var group = canvas.GetComponent<HorizontalOrVerticalLayoutGroup>();
        if (group == null)
        {
            Debug.LogError($"Canvas '{canvasId}' does not contain a layout group.");
            return;
        }

        var groupRectTransform = group.GetComponent<RectTransform>();
        var elementRectTransform = element.GetComponent<RectTransform>();

        elementRectTransform.SetParent(groupRectTransform, worldPositionStays:false);

        _elements[canvasId][uid] = element;
    }

    public void ClearCanvas(string canvasId, ref Dictionary<string, GameObject> elementSet)
    {
        List<string> deletedKeys = new();
        foreach (var pair in _elements[canvasId])
        {
            Destroy(pair.Value);
            deletedKeys.Add(pair.Key);
        }
        foreach (var deletedKey in deletedKeys)
        {
            // TODO: Refactor key handling.
            _elements.Remove(deletedKey);
            elementSet.Remove(deletedKey);
        }
    }

    public void MoveCanvas(string canvasId, Vector3 position, Camera _uiCamera)
    {
        Canvas canvas;
        if (!_canvases.TryGetValue(canvasId, out canvas))
        {
            Debug.LogError($"Canvas '{canvasId}' not found.");
            return;
        }

        var rt = canvas.GetComponent<RectTransform>();
        var screenPos = _uiCamera.WorldToViewportPoint(position);
        rt.anchorMin = screenPos;
        rt.anchorMax = screenPos;
    }

    public void Update()
    {
        _mainCanvas.GetComponent<Canvas>().enabled = !LoadProgressTracker.Instance.IsApplicationPaused;
    }
}
