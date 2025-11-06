
using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace WikiHelper.Output;

public static class FileWriter
{
    public static string DirectoryPath => Path.Combine(Paths.PluginPath, "output");

    public static string FilePath(string name)
    {
        return Path.Combine(DirectoryPath, $"v{Application.version}_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}_{name}.txt");
    }
}