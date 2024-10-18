using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIElementButton : UIGameObject<UIButton>
{
    [SerializeField] TextMeshProUGUI _text;
    [SerializeField] UnityEngine.UI.Button _button;

    protected override void InitializeUIElement(UIButton data)
    {
        _button.onClick.AddListener(
            () => { _canvasManager.OnButtonPressed(data.uid); }
        );
    }

    public override void UpdateUIElement(UIButton data)
    {
        _text.text = data.text;
        _button.interactable = data.enabled;        
        if (data.color != null)
        {
            _button.image.color = new Color(data.color[0], data.color[1], data.color[2], data.color[3]);
        }
        else
        {
            _button.image.color = Color.white;
        }
    }
}
