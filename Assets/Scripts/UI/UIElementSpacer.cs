using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class UIElementSpacer : UIGameObject<UISpacer>
{   
    [SerializeField] LayoutElement _layoutElement;

    protected override void InitializeUIElement(UISpacer data) {}

    public override void UpdateUIElement(UISpacer data)
    {
        _layoutElement.minWidth = data.size;
        _layoutElement.minHeight = data.size;
        _layoutElement.preferredWidth = data.size;
        _layoutElement.preferredHeight = data.size;
    }
}
