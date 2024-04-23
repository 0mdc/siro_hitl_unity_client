using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Singleton that tracks assets that are currently being loaded.
/// Provides global loading events and statistics. 
/// </summary>
public class LoadProgressTracker
{
    static LoadProgressTracker _instance = null;
    HashSet<GfxReplayInstance> _loadingInstances = new();
    float _progress = 1.0f;
    uint _successCount = 0;
    uint _failureCount = 0;

    // TODO: Cleanup.
    public bool _modalDialogueShown = false;

    /// <summary>
    /// Get the LoadProgressTracker singleton.
    /// </summary>
    public static LoadProgressTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new LoadProgressTracker();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Event that is emitted when the first asset is being loaded.
    /// </summary>
    public event System.Action OnLoadStarted;

    /// <summary>
    /// Event that is emitted when the last asset has been loaded (or failed to load).
    /// </summary>
    public event System.Action OnLoadFinished;

    /// <summary>
    /// Estimate the current loading progress.
    /// </summary>
    /// <returns>A value between 0 and 1.</returns>
    public float EstimateProgress()
    {
        if (_loadingInstances.Count == 0)
        {
            _progress = 1.0f;
        }
        else
        {
            _progress = 0.0f;
            foreach (var instance in _loadingInstances)
            {
                _progress += instance.LoadingProgress;
            }
            _progress /= _loadingInstances.Count;
        }
        return _progress;
    }

    /// <summary>
    /// Whether the client is paused. True if loading or modal dialogue shown.
    /// TODO: Either move out or rename this class.
    /// </summary>
    public bool IsApplicationPaused { get
    {
        return IsLoading || _modalDialogueShown;
    }}

    /// <summary>
    /// Whether at least one asset is being loaded.
    /// </summary>
    public bool IsLoading { get { return _loadingInstances.Count > 0; }}

    /// <summary>
    /// Number of assets that are being loaded.
    /// </summary>
    public int LoadingInstanceCount { get { return _loadingInstances.Count; }}

    /// <summary>
    /// Number of assets that were successfully loaded since starting the client.
    /// </summary>
    public uint SuccessCount { get { return _successCount; }}

    /// <summary>
    /// Number of assets that were unsuccessfully loaded since starting the client.
    /// </summary>
    public uint FailureCount { get { return _failureCount; }}

    /// <summary>
    /// Monitor an unloaded GfxReplayInstance.
    /// </summary>
    public void TrackInstance(GfxReplayInstance instance)
    {
        instance.OnLoadStarted += LoadStarted;
        instance.OnLoadSucceeded += LoadSucceeded;
        instance.OnLoadFailed += LoadFailed;
        instance.OnInstanceDestroyed += InstanceDestroyed;
    }

    void UntrackInstance(GfxReplayInstance instance)
    {
        instance.OnLoadStarted -= LoadStarted;
        instance.OnLoadSucceeded -= LoadSucceeded;
        instance.OnLoadFailed -= LoadFailed;
        instance.OnInstanceDestroyed -= InstanceDestroyed;
    }

    void LoadStarted(GfxReplayInstance sender)
    {
        AddInstance(sender);
    }

    void LoadSucceeded(GfxReplayInstance sender)
    {
        ++_successCount;
        RemoveInstance(sender);
    }

    void LoadFailed(GfxReplayInstance sender)
    {
        ++_failureCount;
        RemoveInstance(sender);
    }

    void InstanceDestroyed(GfxReplayInstance sender)
    {
        RemoveInstance(sender);
    }

    void AddInstance(GfxReplayInstance instance)
    {
        if (_loadingInstances.Count == 0)
        {
            OnLoadStarted?.Invoke();
        }
        _loadingInstances.Add(instance);
    }

    void RemoveInstance(GfxReplayInstance instance)
    {
        int previousCount = _loadingInstances.Count;
        if (_loadingInstances.Contains(instance))
        {
            _loadingInstances.Remove(instance);
        }
        if (previousCount == 1 && _loadingInstances.Count == 0)
        {
            OnLoadFinished?.Invoke();
        }
        UntrackInstance(instance);
    }
}
