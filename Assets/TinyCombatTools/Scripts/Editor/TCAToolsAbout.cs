using UnityEditor;
using UnityEngine;

public class TCAToolsAbout : EditorWindow
{
    [MenuItem("Tiny Combat Arena/About", priority = 100)]
    private static void ShowAboutWindow()
    {
        var window = GetWindow(
            typeof(TCAToolsAbout),
            utility: false,
            title: "About");
        window.Show();
        window.maxSize = new Vector2(300, 60);
        window.minSize = window.maxSize;
    }

    [MenuItem("Tiny Combat Arena/Open GitHub Page", priority = 1)]
    private static void OpenGitHubPage()
    {
        Application.OpenURL("https://github.com/brihernandez/TinyCombatTools");
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Tiny Combat Tools");
        EditorGUILayout.LabelField("Released February 14 2025");
        EditorGUILayout.LabelField($"Version {Application.version}");

        EditorGUILayout.EndVertical();
    }
}
