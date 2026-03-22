using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class SceneManagementTool : EditorWindow
{
    private string[] allScenePaths;
    private HashSet<string> loadedSceneNames;
    private List<EditorBuildSettingsScene> scenesInBuild;

    [MenuItem("Tools/ResXR Scene Management Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneManagementTool>("ResXR Scene Management");
        window.UpdateSceneList();
        window.UpdateLoadedSceneNames();
    }

    private void OnEnable()
    {
        UpdateSceneList();
        UpdateLoadedSceneNames();
    }

    private void UpdateSceneList()
    {
        allScenePaths = Directory.GetFiles(Application.dataPath, "*.unity", SearchOption.AllDirectories);
        scenesInBuild = EditorBuildSettings.scenes.ToList();
    }

    private void UpdateLoadedSceneNames()
    {
        loadedSceneNames = new HashSet<string>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            loadedSceneNames.Add(scene.name);
        }
    }

    private void OnGUI()
    {
        // Initialize styles within OnGUI to avoid null reference exceptions
        GUIStyle headerStyle = new GUIStyle(EditorStyles.largeLabel)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        GUIStyle buttonStyle = new GUIStyle(EditorStyles.miniButton)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter
        };

        GUILayout.Label("ResXR Scene Management", headerStyle);

        GUILayout.Space(10);
        GUILayout.Label("Currently Open Scenes", EditorStyles.boldLabel);

        foreach (string loadedSceneName in loadedSceneNames)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(loadedSceneName, GUILayout.Width(200));

            // Find the scene by its name to unload it
            Scene scene = SceneManager.GetSceneByName(loadedSceneName);
            if (scene.IsValid() && GUILayout.Button("Unload", buttonStyle))
            {
                EditorSceneManager.CloseScene(scene, true);
                UpdateLoadedSceneNames(); // Refresh the list after unloading a scene
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(20);

        GUILayout.Label("All Scenes in Project", EditorStyles.boldLabel);

        foreach (string scenePath in allScenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // Only display load button if the scene is not already loaded
            if (!loadedSceneNames.Contains(sceneName))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(sceneName, GUILayout.Width(200));

                string relativePath = "Assets" + scenePath.Substring(Application.dataPath.Length); // Convert to relative path
                if (GUILayout.Button("Load", buttonStyle))
                {
                    EditorSceneManager.OpenScene(relativePath, OpenSceneMode.Additive);
                    UpdateLoadedSceneNames(); // Refresh the list after loading a scene
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Refresh Scene List", buttonStyle))
        {
            UpdateSceneList();
            UpdateLoadedSceneNames();
            Repaint(); // Repaint the editor window to show the updated lists
        }

        GUILayout.Space(20);

        // Scenes in Build Settings
        GUILayout.Label("Scenes in Build", EditorStyles.boldLabel);
        foreach (var buildScene in scenesInBuild)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Path.GetFileNameWithoutExtension(buildScene.path), GUILayout.Width(200));
            if (GUILayout.Button(buildScene.enabled ? "Remove from Build" : "Add to Build", buttonStyle))
            {
                buildScene.enabled = !buildScene.enabled;
                EditorBuildSettings.scenes = scenesInBuild.ToArray(); // Update the build settings
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(30);

        // Button to open the Build Settings window
        if (GUILayout.Button("Open Build Settings"))
        {
            EditorWindow.GetWindow(typeof(BuildPlayerWindow));
        }
    }
}