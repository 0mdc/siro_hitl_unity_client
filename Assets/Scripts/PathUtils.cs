using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Utilities for processing paths.
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// Remove '..' and '.' from a relative path.
    /// </summary>
    public static string SimplifyRelativePath(string path)
    {
        Assert.IsNotNull(path);
        string[] parts = path.Split(new char[] { '/' }, System.StringSplitOptions.RemoveEmptyEntries);
        var simplifiedParts = new List<string>();

        foreach (var part in parts)
        {
            if (part == ".." && simplifiedParts.Count > 0)
            {
                simplifiedParts.RemoveAt(simplifiedParts.Count - 1);
            }
            else if (part != "." && part != "..")
            {
                simplifiedParts.Add(part);
            }
        }

        return string.Join("/", simplifiedParts);
    }

    /// <summary>
    /// Remove the file extension from a path.
    /// </summary>
    public static string RemoveExtension(string path)
    {
        Assert.IsNotNull(path);
        if (path == "")
        {
            return "";
        }
        return Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>
    /// Converts a Habitat path into a Unity address asset.
    /// </summary>
    public static string HabitatPathToUnityAddress(string path)
    {
        Assert.IsNotNull(path);
        return RemoveExtension(SimplifyRelativePath(path));
    }
}
