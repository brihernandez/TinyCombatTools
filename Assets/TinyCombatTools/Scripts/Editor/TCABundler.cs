using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;

/// <summary>
/// This is a mirror of the ModData as seen in the game's main codebase.
/// If any changes are made to this in the main codebase, the tools example must also be updated.
/// </summary>
public class ModData
{
    public string Name = "";
    public string DisplayName = "";
    public string Description = "";
    public string Thumbnail = "";
    public string Preview = "";
    public ulong Id = 0;
    public List<string> Assets = new List<string>();
}

public class TCABundler : EditorWindow
{
    private string ProjectModFolder = "MOD";
    private string ExportPath = "";
    private string BundleName = "";

    private Texture2D ThumbnailImage = null;
    private Texture2D PreviewImage = null;

    private Vector2 AssetBundleScrollPosition;
    private Vector2 MainScrollPosition;

    public const int VersionMajor = 2;
    public const int VersionMinor = 0;

    private static ModData Mod = new ModData();

    private const string DefaultThumbnailImageName = "thumb.png";
    private const string DefaultPreviewImageName = "preview.png";

    private const int ThumbnailHeight = 256;
    private const int ThumbnailWidth = 256;

    private const int PreviewWidth = 635;
    private const int PreviewHeight = 358;

    [MenuItem("Tiny Combat Arena/Open Asset Bundler", priority = 0)]
    private static void ShowBundlerWindow()
    {
        var window = GetWindow(typeof(TCABundler), utility: false, title: $"TCA Bundler {VersionMajor}.{VersionMinor}");
        window.Show();
        window.minSize = new Vector2(600, 800);
    }

    private Texture2D TakeSceneScreenshot(string outPath, int X, int Y)
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

        var localAssetPath = Path.Combine("Assets", ProjectModFolder, DefaultThumbnailImageName);
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

    private void GenerateMODJson()
    {
        BundleName = "assets";
        Mod.Thumbnail = CreateAssetPathFromFilePath(AssetDatabase.GetAssetPath(ThumbnailImage));
        Mod.Preview = CreateAssetPathFromFilePath(AssetDatabase.GetAssetPath(PreviewImage));
        Mod.Assets.Clear();
        Mod.Assets = new List<string>(new[] { BundleName });

        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.Converters.Add(new StringEnumConverter());
        settings.Formatting = Formatting.Indented;

        var jsonText = JsonConvert.SerializeObject(Mod, settings);
        Debug.Log(jsonText);
        Debug.Log(BundleName);
        File.WriteAllText(ExportPath + "/Mod.json", jsonText);
    }

    private void OnGUI()
    {
        MainScrollPosition = EditorGUILayout.BeginScrollView(MainScrollPosition, false, true);
        bool isExportAllowed = true;

        EditorGUILayout.LabelField("1. Select folder to bundle assets from", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("The location of the assets within the Unity project affect the resulting filepaths. It can be worthwhile to use unique folder names to avoid naming collisions.\n\nThis folder MUST be inside the Project's \"Assets\" folder!", EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(ProjectModFolder, EditorStyles.textField);

        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            var selectedFolder = EditorUtility.OpenFolderPanel(
                title: "Select for to bundle assets from",
                folder: Application.dataPath,
                defaultName: "Default");

            if (selectedFolder.StartsWith(Application.dataPath))
                ProjectModFolder = selectedFolder.Replace(Application.dataPath + "/", "");

            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("2. Set the mod details", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("This will be used to generate the required Mod.json file.\n\nAny existing Mod.json file inside the asset path will be rewritten!", EditorStyles.helpBox);

        if (Mod == null)
            Mod = new ModData();

        Mod.Name = EditorGUILayout.TextField("Name", Mod.Name);
        isExportAllowed &= CheckFieldLength(Mod.Name, GetNiceName(nameof(Mod.Name)));
        Mod.DisplayName = EditorGUILayout.TextField("Display Name", Mod.DisplayName);
        isExportAllowed &= CheckFieldLength(Mod.DisplayName, GetNiceName(nameof(Mod.DisplayName)));

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Description", GUILayout.Width(150));
        Mod.Description = EditorGUILayout.TextArea(Mod.Description, EditorStyles.textArea, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
        isExportAllowed &= CheckFieldLength(Mod.Description, GetNiceName(nameof(Mod.Description)));

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Generate or verify thumbnail", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"A \"{DefaultThumbnailImageName}\" file will be created in the mod folder path if you press \"Generate Thumbnail\" (using your main camera view), or you can add one yourself. Make sure the project folder it's a valid one!", EditorStyles.helpBox);

        ThumbnailImage = EditorGUILayout.ObjectField(ThumbnailImage, typeof(Texture2D), false) as Texture2D;
        if (GUILayout.Button("Generate Thumbnail"))
        {
            var rootPath = Path.Combine(Application.dataPath, ProjectModFolder);
            if (!Directory.Exists(rootPath))
            {
                Debug.LogError($"Project path {rootPath} doesn't exist");
            }
            else
            {
                ThumbnailImage = TakeSceneScreenshot(Path.Combine(rootPath, DefaultThumbnailImageName), ThumbnailWidth, ThumbnailHeight);
                Repaint();
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        var pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
        isExportAllowed &= ThumbnailImage != null;

        if (ThumbnailImage != null)
        {
            GUILayout.Box(
                ThumbnailImage,
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
        EditorGUILayout.LabelField("4. Generate or verify Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"A \"{DefaultPreviewImageName}\" file will be created in the mod folder path if you press \"Generate preview\" (using your main camera view), or you can add one yourself. Make sure the project folder it's a valid one!", EditorStyles.helpBox);

        PreviewImage = EditorGUILayout.ObjectField(PreviewImage, typeof(Texture2D), false) as Texture2D;
        if (GUILayout.Button("Generate Preview"))
        {
            var rootPath = Path.Combine(Application.dataPath, ProjectModFolder);
            if (!Directory.Exists(rootPath))
            {
                Debug.LogError($"Project path {rootPath} doesn't exist");
            }
            else
            {
                PreviewImage = TakeSceneScreenshot(Path.Combine(rootPath, DefaultPreviewImageName), PreviewWidth, PreviewHeight);
                Repaint();
            }
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        isExportAllowed &= PreviewImage != null;

        if (PreviewImage != null)
        {
            GUILayout.Box(
                PreviewImage,
                GUILayout.Width(PreviewImage.width  / 2 * pixelsPerPoint),
                GUILayout.Height(PreviewImage.height  / 2 * pixelsPerPoint));
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
        EditorGUILayout.LabelField("Below are the assets to be exported, along with their the paths that can be used in JSON config files. Make sure to only export what you need!\n\nExample:\nassets/mod/aircraft/a10a/a10a.fbx\nassets/mod/aircraft/a10a/a10mat.mat\nassets/mod/aircraft/a10a/a10palette.png", EditorStyles.helpBox);
        var paths = GetAllExportPaths(ProjectModFolder);

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
            EditorGUILayout.LabelField("NO ASSETS FOUND!");
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("6. Export mod to game", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Enter a path for the mod definition and assets to be exported. Typically, this will be your mod's folder inside of the game's Mod folder.\n\nExample:\nC:/Program Files/Steam/steamapps/common/TinyCombatArena/Mods/A10/{BundleName}", EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(ExportPath, EditorStyles.textField);

        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            ExportPath = EditorUtility.SaveFolderPanel(
                title: "Select export folder",
                folder: Application.dataPath,
                defaultName: ""
            );
            Repaint();
        }
        EditorGUILayout.EndHorizontal();
        if (ExportPath.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Export path must be defined!",
                MessageType.Error, true);
        }

        EditorGUILayout.Space(10);
        isExportAllowed &= ExportPath.Length > 0 && paths.Count > 0;
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("7. Generate Files", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("You can either Generate as a new mod, or to update an existing one.", EditorStyles.helpBox, GUILayout.ExpandWidth(true));

        GUI.enabled = isExportAllowed;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export Bundle"))
        {
            GenerateMODJson();
            BuildBundle(Path.Combine(ExportPath, BundleName));
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Open Export Folder"))
            System.Diagnostics.Process.Start(Path.GetDirectoryName(ExportPath));

        GUI.enabled = true;
        bool hasValidExportPath = ExportPath.Length > 0 && paths.Count > 0;
        var assetListPath = hasValidExportPath ? Path.Combine(Path.GetDirectoryName(ExportPath), "assetlist.txt") : "";
        GUI.enabled = hasValidExportPath && File.Exists(assetListPath);

        if (GUILayout.Button("Open Asset List"))
            System.Diagnostics.Process.Start(assetListPath);

        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        EditorGUILayout.EndScrollView();
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
        build.assetNames = GetAllExportPaths(ProjectModFolder).ToArray();

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
        assetListText.AppendLine("Example usage in an aircraft definition:\n\"ModelPath\": \"assets/microfalcon/export/aircraft/resources/a10a/a10a.fbx\"");
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
