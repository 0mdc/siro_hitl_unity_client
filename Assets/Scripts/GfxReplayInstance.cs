using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// Represents a Habitat object instance from gfx-replay.
/// Acts as a placeholder when the object is loading or has failed to load.
/// </summary>
public class GfxReplayInstance : MonoBehaviour
{
    private const int LOAD_RETRY_COUNT = 5;
    private const float INCREMENTAL_WAIT_TIME_PER_RETRY = 1.0f;
    private GameObject _prefab;
    private Coroutine _loadingCoroutine;
    private bool _visible = true;
    private int _layer = 0;

    /// <summary>
    /// Gfx-replay rigId associated with this instance.
    /// </summary>
    public int rigId {get; private set;} = Constants.ID_UNDEFINED;

    public int objectId {get; private set;} = Constants.ID_UNDEFINED;

    public int semanticId {get; private set;} = Constants.ID_UNDEFINED;

    /// <summary>
    /// Gfx-replay skinned mesh associated with this instance.
    /// </summary>
    public GfxReplaySkinnedMesh skinnedMesh {get; private set;} = null;

    /// <summary>
    /// Loading progress. 0 when not loaded, 1 when fully loaded.
    /// </summary>
    public float LoadingProgress {get; private set;} = 0.0f;

    public event System.Action<GfxReplayInstance> OnLoadStarted;
    public event System.Action<GfxReplayInstance> OnLoadSucceeded;
    public event System.Action<GfxReplayInstance> OnLoadFailed;
    public event System.Action<GfxReplayInstance> OnInstanceDestroyed;

    /// <summary>
    /// Instantiates a 'GfxReplayInstance' object from the supplied address.
    /// If the object is not yet loaded, launches an asynchronous loading job and returns a placeholder.
    /// </summary>
    /// <param name="objectName">Name of the GameObject (visible from the Editor in Play mode).</param>
    /// <param name="loadInfo">Gfx-replay load structure. See Keyframe.cs.</param>
    /// <param name="creationInfo">Gfx-replay creation structure. See Keyframe.cs.</param>
    /// <returns>Object instance.</returns>
    public static GfxReplayInstance LoadAndInstantiate(string objectName, Load loadInfo, Creation creation)
    {
        var newInstance = new GameObject(objectName).AddComponent<GfxReplayInstance>();
        LoadProgressTracker.Instance.TrackInstance(newInstance);
        string address = PathUtils.HabitatPathToUnityAddress(creation.filepath);
        newInstance.Load(address, loadInfo, creation);
        newInstance.rigId = creation.rigId;
        return newInstance;
    }

    /// <summary>
    /// Process a 'RigCreation' keyframe.
    /// </summary>
    public void ProcessRigCreation(RigCreation rigCreation)
    {
        if (skinnedMesh != null)
        {
            skinnedMesh?.ProcessRigCreation(rigCreation);
        }
    }

    /// <summary>
    /// Process a 'RigUpdate' keyframe.
    /// </summary>
    public void ProcessRigUpdate(RigUpdate rigUpdate)
    {
        if (skinnedMesh != null)
        {
            skinnedMesh?.ProcessRigUpdate(rigUpdate);
        }
    }

    /// <summary>
    /// Process a 'Metadata' keyframe.
    /// </summary>
    public void ProcessMetadata(InstanceMetadata metadata)
    {
        objectId = metadata.objectId;
        semanticId = metadata.semanticId;
    }

    /// <summary>
    /// Destroy the GameObject as well as all of its children.
    /// </summary>
    public void Destroy()
    {
        // OnDisable() will be called by the engine after this.
        Destroy(this.gameObject);
    }

    public void SetVisibility(bool visible)
    {
        _visible = visible;
        ApplyObjectProperties();
    }

    public void SetLayer(int layer)
    {
        _layer = layer;
        ApplyObjectProperties();
    }

    private void ApplyObjectProperties()
    {
        var renderers = transform.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = _visible;
            renderer.gameObject.layer = _layer;
        }
    }

    // TODO: Optimization: Skip the offset node. Instead, bake the transform into the instance root node.
    GameObject CreateOffsetNode(Frame frame)
    {
        GameObject offsetNode = new GameObject("Offset");
        offsetNode.transform.localRotation = CoordinateSystem.ComputeFrameRotationOffset(frame);
        return offsetNode;
    }

    void Load(string address, Load loadInfo, Creation creation)
    {
        _loadingCoroutine = StartCoroutine(LoadAsync(address, loadInfo, creation));
    }

    IEnumerator LoadAsync(string address, Load loadInfo, Creation creation)
    {
        // Create skinned mesh placeholder.
        rigId = creation.rigId;
        if (rigId != Constants.ID_UNDEFINED)
        {
            skinnedMesh = new GfxReplaySkinnedMesh(this);
        }

        // Check if key exists
        var keyExistsHandle = Addressables.LoadResourceLocationsAsync(address);
        yield return keyExistsHandle;
        if (keyExistsHandle.Result.Count > 0)
        {
            OnLoadStarted?.Invoke(this);
            int retryCount = 0;
            do 
            {
                var loadHandle = Addressables.LoadAssetAsync<GameObject>(address);
                while (!loadHandle.IsDone)
                {
                    LoadingProgress = loadHandle.PercentComplete;
                    yield return null;
                }
                if (loadHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    _prefab = loadHandle.Result;

                    // Finish object setup.
                    LoadingProgress = 1.0f;
                    OnLoadSucceeded?.Invoke(this);
                    PostLoad(_prefab, loadInfo, creation);
                    yield break;
                }
                else
                {
                    float retryWaitTime = ++retryCount * INCREMENTAL_WAIT_TIME_PER_RETRY;
                    Debug.LogError($"Unable to load GameObject for '{address}'. Retrying in {string.Format("{0:F1}", retryWaitTime)}s.");
                    yield return new WaitForSecondsRealtime(INCREMENTAL_WAIT_TIME_PER_RETRY * ++retryCount);
                }
            } while (_prefab == null && retryCount <= LOAD_RETRY_COUNT);
         
            Debug.LogError($"Unable to load '{address}' after {LOAD_RETRY_COUNT} attempts. Deactivating asset.");
            OnLoadFailed?.Invoke(this);
            yield break;
        }
        else
        {
            Debug.LogError($"Addressable key does not exist: '{address}'.");

            if (address.Contains("data/fpss/objects/"))
            {
                Debug.LogWarning($"Attempting to load {address} from objects_ovmm.");
                address = System.IO.Path.Join("data/objects_ovmm/train_val/hssd/assets/objects/", address.Split('/').Last());
                yield return LoadAsync(address, loadInfo, creation);
            }
            yield break;
        }
    }

    void PostLoad(GameObject prefab, Load loadInfo, Creation creation)
    {
        // Create offset node, which handles 'load.frame' and 'creation.scale'.
        GameObject offsetNode = CreateOffsetNode(loadInfo.frame);
        if (creation.scale != null)
        {
            offsetNode.transform.localScale = creation.scale.ToVector3();
        }
        offsetNode.transform.SetParent(transform, worldPositionStays: false);

        // Instantiate the loaded object.
        GameObject instance = Instantiate(prefab);
        instance.transform.SetParent(offsetNode.transform, worldPositionStays: false);

        // Initialize the skinned mesh.
        if (skinnedMesh != null)
        {
            var skinnedMeshRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
            {
                skinnedMesh.Initialize(instance.GetComponentInChildren<SkinnedMeshRenderer>());
            }
        }

        // Hack: Refresh materials in the Editor.
        //       If testing with real assets, shaders may be compiled for Android or WebGL.
        //       This re-generates them for the Editor to avoid pink materials.
        #if UNITY_EDITOR
            var renderers = transform.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var material in renderer.materials)
                {
                    material.shader = Shader.Find(material.shader.name);
                }
            }
        #endif

        // (Re)apply object properties after loading.
        ApplyObjectProperties();
    }

    // MonoBehaviour function that is called when the object is destroyed.
    private void OnDisable()
    {
        OnInstanceDestroyed?.Invoke(this);
        // Stop loading.
        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            LoadingProgress = 0.0f;
        }
        // Decrement refcount.
        if (_prefab != null)
        {
            Addressables.Release(_prefab);
        }
    }
}
