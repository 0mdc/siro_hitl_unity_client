using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIElementToggle : UIGameObject<UIToggle>
{
    [SerializeField] public TextMeshProUGUI _textFalse;
    [SerializeField] public TextMeshProUGUI _textTrue;
    [SerializeField] public LayoutElement _layoutTrue;
    [SerializeField] public LayoutElement _layoutFalse;
    [SerializeField] public Sprite _spriteOff;
    [SerializeField] public Sprite _spriteOn;
    [SerializeField] public UnityEngine.UI.Button _button;

    bool _toggled;

    protected override void InitializeUIElement(UIToggle data)
    {
        _button.onClick.AddListener(
            () => { _canvasManager.OnButtonPressed(data.uid); }
        );
    }

    public override void UpdateUIElement(UIToggle data)
    {
        _toggled = data.toggled;
        
        _button.interactable = data.enabled;
        _button.image.sprite = _toggled ? _spriteOn : _spriteOff;
        if (data.color != null)
        {
            _button.image.color = new Color(data.color[0], data.color[1], data.color[2], data.color[3]);
        }
        _textFalse.text = data.textFalse;
        _textTrue.text = data.textTrue;
        _textFalse.fontWeight = _toggled ? FontWeight.Regular : FontWeight.Bold;
        _textTrue.fontWeight = _toggled ? FontWeight.Bold : FontWeight.Regular;

        // Keep the toggle centered.
        float preferredWidth = Mathf.Max(_textFalse.preferredWidth, _textTrue.preferredWidth);
        _layoutFalse.preferredWidth = preferredWidth;
        _layoutTrue.preferredWidth = preferredWidth;
    }

    void Update()
    {
        float preferredWidth = Mathf.Max(_textFalse.preferredWidth, _textTrue.preferredWidth);
        _layoutFalse.preferredWidth = preferredWidth;
        _layoutTrue.preferredWidth = preferredWidth;
    }
}
