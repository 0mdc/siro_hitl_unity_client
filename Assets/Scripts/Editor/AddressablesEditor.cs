using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using Newtonsoft.Json;
using System.IO;
using UnityEngine.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Linq;
using System.Data;
using Newtonsoft.Json.Linq;

namespace Habitat.Editor
{
public static class AddressablesEditor
{
    [Serializable]
    // Metadata file content.
    public class MetadataFile
    {
        public int version;
        public string local_group_name;
        public Dictionary<string, List<string>> groups;
    }

    public const string METADATA_FILE_NAME = "metadata.json";
    public const int METADATA_FILE_VERSION = 1;
    public const string INPUT_ASSET_FOLDER = "Assets/HabitatData";
    public const string EDITOR_HABITAT_METADATA_PATH = INPUT_ASSET_FOLDER + "/" + METADATA_FILE_NAME;
    public const string OUTPUT_ASSET_FOLDER = "ServerData";
    const string PROFILE_NAME = "habitat";

    public static bool MetadataExist()
    {
        return File.Exists(EDITOR_HABITAT_METADATA_PATH);
    }
    public static string AddressToUnityPath(string path)
    {
        return Path.Combine(INPUT_ASSET_FOLDER, path);
    }

    /// <summary>
    /// Create a short package name.
    /// Label names are combined together. To avoid collisions, the first character is alpha and subsequent characters are digits.
    /// Examples: "a", "F0", "d94"
    /// </summary>
    /// <param name="counter">Sequential counter from which a label name is created.</param>
    /// <returns></returns>
    public static string CreateShortLabelName(uint counter)
    {
        const uint LETTER_COUNT = 52;
        const uint LOWERCASE_COUNT = 26;
        const uint LOWERCASE_INDEX = 'a';
        const uint UPPERCASE_INDEX = 'A';
        uint char_index = counter % LETTER_COUNT;
        uint alpha_index = counter % LOWERCASE_COUNT;
        uint char_range = char_index < LOWERCASE_COUNT ? LOWERCASE_INDEX : UPPERCASE_INDEX;
        uint digits = counter / LETTER_COUNT;
        char prefix = (char)(char_range + alpha_index);
        string suffix = digits == 0 ? "" : (digits - 1u).ToString();
        return prefix + suffix;
    }

    public static MetadataFile ReadMetadataFile()
    {
        if (!MetadataExist())
        {
            Debug.LogError($"Cannot find {EDITOR_HABITAT_METADATA_PATH}.");
            return null;
        }

        string textContent = File.ReadAllText(EDITOR_HABITAT_METADATA_PATH);

        // Parse version without the type system to warn whether backward compatibility has been broken.
        dynamic dictContent = JObject.Parse(textContent);
        int? version = dictContent.version;
        if (version == null || METADATA_FILE_VERSION != version.Value)
        {
            Debug.LogWarning($"This Unity project supports {METADATA_FILE_NAME} version {METADATA_FILE_VERSION}. The selected metadata file (version {version}) may not work as intended.");
        }

        return JsonConvert.DeserializeObject<MetadataFile>(textContent, new JsonSerializerSettings());
    }

    public static AssetReference AddAssetToAddressables(string guid)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        return settings.CreateAssetReference(guid);
    }

    public static string GetAssetGUID(string unityAssetPath)
    {
        return AssetDatabase.AssetPathToGUID(unityAssetPath);
    }

    /// <summary>
    /// Returns true if the asset is in all groups.
    /// In this case, the asset will be provided along with the build.
    /// </summary>
    static bool ShouldAssetBePackagedInLocalGroup(string assetAddress, Dictionary<string, int> occurrences, int packageCountWithoutLocal)
    {
        if (occurrences.TryGetValue(assetAddress, out int occurrence))
        {
            return occurrence == packageCountWithoutLocal;
        }
        else
        {
            Debug.LogError($"Unknown asset: {assetAddress}");
            return false;
        }
    }

    [MenuItem ("Habitat/Open Remote Asset Directory")]
    static void OpenRemoteAssetDirectory()
    {
        string outputDataDir = Path.Combine(Directory.GetCurrentDirectory(), OUTPUT_ASSET_FOLDER);
        if (Directory.Exists(outputDataDir))
        {
            Application.OpenURL($"file://{outputDataDir}");
        }
        else
        {
            Debug.LogError("The server asset directory has not been created. Use 'Habitat/Build Catalog' to build it.");
        }
    }

    static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
    {
        var sourceInfo = new DirectoryInfo(sourceDir);
        if (!sourceInfo.Exists)
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourceInfo.FullName}");
        }

        DirectoryInfo[] sourceDirs = sourceInfo.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in sourceInfo.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: true);
        }

        if (recursive)
        {
            foreach (DirectoryInfo subDir in sourceDirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, newDestinationDir, true);
            }
        }
    }

    /// <summary>
    /// Search for the Habitat data folder. It is defined as the directory containing the metadata json file.
    /// This function aims to improve developer experience by tolerating selection of a parent directory.
    /// </summary>
    /// <param name="inputDir">Candidate directory.</param>
    /// <returns>Directory containing Habitat data. Returns null if invalid.</returns>
    static string FindHabitatDataDirectory(string inputDir, int maxDepth = 2)
    {
        if (maxDepth <= 0)
        {
            return null;
        }

        // Check if directory contains metadata.
        var inputInfo = new DirectoryInfo(inputDir);
        {
            var files = inputInfo.GetFiles();
            foreach (var fileInfo in files)
            {
                if (fileInfo.Name == METADATA_FILE_NAME)
                {
                    return inputDir;
                }
            }
        }
        // Search for a valid subdirectory.
        maxDepth--;
        var subDirs = inputInfo.GetDirectories();
        foreach (var subdirInfo in subDirs)
        {
            string subdirResult = FindHabitatDataDirectory(subdirInfo.FullName, maxDepth - 1);
            if (subdirResult != null)
            {
                return subdirResult;
            }
        }
        // If the directory tree does not contain metadata, return null.
        return null;
    }

    /// <summary>
    /// Import Habitat datasets (data folder) into the project.
    /// The incoming data folder is expected to be beside a METADATA_FILE_NAME file.
    /// After this step is done:
    /// - The assets can be used in the Editor for testing.
    /// - The addressables catalog can be built. See BuildCatalog().
    /// </summary>
    [MenuItem ("Habitat/Import Habitat Data")]
    static void ImportHabitatDataDirectory()
    {
        string unityInputDataDir = Path.Combine(Directory.GetCurrentDirectory(), INPUT_ASSET_FOLDER);
        if (Directory.Exists(unityInputDataDir))
        {
            if (!EditorUtility.DisplayDialog(
                title: "Import Habitat Data",
                message: $"This will delete all current Habitat data ({INPUT_ASSET_FOLDER}). Continue?",
                ok: "OK",
                cancel: "Cancel"
            ))
            return;
        }
        
        string metadataPath = EditorUtility.OpenFilePanel(
            title: $"Import {METADATA_FILE_NAME}.",
            directory: "",
            extension: "json"
        );
        if (metadataPath == null || !File.Exists(metadataPath))
        {
            return;
        }

        var subdirectories = new FileInfo(metadataPath).Directory.GetDirectories();
        if (subdirectories.Length == 0)
        {
            Debug.LogWarning($"Specified '{METADATA_FILE_NAME}' should be packaged with a 'data' folder.");
            return;
        }

        if (Directory.Exists(unityInputDataDir))
        {
            Debug.Log($"Deleting {INPUT_ASSET_FOLDER} directory...");
            Directory.Delete(unityInputDataDir, true);
            string metaFilePath = unityInputDataDir + ".meta";
            if (File.Exists(metaFilePath))
            {
                File.Delete(metaFilePath);
            }
            AssetDatabase.Refresh();
        }
        
        Debug.Log($"Creating {INPUT_ASSET_FOLDER} directory...");
        Directory.CreateDirectory(unityInputDataDir);

        Debug.Log($"Importing {METADATA_FILE_NAME}...");
        string inputMetadataFile = Path.Combine(unityInputDataDir, METADATA_FILE_NAME);
        File.Copy(metadataPath, inputMetadataFile, overwrite: true);

        Debug.Log("Importing Habitat Data Folder...");
        CopyDirectory(new FileInfo(metadataPath).Directory.FullName, unityInputDataDir, recursive: true);

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// This function builds the addressables catalog.
    /// </summary>
    [MenuItem ("Habitat/Build Catalog")]
    static void BuildCatalog ()
    {
        if (!MetadataExist())
        {
            Debug.LogError("No data has been imported to Unity. Use 'Habitat/Import Habitat Data' before creating a catalog.");
            return;
        }

        // HACK: Unity Addressables relies on serialized objects that are manually authored (AddressableAssetsData).
        //       We are procedurally generating them, therefore including them in .gitignore.
        //       GetSettings(true) initializes the Addressables assets for the project, which is normally done via Editor GUI.
        if (!AddressableAssetSettingsDefaultObject.SettingsExists)
        {
            Debug.Log("Creating 'AddressableAssetsData'.");
        }
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(create: true);
        settings.BuildRemoteCatalog = true;
        settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

        // (Re)create custom profile
        if (settings.profileSettings.GetAllProfileNames().Contains(PROFILE_NAME))
        {
            var id = settings.profileSettings.GetProfileId(PROFILE_NAME);
            settings.profileSettings.RemoveProfile(id);
            settings.profileSettings.SetDirty(AddressableAssetSettings.ModificationEvent.ProfileRemoved, id, true);
        }
        {
            var id = settings.profileSettings.AddProfile(PROFILE_NAME, settings.activeProfileId);
            settings.profileSettings.SetValue(id, "Local.BuildPath", "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]");
            settings.profileSettings.SetValue(id, "Local.LoadPath", "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]");
            // Set remote build path.
            // This path is where the files resulting from building addressables bundles will be located.
            // These files must be put on the server for provisioning.
            settings.profileSettings.SetValue(id, "Remote.BuildPath", $"{OUTPUT_ASSET_FOLDER}/[BuildTarget]");
            // Set remote load path.
            // This path is where the application will find the assets at runtime.
            // The expressions in [brackets] are evaluated at build-time.
            //     Because assets are platform-specific, one set of assets needs to be built for each platform (WebGL, Android, ...).
            // The expressions in {braces} are evaluated at runtime, upon initializing the Addressables system.
            //     Using magic, the addressable system will find the static variables in the HabitatAssetServer static class.
            //     See https://docs.unity3d.com/Packages/com.unity.addressables@1.18/manual/AddressableAssetsProfiles.html.
            //     Therefore, the static variables 'HabitatAssetServer.Address' and 'HabitatAssetServer.Port' must be set before calling any Addressables API.
            settings.profileSettings.SetValue(id, "Remote.LoadPath", "{HabitatAssetServer.Protocol}://{HabitatAssetServer.Address}:{HabitatAssetServer.Port}/{HabitatAssetServer.Path}/[BuildTarget]");
            settings.profileSettings.SetDirty(AddressableAssetSettings.ModificationEvent.ProfileAdded, id, true);
            // Set active profile.
            settings.activeProfileId = id;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.ActiveProfileSet, id, true);
            // HACK: Refresh main settings to reflect new values. Without this code, the behaviour is surprisingly flaky.
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
        }
        
        // Re-create groups
        AddressableAssetGroup[] groupsToRemove = settings.groups.Where(g => g != settings.DefaultGroup).ToArray();
        foreach (var g in groupsToRemove)
        {
            settings.RemoveGroup(g);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, g, true);
        }

        // Re-create labels
        var labels = settings.GetLabels();
        for (int i = 0; i < labels.Count; ++i)
        {
            string label = labels[i];
            if (label != "default")
            {
                settings.RemoveLabel(label);
            }
        }

        // Read metadata json file.
        var metadataFile = ReadMetadataFile();

        // Generate addressable groups.
        var remoteGroup = CreateAddressableGroup("remote");
        {
            BundledAssetGroupSchema bags = CreateOrGetAddressableGroupSchema<BundledAssetGroupSchema>(remoteGroup);
            bags.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel; // Create a bundle per unique set of labels.
            bags.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bags.IncludeAddressInCatalog = true; // Enable loading assets by address.
            bags.IncludeInBuild = true; // Enable building this group during AddressableAssetSettings.BuildPlayerContent().
            bags.IncludeGUIDInCatalog = false; // Disable loading by AssetReference.
            bags.IncludeLabelsInCatalog = true; // Disable labels - we only use them to create bundles.
            bags.UseAssetBundleCache = true; // Enables local cache.
            bags.UseAssetBundleCrc = true; // Enable loading assets from cache.
            bags.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenSpaceIsNeededInCache;
            // This makes the system export the bundles and catalog at 'ServerData/[Platform]':
            var idInfo = settings.profileSettings.GetProfileDataByName("Remote.BuildPath");
            remoteGroup.GetSchema<BundledAssetGroupSchema>().BuildPath.SetVariableById(settings, idInfo.Id);
            // This convoluted API enables the application to specify an IP and port to fetch the data.
            // Unintuitively, the address where assets are located must be specified at build-time.
            // String interpolation is used in runtime to change the address.
            idInfo = settings.profileSettings.GetProfileDataByName("Remote.LoadPath");
            remoteGroup.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableById(settings, idInfo.Id);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, remoteGroup, true);
        }
        var localGroup = CreateAddressableGroup(metadataFile.local_group_name);
        {
            BundledAssetGroupSchema bags = CreateOrGetAddressableGroupSchema<BundledAssetGroupSchema>(localGroup);
            bags.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether; // Create a single bundle.
            bags.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            bags.IncludeAddressInCatalog = true; // Enable loading assets by address.
            bags.IncludeInBuild = true; // Enable building this group during AddressableAssetSettings.BuildPlayerContent().
            bags.IncludeGUIDInCatalog = false; // Disable loading by AssetReference.
            bags.IncludeLabelsInCatalog = false; // Disable labels.
            bags.UseAssetBundleCache = true; // Enables local cache.
            bags.UseAssetBundleCrc = true; // Enable loading assets from cache.
            bags.AssetBundledCacheClearBehavior = BundledAssetGroupSchema.CacheClearBehavior.ClearWhenWhenNewVersionLoaded;
            // This makes the system export bundles in a folder that Unity will package along with the build.
            var idInfo = settings.profileSettings.GetProfileDataByName("Local.BuildPath");
            localGroup.GetSchema<BundledAssetGroupSchema>().BuildPath.SetVariableById(settings, idInfo.Id);
            // This tells Unity to load these assets locally.
            idInfo = settings.profileSettings.GetProfileDataByName("Local.LoadPath");
            localGroup.GetSchema<BundledAssetGroupSchema>().LoadPath.SetVariableById(settings, idInfo.Id);
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupSchemaModified, localGroup, true);
        }

        uint labelCounter = 0;
        foreach (var group in metadataFile.groups)
        {
            // Check if this is the local package.
            bool isLocalGroup = group.Key == metadataFile.local_group_name;

            // If this is not a local group, create a new label for items in this group.
            // Labels can get pretty long when combined. Therefore, it is shortened.
            string label = isLocalGroup ? null : CreateShortLabelName(labelCounter++);
            if (label != null)
            {
                settings.AddLabel(label);
            }

            foreach (var fileName in group.Value)
            {
                // Check whether this asset needs to be packaged with the local group.
                // TODO: If the asset is in all remote packages, mark it as local.
                bool isLocalAsset = isLocalGroup;

                // Identify the asset in Unity.
                var unityAssetPath = AddressToUnityPath(fileName);
                string guid = GetAssetGUID(unityAssetPath);

                // Assign the group.
                var addressableGroup = isLocalAsset ? localGroup : remoteGroup;
                settings.CreateOrMoveEntry(guid, addressableGroup);
                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                {
                    Debug.LogError($"Asset {fileName} was not imported correctly into Unity. It will be excluded from the catalog.");
                    continue;
                }

                // Assign the address.
                // Habitat excludes the extension, so we remove the extension here.
                int extensionLength = new FileInfo(fileName).Extension.Length;
                entry.address = fileName[..^extensionLength];

                // Assign the label.
                // Unity will package all assets with the same combination of labels together.
                // If the package is local, we simply don't use the label and package it with the other local assets.
                if (!isLocalAsset)
                {
                    entry.labels.Add(label);
                }

                // Update the serializable data so that changes are reflected in the Editor.
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }
        }

        // Save changes done to serializable settings and assets.
        AssetDatabase.SaveAssets();

        // Delete previous build output.
        if (Directory.Exists(OUTPUT_ASSET_FOLDER))
        {
            Directory.Delete(OUTPUT_ASSET_FOLDER, true);
        }

        // Clean builder cache.
        AddressableAssetSettings.CleanPlayerContent(AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);

        // Build addressables using the current platform.
        AddressableAssetSettings.BuildPlayerContent();
    }

    public static AddressableAssetGroup CreateAddressableGroup(string groupName)
    {
        if (string.IsNullOrEmpty(groupName))
        {   
            Debug.LogError($"Invalid group name: {groupName}.");
            return null;
        }
        
        var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
        var schemasToCopy = addressableSettings.DefaultGroup.Schemas;

        AddressableAssetGroup group = addressableSettings.CreateGroup(
            groupName:groupName,
            setAsDefaultGroup:false,
            readOnly: false,
            postEvent: false,
            schemasToCopy: schemasToCopy
        );
        
        addressableSettings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupAdded, group, true);

        return group;
    }

    public static T CreateOrGetAddressableGroupSchema<T>(AddressableAssetGroup group) where T : AddressableAssetGroupSchema
    {
        return group.GetSchema<T>() ?? group.AddSchema<T>();
    }
}
}
