using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputTrackerMouse : IClientStateProducer
{
    // Left: 0, Right: 1, Middle: 2
    const int MOUSE_BUTTON_COUNT = 3;

    Camera _camera;

    MouseInputData _inputData = new MouseInputData();

    bool[] _buttonHeld;
    bool[] _buttonUp;
    bool[] _buttonDown;

    Vector2 _scrollDelta = Vector2.zero;
    Vector2 _lastMousePosition = Vector3.zero;
    Vector2 _mousePositionDelta = Vector2.zero;

    Ray _mouseRay = new Ray();

    public InputTrackerMouse(Camera camera)
    {
        _camera = camera;

        // Create arrays that hold whether buttons were pressed since the last OnEndFrame() call.
        _buttonHeld = new bool[MOUSE_BUTTON_COUNT];
        _buttonUp = new bool[MOUSE_BUTTON_COUNT];
        _buttonDown = new bool[MOUSE_BUTTON_COUNT];
    }

    public void Update()
    {
        // Don't update if paused.
        if (LoadProgressTracker.Instance.IsApplicationPaused)
        {
            return;
        }

        bool hoveringUI = IsMouseHoveringUI();

        // Record all mouse actions that occurred OnEndFrame() call.
        // Note that multiple Unity frames may occur during that time.
        int mouseIndex = 0;
        if (!hoveringUI)
        {
            for (KeyCode key = KeyCode.Mouse0; key <= KeyCode.Mouse2; key++, mouseIndex++)
            {
                if (Input.GetKey(key))
                {
                    _buttonHeld[mouseIndex] = true;
                }
                if (Input.GetKeyUp(key))
                {
                    _buttonUp[mouseIndex] = true;
                }
                else if (Input.GetKeyDown(key))
                {
                    _buttonDown[mouseIndex] = true;
                }
            }
        }

        _scrollDelta += Input.mouseScrollDelta; // Cumulative

        if (Input.mousePresent)
        {
            _mouseRay = _camera.ScreenPointToRay(Input.mousePosition);

            Vector2 mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            _mousePositionDelta += mousePosition - _lastMousePosition;
            _lastMousePosition = mousePosition;
        }
    }

    /// <summary>
    /// Returns whether the mouse is hovering a UI element.
    /// </summary>
    public bool IsMouseHoveringUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        // TODO: Tech debt: The logic currently only checks for buttons components.
        //       Reconfigure the UI such as only the desired objects are raycastable.
        var pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        List<RaycastResult> results = new();
        eventSystem.RaycastAll(pointerEventData, results);
        bool clickingUI = false;
        foreach (var result in results)
        {
            bool isButton = result.gameObject.GetComponent<UnityEngine.UI.Button>() != null;
            if (isButton)
            {
                clickingUI = true;
                break;
            }
        }
        return clickingUI;
    }

    public void OnEndFrame()
    {
        System.Array.Clear(_buttonHeld, 0, _buttonHeld.Length);
        System.Array.Clear(_buttonUp, 0, _buttonUp.Length);
        System.Array.Clear(_buttonDown, 0, _buttonDown.Length);

        _scrollDelta = Vector2.zero;
        _mousePositionDelta = Vector2.zero;
    }

    public void UpdateClientState(ref ClientState state)
    {
        _inputData.buttons.buttonHeld.Clear();
        _inputData.buttons.buttonUp.Clear();
        _inputData.buttons.buttonDown.Clear();

        if (Input.mousePresent) // TODO: Don't send mouse info if we don't have it
        {
            for (int i = 0; i < MOUSE_BUTTON_COUNT; ++i)
            {
                // Omit buttons that were not used.
                if (_buttonHeld[i]) _inputData.buttons.buttonHeld.Add(i);
                if (_buttonUp[i]) _inputData.buttons.buttonUp.Add(i);
                if (_buttonDown[i]) _inputData.buttons.buttonDown.Add(i);
            }
            _inputData.scrollDelta[0] = _scrollDelta.x;
            _inputData.scrollDelta[1] = _scrollDelta.y;

            _inputData.mousePositionDelta[0] = _mousePositionDelta.x;
            _inputData.mousePositionDelta[1] = -_mousePositionDelta.y;

            _inputData.rayOrigin = CoordinateSystem.ToHabitatVector(_mouseRay.origin).ToArray();
            _inputData.rayDirection = CoordinateSystem.ToHabitatVector(_mouseRay.direction).ToArray();
        }

        state.mouse = _inputData;
    }
}
