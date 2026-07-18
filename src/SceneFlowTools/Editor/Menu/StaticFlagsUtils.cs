using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Editor
{
    public static class StaticFlagsUtils
    {
        [MenuItem("Tools/Clear All Static Flags In Loaded Scenes")]
        private static void Clear()
        {
            int count = 0;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                    continue;

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    count += ClearStaticRecursive(root);
                }

                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"Cleared static flags on {count} GameObjects.");
        }

        private static int ClearStaticRecursive(GameObject go)
        {
            Undo.RecordObject(go, "Clear Static Flags");
            

            GameObjectUtility.SetStaticEditorFlags(go, 0);

            int count = 1;

            foreach (Transform child in go.transform)
            {
                count += ClearStaticRecursive(child.gameObject);
            }

            return count;
        }
    }
}