using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIGameObject : MonoBehaviour {}

public class UIGameObject<T> : UIGameObject where T : UIElement
{
    protected CanvasManager _canvasManager;
    protected string _uid;

    public static UIGameObject<T> Create(UIGameObject<T> prefab, CanvasManager canvasManager, T data)
    {
        var obj = GameObject.Instantiate(prefab);
        obj._canvasManager = canvasManager;
        obj._uid = data.uid;
        obj.InitializeUIElement(data);
        obj.UpdateUIElement(data);
        return obj;
    }

    protected virtual void InitializeUIElement(T data) {}

    public virtual void UpdateUIElement(T data) {}
}