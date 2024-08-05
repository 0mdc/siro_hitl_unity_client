using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityFx.Outline;

public class GfxReplayPlayer : IUpdatable
{
    public struct MovementData
    {
        public Vector3 startPosition;    // Starting position of the interpolation
        public Quaternion startRotation; // Starting rotation of the interpolation

        public Vector3 endPosition;      // Target position of the interpolation
        public Quaternion endRotation;   // Target rotation of the interpolation

        public float startTime;          // Time when this movement data was created or updated
    }

    const bool USE_KEYFRAME_INTERPOLATION = false;

    private Dictionary<int, GfxReplayInstance> _instanceDictionary = new();
    private Dictionary<int, int> _objectIdToInstanceKey = new();
    private Dictionary<string, Load> _loadDictionary = new();
    private Dictionary<int, MovementData> _movementData = new();
    Dictionary<int, GfxReplayInstance> _skinnedMeshes = new();
    CoroutineContainer _coroutines;
    IKeyframeMessageConsumer[] _messageConsumers;
    private float _keyframeInterval = 0.1f;  // assume 10Hz, but see also SetKeyframeRate
    UnityFx.Outline.OutlineLayerCollection _outlineLayers;

    public GfxReplayPlayer(IKeyframeMessageConsumer[] messageConsumers, UnityFx.Outline.OutlineLayerCollection outlineLayers = null)
    {
        _messageConsumers = messageConsumers;
        _coroutines = CoroutineContainer.Create("GfxReplayPlayer");
        _outlineLayers = outlineLayers;
    }

    public void SetKeyframeRate(float rate)
    {
        Assert.IsTrue(rate > 0.0F);
        float adjustedRate = Mathf.Clamp(rate, 10, 30);
        _keyframeInterval = 1.0F / adjustedRate;
    }

    private void ProcessStateUpdatesImmediate(KeyframeData keyframe)
    {
        // Handle State Updates
        if (keyframe.stateUpdates != null)
        {
            foreach (var update in keyframe.stateUpdates)
            {
                if (_instanceDictionary.ContainsKey(update.instanceKey))
                {
                    Transform instance = _instanceDictionary[update.instanceKey].transform;

                    Vector3 translation = CoordinateSystem.ToUnityVector(update.state.absTransform.translation);
                    Quaternion rotation = CoordinateSystem.ToUnityQuaternion(update.state.absTransform.rotation);

                    instance.position = translation;
                    instance.rotation = rotation;
                }
            }
        }
    }

    private void ProcessStateUpdatesForInterpolation(KeyframeData keyframe)
    {
        if (keyframe.stateUpdates != null)
        {
            foreach (var update in keyframe.stateUpdates)
            {
                if (_instanceDictionary.ContainsKey(update.instanceKey))
                {
                    GameObject instance = _instanceDictionary[update.instanceKey].gameObject;

                    Vector3 newTranslation = CoordinateSystem.ToUnityVector(update.state.absTransform.translation);
                    Quaternion newRotation = CoordinateSystem.ToUnityQuaternion(update.state.absTransform.rotation);

                    // Check if the instance is at the origin
                    if (instance.transform.position == Vector3.zero)
                    {
                        // Snap to the new position and rotation
                        instance.transform.position = newTranslation;
                        instance.transform.rotation = newRotation;
                    }
                    else
                    {
                        // Set up the interpolation
                        if (!_movementData.ContainsKey(update.instanceKey))
                        {
                            _movementData[update.instanceKey] = new MovementData
                            {
                                startPosition = instance.transform.position,
                                startRotation = instance.transform.rotation,
                                endPosition = newTranslation,
                                endRotation = newRotation,
                                startTime = Time.time
                            };
                        }
                        else
                        {
                            // Update the existing MovementData
                            var data = _movementData[update.instanceKey];
                            data.startPosition = instance.transform.position;
                            data.startRotation = instance.transform.rotation;
                            data.endPosition = newTranslation;
                            data.endRotation = newRotation;
                            data.startTime = Time.time;

                            _movementData[update.instanceKey] = data;
                        }
                    }
                }
            }
        }
    }

    private void ProcessStateUpdates(KeyframeData keyframe)
    {
        if (USE_KEYFRAME_INTERPOLATION)
        {
            ProcessStateUpdatesForInterpolation(keyframe);
        }
        // Suppress 'Unreachable Code' warning.
        #pragma warning disable 0162 
        else
        {
            ProcessStateUpdatesImmediate(keyframe);
        }
        #pragma warning restore 0162

    }

    private void UpdateForInterpolatedStateUpdates()
    {
        // Use a list to keep track of keys to remove after processing
        List<int> keysToRemove = new List<int>();

        foreach (var kvp in _instanceDictionary)
        {
            int instanceKey = kvp.Key;
            Transform instance = kvp.Value.transform;

            if (_movementData.ContainsKey(instanceKey))
            {
                var data = _movementData[instanceKey];
                float t = (Time.time - data.startTime) / _keyframeInterval;

                if (t < 1.0f)
                {
                    instance.position = Vector3.Lerp(data.startPosition, data.endPosition, t);
                    instance.rotation = Quaternion.Slerp(data.startRotation, data.endRotation, t);
                }
                else
                {
                    instance.position = data.endPosition;
                    instance.rotation = data.endRotation;

                    // Mark this key for removal
                    keysToRemove.Add(instanceKey);
                }
            }
        }

        // Remove processed keys from _movementData
        foreach (var key in keysToRemove)
        {
            _movementData.Remove(key);
        }

    }

    public void Update()
    {
        foreach (var messageConsumer in _messageConsumers)
        {
            messageConsumer.Update();
        }

        if (USE_KEYFRAME_INTERPOLATION)
        {
            UpdateForInterpolatedStateUpdates();
        }
    }

    public void ProcessKeyframe(KeyframeData keyframe)
    {
        // Handle messages
        if (keyframe.message != null)
        {
            foreach (var messageConsumer in _messageConsumers)
            {
                messageConsumer.ProcessMessage(keyframe.message);
            }
        }

        // Handle Loads
        if (keyframe.loads != null)
        {
            foreach (var load in keyframe.loads)
            {
                _loadDictionary[load.filepath] = load;
            }
        }

        // Handle Creations
        if (keyframe.creations != null)
        {
            foreach (var creationItem in keyframe.creations)
            {
                var source = creationItem.creation.filepath;
                if (!_loadDictionary.TryGetValue(source, out Load load))
                {
                    Debug.LogError("Unable to find loads entry for " + source);
                    continue;
                }

                var instance = GfxReplayInstance.LoadAndInstantiate(
                    creationItem.instanceKey.ToString(),
                    load,
                    creationItem.creation
                );

                int rigId = instance.rigId;
                if (rigId != Constants.ID_UNDEFINED)
                {
                    _skinnedMeshes[rigId] = instance;
                }

                _instanceDictionary[creationItem.instanceKey] = instance;
            }
        }

        // Handle Deletions
        if (keyframe.deletions != null)
        {
            foreach (var key in keyframe.deletions)
            {
                if (_instanceDictionary.TryGetValue(key, out GfxReplayInstance instance))
                {
                    if (instance.rigId != Constants.ID_UNDEFINED)
                    {
                        _skinnedMeshes.Remove(instance.rigId);
                    }

                    if (_objectIdToInstanceKey.ContainsKey(instance.objectId))
                    {
                        _objectIdToInstanceKey.Remove(instance.objectId);
                    }

                    _instanceDictionary[key].Destroy();
                    _instanceDictionary.Remove(key);
                }
            }
            _coroutines.StartCoroutine(ReleaseUnusedMemory());
        }

        if (keyframe.metadata != null)
        {
            foreach (var metadata in keyframe.metadata)
            {
                int key = metadata.instanceKey;
                if (_instanceDictionary.TryGetValue(key, out GfxReplayInstance instance))
                {
                    instance.ProcessMetadata(metadata.metadata);
                    _objectIdToInstanceKey[instance.objectId] = key;
                }
            }
        }

        if (keyframe.rigCreations != null)
        {
            foreach (var rigCreation in keyframe.rigCreations)
            {
                int rigId = rigCreation.id;
                if (_skinnedMeshes.TryGetValue(rigId, out GfxReplayInstance instance))
                {
                    instance.ProcessRigCreation(rigCreation);
                }
            }
        }

        if (keyframe.rigUpdates != null)
        {
            foreach (var rigUpdate in keyframe.rigUpdates)
            {
                int rigId = rigUpdate.id;
                if (_skinnedMeshes.TryGetValue(rigId, out GfxReplayInstance instance))
                {
                    instance.ProcessRigUpdate(rigUpdate);
                }
            }
        }

        ProcessStateUpdates(keyframe);

        // Process object properties.
        if (keyframe.message?.objects != null)
        {
            foreach (var pair in keyframe.message.objects)
            {
                int objectId = pair.Key;
                ObjectProperties properties = pair.Value;
                if (_objectIdToInstanceKey.TryGetValue(objectId, out int instanceKey))
                {
                    if (_instanceDictionary.TryGetValue(instanceKey, out GfxReplayInstance instance))
                    {
                        if (properties.visible.HasValue)
                        {
                            instance.SetVisibility(properties.visible.Value);
                        }
                        if (properties.layer.HasValue)
                        {
                            // Convention:
                            // -1: Default layer (0).
                            // 0-7: Layers 8-15.
                            int inputLayer = properties.layer.Value;
                            int unityLayer = 0;
                            if (inputLayer > -1 && inputLayer < ViewportHandler.LAYER_COUNT)
                            {
                                unityLayer = inputLayer + ViewportHandler.FIRST_LAYER_INDEX;
                            }
                            instance.SetLayer(unityLayer);
                        }                        
                    }
                    else
                    {
                        Debug.Log($"Instance with object ID {pair.Key} not found.");
                    }
                }
            }
        }

        // Process object outlines.
        _outlineLayers.Clear();
        if (keyframe.message?.outlines != null)
        {
            var outlines = keyframe.message.outlines;
            outlines.Sort((x, y) => x.priority.CompareTo(y.priority));
            foreach (var outline in outlines)
            {
                OutlineLayer layer = _outlineLayers.AddLayer();
                var color = outline.color;
                layer.OutlineColor = new Color(color[0], color[1], color[2], color[3]);
                layer.OutlineWidth = (int)outline.width;
                layer.MergeLayerObjects = true;
                layer.OutlineRenderMode = OutlineRenderFlags.Blurred;
                
                foreach (int objectId in outline.objectIds)
                {
                    if (_objectIdToInstanceKey.TryGetValue(objectId, out int key))
                    {
                        GameObject obj = _instanceDictionary[key].gameObject;
                        layer.Add(obj);
                    }
                }
            }
        }
    }

    public void DeleteAllInstancesFromKeyframes()
    {
        foreach (var kvp in _instanceDictionary)
        {
            kvp.Value.Destroy();
        }
        _coroutines.StartCoroutine(ReleaseUnusedMemory());
        Debug.Log($"Deleted all {_instanceDictionary.Count} instances!");
        _instanceDictionary.Clear();
        _objectIdToInstanceKey.Clear();
    }

    /// <summary>
    /// Unloads the unused resources from GPU and CPU memory.
    /// This is normally done automatically when changing scene.
    /// We must call this manually to avoid leaks because we are never changing scene.
    /// This is a slow operation - use the callback to execute code after this is done.
    /// </summary>
    /// <param name="callback">Code to execute when this is done.</param>
    /// <returns></returns>
    IEnumerator ReleaseUnusedMemory(Action callback = null)
    {
        // Wait for objects to be destroyed.
        yield return new WaitForEndOfFrame();

        // Unload unused assets.
        var asyncOp = Resources.UnloadUnusedAssets();

        // Wait for the operation to be done.
        while (!asyncOp.isDone)
        {
            yield return null;
        }

        // Invoke callback.
        callback?.Invoke();
    }
}
