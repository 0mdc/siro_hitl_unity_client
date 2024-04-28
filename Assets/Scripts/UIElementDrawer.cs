using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIElementDrawer : IKeyframeMessageConsumer, IClientStateProducer
{
    /// <summary>
    /// Modal dialogue with buttons.
    /// </summary>
    class ModalDialogue : MonoBehaviour
    {
        Camera _camera = null;
        Dialog _dialog = null;
        HashSet<string> _clickedButtons = new HashSet<string>();

        public List<string> ClickedButtons { get { return _clickedButtons.ToList(); } }

        public void Reset()
        {
            _clickedButtons.Clear();
        }

        public void Initialize(Camera camera)
        {
            _camera = camera;
        }

        public void UpdateState(Dialog dialog)
        {
            _dialog = dialog;

            // TODO: Cleanup
            LoadProgressTracker.Instance._modalDialogueShown = dialog != null;
        }

        void OnGUI()
        {
            if (_dialog == null || _camera == null || LoadProgressTracker.Instance.IsLoading)
            {
                return;
            }

            // TODO: Size
            int width = _camera.pixelWidth;
            int height = _camera.pixelHeight;
            int windowWidth = width / 3;
            int windowHeight = width / 5;
            int windowXOffset = windowWidth - (windowWidth / 2);
            int windowYOffset = windowHeight - (windowHeight / 2);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUI.color = Color.white;
            GUILayout.Window(
                9, // TODO
                new Rect(16, 16, 0, 0),
                WindowUpdate,
                _dialog.title,
                GUILayout.Width(256)
            );
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        void WindowUpdate(int windowId)
        {
            GUILayout.BeginVertical();
            GUI.color = Color.white;
            GUILayout.Label(_dialog.text);
            
            GUILayout.BeginHorizontal();
            foreach (var button in _dialog.buttons)
            {
                GUI.enabled = button.enabled;
                bool clicked = GUILayout.Button(button.text);
                if (clicked)
                {
                    _clickedButtons.Add(button.id);
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }

    UIElements _uiElements = new UIElements();

    ModalDialogue _modalDialogue;

    public UIElementDrawer(Camera camera)
    {
        _modalDialogue = new GameObject("LoadingScreenOverlay").AddComponent<ModalDialogue>();
        _modalDialogue.Initialize(camera);
    }

    public void ProcessMessage(Message message)
    {
        Dialog dialog = message.dialog;
        _modalDialogue.UpdateState(dialog);
    }

    void IUpdatable.Update() {}

    public void UpdateClientState(ref ClientState state)
    {
        _uiElements.buttonsPressed = _modalDialogue.ClickedButtons;
        state.ui = _uiElements;
    }

    public void OnEndFrame()
    {
        _modalDialogue.Reset();
    }
}
