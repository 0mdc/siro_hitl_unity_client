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
/// <summary>
/// This class contains a set of Editor tools to import Habitat data and make it available using Addressables.
/// </summary>
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

    /// <summary>Name of the metadata file consumed by this asset pipeline.</summary>
    public const string METADATA_FILE_NAME = "metadata.json";

    /// <summary>Version of the metadata file. It is bumped when backward compatibility is broken.</summary>
    public const int METADATA_FILE_VERSION = 1;

    /// <summary>Directory where Habitat data is imported. Ignored on git.</summary>
    public const string INPUT_ASSET_FOLDER = "Assets/HabitatData";

    /// <summary>Location where METADATA_FILE_NAME is imported.</summary>
    public const string EDITOR_HABITAT_METADATA_PATH = INPUT_ASSET_FOLDER + "/" + METADATA_FILE_NAME;

    /// <summary>Location where addressable assets are exported. Ignored on git.</summary>
    public const string OUTPUT_ASSET_FOLDER = "ServerData";

    /// <summary>Name of the addressable profile.</summary>
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
    /// Create a short addressable label name from an index.
    /// Asset dependencies are assigned a label. For example, a window that is used by 3 scenes will be assigned 3 labels.
    /// Label combinations are included in addressable assets and bundle names. Combinations can be long, so they are shortened.
    /// To avoid collisions, the first character of the label is a letter, and subsequent characters are digits.
    /// Examples: "a", "F0", "d94"
    /// </summary>
    /// <param name="counter">Sequential counter from which a label name is created.</param>
    /// <returns></returns>
    public static string CreateShortLabelName(uint index)
    {
        const uint LETTER_COUNT = 52;
        const uint LOWERCASE_COUNT = 26;
        const uint LOWERCASE_FIRST_CHAR = 'a';
        const uint UPPERCASE_FIRST_CHAR = 'A';
        uint char_index = index % LETTER_COUNT;
        uint char_case_index = index % LOWERCASE_COUNT;
        uint first_char_index = char_index < LOWERCASE_COUNT ? LOWERCASE_FIRST_CHAR : UPPERCASE_FIRST_CHAR;
        uint digits = index / LETTER_COUNT;
        char prefix = (char)(first_char_index + char_case_index);
        string suffix = digits == 0 ? "" : (digits - 1u).ToString();
        return prefix + suffix;
    }

    /// <summary>
    /// Read the METADATA_FILE_NAME and deserializes it.
    /// </summary>
    /// <returns>Deserialized MetadataFile object.</returns>
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

    /// <summary>
    /// Get the internal Unity GUID from a specified path.
    /// </summary>
    /// <param name="unityAssetPath">Path relative to the Unity project folder.</param>
    /// <returns>Internal GUID.</returns>
    public static string GetAssetGUID(string unityAssetPath)
    {
        return AssetDatabase.AssetPathToGUID(unityAssetPath);
    }

    /// <summary>Copy a directory and all of its content recursively.</summary>
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
    /// Open the directory that contains the generated addressables assets.
    /// </summary>
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

    /// <summary>
    /// Import Habitat datasets (data folder) into the project.
    /// The incoming data folder is expected to be beside a METADATA_FILE_NAME file.<br/>
    /// After this step is done:<br/>
    /// * The assets can be used in the Editor for testing.<br/>
    /// * The addressables catalog can be built. See BuildCatalog().<br/>
    /// </summary>
    [MenuItem ("Habitat/Import Habitat Data")]
    static void ImportHabitatDataDirectory()
    {
        // Warn the user that previous data will be deleted.
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
        
        // Open dialogue to select a METADATA_FILE_NAME file.
        string metadataPath = EditorUtility.OpenFilePanel(
            title: $"Import {METADATA_FILE_NAME}.",
            directory: "",
            extension: "json"
        );
        if (metadataPath == null || !File.Exists(metadataPath))
        {
            return;
        }

        // Warn the user if a 'data' folder is not located next to METADATA_FILE_NAME. 
        var subdirectories = new FileInfo(metadataPath).Directory.GetDirectories();
        if (subdirectories.Length == 0)
        {
            Debug.LogWarning($"Specified '{METADATA_FILE_NAME}' should be packaged with a 'data' folder.");
            return;
        }

        // Delete the previous data directory.
        if (Directory.Exists(unityInputDataDir))
        {
            Debug.Log($"Deleting {INPUT_ASSET_FOLDER} directory...");
            Directory.Delete(unityInputDataDir, true);
            string metaFilePath = unityInputDataDir + ".meta";
            if (File.Exists(metaFilePath))
            {
                File.Delete(metaFilePath);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        // Create the data directory.
        Debug.Log($"Creating {INPUT_ASSET_FOLDER} directory...");
        Directory.CreateDirectory(unityInputDataDir);

        // Import METADATA_FILE_NAME.
        Debug.Log($"Importing {METADATA_FILE_NAME}...");
        string inputMetadataFile = Path.Combine(unityInputDataDir, METADATA_FILE_NAME);
        File.Copy(metadataPath, inputMetadataFile, overwrite: true);

        // Import subdirectories.
        Debug.Log("Importing Habitat Data Folder...");
        CopyDirectory(new FileInfo(metadataPath).Directory.FullName, unityInputDataDir, recursive: true);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Build the addressables catalog from the imported metadata file and Habitat datasets.<br/>
    /// * There are two groups: Local and Remote.<br/>
    /// * The local assets are packaged with the build.<br/>
    /// * The remote assets are exported to OUTPUT_ASSET_FOLDER.<br/>
    /// * OUTPUT_ASSET_FOLDER needs to be copied to a remote server for provisioning, but can be used in the editor for testing.
    /// * Labels are used to mark dependencies. Assets with the same labels are bundled together.
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

                if (string.IsNullOrEmpty(guid) || !File.Exists(unityAssetPath))
                {
                    Debug.LogError($"Asset {fileName} was not imported correctly into Unity. It will be excluded from the catalog.");
                    continue;
                }

                // Assign the group.
                var addressableGroup = isLocalAsset ? localGroup : remoteGroup;
                settings.CreateOrMoveEntry(guid, addressableGroup);
                var entry = settings.FindAssetEntry(guid);

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

    /// <summary>Boilerplate to create an addressable group.</summary>
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
