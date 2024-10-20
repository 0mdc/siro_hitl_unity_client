using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
// Message that is sent from the client to the server periodically.
public class ClientState
{
    public AvatarData avatar;
    public ButtonInputData input;
    public MouseInputData mouse;
    public Dictionary<string, string> connectionParamsDict;
    public int? recentServerKeyframeId = null;
    public bool isLoading;
    public UIElements ui;
    public UIElements legacyUi;
}

[Serializable]
// Contains the avatar head and controller poses.
public class AvatarData
{
    public PoseData root = new PoseData();
    public PoseData[] hands = new PoseData[]
    {
        new PoseData(),
        new PoseData()
    };
}

[Serializable]
// Serializable transform.
public class PoseData
{
    public float[] position = new float[3];
    public float[] rotation = new float[4];

    public void FromGameObject(GameObject gameObject)
    {
        position = CoordinateSystem.ToHabitatVector(gameObject.transform.position).ToArray();
        rotation = CoordinateSystem.ToHabitatQuaternion(gameObject.transform.rotation).ToArray();
    }
}

[Serializable]
// Collection of buttons that were held, pressed or released since the last client message.
public class ButtonInputData
{
    public List<int> buttonHeld = new List<int>();
    public List<int> buttonUp = new List<int>();
    public List<int> buttonDown = new List<int>();
}

[Serializable]
// Mouse input.
public class MouseInputData
{
    public ButtonInputData buttons = new ButtonInputData();

    public float[] scrollDelta = new float[2];
    public float[] mousePositionDelta = new float[2];
    public float[] rayOrigin = new float[3];
    public float[] rayDirection = new float[3];
}

[Serializable]
public class UIElements
{
    // Collection of UI buttons that were pressed since the last client message.
    public List<string> buttonsPressed = new();

    // Collection of textboxes and their latest content since the last client message.
    public Dictionary<string, string> textboxes = new();
}
