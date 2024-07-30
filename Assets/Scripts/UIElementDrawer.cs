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
        HashSet<string> _clickedButtons = new();
        Dictionary<string, string> _textboxes = new();

        public List<string> ClickedButtons { get { return _clickedButtons.ToList(); } }
        public Dictionary<string, string> Textboxes { get { return _textboxes; } }


        GUIStyle _textAreaStyle = null;

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

            // Sloppy: Only 1 textbox per dialog box
            if (dialog == null || dialog.textbox == null)
            {
                _textboxes.Clear();
            }
            else
            {
                List<string> keysToRemove = new();
                foreach (string key in _textboxes.Keys)
                {
                    if (key != dialog.textbox.id)
                    {
                        keysToRemove.Add(key);
                    }
                }
                foreach (string key in keysToRemove)
                {
                    _textboxes.Remove(key);
                }
            }

            // TODO: Cleanup
            LoadProgressTracker.Instance._modalDialogueShown = dialog != null;
        }

        void OnGUI()
        {
            if (_dialog == null || _camera == null || LoadProgressTracker.Instance.IsLoading || LoadProgressTracker.Instance._applicationTerminated)
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

            Textbox textbox = _dialog.textbox;
            if (textbox != null)
            {
                string textboxId = textbox.id;
                GUI.enabled = textbox.enabled;
                if (!_textboxes.ContainsKey(textboxId)) {
                    _textboxes[textboxId] = textbox.text;
                }
                if (_textAreaStyle == null)
                {
                    _textAreaStyle = new GUIStyle(GUI.skin.GetStyle("TextArea"))
                    {
                        wordWrap = true
                    };
                }
                _textboxes[textboxId] = GUILayout.TextArea(
                    text:_textboxes[textboxId],
                    maxLength:2048,
                    style:_textAreaStyle,
                    options: GUILayout.ExpandHeight(true)
                );
                GUI.enabled = true;
            }
            
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
        _modalDialogue = new GameObject("UIElementDrawer").AddComponent<ModalDialogue>();
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
        _uiElements.textboxes = _modalDialogue.Textboxes;
        state.legacyUi = _uiElements;
    }

    public void OnEndFrame()
    {
        _modalDialogue.Reset();
    }
}
