using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "Habitat/AppConfig.Config", order = 1)]
public class AppConfig : ScriptableObject
{
    [Header("GuiDrawer")]

    [Tooltip("Size of the line object pool.\nIf the amount of highlights received from the server is higher that this number, the excess will be discarded.\nThis value cannot be changed during runtime.")]
    public int linePoolSize = 256;

    [Tooltip("Size of the circle object pool.\nIf the amount of highlights received from the server is higher that this number, the excess will be discarded.\nThis value cannot be changed during runtime.")]
    public int circlePoolSize = 64;

    [Tooltip("Line segment count composing the highlight circles.\nThis value cannot be changed during runtime.")]
    public int circleResolution = 32;

    [Tooltip("Default vertex color of the highlight lines.")]
    public Color highlightDefaultColor = Color.white;

    [Tooltip("Materials used to shade the highlight circles.")]
    public Material[] highlightMaterials;

    [Tooltip("Thickness of the highlight circles, in meters.")]
    public float highlightWidth = 0.015f;

    [Tooltip("Base radius of the highlight circles, in meters. It is multiplied by the radius received from server messages.")]
    public float highlightBaseRadius = 1.0f;
}