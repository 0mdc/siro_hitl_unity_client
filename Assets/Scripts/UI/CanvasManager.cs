using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class CanvasManager : IKeyframeMessageConsumer, IClientStateProducer
{
#region IKeyframeMessageConsumer

    Camera _uiCamera;
    Camera _mainCamera;
    UIPrefabs _uiPrefabs;
    MainCanvas _mainCanvas;

    Dictionary<string, GameObject> _uiElements = new ();
    string _tooltip = null;

    public CanvasManager(Camera mainCamera, UIPrefabs uiPrefabs)
    {
        _mainCamera = mainCamera;
        _uiPrefabs = uiPrefabs;

        _uiCamera = new GameObject("UI Camera").AddComponent<Camera>();
        _uiCamera.cullingMask = LayerMask.GetMask("UI");
        _uiCamera.transform.SetParent(mainCamera.transform, worldPositionStays:false);

        _mainCanvas = new MainCanvas(_uiCamera, _uiPrefabs);

        // Make the UI camera an "overlay camera", which renders on top of others.
        var uiCameraData = _uiCamera.GetUniversalAdditionalCameraData();
        uiCameraData.renderType = CameraRenderType.Overlay;

        // Add the UI camera as an overlay to the main camera.
        var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        mainCameraData.cameraStack.Add(_uiCamera);
    }

    public void ClearAllCanvases()
    {
        _mainCanvas.ClearAllCanvases(ref _uiElements);
    }

    public void ProcessMessage(Message message)
    {
        var uiUpdates = message.uiUpdates;
        if (uiUpdates != null)
        {
            foreach (var pair in uiUpdates)
            {
                ProcessCanvasUpdate(pair.Key, pair.Value);
            }
        }

        // Move canvases.
        /*
        var canvasPositions = message.canvasPositions;
        if (canvasPositions != null)
        {
            foreach (var pair in canvasPositions)
            {
                var position = CoordinateSystem.ToUnityVector(pair.Value);
                _mainCanvas.MoveCanvas(pair.Key, position, _uiCamera);
            }
        }
        */
    }

    private void ProcessCanvasUpdate(string canvasUid, UICanvasUpdate canvasUpdate)
    {
        if (canvasUpdate.clear)
        {
            _mainCanvas.ClearCanvas(canvasUid, ref _uiElements);
        }

        if (canvasUpdate.elements != null)
        {
            foreach (var element in canvasUpdate.elements)
            {
                CreateOrUpdateUIElement(canvasUid, element);
            }
        }
    }

    public void CreateOrUpdateUIElement(string canvasUid, UIElementUpdate uiUpdate)
    {
        string canvasKey = canvasUid;

        // Update canvas properties.
        _mainCanvas.UpdateCanvas(canvasUid, uiUpdate.canvasProperties);

        // Process elements by type.
        CreateOrUpdateUIElement(canvasKey, uiUpdate.button);
        CreateOrUpdateUIElement(canvasKey, uiUpdate.toggle);
        CreateOrUpdateUIElement(canvasKey, uiUpdate.label);
        CreateOrUpdateUIElement(canvasKey, uiUpdate.listItem);
        CreateOrUpdateUIElement(canvasKey, uiUpdate.separator);
        CreateOrUpdateUIElement(canvasKey, uiUpdate.spacer);
    }

    public void CreateOrUpdateUIElement(string canvasKey, UIElement uiElement)
    {
        if (uiElement == null)
        {
            return;
        }
        
        if (_uiElements.TryGetValue(uiElement.uid, out GameObject obj))
        {
            UpdateUIElement(uiElement, obj);
        }
        else
        {
            GameObject newObj = InstantiateUIElement(uiElement);
            _mainCanvas.AddUIElement(canvasKey, uiElement.uid, newObj);
            _uiElements[uiElement.uid] = newObj;
        }
    }

    public GameObject InstantiateUIElement(UIElement uiElement)
    {
        switch (uiElement) {
            case UIButton button:
                return UIElementButton.Create(_uiPrefabs.ButtonPrefab, this, button).gameObject;
            case UILabel label:
                return UIElementLabel.Create(_uiPrefabs.LabelPrefab, this, label).gameObject;
            case UIListItem listItem:
                return UIElementListItem.Create(_uiPrefabs.ListItemPrefab, this, listItem).gameObject;
            case UIToggle toggle:
                return UIElementToggle.Create(_uiPrefabs.TogglePrefab, this, toggle).gameObject;
            case UISeparator separator:
                return UIElementSeparator.Create(_uiPrefabs.SeparatorPrefab, this, separator).gameObject;
            case UISpacer spacer:
                return UIElementSpacer.Create(_uiPrefabs.SpacerPrefab, this, spacer).gameObject;
            default:
                return new GameObject(uiElement.uid.ToString(), typeof(RectTransform));
        }
    }

    public void UpdateUIElement(UIElement uiElement, GameObject obj)
    {
        switch (uiElement) {
            case UIButton button:
                obj.GetComponent<UIElementButton>().UpdateUIElement(button);
                break;
            case UILabel label:
                obj.GetComponent<UIElementLabel>().UpdateUIElement(label);
                break;
            case UIListItem listItem:
                obj.GetComponent<UIElementListItem>().UpdateUIElement(listItem);
                break;
            case UIToggle toggle:
                obj.GetComponent<UIElementToggle>().UpdateUIElement(toggle);
                break;
            default:
                // Unsupported element.
                break;
        }
    }

    public Rect RectTransformToScreenSpace(RectTransform transform)
    {
        Vector3[] corners = new Vector3[4];
        transform.GetWorldCorners(corners);

        for (int i = 0; i < corners.Length; i++)
        {
            corners[i] = _uiCamera.WorldToScreenPoint(corners[i]);
            corners[i] = new Vector3(Mathf.RoundToInt(corners[i].x), Mathf.RoundToInt(corners[i].y), corners[i].z);
        }

        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float width = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x) - minX;
        float height = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y) - minY;

        return new Rect(minX, minY, width, height);
    }

    public void UpdateTooltip(string tooltip, RectTransform transform)
    {
        if (_tooltip != tooltip)
        {
            _tooltip = tooltip;
            if (string.IsNullOrEmpty(tooltip) || transform == null)
            {
                _mainCanvas.ClearCanvas("tooltip", ref _uiElements);
            }
            else
            {
                CreateOrUpdateUIElement("tooltip", new UIElementUpdate
                {
                    canvasProperties=new()
                    {
                        padding=12,
                        backgroundColor=new float[]{0.5f, 0.5f, 0.5f, 1.0f} 
                    },
                    label=new()
                    {
                        uid="__tooltip",
                        text=tooltip,
                        bold=false,
                        fontSize=24,
                        horizontalAlignment=0,
                    }
                });

                // Sloppy
                var canvasRect = RectTransformToScreenSpace(transform.parent as RectTransform);
                var canvasRightEdgeX = (canvasRect.center + canvasRect.width * Vector2.one * 0.5f).x;

                var controlRect = RectTransformToScreenSpace(transform);
                var controlCenterY = controlRect.center.y;
                var screenPoint = new Vector2(canvasRightEdgeX + 12, controlCenterY);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(transform.parent.parent as RectTransform, screenPoint, _uiCamera, out Vector2 localPoint);                

                _mainCanvas.MoveCanvas("tooltip", localPoint);
            }
        }
    }


    public void Update()
    {
        _mainCanvas.Update();
    }

#endregion
#region IClientStateProducer

    UIElements _output = new();

    public void OnButtonPressed(string uid)
    {
        _output.buttonsPressed.Add(uid);
    }

    public void OnTextChanged(string uid, string text)
    {
        _output.textboxes[uid] = text;
    }

    public void UpdateClientState(ref ClientState state)
    {
        state.ui = _output;
    }

    public void OnEndFrame()
    {
        _output.buttonsPressed.Clear();
        _output.textboxes.Clear();
    }

#endregion
}
