using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class UIElementListItem : UIGameObject<UIListItem>
{   
    [SerializeField] TextMeshProUGUI _textLeft;
    [SerializeField] TextMeshProUGUI _textRight;

    protected override void InitializeUIElement(UIListItem data) {}

    public override void UpdateUIElement(UIListItem data)
    {
        _textLeft.text = data.textLeft;
        _textRight.text = data.textRight;
        _textLeft.fontSize = data.fontSize;
        _textRight.fontSize = data.fontSize;

        var color = data.color;
        if (color != null && color.Length == 4)                
        {
            var clr = new Color(color[0], color[1], color[2], color[3]);
            _textLeft.color = clr;
            _textRight.color = clr;
        }
    }
}
