using System.Collections;
using System.Collections.Generic;
using Unity.Loading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;

public class CanvasData
{
    public string uid;
    public Canvas canvas;
    public RectTransform transform;
    public LayoutGroup group;
    public GraphicRaycaster raycaster;
    public Dictionary<string, GameObject> content = new();

    public CanvasData(
        string uid,
        Canvas canvas,
        RectTransform transform,
        LayoutGroup group,
        GraphicRaycaster raycaster)
    {
        this.uid = uid;
        this.canvas = canvas;
        this.transform = transform;
        this.group = group;
        this.raycaster = raycaster;
    }
}


public class MainCanvas : IUpdatable
{
    Canvas _root;
    InputSystemUIInputModule _inputSystem;
    Dictionary<string, CanvasData> _canvases = new();

    public MainCanvas(Camera uiCamera)
    {
        if (GameObject.FindFirstObjectByType<InputSystemUIInputModule>() == null)
        {
            _inputSystem = new GameObject("Event System").AddComponent<InputSystemUIInputModule>();
            _inputSystem.gameObject.AddComponent<EventSystem>();
        }

        _root = CreateRootCanvas(uiCamera);

        float uiScale = 0.55f;
        int padding = 16;

        var uids = new string[]
        {
            "top_left",
            "top",
            "top_right",
            "left",
            "center",
            "right",
            "bottom_left",
            "bottom",
            "bottom_right",
        };

        for (int h = 0; h < 3; ++h)
        {
            for (int v = 0; v < 3; ++v)
            {
                int alignment = 3 * v + h;
                string uid = uids[alignment];
                var canvas = CreateCanvas(uid, h, v, padding, uiScale);

                _canvases[uid] = canvas;
            }
        }
    }

    public Canvas CreateRootCanvas(Camera uiCamera)
    {
        var container = new GameObject("UI");
        container.layer = LayerMask.NameToLayer("UI");

        var canvas = container.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = uiCamera;
        canvas.vertexColorAlwaysGammaSpace = true;
        
        var canvasScaler = container.AddComponent<CanvasScaler>();
        canvasScaler.scaleFactor = 1f;
        canvasScaler.referencePixelsPerUnit = 100f;

        container.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    public CanvasData CreateCanvas(string uid, int horizontalAlignment, int verticalAlignment, int padding, float uiScale)
    {
        var container = new GameObject(uid);
        container.layer = LayerMask.NameToLayer("UI");
        var canvas = container.AddComponent<Canvas>();

        var rect = container.GetComponent<RectTransform>();        
        rect.SetParent(_root.transform, false);
        int alignment = 3 * verticalAlignment + horizontalAlignment;
        rect.localScale = uiScale * Vector3.one;
        rect.pivot = new Vector2(
            x: horizontalAlignment * 0.5f,
            y: (2f - verticalAlignment) * 0.5f
        );
        rect.anchorMin = rect.pivot;
        rect.anchorMax = rect.pivot;
        rect.anchoredPosition = new Vector2(
            x: (horizontalAlignment - 1f) * padding,
            y: (2f - verticalAlignment - 1f) * padding
        );
        rect.anchoredPosition = new Vector2(
            x: (2f - horizontalAlignment - 1f) * padding,
            y: (verticalAlignment - 1f) * padding
        );

        var group = container.AddComponent<VerticalLayoutGroup>();
        group.spacing = 6;
        group.childAlignment = (TextAnchor)alignment;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childScaleWidth = false;
        group.childScaleHeight = false;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = true;

        var contentSizeFitter = container.AddComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var raycaster = container.AddComponent<GraphicRaycaster>();

        return new CanvasData(uid, canvas, rect, group, raycaster);
    }

    public void AddUIElement(string canvasId, string uid, GameObject element)
    {
        CanvasData data;
        if (!_canvases.TryGetValue(canvasId, out data))
        {
            Debug.LogError($"Canvas '{canvasId}' not found.");
            return;
        }

        var elementRectTransform = element.GetComponent<RectTransform>();
        elementRectTransform.SetParent(data.transform, worldPositionStays:false);
        data.content[uid] = element;
    }

    public void ClearCanvas(string canvasId, ref Dictionary<string, GameObject> elementSet)
    {
        CanvasData data;
        if (!_canvases.TryGetValue(canvasId, out data))
        {
            Debug.LogError($"Canvas '{canvasId}' not found.");
            return;
        }

        List<string> deletedKeys = new();
        foreach (var pair in data.content)
        {
            GameObject.Destroy(pair.Value);
            deletedKeys.Add(pair.Key);
        }
        foreach (var deletedKey in deletedKeys)
        {
            // TODO: Refactor key handling.
            data.content.Remove(deletedKey);
            elementSet.Remove(deletedKey);
        }
    }

    public void ClearAllCanvases(ref Dictionary<string, GameObject> elementSet)
    {
        foreach (string canvasId in _canvases.Keys)
        {
            ClearCanvas(canvasId, ref elementSet);
        }
    }

    public void MoveCanvas(string canvasId, Vector3 position, Camera _uiCamera)
    {
        CanvasData data;
        if (!_canvases.TryGetValue(canvasId, out data))
        {
            Debug.LogError($"Canvas '{canvasId}' not found.");
            return;
        }

        var screenPos = _uiCamera.WorldToViewportPoint(position);
        data.transform.anchorMin = screenPos;
        data.transform.anchorMax = screenPos;
    }

    public void Update()
    {
        // TODO: Refactor
        _root.GetComponent<Canvas>().enabled = !LoadProgressTracker.Instance._modalDialogueShown;
    }
}
