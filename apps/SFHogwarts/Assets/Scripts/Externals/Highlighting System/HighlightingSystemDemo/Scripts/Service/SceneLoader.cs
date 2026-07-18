using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
using System.IO;
#endif

public class SceneLoader : MonoBehaviour
{
    public static readonly string[] sceneNames =
        { "Welcome", "Colors", "Transparency", "Occlusion", "OccluderModes", "Scripting", "Compound", "Mobile" };

    private readonly int h = 30;

    private readonly int ox = 20;
    private readonly int oy = 100;

    // 
    private void OnGUI()
    {
        GUI.Label(new Rect(ox, oy + 10, 500, 100), "Load demo scene:");

        for (var i = 0; i < sceneNames.Length; i++)
        {
            var scene = sceneNames[i];

            if (GUI.Button(new Rect(ox, oy + 30 + i * h, 120, 20), scene)) SceneManager.LoadScene(scene);
        }
    }

#if UNITY_EDITOR
    private const string scenesPath = "Assets/HighlightingSystemDemo/Scenes/";
    private const string extension = ".unity";

    private List<string> missingSceneNames;
    private List<string> missingScenePaths;

    // 
    private void Start()
    {
        // Create list of missing demo scenes
        CheckMissingScenes();

        // In case we have missing demo scenes
        if (missingSceneNames != null && missingSceneNames.Count > 0)
        {
            // Ask user to add missing scenes to the editor build settings
            var message = "Add these demo scenes to the editor build settings?\n";
            var l = missingSceneNames.Count;
            for (var i = 0; i < l; i++)
                message += string.Format(i != l - 1 ? "'{0}', " : "'{0}'.", missingSceneNames[i]);
            var answer = EditorUtility.DisplayDialog("Highlighting System", message, "Yes", "No");
            if (answer)
            {
                AddMissingScenes();

                // Stop Playing to allow changes to take effect
                StartCoroutine(StopNextFrame());
            }
        }
    }

    // 
    private void CheckMissingScenes()
    {
        var l = sceneNames.Length;
        missingSceneNames = new List<string>(l);
        missingScenePaths = new List<string>(l);

        // Build list with full scene paths
        for (var i = 0; i < l; i++)
        {
            var sceneName = sceneNames[i];
            missingSceneNames.Add(sceneName);
            missingScenePaths.Add(scenesPath + sceneName + extension);
        }

        // Remove existing scenes from the list
        var existingScenes = EditorBuildSettings.scenes;
        for (var i = missingScenePaths.Count - 1; i >= 0; i--)
        {
            var scenePath = missingScenePaths[i];
            for (var j = 0; j < existingScenes.Length; j++)
            {
                var scene = existingScenes[j];
                if (Equals(scene.path, scenePath))
                {
                    missingSceneNames.RemoveAt(i);
                    missingScenePaths.RemoveAt(i);
                    break;
                }
            }
        }
    }

    // 
    private void AddMissingScenes()
    {
        var existingScenes = EditorBuildSettings.scenes;
        var l = existingScenes.Length;

        // Create new extended list of scenes and copy existing ones to it
        var newScenes = new EditorBuildSettingsScene[l + missingScenePaths.Count];
        existingScenes.CopyTo(newScenes, 0);

        // Add missing scenes
        for (var i = 0; i < missingScenePaths.Count; i++)
            newScenes[l + i] = new EditorBuildSettingsScene(missingScenePaths[i], true);

        // Assign new scene list
        EditorBuildSettings.scenes = newScenes;
    }

    // 
    private IEnumerator StopNextFrame()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        EditorApplication.isPlaying = false;
    }
#endif
}