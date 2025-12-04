using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Runtime
{
    public static class SceneUtils
    {
        public static string GetPathInScene(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }

            return path;
        }

        public static string GetSceneAssetFolderPath(Scene scene, bool createIfNotExists = false)
        {
            string scenePath = scene.path;
            string sceneFolder = Path.GetDirectoryName(scenePath);
            if (string.IsNullOrEmpty(sceneFolder))
            {
                throw new Exception("Scene folder path is null or empty. Make sure the scene is saved in the project.");
            }

            string sceneName = Path.GetFileNameWithoutExtension(scenePath);
#if UNITY_EDITOR
            if (createIfNotExists && !AssetDatabase.IsValidFolder(Path.Combine(sceneFolder, sceneName)))
            {
                AssetDatabase.CreateFolder(sceneFolder, sceneName);
            }
#endif
            return Path.Combine(sceneFolder, sceneName);
        }

        public static string SaveSceneAsset(Scene scene, string fileName, ScriptableObject data)
        {
            string path = GetSceneAssetFolderPath(scene, true);
            string filePath = Path.Combine(path, $"{fileName}.asset");
#if UNITY_EDITOR
            if (File.Exists(filePath))
            {
                AssetDatabase.DeleteAsset(filePath);
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.CreateAsset(data, filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
            return filePath;
        }

        public static void DeleteSceneAsset(Scene scene, string fileName)
        {
            string path = GetSceneAssetFolderPath(scene);
            string filePath = Path.Combine(path, $"{fileName}.asset");
            if (!File.Exists(filePath)) return;
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(filePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        public static T LoadSceneAsset<T>(Scene scene, string fileName) where T : ScriptableObject
        {
#if UNITY_EDITOR
            string path = GetSceneAssetFolderPath(scene);
            string filePath = Path.Combine(path, $"{fileName}.asset");
            if (!File.Exists(filePath)) return null;
            return AssetDatabase.LoadAssetAtPath<T>(filePath);
#else
            return null;
#endif
        }
    }
}