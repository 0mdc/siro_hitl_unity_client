using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class UIElementLabel : UIGameObject<UILabel>
{   
    [SerializeField] TextMeshProUGUI _text;

    protected override void InitializeUIElement(UILabel data) {}

    public override void UpdateUIElement(UILabel data)
    {
        _text.text = data.text;
        switch (data.horizontalAlignment)
        {
            case 0:
                _text.alignment = TextAlignmentOptions.Left;
                break;
            case 1:
                _text.alignment = TextAlignmentOptions.Center;
                break;
            case 2:
                _text.alignment = TextAlignmentOptions.Right;
                break;
            default:
                _text.alignment = TextAlignmentOptions.Left;
                break;
        }
        var color = data.color;
        if (color != null && color.Length == 4)                
        {
            _text.color = new Color(color[0], color[1], color[2], color[3]);
        }
        _text.fontSize = data.fontSize;
        _text.fontWeight = data.bold ? FontWeight.Bold : FontWeight.Regular;
    }
}
