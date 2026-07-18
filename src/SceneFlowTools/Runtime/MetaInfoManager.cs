using System;
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
    public class MetaInfoManager : MonoBehaviour
    {
        private const double ValidationDebounceSeconds = 1.0;

        [NonSerialized] public MetaInfoValidationReport lastValidationReport = new MetaInfoValidationReport();

#if UNITY_EDITOR
        private static bool _validationRequested;
        private static double _nextValidationTime;
        private static string _pendingReason;
        private static int _pendingRequestCount;

        private void OnEnable()
        {
            if (Application.isPlaying) return;
            RequestValidation("MetaInfoManager OnEnable");
        }

        private void Start()
        {
            if (Application.isPlaying) return;
            RequestValidation("MetaInfoManager Start");
        }

        public static void RequestValidation(string reason)
        {
            if (Application.isPlaying) return;
            _pendingRequestCount++;
            if (string.IsNullOrEmpty(_pendingReason))
            {
                _pendingReason = reason;
            }

            if (!_validationRequested)
            {
                _validationRequested = true;
                EditorApplication.update += RunValidationWhenReady;
            }

            _nextValidationTime = EditorApplication.timeSinceStartup + ValidationDebounceSeconds;
        }

        private static void RunValidationWhenReady()
        {
            if (EditorApplication.timeSinceStartup < _nextValidationTime) return;

            EditorApplication.update -= RunValidationWhenReady;
            _validationRequested = false;

            string reason = _pendingRequestCount <= 1
                ? _pendingReason
                : $"{_pendingReason} (+{_pendingRequestCount - 1} merged requests)";
            _pendingReason = null;
            _pendingRequestCount = 0;

            foreach (MetaInfoManager manager in FindManagers())
            {
                manager.ValidateNow(reason);
            }
        }

        public void ValidateNow(string reason)
        {
            lastValidationReport = Scan(reason);
            LogValidationErrors(lastValidationReport);
        }

        public static MetaInfoValidationReport Scan(string reason)
        {
            List<GameObject> allObjects = CollectSceneGameObjects();
            List<MetaInfo> metaInfos = allObjects
                .Select(x => x.GetComponent<MetaInfo>())
                .Where(x => x != null)
                .OrderBy(x => GetHierarchySortKey(x.gameObject))
                .ToList();

            Dictionary<string, List<MetaInfo>> idGroups = new Dictionary<string, List<MetaInfo>>();
            foreach (MetaInfo metaInfo in metaInfos)
            {
                if (string.IsNullOrEmpty(metaInfo.uid)) continue;
                if (!idGroups.ContainsKey(metaInfo.uid))
                {
                    idGroups[metaInfo.uid] = new List<MetaInfo>();
                }

                idGroups[metaInfo.uid].Add(metaInfo);
            }

            List<MetaInfoDuplicateGroup> duplicateGroups = idGroups
                .Where(x => x.Value.Count > 1)
                .OrderBy(x => x.Key)
                .Select(x => new MetaInfoDuplicateGroup
                {
                    id = x.Key,
                    objects = x.Value
                        .OrderBy(metaInfo => GetHierarchySortKey(metaInfo.gameObject))
                        .Select(metaInfo => metaInfo.gameObject)
                        .ToList()
                })
                .ToList();

            return new MetaInfoValidationReport
            {
                scannedObjectCount = allObjects.Count,
                metaInfoCount = metaInfos.Count,
                missingMetaInfoCount = allObjects.Count - metaInfos.Count,
                emptyIdCount = metaInfos.Count(x => string.IsNullOrEmpty(x.uid)),
                duplicateGroupCount = duplicateGroups.Count,
                duplicateObjectCount = duplicateGroups.Sum(x => Math.Max(0, x.objects.Count - 1)),
                lastValidationReason = reason,
                lastValidationTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                duplicateGroups = duplicateGroups
            };
        }

        private static void LogValidationErrors(MetaInfoValidationReport report)
        {
            if (report == null) return;

            if (report.emptyIdCount > 0)
            {
                string samples = string.Join(", ", CollectSceneMetaInfos()
                    .Where(x => string.IsNullOrEmpty(x.uid))
                    .OrderBy(x => GetHierarchySortKey(x.gameObject))
                    .Take(5)
                    .Select(x => x.gameObject.name));
                Debug.LogError(
                    $"MetaInfo validation failed: emptyIds={report.emptyIdCount}, samples=[{samples}]");
            }

            if (report.missingMetaInfoCount > 0)
            {
                string samples = string.Join(", ", CollectSceneGameObjects()
                    .Where(x => x.GetComponent<MetaInfo>() == null)
                    .OrderBy(GetHierarchySortKey)
                    .Take(5)
                    .Select(x => x.name));
                Debug.LogError(
                    $"MetaInfo validation failed: missingMetaInfo={report.missingMetaInfoCount}, samples=[{samples}]");
            }

            if (report.duplicateGroupCount > 0)
            {
                string samples = string.Join("; ", report.duplicateGroups.Take(5).Select(group =>
                    $"{group.id}: {string.Join(", ", group.objects.Take(3).Select(x => x.name))}"));
                Debug.LogError(
                    $"MetaInfo validation failed: duplicateGroups={report.duplicateGroupCount}, duplicateObjectsToFix={report.duplicateObjectCount}, samples=[{samples}]");
            }
        }

        public static int GenerateToAllGameObjects()
        {
            int changed = GenerateIdsForMissingMetaInfo() + GenerateIdsForEmptyMetaInfo();
            RequestValidation("Generate MetaInfo");
            return changed;
        }

        public static int GenerateIdsForMissingMetaInfo()
        {
            HashSet<string> reservedIds = CollectReservedIds();
            int changed = 0;
            foreach (GameObject obj in CollectSceneGameObjects())
            {
                if (obj.GetComponent<MetaInfo>() != null) continue;
                MetaInfo metaInfo = Undo.AddComponent<MetaInfo>(obj);
                metaInfo.uid = GenerateUniqueId(reservedIds);
                EditorUtility.SetDirty(metaInfo);
                EditorUtility.SetDirty(obj);
                changed++;
            }

            MarkLoadedScenesDirty();
            RequestValidation("Generate IDs for missing MetaInfo");
            return changed;
        }

        public static int GenerateIdsForEmptyMetaInfo()
        {
            HashSet<string> reservedIds = CollectReservedIds();
            int changed = 0;
            foreach (MetaInfo metaInfo in CollectSceneMetaInfos())
            {
                if (!string.IsNullOrEmpty(metaInfo.uid)) continue;
                Undo.RecordObject(metaInfo, "Generate empty MetaInfo ID");
                metaInfo.uid = GenerateUniqueId(reservedIds);
                EditorUtility.SetDirty(metaInfo);
                changed++;
            }

            MarkLoadedScenesDirty();
            RequestValidation("Generate IDs for empty MetaInfo");
            return changed;
        }

        public static int RegenerateDuplicateIds()
        {
            MetaInfoValidationReport report = Scan("Regenerate duplicate IDs");
            HashSet<string> reservedIds = CollectReservedIds();
            int changed = 0;

            foreach (MetaInfoDuplicateGroup duplicateGroup in report.duplicateGroups)
            {
                List<GameObject> objects = duplicateGroup.objects
                    .Where(x => x != null)
                    .OrderBy(GetHierarchySortKey)
                    .ToList();
                for (int i = 1; i < objects.Count; i++)
                {
                    MetaInfo metaInfo = objects[i].GetComponent<MetaInfo>();
                    if (metaInfo == null) continue;
                    Undo.RecordObject(metaInfo, "Regenerate duplicate MetaInfo ID");
                    metaInfo.uid = GenerateUniqueId(reservedIds);
                    EditorUtility.SetDirty(metaInfo);
                    changed++;
                }
            }

            MarkLoadedScenesDirty();
            RequestValidation("Regenerate duplicate MetaInfo IDs");
            return changed;
        }

        public static int RegenerateAllIds()
        {
            GenerateIdsForMissingMetaInfo();

            HashSet<string> reservedIds = new HashSet<string>();
            int changed = 0;
            foreach (MetaInfo metaInfo in CollectSceneMetaInfos())
            {
                Undo.RecordObject(metaInfo, "Regenerate all MetaInfo IDs");
                metaInfo.uid = GenerateUniqueId(reservedIds);
                EditorUtility.SetDirty(metaInfo);
                changed++;
            }

            MarkLoadedScenesDirty();
            RequestValidation("Regenerate all MetaInfo IDs");
            return changed;
        }

        public static int RemoveAllMetaInfo()
        {
            int changed = 0;
            foreach (MetaInfo metaInfo in CollectSceneMetaInfos())
            {
                Undo.DestroyObjectImmediate(metaInfo);
                changed++;
            }

            MarkLoadedScenesDirty();
            RequestValidation("Remove all MetaInfo");
            return changed;
        }

        private static List<MetaInfoManager> FindManagers()
        {
            return UnityEngine.Object.FindObjectsOfType<MetaInfoManager>(true)
                .Where(x => x != null && IsLoadedSceneObject(x.gameObject))
                .OrderBy(x => GetHierarchySortKey(x.gameObject))
                .ToList();
        }

        private static List<MetaInfo> CollectSceneMetaInfos()
        {
            return UnityEngine.Object.FindObjectsOfType<MetaInfo>(true)
                .Where(x => x != null && IsLoadedSceneObject(x.gameObject))
                .OrderBy(x => GetHierarchySortKey(x.gameObject))
                .ToList();
        }

        private static HashSet<string> CollectReservedIds()
        {
            return CollectSceneMetaInfos()
                .Select(x => x.uid)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSet();
        }

        private static string GenerateUniqueId(HashSet<string> reservedIds)
        {
            string id;
            do
            {
                id = MetaInfo.NewUid();
            } while (!reservedIds.Add(id));

            return id;
        }

        private static void MarkLoadedScenesDirty()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }
#endif

        public static List<GameObject> CollectSceneGameObjects()
        {
            return UnityEngine.Object.FindObjectsOfType<Transform>(true)
                .Where(x => x != null && IsLoadedSceneObject(x.gameObject))
                .Select(x => x.gameObject)
                .Distinct()
                .OrderBy(GetHierarchySortKey)
                .ToList();
        }

        public static bool IsLoadedSceneObject(GameObject obj)
        {
            return obj != null && obj.scene.IsValid() && obj.scene.isLoaded;
        }

        public static string GetHierarchySortKey(GameObject obj)
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
    }

    [Serializable]
    public class MetaInfoValidationReport
    {
        public int scannedObjectCount;
        public int metaInfoCount;
        public int missingMetaInfoCount;
        public int emptyIdCount;
        public int duplicateGroupCount;
        public int duplicateObjectCount;
        public string lastValidationReason;
        public string lastValidationTime;
        public List<MetaInfoDuplicateGroup> duplicateGroups = new List<MetaInfoDuplicateGroup>();
    }

    [Serializable]
    public class MetaInfoDuplicateGroup
    {
        public string id;
        public List<GameObject> objects = new List<GameObject>();
    }
}
