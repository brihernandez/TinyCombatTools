using UnityEditor;
using UnityEngine;

[System.Serializable]
public class ModSettings
{
    public string ProjectModFolder = "MOD";
    public string ExportPath = "";
    public string BundleName = "";
    public Texture2D ThumbnailImage = null;
    public Texture2D PreviewImage = null;

    public ModSettings() { }

    public ModSettings(ModSettings other)
    {
        ProjectModFolder = other.ProjectModFolder;
        ExportPath = other.ExportPath;
        BundleName = other.BundleName;
        ThumbnailImage = other.ThumbnailImage;
        PreviewImage = other.PreviewImage;
    }

    public bool Equals(ModSettings other)
    {
        if (other == null)
            return false;

        return ProjectModFolder == other.ProjectModFolder
               && ExportPath == other.ExportPath
               && BundleName == other.BundleName
               && ThumbnailImage == other.ThumbnailImage
               && PreviewImage == other.PreviewImage;
    }
}

/// <summary>
/// Holds all the persistent mod settings so that you don't have to re-enter data every time
/// you open up the tools.
/// </summary>
[CreateAssetMenu(fileName = "ModBuilderSettings", menuName = "Tiny Combat Arena/Mod Builder Settings")]
public class ModBuilderSettings : ScriptableObject
{
    public ModSettings Settings = new ModSettings();
    public ModData Mod = new ModData();

    public static ModBuilderSettings CreateDefaultModBuilderSettings()
    {
        const string DefaultModSettingsPath = "Assets/TinyCombatTools/Settings/Mods/DefaultMod.asset";
        var modBuilderSettings = CreateInstance<ModBuilderSettings>();
        AssetDatabase.CreateAsset(modBuilderSettings, DefaultModSettingsPath);
        return modBuilderSettings;
    }
}
