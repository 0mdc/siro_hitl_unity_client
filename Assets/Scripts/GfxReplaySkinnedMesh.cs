using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Allows to control a skinned/rigged mesh from gfx-replay "RigCreation" and "RigUpdate" keyframes.
/// Acts as a placeholder until 'Initialize' is called, and until the first 'RigCreation' is received.
/// </summary>
public class GfxReplaySkinnedMesh
{
    private Transform[] _bones;

    SkinnedMeshRenderer _skinnedMeshRenderer;
    GfxReplayInstance _parent;
    bool _initialized = false;
    List<string> _boneNames;
    RigUpdate _lastRigUpdate;

    public GfxReplaySkinnedMesh(GfxReplayInstance parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Initialize the skinned mesh.
    /// To be called after the skinned mesh has been loaded.
    /// </summary>
    public void Initialize(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        if (skinnedMeshRenderer == null)
        {
            return;
        }
        _skinnedMeshRenderer = skinnedMeshRenderer;

        // Hide the skinned mesh until it is configured (i.e. until RigCreation is received).
        _skinnedMeshRenderer.enabled = false;

        // We don't update the bounding box every update manually.
        _skinnedMeshRenderer.updateWhenOffscreen = true;

        // If we already received bone names from gfx-replay, start mapping.
        if (_boneNames != null)
        {
            _initialized = mapGfxReplayBonesToSkinnedMesh(_boneNames, _skinnedMeshRenderer);
        }
    }

    public void ProcessRigCreation(RigCreation rigCreation)
    {
        _boneNames = rigCreation.boneNames;

        // If we already initialized the skinned mesh, start mapping.
        if (_skinnedMeshRenderer != null)
        {
            _initialized = mapGfxReplayBonesToSkinnedMesh(_boneNames, _skinnedMeshRenderer);
        }
    }

    public void ProcessRigUpdate(RigUpdate rigUpdate) {
        // If the skinned mesh is not yet initialized, remember the pose so that it can be applied after loading.
        if (!_initialized)
        {
            _lastRigUpdate = rigUpdate;
            return;
        }

        var pose = rigUpdate.pose;
        Assert.IsNotNull(_bones);
        Assert.AreEqual(pose.Count, _bones.Length);

        for (int i = 0; i < pose.Count; ++i)
        {
            _bones[i].position = CoordinateSystem.ToUnityVector(pose[i].t);
            // TODO: The bones are offset by 180 degrees around the Y-axis.
            _bones[i].rotation = CoordinateSystem.ToUnityQuaternion(pose[i].r) * Quaternion.Euler(0, 180, 0);
        }
    }

    /// <summary>
    /// Map gfx-replay bone names to skinned mesh bone names so that 'RigUpdate' indices can be matched to a bone.
    /// For mapping to succeed, the following criteria have to be met:
    /// * A skinned mesh renderer must be present (see Initialize()).
    /// * Bone names must be received (see ProcessRigCreation()).
    /// * Bone counts of incoming rig and skinned mesh renderer must match.
    /// * Bone names of incoming rig and skinned mesh renderer must math.
    /// <returns>Returns true if mapping succeeded.</returns>
    /// </summary>
    bool mapGfxReplayBonesToSkinnedMesh(List<string> boneNames, SkinnedMeshRenderer skinnedMeshRenderer)
    {
        if (_initialized)
        {
            return true;
        }
        if (skinnedMeshRenderer == null || boneNames == null)
        {
            return false;
        }
        if (skinnedMeshRenderer.bones.Length != boneNames.Count + 1) // 'boneNames' doesn't include the root.
        {
            Debug.LogError($"Skinned object '{_parent.name}' does not have the same number of bones than the rig {_parent.rigId}.");
            return false;
        }

        // Match Unity bones to Habitat bone indices using bone names.
        _bones = new Transform[boneNames.Count];
        
        int matchedBoneCount = 0;
        for (int i = 0; i < boneNames.Count; ++i)
        {
            for (int j = 0; j < skinnedMeshRenderer.bones.Length; ++j)
            {
                if (boneNames[i] == skinnedMeshRenderer.bones[j].gameObject.name)
                {
                    _bones[i] = skinnedMeshRenderer.bones[j];
                    ++matchedBoneCount;
                    continue;
                }
            }
        }
        if (matchedBoneCount != boneNames.Count)
        {
            Debug.LogError($"Skinned object '{_parent.name}' does not match the bones defined in rig {_parent.rigId}.");
            return false;
        }

        skinnedMeshRenderer.enabled = true;

        // If a pose was already received, apply it immediately.
        if (_lastRigUpdate != null)
        {
            ProcessRigUpdate(_lastRigUpdate);
            _lastRigUpdate = null;
        }

        return true;
    }
}
