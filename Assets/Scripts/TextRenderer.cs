using System;
using System.Collections.Generic;
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

        GUIStyle fontStyle = null;

        public void OnGUI()
        {
            if (fontStyle == null) {
                fontStyle = new GUIStyle(GUI.skin.GetStyle("Label"))
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 18
                };
            }
            if (texts == null || LoadProgressTracker.Instance.IsLoading)
            {
                return;
            }

            // TODO: Consolidate text system and implement alignment.
            int offset = 0;
            List<Color> passes = new List<Color>{ Color.black, Color.white }; // Hack: Shadow for better display
            foreach (Color color in passes) {
                GUILayout.BeginArea(new Rect(16 + offset, 16 + offset, 500, 500));
                GUILayout.BeginVertical();
                GUI.color = color;
                foreach (var text in texts)
                {
                    GUILayout.Label(text.text, fontStyle);
                }
                GUILayout.EndVertical();
                GUILayout.EndArea();
                offset += -2;
            }
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
}
