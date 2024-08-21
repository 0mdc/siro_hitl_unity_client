using System;
using System.Collections.Generic;

public static class Constants
{
    public const int ID_UNDEFINED = -1;
}

[Serializable]
public class AbsTransform
{
    public List<float> translation;
    public List<float> rotation;
}

[Serializable]
public class KeyframeWrapper
{
    public KeyframeData[] keyframes;
}

[Serializable]
public class KeyframeData
{
    public Load[] loads;
    public RigCreation[] rigCreations;
    public CreationItem[] creations;
    public InstanceMetadataItem[] metadata;
    public StateUpdateItem[] stateUpdates;
    public RigUpdate[] rigUpdates;
    public int[] deletions;
    public Message message;
}

[Serializable]
public class Load
{
    public int type;
    public string filepath;
    public Frame frame;
    // public float virtualUnitToMeters;
    // public bool forceFlatShading;
    // public bool splitInstanceMesh;
    // public string shaderTypeToUse;
    // public bool hasSemanticTextures;
}

[Serializable]
public class Frame
{
    public float[] up;
    public float[] front;
    public float[] origin;
}

[Serializable]
public class CreationItem
{
    public int instanceKey;
    public Creation creation;
}

[Serializable]
public class Creation
{
    public string filepath;
    public float[] scale;
    public int rigId;
}

[Serializable]
public class StateUpdateItem
{
    public int instanceKey;
    public StateUpdate state;
}

[Serializable]
public class StateUpdate
{
    public AbsTransform absTransform;
}

[Serializable]
public class InstanceMetadataItem
{
    public int instanceKey;
    public InstanceMetadata metadata;
}

[Serializable]
public class InstanceMetadata
{
    public int objectId;
    public int semanticId;
}

[Serializable]
public class RigCreation
{
    public int id;
    public List<string> boneNames;
}

[Serializable]
public class RigUpdate
{
    [Serializable]
    public class BoneTransform
    {
        public List<float> t;
        public List<float> r;
    }

    public int id;
    public List<BoneTransform> pose;
}

[Serializable]
public class Message
{
    public Circle[] circles;
    public Line[] lines;
    public List<float> teleportAvatarBasePosition;
    public bool sceneChanged;
    // nonindexed triangle list, serialized as a flat list of floats
    public List<float> navmeshVertices;
    public List<TextMessage> texts;
    public AbsTransform camera;
    public int serverKeyframeId;
    public Dictionary<int, ObjectProperties> objects;
    public Dictionary<int, ViewportProperties> viewports;
    public List<ObjectOutline> outlines;
    public List<int> selectedObjects;
    public Dialog dialog;
}

[Serializable]
public class Circle
{
    // Note: short variable names and values to reduce json data size.
    public float[] t; // Position
    public float r; // Radius
    public float[] n; // Normal
    public int b = 0; // 0 = Face normal, 1 = Billboard (face camera)
    public int[] c; // Color, RGBA, 0-255
}

[Serializable]
public class Line
{
    // Note: short variable names and values to reduce json data size.
    public float[] a; // Position
    public float[] b; // Position
    public int[] c; // Color, RGBA, 0-255
}

[Serializable]
public class ObjectOutline
{
    public int priority;
    public float[] color;
    public float width;
    public List<int> objectIds;
}

[Serializable]
public class TextMessage
{
    public string text;
    public List<float> position;
}

[Serializable]
public class ObjectProperties
{
    public bool? visible;
    public int? layer;
}

[Serializable]
public class ViewportProperties
{
    public bool? enabled;
    public int[] layers;
    public float[] rect;
    public AbsTransform camera;
}

[Serializable]
public class Dialog
{
    public string title;
    public string text;
    public Button[] buttons;
    public Textbox textbox;
}

[Serializable]
public class Button
{
    public string id;
    public string text;
    public bool enabled;
}

[Serializable]
public class Textbox
{
    public string id;
    public string text;
    public bool enabled;
}