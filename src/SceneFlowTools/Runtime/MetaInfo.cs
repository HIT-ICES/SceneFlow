using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MetaInfo : MonoBehaviour
    {
        public string uid;

        public override string ToString()
        {
            return $"MetaInfo: {uid}";
        }

        public static List<(GameObject obj, string id)> CollectAll()
        {
            return FindObjectsOfType<GameObject>()
                .Select(x => (x, x.GetComponent<MetaInfo>()?.uid))
                .Where(x => !string.IsNullOrEmpty(x.uid)).ToList();
        }

        public static Dictionary<string, GameObject> CollectAllDict()
        {
            return CollectAll().ToDictionary(x => x.id, x => x.obj);
        }

        public static Dictionary<GameObject, string> CollectAllDictReversed()
        {
            return CollectAll().ToDictionary(x => x.obj, x => x.id);
        }

        public static void AssertExistsIds(IEnumerable<string> ids)
        {
            var existingIds = CollectAll().Select(x => x.id).ToHashSet();
            foreach (var id in ids)
            {
                if (!existingIds.Contains(id))
                {
                    Debug.LogError($"MetaInfo with id {id} does not exist in the current scene.");
                }
            }
        }


#if UNITY_EDITOR
        private void Reset()
        {
            uid = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            string correctUid = GlobalObjectId.GetGlobalObjectIdSlow(gameObject).ToString();
            if (uid == correctUid) return;
            uid = correctUid;
            EditorUtility.SetDirty(this);
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        public static void GenerateToAllGameObjects()
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj.GetComponent<MetaInfo>() == null)
                {
                    obj.AddComponent<MetaInfo>();
                    EditorUtility.SetDirty(obj);
                }
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void RemoveAll()
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                var metaInfo = obj.GetComponent<MetaInfo>();
                if (metaInfo == null) continue;
                Destroy(metaInfo);
                EditorUtility.SetDirty(obj);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }
}