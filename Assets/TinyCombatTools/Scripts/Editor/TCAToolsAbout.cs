using UnityEditor;
using UnityEngine;

public class TCAToolsAbout : EditorWindow
{
    [MenuItem("Tiny Combat Arena/About")]
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

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField("Tiny Combat Tools");
        EditorGUILayout.LabelField("Released October 31 2023");
        EditorGUILayout.LabelField("Version 1.0");

        EditorGUILayout.EndVertical();
    }
}
