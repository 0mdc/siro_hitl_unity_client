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

    Dictionary<string, GameObject> _uiElements = new ();

    MainCanvas _mainCanvas;

    public CanvasManager(Camera mainCamera, UIPrefabs uiPrefabs, MainCanvas mainCanvas)
    {
        _mainCamera = mainCamera;

        _uiCamera = new GameObject("UI Camera").AddComponent<Camera>();
        _uiCamera.cullingMask = LayerMask.GetMask("UI");
        _uiCamera.transform.SetParent(mainCamera.transform, worldPositionStays:false);

        // Make the UI camera an "overlay camera", which renders on top of others.
        var uiCameraData = _uiCamera.GetUniversalAdditionalCameraData();
        uiCameraData.renderType = CameraRenderType.Overlay;

        // Add the UI camera as an overlay to the main camera.
        var mainCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        mainCameraData.cameraStack.Add(_uiCamera);

        _uiPrefabs = uiPrefabs;
        _mainCanvas = mainCanvas;

        _mainCanvas.Initialize(_uiCamera);
    }

    public void ClearAllCanvases()
    {
        _mainCanvas.ClearAllCanvases(ref _uiElements);
    }

    public void ProcessMessage(Message message)
    {
        // Delete UI elements first.
        var clearCanvases = message.clearCanvases;
        if (clearCanvases != null)
        {
            foreach (string canvasKey in clearCanvases)
            {
                _mainCanvas.ClearCanvas(canvasKey, ref _uiElements);
            }
        }

        // Add new UI elements.
        var uiUpdates = message.uiUpdates;
        if (uiUpdates != null)
        {
            foreach (var uiUpdate in uiUpdates)
            {
                AddUIElement(uiUpdate);
            }
        }

        // Move canvases.
        var canvasPositions = message.canvasPositions;
        if (canvasPositions != null)
        {
            foreach (var pair in canvasPositions)
            {
                // TODO: Currently, only 'floating' can be moved.
                if (pair.Key != "floating") continue;

                var position = CoordinateSystem.ToUnityVector(pair.Value);
                _mainCanvas.MoveCanvas(pair.Key, position, _uiCamera);
            }
        }
    }

    public void AddUIElement(UIUpdate uiUpdate)
    {
        string canvasKey = uiUpdate.canvas;
        
        AddUIElement(canvasKey, uiUpdate.button);
        AddUIElement(canvasKey, uiUpdate.toggle);
        AddUIElement(canvasKey, uiUpdate.label);
        AddUIElement(canvasKey, uiUpdate.listItem);
    }

    public void AddUIElement(string canvasKey, UIElement uiElement)
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


    public void Update()
    {
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
