using System;
using TMPro;
using UnityEngine;

public class TextRenderer : IKeyframeMessageConsumer
{
    /// <summary>
    /// Simple MonoBehaviour that renders on-screen text for TextRenderer.
    /// </summary>
    // TODO: Handle formatting.
    public class TextRendererGUI : MonoBehaviour
    {
        public TextMessage[] texts = new TextMessage[0];

        public void OnGUI()
        {
            if (texts == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(16, 16, 500, 500));
            GUILayout.BeginVertical();
            GUI.color = Color.white;
            foreach (var text in texts)
            {
                GUILayout.Label(text.text);
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }

    TextRendererGUI _textRenderer;

    public TextRenderer()
    {
        _textRenderer = new GameObject("TextRendererGUI").AddComponent<TextRendererGUI>();
    }

    public void Update() {}

    public void ProcessMessage(Message message)
    {
        _textRenderer.texts = message.texts?.ToArray();
    }

    public void PostProcessMessage(Message message) {}
}
