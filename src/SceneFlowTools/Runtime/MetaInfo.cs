using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
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
        public const string IdPrefix = "sf_";

        public string uid;

        public override string ToString()
        {
            return $"MetaInfo: {uid}";
        }

        public static string NewUid()
        {
            return $"{IdPrefix}{Guid.NewGuid():N}";
        }

        public static List<(GameObject obj, string id)> CollectAll()
        {
            // Reverts adfcc14d80aac5ba1e35c3c4896c59f916de834a's include-inactive behavior.
            // That change appears to have been incorrect for experiment/runtime object collection,
            // but correcting it here may have side effects for workflows that expected inactive
            // scene objects to be addressable through MetaInfo.CollectAll().
            return UnityEngine.Object.FindObjectsOfType<MetaInfo>(false)
                .Where(x => x != null && x.gameObject.scene.IsValid() && x.gameObject.scene.isLoaded)
                .Where(x => !string.IsNullOrEmpty(x.uid))
                .OrderBy(x => GetHierarchySortKey(x.gameObject))
                .Select(x => (x.gameObject, x.uid))
                .ToList();
        }

        public static Dictionary<string, GameObject> CollectAllDict()
        {
            List<(GameObject obj, string id)> allObjects = CollectAll();
            Dictionary<string, GameObject> result = new Dictionary<string, GameObject>();
            foreach (var (obj, id) in allObjects)
            {
                if (!result.ContainsKey(id))
                {
                    result[id] = obj;
                }
            }

            LogDuplicateIds(BuildDuplicateMap(allObjects));
            return result;
        }

        public static Dictionary<GameObject, string> CollectAllDictReversed()
        {
            List<(GameObject obj, string id)> allObjects = CollectAll();
            LogDuplicateIds(BuildDuplicateMap(allObjects));
            return allObjects.ToDictionary(x => x.obj, x => x.id);
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

        private static Dictionary<string, List<GameObject>> BuildDuplicateMap(List<(GameObject obj, string id)> allObjects)
        {
            return allObjects
                .GroupBy(x => x.id)
                .Where(x => x.Count() > 1)
                .ToDictionary(x => x.Key, x => x.Select(v => v.obj).ToList());
        }

        private static void LogDuplicateIds(Dictionary<string, List<GameObject>> duplicates)
        {
            if (duplicates.Count == 0) return;

            string samples = string.Join("; ", duplicates.Take(5).Select(kv =>
                $"{kv.Key}: {string.Join(", ", kv.Value.Take(3).Select(x => x.name))}"));
            Debug.LogError(
                $"Duplicate MetaInfo uid detected. duplicateGroups={duplicates.Count}, samples=[{samples}]");
        }

        private static string GetHierarchySortKey(GameObject obj)
        {
            List<string> parts = new List<string>();
            Transform current = obj.transform;
            while (current != null)
            {
                parts.Add($"{current.GetSiblingIndex():D6}:{current.name}");
                current = current.parent;
            }

            parts.Reverse();
            return $"{obj.scene.handle:D6}:{obj.scene.path}/{string.Join("/", parts)}";
        }


#if UNITY_EDITOR
        private void Reset()
        {
            if (string.IsNullOrEmpty(uid))
            {
                uid = NewUid();
            }

            MetaInfoManager.RequestValidation("MetaInfo Reset");
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            MetaInfoManager.RequestValidation("MetaInfo OnValidate");
        }

        public static void GenerateToAllGameObjects()
        {
            MetaInfoManager.GenerateToAllGameObjects();
        }

        public static void RemoveAll()
        {
            MetaInfoManager.RemoveAllMetaInfo();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
#endif
    }
}
