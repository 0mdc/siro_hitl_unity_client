using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "UIPrefabs", menuName = "Habitat/UIPrefabs.prefab", order = 1)]
public class UIPrefabs : ScriptableObject
{
    public Sprite CanvasBackground;
    public UIElementButton ButtonPrefab;
    public UIElementLabel LabelPrefab;
    public UIElementListItem ListItemPrefab;
    public UIElementToggle TogglePrefab;
    public UIElementSeparator SeparatorPrefab;
    public UIElementSpacer SpacerPrefab;
}
