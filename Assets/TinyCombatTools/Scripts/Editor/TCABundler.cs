using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class TCABundler : EditorWindow
{
    public string ProjectModFolder = "MOD";
    public string ExportPath = "";
    public bool ShowMetaFiles = false;

    public const int VersionMajor = 1;
    public const int VersionMinor = 0;
    public const string BundleDefaultName = "assets";

    private Vector2 ScrollPos = Vector2.zero;

    [MenuItem("Tiny Combat Arena/Open Asset Bundler", priority = 0)]
    private static void ShowBundlerWindow()
    {
        var window = GetWindow(typeof(TCABundler), utility: false, title: $"TCA Bundler {VersionMajor}.{VersionMinor}");
        window.Show();
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

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
        EditorGUILayout.LabelField("2. Verify assets to be exported", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Below are the assets to be exported, along with their the paths that can be used in JSON config files. Make sure to only export what you need!\n\nExample:\nAssets\\MOD\\Aircraft\\A10A\\A10A.fbx\nAssets\\MOD\\Aircraft\\A10A\\A10Mat.mat\nAssets\\MOD\\Aircraft\\A10A\\A10Palette.png", EditorStyles.helpBox);

        var paths = GetAllExportPaths();
        if (paths.Count > 0)
        {
            EditorGUILayout.LabelField("Assets to bundle:");
            ScrollPos = EditorGUILayout.BeginScrollView(
                scrollPosition: ScrollPos,
                EditorStyles.helpBox);
            foreach (var path in paths)
                EditorGUILayout.LabelField(path);
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("NO ASSETS FOUND!");
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("3. Export Asset Bundle", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Can be exported directly into your mod's folder inside the game's install.\n\nExample:\nC:/Program Files/Steam/steamapps/common/TinyCombatArena/Mods/A10/{BundleDefaultName}", EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(ExportPath, EditorStyles.textField);
        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            ExportPath = EditorUtility.SaveFilePanel(
                title: "Select export folder",
                directory: ExportPath,
                defaultName: BundleDefaultName,
                extension: "");
            Repaint();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        bool hasValidExportPath = ExportPath.Length > 0 && paths.Count > 0;
        GUI.enabled = hasValidExportPath;
        if (GUILayout.Button("Export Bundle"))
            BuildBundle(ExportPath);

        EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Open Export Folder"))
                System.Diagnostics.Process.Start(Path.GetDirectoryName(ExportPath));
            GUI.enabled = true;

        var assetListPath = hasValidExportPath ? Path.Combine(Path.GetDirectoryName(ExportPath), "assetlist.txt") : "";
        GUI.enabled = hasValidExportPath && File.Exists(assetListPath);
        if (GUILayout.Button("Open Asset List"))
            System.Diagnostics.Process.Start(assetListPath);
        GUI.enabled = false;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private List<string> GetAllExportPaths()
    {
        var bundleSourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, ProjectModFolder));
        if (!Directory.Exists(bundleSourcePath))
            return new List<string>();

        var files = new List<string>(Directory.GetFiles(
            path: bundleSourcePath,
            searchPattern: "*.*",
            SearchOption.AllDirectories));

        // .meta files aren't needed.
        files.RemoveAll(name => name.EndsWith(".meta"));

        // Everything asset bundle related is forced to lowercase.
        for (int i = 0; i < files.Count; ++i)
        {
            var x = files[i].Replace(Path.GetFullPath(Application.dataPath), "Assets");
            x = x.Replace(Path.DirectorySeparatorChar, '/');
            files[i] = x.ToLower();
        }

        return files;
    }

    private void BuildBundle(string exportPath)
    {
        var bundleName = Path.GetFileName(exportPath);
        var folderPath = Path.GetDirectoryName(exportPath);
        Debug.Log($"Building bundle with name {bundleName}!");

        Directory.CreateDirectory(folderPath);

        var build = new AssetBundleBuild();
        build.assetBundleName = bundleName;
        build.assetNames = GetAllExportPaths().ToArray();

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
