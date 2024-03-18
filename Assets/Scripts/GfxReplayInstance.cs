using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a Habitat object instance from gfx-replay.
/// Acts as a placeholder when the object is loading or has failed to load.
/// </summary>
public class GfxReplayInstance : MonoBehaviour
{
    /// <summary>
    /// Gfx-replay rigId associated with this instance.
    /// </summary>
    public int rigId {get; private set;} = Constants.ID_UNDEFINED;

    /// <summary>
    /// Gfx-replay skinned mesh associated with this instance.
    /// </summary>
    public GfxReplaySkinnedMesh skinnedMesh {get; private set;} = null;

    /// <summary>
    /// Instantiates a 'GfxReplayInstance' object from the supplied address.
    /// If the object is not yet loaded, launches an asynchronous loading job and returns a placeholder.
    /// </summary>
    /// <param name="objectName">Name of the GameObject (visible from the Editor in Play mode).</param>
    /// <param name="loadInfo">Gfx-replay load structure. See Keyframe.cs.</param>
    /// <param name="creationInfo">Gfx-replay creation structure. See Keyframe.cs.</param>
    /// <returns>Object instance.</returns>
    public static GfxReplayInstance LoadAndInstantiate(string objectName, Load loadInfo, Creation creationInfo)
    {
        var newInstance = new GameObject(objectName).AddComponent<GfxReplayInstance>();
        string address = PathUtils.HabitatPathToUnityAddress(creationInfo.filepath);
        newInstance.Load(address, loadInfo, creationInfo);
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
    /// Destroy the GameObject as well as all of its children.
    /// </summary>
    public void Destroy()
    {
        // OnDisable() will be called by the engine after this.
        Destroy(this.gameObject);
    }

    // TODO: Optimization: Skip the offset node. Instead, bake the transform into the instance root node.
    GameObject CreateOffsetNode(Frame frame)
    {
        GameObject offsetNode = new GameObject("Offset");
        offsetNode.transform.localRotation = CoordinateSystem.ComputeFrameRotationOffset(frame);
        return offsetNode;
    }


    void Load(string address, Load loadInfo, Creation creationInfo)
    {
        rigId = creationInfo.rigId;
        if (rigId != Constants.ID_UNDEFINED)
        {
            skinnedMesh = new GfxReplaySkinnedMesh(this);
        }

        // TODO: Asynchronous resource loading.
        GameObject prefab = Resources.Load<GameObject>(address);

        if (prefab == null)
        {
            Debug.LogError($"Unable to load GameObject for '{address}'.");
            return;
        }

        PostLoad(prefab, loadInfo, creationInfo);
    }

    void PostLoad(GameObject prefab, Load loadInfo, Creation creationInfo)
    {
        // Create offset node, which handles 'load.frame' and 'creation.scale'.
        GameObject offsetNode = CreateOffsetNode(loadInfo.frame);
        if (creationInfo.scale != null)
        {
            offsetNode.transform.localScale = creationInfo.scale.ToVector3();
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
    }

    // MonoBehaviour function that is called when the object is destroyed.
    private void OnDisable()
    {
        // TODO: Stop asynchronous loading.
    }
}
