using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This is a mirror of the ModData as seen in the game's main codebase.
/// If any changes are made to this in the main codebase, the tools example must also be updated.
/// </summary>
[System.Serializable]
public class ModData : System.IEquatable<ModData>
{
    public string Name = "";
    public string ShortName = "";
    public string DisplayName = "";
    public string Summary = "";
    public string Description = "";
    public string Thumbnail = "";
    public string Preview = "";
    public ulong Id = 0;
    public List<string> Assets = new List<string>();

    public ModData() { }

    public ModData(ModData other)
    {
        Name = other.Name;
        ShortName = other.ShortName;
        DisplayName = other.DisplayName;
        Description = other.Description;
        Summary = other.Summary;
        Thumbnail = other.Thumbnail;
        Preview = other.Preview;
        Id = other.Id;
        Assets = new List<string>(other.Assets);
    }

    public bool Equals(ModData other)
    {
        if (other == null)
            return false;

        bool areListsEqual = false;
        if (Assets.Count == other.Assets.Count)
        {
            areListsEqual = true;
            for (int i = 0; i < Assets.Count; ++i)
                areListsEqual &= Assets[i] == other.Assets[i];
        }

        return other.Name == Name
            && other.ShortName == ShortName
            && other.DisplayName == DisplayName
            && other.Description == Description
            && other.Summary == Summary
            && other.Thumbnail == Thumbnail
            && other.Preview == Preview
            && other.Id == Id
            && areListsEqual;
    }
}

public class TCABundler : EditorWindow
{
    private Vector2 AssetBundleScrollPosition;
    private Vector2 MainScrollPosition;

    private static ModSettings Settings = new ModSettings();
    private static ModData Mod = new ModData();
    private static ModBuilderSettings PersistentSettings = null;

    private const string DefaultThumbnailImageName = "thumb.png";
    private const string DefaultPreviewImageName = "preview.png";

    private const int ThumbnailHeight = 256;
    private const int ThumbnailWidth = 256;

    private const int PreviewWidth = 1280;
    private const int PreviewHeight = 720;

    [MenuItem("Tiny Combat Arena/Open Mod Builder", priority = 0)]
    private static TCABundler ShowBundlerWindow()
    {
        TCABundler window = (TCABundler)GetWindow(typeof(TCABundler), utility: false, title: $"TCA Mod Builder {Application.version}");

        window.saveChangesMessage = "Saving the state of the Mod Builder window will preserve all your Mod's settings such as name, description, etc. next time you open this Unity project.\n\nSave changes to the Mod Builder tool?";
        window.minSize = new Vector2(PreviewWidth / 2, 800);
        window.Show();
        return window;
    }

    public void LoadPersistentSettings(ModBuilderSettings settings)
    {
        PersistentSettings = settings;
        Settings = new ModSettings(PersistentSettings.Settings);
        Mod = new ModData(PersistentSettings.Mod);
    }

    private void OnEnable()
    {
        if (PersistentSettings != null)
            LoadPersistentSettings(PersistentSettings);
    }

    private Texture2D TakeSceneScreenshot(string outPath,  int X, int Y)
    {
        var cam = Camera.main;
        var oldRT = cam.targetTexture;
        cam.targetTexture = RenderTexture.GetTemporary(X, Y);
        var outputTexture = new Texture2D(X, Y, TextureFormat.RGB24, false);

        RenderTexture.active = cam.targetTexture;
        cam.Render();
        outputTexture.ReadPixels(new Rect(0, 0, X, Y), 0, 0);
        outputTexture.Apply();
        Debug.Log("Texture created and readable: " + outputTexture.isReadable);

        cam.targetTexture = oldRT;
        File.WriteAllBytes(outPath, outputTexture.EncodeToPNG());
        AssetDatabase.Refresh();

        var localAssetPath = CreateAssetPathFromFilePath(outPath);
        var savedScreenshotImporter = AssetImporter.GetAtPath(localAssetPath) as TextureImporter;

        // Preview images should look nice when scaled, which means they have to use settings that
        // are not-normal for Tiny Combat.
        var previewPresetPath = "Assets/TinyCombatTools/Settings/Presets/PreviewImages.preset";
        var previewImagePreset = AssetDatabase.LoadAssetAtPath<UnityEditor.Presets.Preset>(previewPresetPath);

        previewImagePreset.ApplyTo(savedScreenshotImporter);
        bool wasApplied = previewImagePreset.ApplyTo(savedScreenshotImporter);
        Debug.Log($"Preview image preset applied: {wasApplied}");

        // Reimport the asset with the new smooth settings.
        AssetDatabase.ImportAsset(localAssetPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(localAssetPath);
    }

    private static bool CheckFieldLength(string content, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            EditorGUILayout.HelpBox($"\"{fieldName}\" must be supplied", MessageType.Error, false);
            return false;
        }

        if (content.Trim().Length < 4)
        {
            EditorGUILayout.HelpBox($"\"{fieldName}\" length must be >= 4", MessageType.Error, false);
            return false;
        }
        return true;
    }

    private static string GetNiceName(string fieldName)
    {
        return ObjectNames.NicifyVariableName(fieldName);
    }

    private void WriteModJSONToDisk()
    {
        PersistentSettings.Settings.BundleName = $"assets{PersistentSettings.Mod.Name}";
        PersistentSettings.Mod.Thumbnail = CreateAssetPathFromFilePath(AssetDatabase.GetAssetPath(PersistentSettings.Settings.ThumbnailImage));
        PersistentSettings.Mod.Preview = CreateAssetPathFromFilePath(AssetDatabase.GetAssetPath(PersistentSettings.Settings.PreviewImage));
        PersistentSettings.Mod.Assets.Clear();
        PersistentSettings.Mod.Assets = new List<string>(new[] { PersistentSettings.Settings.BundleName });

        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.Converters.Add(new StringEnumConverter());
        settings.Formatting = Formatting.Indented;

        var jsonText = JsonConvert.SerializeObject(PersistentSettings.Mod, settings);
        Debug.Log(jsonText);
        Debug.Log(PersistentSettings.Settings.BundleName);
        Directory.CreateDirectory(PersistentSettings.Settings.ExportPath);
        File.WriteAllText(PersistentSettings.Settings.ExportPath + "/Mod.json", jsonText);
    }

    private void OnGUI()
    {
        // Save button at the top is only usable if changes exist.
        EditorGUILayout.BeginHorizontal();

        var newPersistentSettings = EditorGUILayout.ObjectField(PersistentSettings, typeof(ModBuilderSettings), false) as ModBuilderSettings;
        if (PersistentSettings != newPersistentSettings)
            LoadPersistentSettings(newPersistentSettings);

        GUI.enabled = hasUnsavedChanges;
        if (GUILayout.Button("Save Mod Export Settings", GUILayout.ExpandWidth(true)))
            SaveChanges();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (PersistentSettings == null)
        {
            EditorGUILayout.HelpBox("Assign a Mod Builder Settings file to start!", MessageType.Error);
            EditorGUILayout.HelpBox("Use the button below to create a new mod from scratch.\nA new Mod Builder Settings file can be created at any time in the Project window by right clicking\nCreate->Tiny Combat Arena->Mod Builder Settings.", MessageType.Info);
            if (GUILayout.Button("New Mod Builder Settings", GUILayout.Height(40)))
            {
                var newMod = ModBuilderSettings.CreateDefaultModBuilderSettings();
                LoadPersistentSettings(newMod);
            }
            return;
        }

        MainScrollPosition = EditorGUILayout.BeginScrollView(MainScrollPosition, false, true);
        bool isExportAllowed = true;

        EditorGUILayout.LabelField("1. Select folder to bundle assets from", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("The location of the assets within the Unity project affect the resulting filepaths. It can be worthwhile to use unique folder names to avoid naming collisions.\n\nThis folder MUST be inside the Project's \"Assets\" folder!", EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        Settings.ProjectModFolder = EditorGUILayout.TextField(Settings.ProjectModFolder);

        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            var selectedFolder = EditorUtility.OpenFolderPanel(
                title: "Select for to bundle assets from",
                folder: Application.dataPath,
                defaultName: "Default");

            if (selectedFolder.StartsWith(Application.dataPath))
                Settings.ProjectModFolder = selectedFolder.Replace(Application.dataPath + "/", "");

            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        if (Settings.ProjectModFolder == "MOD")
            EditorGUILayout.HelpBox("It's HIGHLY ADVISED to make this a unique name, e.g. \"MOD_A10\", in order to avoid potential path conflicts with other mods.", MessageType.Warning);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. Set the mod details", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("This will be used to generate the required Mod.json file.\n\nAny existing Mod.json file inside the asset path will be overwritten!", EditorStyles.helpBox);

        Mod.Name = EditorGUILayout.TextField(
            new GUIContent("Name", "The name of the mod itself. Should match the name of the mod's folder."),
            Mod.Name);
        Mod.ShortName = EditorGUILayout.TextField(
            new GUIContent("Short Name", "The name displayed in the mod browser's mod list. Should be short enough to fit on a single line."),
            Mod.ShortName);
        Mod.DisplayName = EditorGUILayout.TextField(
            new GUIContent("Display Name", "The easily readable name of the mod. This is the full name of your mod, and the name which your mod will be listed under if uploaded to Steam Workshop."),
            Mod.DisplayName);
        Mod.Summary = EditorGUILayout.TextField(
            new GUIContent("Summary", "Short sentence used to describe your mod in the ingame mod browser list."),
            Mod.Summary);

        // Use a horizontal group so the description textarea can be nice and big.
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(
            new GUIContent("Description", "The full description of your mod, seen when the mod is selected in the ingame Mod Browser. This will also be the default text if the mod gets uploaded to Steam Workshop."),
            GUILayout.Width(150));
        Mod.Description = EditorGUILayout.TextArea(Mod.Description, EditorStyles.textArea, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        // These three fields are REQUIRED for a mod to export correctly.
        isExportAllowed &= CheckFieldLength(Mod.Name, GetNiceName(nameof(Mod.Name)));
        isExportAllowed &= CheckFieldLength(Mod.DisplayName, GetNiceName(nameof(Mod.DisplayName)));
        isExportAllowed &= CheckFieldLength(Mod.Description, GetNiceName(nameof(Mod.Description)));

        string modIdString = EditorGUILayout.TextField("Steam Mod ID", Mod.Id.ToString());
        var wasValidModId = ulong.TryParse(modIdString, out Mod.Id);
        if (!wasValidModId)
            EditorGUILayout.HelpBox("Invalid SteamID!", MessageType.Error);
        if (Mod.Id != 0)
            EditorGUILayout.HelpBox("Only enter a Steam Mod ID if your mod has already been uploaded to workshop!\nUse this to ensure that your mod can be correctly updated from the Modding menu in Tiny Combat Arena.\nIf your mod is NOT on Steam Workshop, this MUST be set to 0!", MessageType.Warning);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Assign or Generate Thumbnail", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"This 1:1 screenshot will be used as the mod's icon in the ingame Mod Browser.\n\n If generating a screenshot, a \"{DefaultThumbnailImageName}\" will be created in the mod folder path (using your main camera view). Ensure the project folder is valid!", EditorStyles.helpBox);

        Settings.ThumbnailImage = EditorGUILayout.ObjectField(Settings.ThumbnailImage, typeof(Texture2D), false) as Texture2D;
        if (GUILayout.Button("Generate Thumbnail"))
        {
            var rootPath = Path.Combine(Application.dataPath, Settings.ProjectModFolder);
            if (!Directory.Exists(rootPath))
            {
                Debug.LogError($"Project path {rootPath} doesn't exist");
            }
            else
            {
                Settings.ThumbnailImage = TakeSceneScreenshot(Path.Combine(rootPath, DefaultThumbnailImageName), ThumbnailWidth, ThumbnailHeight);
                Repaint();
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        var pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
        isExportAllowed &= Settings.ThumbnailImage != null;

        if (Settings.ThumbnailImage != null)
        {
            GUILayout.Box(
                Settings.ThumbnailImage,
                GUILayout.Height(ThumbnailWidth * pixelsPerPoint),
                GUILayout.Width(ThumbnailHeight * pixelsPerPoint));
        }
        else
        {
            GUILayout.Box(
                $"\n\n\nMissing thumbnail image!\n\nPlease generate one or assign an image.",
                GUILayout.Height(ThumbnailWidth * pixelsPerPoint),
                GUILayout.Width(ThumbnailHeight * pixelsPerPoint));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("4. Assign or Generate Steam Screenshot", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"This 16:9 screenshot will be used as the preview screenshot on Steam Workshop.\n\n If generating a screenshot, a \"{DefaultPreviewImageName}\" will be created in the mod folder path (using your main camera view). Ensure the project folder is valid!", EditorStyles.helpBox);

        Settings.PreviewImage = EditorGUILayout.ObjectField(Settings.PreviewImage, typeof(Texture2D), false) as Texture2D;
        if (GUILayout.Button("Generate Preview"))
        {
            var rootPath = Path.Combine(Application.dataPath, Settings.ProjectModFolder);
            if (!Directory.Exists(rootPath))
            {
                Debug.LogError($"Project path {rootPath} doesn't exist");
            }
            else
            {
                Settings.PreviewImage = TakeSceneScreenshot(Path.Combine(rootPath, DefaultPreviewImageName), PreviewWidth, PreviewHeight);
                Repaint();
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        isExportAllowed &= Settings.PreviewImage != null;

        if (Settings.PreviewImage != null)
        {
            GUILayout.Box(
                Settings.PreviewImage,
                GUILayout.Width(Settings.PreviewImage.width  / 2 * pixelsPerPoint),
                GUILayout.Height(Settings.PreviewImage.height  / 2 * pixelsPerPoint));
        }
        else
        {
            GUILayout.Box(
                $"\n\n\nMissing preview image!\n\nPlease generate or assign an image.",
                GUILayout.Width(PreviewWidth  / 2 * pixelsPerPoint),
                GUILayout.Height(PreviewHeight  / 2 * pixelsPerPoint));
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("5. Verify assets to be exported", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Below are the assets to be exported, along with their the paths that can be used in JSON config files. Make sure to only export what you need!\n\nExample:\nassets/mod_a10/aircraft/a10a/a10a.fbx\nassets/mod_a10/aircraft/a10a/a10mat.mat\nassets/mod_a10/aircraft/a10a/a10palette.png", EditorStyles.helpBox);
        var paths = GetAllExportPaths(Settings.ProjectModFolder);

        if (paths.Count > 0)
        {
            EditorGUILayout.LabelField("Assets to bundle:");
            AssetBundleScrollPosition = EditorGUILayout.BeginScrollView(
                scrollPosition: AssetBundleScrollPosition,
                EditorStyles.helpBox, GUILayout.MinHeight(100), GUILayout.ExpandHeight(true));
            foreach (var path in paths)
                EditorGUILayout.LabelField(path);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("NO ASSETS FOUND!", MessageType.Error);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("6. Set path to export mod definition and assets", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Enter a path for the mod definition and assets to be exported. Typically, this will be your mod's folder inside of the game's Mod folder. You may have to create a new folder for your mod.\n\nExample:\nC:/Program Files/Steam/steamapps/common/TinyCombatArena/Mods/A10/", EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        Settings.ExportPath = EditorGUILayout.TextField(Settings.ExportPath);
        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            Settings.ExportPath = EditorUtility.SaveFolderPanel(
                title: "Select export folder",
                folder: Application.dataPath,
                defaultName: ""
            );
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        if (Settings.ExportPath.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Export path must be defined!",
                MessageType.Error, true);
        }

        EditorGUILayout.Space(10);
        isExportAllowed &= Settings.ExportPath.Length > 0 && paths.Count > 0;
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("7. Generate Files", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Exports the mod.json definition file, and the associated assets.", EditorStyles.helpBox, GUILayout.ExpandWidth(true));

        GUI.enabled = isExportAllowed;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export Mod"))
        {
            SaveChanges();
            WriteModJSONToDisk();
            BuildBundle(Path.Combine(PersistentSettings.Settings.ExportPath, PersistentSettings.Settings.BundleName));
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Open Export Folder"))
            System.Diagnostics.Process.Start(Path.GetDirectoryName(Settings.ExportPath));

        GUI.enabled = true;
        bool hasValidExportPath = Settings.ExportPath.Length > 0 && paths.Count > 0;
        var assetListPath = hasValidExportPath ? Path.Combine(Settings.ExportPath, "assetlist.txt") : "";
        GUI.enabled = hasValidExportPath && File.Exists(assetListPath);

        if (GUILayout.Button("Open Asset List"))
            System.Diagnostics.Process.Start(assetListPath);

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.EndScrollView();

        var wasSettingsChanged = !Settings.Equals(PersistentSettings.Settings);
        var wasModChanged = !Mod.Equals(PersistentSettings.Mod);
        hasUnsavedChanges = wasSettingsChanged || wasModChanged;
    }

    public override void SaveChanges()
    {
        base.SaveChanges();
        PersistentSettings.Settings = new ModSettings(Settings);
        PersistentSettings.Mod = new ModData(Mod);
        EditorUtility.SetDirty(PersistentSettings);
        AssetDatabase.SaveAssetIfDirty(PersistentSettings);
    }

    public static List<string> GetAllExportPaths(string projectModFolder)
    {
        var bundleSourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, projectModFolder));
        if (!Directory.Exists(bundleSourcePath))
            return new List<string>();

        var files = new List<string>(Directory.GetFiles(
            path: bundleSourcePath,
            searchPattern: "*.*",
            SearchOption.AllDirectories));

        // .meta files aren't needed.
        files.RemoveAll(name => name.EndsWith(".meta"));

        // Asset bundles have a specific kind of formatting.
        for (int i = 0; i < files.Count; ++i)
            files[i] = CreateAssetPathFromFilePath(files[i]);

        return files;
    }

    public static string CreateAssetPathFromFilePath(string filePath)
    {
        // Everything asset bundle related is forced to lowercase.
        filePath = Path.GetFullPath(filePath);
        var assetPath = filePath.Replace(Path.GetFullPath(Application.dataPath), "Assets");
        assetPath = assetPath.Replace(Path.DirectorySeparatorChar, '/');
        assetPath = assetPath.ToLower();
        return assetPath;
    }

    private void BuildBundle(string exportPath)
    {
        var bundleName = Path.GetFileName(exportPath);
        var folderPath = Path.GetDirectoryName(exportPath);
        Debug.Log($"Building bundle with name {bundleName}!");

        Directory.CreateDirectory(folderPath);

        var build = new AssetBundleBuild();
        build.assetBundleName = bundleName;
        build.assetNames = GetAllExportPaths(PersistentSettings.Settings.ProjectModFolder).ToArray();

        // Assets create several temporary files that are unneeded. Write the asset bundle
        // first to a temporary location.
        var temporaryPath = Path.GetTempPath();
        BuildPipeline.BuildAssetBundles(
            temporaryPath,
            new AssetBundleBuild[] { build },
            BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows);

        // Move the only file we actually care about to the actual export path.
        File.Copy(Path.Combine(temporaryPath, bundleName), exportPath, overwrite: true);

        var assetListFilePath = Path.GetFullPath(Path.Combine(folderPath, "assetlist.txt"));
        var assetListText = new System.Text.StringBuilder();
        assetListText.AppendLine($"Below are the paths for all the assets included in the asset bundle \"{bundleName}\"");
        assetListText.AppendLine("Example usage in an aircraft definition:\n\"ModelPath\": \"assets/mod_attack/aircraft/a10a/a10a.fbx\"");
        assetListText.AppendLine();

        foreach (var path in build.assetNames)
            assetListText.AppendLine(path);
        File.WriteAllText(assetListFilePath, assetListText.ToString());

        var loadedBundle = AssetBundle.LoadFromFile(exportPath);
        if (loadedBundle != null)
        {
            Debug.Log($"Successfully exported bundle named \"{bundleName}\" to {folderPath} with paths:");
            foreach (var s in loadedBundle.GetAllAssetNames())
                Debug.Log(s);
            loadedBundle.Unload(false);
        }
        else
        {
            Debug.LogError($"Failed to export bundle to {exportPath}!");
        }
    }
}
