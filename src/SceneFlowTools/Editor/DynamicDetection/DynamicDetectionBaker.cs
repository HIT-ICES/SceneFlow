using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using Utils;

namespace SceneFlowTools.Editor.DynamicDetection
{
    public class DynamicDetectionBakeOptions
    {
        public bool PromptBeforeRun = true;
        public bool ForceRefreshScripts;
        public bool UseCache = true;
        public int DirectBatchSize = 8;
        public int AgentMaxToolCalls = 6;
        public bool PropagateDynamicToChildren = true;
    }

    public static class DynamicDetectionBaker
    {
        public const string AgentDetectionDataVersion = "Agent:v2-static-full-submit-fallback-20260709";
        public static DynamicDetectionBakeStats LastBakeStats { get; private set; } = new();

        private static readonly HashSet<Type> IgnoredComponentTypes = new()
        {
            typeof(Transform),
            typeof(RectTransform),
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(SpriteRenderer),
            typeof(ParticleSystemRenderer),
            typeof(LineRenderer),
            typeof(TrailRenderer),
            typeof(CanvasRenderer),
            typeof(AudioSource),
            typeof(AudioListener),
            typeof(Light),
            typeof(LightProbeGroup),
            typeof(ReflectionProbe),
            typeof(Rigidbody),
            typeof(Collider),
            typeof(Joint),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(EventSystem),
            typeof(GraphicRaycaster),
            typeof(CharacterController),
            typeof(Camera),
        };

        public static DynamicDetectionData Bake(DynamicDetectionManager manager, DynamicDetectionBakeOptions options = null)
        {
            options ??= new DynamicDetectionBakeOptions();
            double totalStart = EditorApplication.timeSinceStartup;
            LastBakeStats = new DynamicDetectionBakeStats();
            manager.cachedGizmosDynamic = null;

            if (manager.data == null)
            {
                manager.data = ScriptableObject.CreateInstance<DynamicDetectionData>();
                manager.data.ObjectsDynamicInfo = new List<GObjectDynamicInfo>();
                manager.data.ScriptsDynamicInfo = new List<ScriptDynamicInfo>();
                SceneUtils.SaveSceneAsset(manager.gameObject.scene, "DynamicDetectionResults", manager.data);
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }

            options.PropagateDynamicToChildren = manager.propagateDynamicToChildren;
            Debug.Log("Detecting dynamic behaviors in the scene...");
            Dictionary<string, ScriptDynamicInfo> scriptInfoDict = new();
            string savedDetectionMode = string.IsNullOrEmpty(manager.data.DetectionMode)
                ? DynamicDetectionMode.DirectLLM.ToString()
                : manager.data.DetectionMode;
            string currentDetectionModeKey = GetDetectionModeKey(manager.detectionMode);
            bool detectionModeChanged = savedDetectionMode != currentDetectionModeKey || options.ForceRefreshScripts;
            foreach (var scriptDynamicInfo in manager.data.ScriptsDynamicInfo)
            {
                if (detectionModeChanged) continue;
                scriptInfoDict[scriptDynamicInfo.ScriptPath] = scriptDynamicInfo;
                Debug.Log($"Detected script info: {scriptDynamicInfo.ScriptPath}");
            }

            if (detectionModeChanged)
            {
                Debug.Log($"Dynamic detection mode changed or refresh requested. saved={savedDetectionMode}, current={currentDetectionModeKey}; scripts will be re-detected.");
            }

            var (scriptTypesNeedsUpdate, totalScripts, allScriptTypes) = CollectScriptsNeedsUpdate(scriptInfoDict);
            Debug.Log($"{scriptTypesNeedsUpdate.Count} scripts need update. ({scriptTypesNeedsUpdate.Count}/{totalScripts})");

            if (options.PromptBeforeRun && !ConfirmBake(scriptTypesNeedsUpdate, totalScripts))
            {
                Debug.Log("Dynamic detection cancelled by user.");
                return manager.data;
            }

            BakeScriptInfo(scriptInfoDict, scriptTypesNeedsUpdate, allScriptTypes, manager.detectionMode, options);
            Dictionary<string, GObjectDynamicInfo> objectInfoDict = BakeObjectInfo(scriptInfoDict, options);

            int dynamicObjCount = objectInfoDict.Values.Count(info => info.DynamicType.IsDynamic());
            Debug.Log($"Dynamic detection done. {objectInfoDict.Count} objects detected, {dynamicObjCount} dynamic.");

            manager.data.ScriptsDynamicInfo = scriptInfoDict.Values.ToList();
            manager.data.ObjectsDynamicInfo = objectInfoDict.Values
                .Where(info => info.DynamicType != ObjectDynamicType.Static).ToList();
            manager.data.DetectionMode = currentDetectionModeKey;

            MetaInfo.AssertExistsIds(manager.data.ObjectsDynamicInfo.Select(x => x.ObjectId));

            EditorUtility.SetDirty(manager.data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LastBakeStats.totalBakeSeconds = EditorApplication.timeSinceStartup - totalStart;
            return manager.data;
        }

        public static DynamicDetectionBatchExport BuildExport(DynamicDetectionManager manager)
        {
            var idToObjMap = MetaInfo.CollectAllDict();
            var objToIdMap = MetaInfo.CollectAllDictReversed();
            Dictionary<string, ObjectDynamicType> predicted = manager.data?.ObjectsDynamicInfo?
                .ToDictionary(x => x.ObjectId, x => x.DynamicType) ?? new Dictionary<string, ObjectDynamicType>();
            var markers = DynamicMarker.CollectAllWithPropagation()
                .Where(x => objToIdMap.ContainsKey(x.obj))
                .ToList();
            Dictionary<string, ObjectDynamicType> exactTruth = markers
                .ToDictionary(x => objToIdMap[x.obj], x => x.type);
            Dictionary<string, ObjectDynamicType> truth = manager.propagateDynamicToChildren
                ? PropagateDynamicTypes(markers, objToIdMap)
                : exactTruth;

            DynamicDetectionBatchExport result = new()
            {
                sceneName = manager.gameObject.scene.name,
                scenePath = manager.gameObject.scene.path,
                detectionMode = manager.detectionMode.ToString(),
                detectionModeKey = manager.data?.DetectionMode,
                propagateDynamicToChildren = manager.propagateDynamicToChildren,
                promptTokens = LastBakeStats.promptTokens,
                completionTokens = LastBakeStats.completionTokens,
                toolCalls = LastBakeStats.toolCalls,
                scriptAnalysisSeconds = LastBakeStats.scriptAnalysisSeconds,
                objectBakeSeconds = LastBakeStats.objectBakeSeconds,
                totalBakeSeconds = LastBakeStats.totalBakeSeconds,
            };

            foreach (var kv in idToObjMap.OrderBy(x => x.Key))
            {
                string objectId = kv.Key;
                GameObject obj = kv.Value;
                ObjectDynamicType trueLabel = truth.GetValueOrDefault(objectId, ObjectDynamicType.Static);
                ObjectDynamicType predictedLabel = predicted.GetValueOrDefault(objectId, ObjectDynamicType.Static);
                result.objects.Add(new DynamicDetectionBatchObjectResult
                {
                    objectId = objectId,
                    objectName = obj.name,
                    objectPath = SceneUtils.GetPathInScene(obj.transform),
                    trueLabel = trueLabel.ToString(),
                    predictedLabel = predictedLabel.ToString(),
                    hasDynamicMarker = exactTruth.ContainsKey(objectId),
                    trueLabelPropagated = !exactTruth.ContainsKey(objectId) && truth.ContainsKey(objectId),
                });
            }

            if (manager.data?.ScriptsDynamicInfo != null)
            {
                foreach (var scriptInfo in manager.data.ScriptsDynamicInfo.OrderBy(x => x.ScriptPath))
                {
                    DynamicDetectionBatchScriptResult scriptResult = new()
                    {
                        scriptPath = scriptInfo.ScriptPath
                    };
                    if (scriptInfo.Dynamics != null)
                    {
                        foreach (var dynamicInfo in scriptInfo.Dynamics)
                        {
                            scriptResult.dynamics.Add(new DynamicDetectionBatchDynamicResult
                            {
                                type = dynamicInfo.Type,
                                name = dynamicInfo.Name,
                                dynamicType = dynamicInfo.DynamicType.ToString(),
                            });
                        }
                    }

                    result.scripts.Add(scriptResult);
                }
            }

            return result;
        }

        public static void ExportJson(DynamicDetectionManager manager, string outputJson)
        {
            if (string.IsNullOrEmpty(outputJson)) return;
            string fullPath = Path.GetFullPath(outputJson);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, JsonUtility.ToJson(BuildExport(manager), true));
            Debug.Log($"Dynamic detection batch result exported to {fullPath}");
        }

        private static bool ConfirmBake(List<Type> scriptTypesNeedsUpdate, int totalScripts)
        {
            StringBuilder sb = new();
            sb.AppendLine($"{scriptTypesNeedsUpdate.Count} scripts need update. ({scriptTypesNeedsUpdate.Count}/{totalScripts})\n");
            sb.AppendLine("Do you want to proceed?");
            foreach (var type in scriptTypesNeedsUpdate)
            {
                sb.AppendLine($" - {SourceUtils.GetScriptPathOrNameByType(type)}");
            }

            return EditorUtility.DisplayDialog("Dynamic Detection", sb.ToString(), "Ok", "Cancel");
        }

        private static bool IsIgnoredComponentType(Type type)
        {
            if (IgnoredComponentTypes.Contains(type)) return true;
            foreach (var item in IgnoredComponentTypes)
            {
                if (type.IsSubclassOf(item)) return true;
            }

            return type.Namespace != null && type.Namespace.StartsWith("UnityEngine.UI");
        }

        private static Dictionary<string, GObjectDynamicInfo> BakeObjectInfo(
            Dictionary<string, ScriptDynamicInfo> scriptInfoDict,
            DynamicDetectionBakeOptions options)
        {
            double time = EditorApplication.timeSinceStartup;
            List<GameObject> allObjects = MetaInfo.CollectAll().Select(x => x.obj).ToList();
            Dictionary<string, GObjectDynamicInfo> objectInfoDict = new();
            foreach (GameObject obj in allObjects)
            {
                SetObjectDynamicType(objectInfoDict, obj, ObjectDynamicType.Static);
                var components = obj.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    if (comp is Behaviour { enabled: false }) continue;
                    string scriptPath = SourceUtils.GetScriptPathOrNameByType(comp.GetType());
                    if (scriptPath == null) continue;
                    if (!scriptInfoDict.ContainsKey(scriptPath)) continue;
                    var dynamicInfos = scriptInfoDict[scriptPath].Dynamics;
                    if (dynamicInfos == null || dynamicInfos.Length == 0) continue;
                    foreach (var dynamicInfo in dynamicInfos)
                    {
                        if (dynamicInfo.Type == "this")
                        {
                            if (obj.GetComponentsInChildren<Renderer>().Length == 0) continue;
                            SetObjectDynamicType(objectInfoDict, obj, dynamicInfo.DynamicType,
                                options.PropagateDynamicToChildren);
                        }
                        else if (dynamicInfo.Type == "field")
                        {
                            GameObject fieldObj = GetFieldRelatedGameObject(comp, dynamicInfo.Name);
                            if (fieldObj == null) continue;
                            if (!fieldObj.activeInHierarchy) continue;
                            if (fieldObj.GetComponentsInChildren<Renderer>().Length == 0) continue;
                            Debug.Log(
                                $"Object {obj.name} field {dynamicInfo.Name} references dynamic object {fieldObj.name} (id={fieldObj.GetComponent<MetaInfo>()?.uid})");
                            SetObjectDynamicType(objectInfoDict, fieldObj, dynamicInfo.DynamicType,
                                options.PropagateDynamicToChildren);
                        }
                    }
                }
            }

            ApplyMarkedType(objectInfoDict, options.PropagateDynamicToChildren);
            LastBakeStats.objectBakeSeconds = EditorApplication.timeSinceStartup - time;
            Debug.Log($"Baking object dynamic info done. time_cost={LastBakeStats.objectBakeSeconds:F2}");
            return objectInfoDict;
        }

        private static void SetObjectDynamicType(Dictionary<string, GObjectDynamicInfo> objectInfoDict, GameObject obj,
            ObjectDynamicType dynamicType, bool propagateToChildren = false)
        {
            SetSingleObjectDynamicType(objectInfoDict, obj, dynamicType);
            if (!propagateToChildren || !dynamicType.IsDynamic()) return;
            foreach (MetaInfo childMeta in obj.GetComponentsInChildren<MetaInfo>())
            {
                if (childMeta == null || childMeta.gameObject == obj) continue;
                SetSingleObjectDynamicType(objectInfoDict, childMeta.gameObject, dynamicType);
            }
        }

        private static void SetSingleObjectDynamicType(Dictionary<string, GObjectDynamicInfo> objectInfoDict,
            GameObject obj, ObjectDynamicType dynamicType)
        {
            if (!obj.TryGetComponent(out MetaInfo metaInfo))
            {
                Debug.LogWarning($"Object {obj.name} has no MetaInfo component, skipped.");
                return;
            }

            string objectId = metaInfo.uid;
            if (objectInfoDict.TryGetValue(objectId, out var info))
            {
                if (info.DynamicType < dynamicType)
                    info.DynamicType = dynamicType;
            }
            else
            {
                objectInfoDict[objectId] = new GObjectDynamicInfo
                {
                    ObjectId = objectId,
                    ObjectName = obj.name,
                    DynamicType = dynamicType
                };
            }

            objectInfoDict[objectId].MarkedType =
                obj.GetComponent<DynamicMarker>()?.dynamicType ?? ObjectDynamicType.Static;
        }

        private static void ApplyMarkedType(Dictionary<string, GObjectDynamicInfo> objectInfoDict,
            bool propagateToChildren)
        {
            foreach (var info in objectInfoDict.Values)
            {
                info.MarkedType = ObjectDynamicType.Static;
            }

            foreach ((GameObject obj, ObjectDynamicType type, ObjectDynamicType propagationType) in DynamicMarker.CollectAllWithPropagation())
            {
                if (obj == null || !type.IsDynamic()) continue;
                SetMarkedType(objectInfoDict, obj, type);
                if (!propagateToChildren) continue;
                foreach (MetaInfo childMeta in obj.GetComponentsInChildren<MetaInfo>())
                {
                    if (childMeta == null || childMeta.gameObject == obj) continue;
                    SetMarkedType(objectInfoDict, childMeta.gameObject, propagationType);
                }
            }
        }

        private static void SetMarkedType(Dictionary<string, GObjectDynamicInfo> objectInfoDict, GameObject obj,
            ObjectDynamicType type)
        {
            if (!obj.TryGetComponent(out MetaInfo metaInfo)) return;
            if (!objectInfoDict.TryGetValue(metaInfo.uid, out var info)) return;
            if (info.MarkedType < type)
            {
                info.MarkedType = type;
            }
        }

        private static Dictionary<string, ObjectDynamicType> PropagateDynamicTypes(
            List<(GameObject obj, ObjectDynamicType type, ObjectDynamicType propagationType)> markers,
            Dictionary<GameObject, string> objToIdMap)
        {
            Dictionary<string, ObjectDynamicType> propagated = markers
                .ToDictionary(x => objToIdMap[x.obj], x => x.type);
            foreach (var marker in markers)
            {
                if (!marker.propagationType.IsDynamic()) continue;
                foreach (MetaInfo childMeta in marker.obj.GetComponentsInChildren<MetaInfo>())
                {
                    if (childMeta == null || string.IsNullOrEmpty(childMeta.uid)) continue;
                    if (childMeta.gameObject == marker.obj) continue;
                    ObjectDynamicType current = propagated.GetValueOrDefault(childMeta.uid, ObjectDynamicType.Static);
                    if (current < marker.propagationType)
                    {
                        propagated[childMeta.uid] = marker.propagationType;
                    }
                }
            }

            return propagated;
        }

        private static string GetCommonPropertyName(string s)
        {
            return "m_" + char.ToUpper(s[0]) + s.Substring(1);
        }

        private static GameObject GetFieldRelatedGameObject(Component comp, string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
            {
                Debug.LogWarning(
                    $"Dynamic field name is empty in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())}");
                return null;
            }

            FieldInfo field = comp.GetType().GetField(fieldName);
            SerializedObject serObj = new SerializedObject(comp);
            SerializedProperty serializedProperty = serObj.FindProperty(fieldName);
            if (serializedProperty == null)
            {
                serializedProperty = serObj.FindProperty(GetCommonPropertyName(fieldName));
            }

            if (field == null && serializedProperty == null)
            {
                Debug.LogWarning(
                    $"Dynamic field {fieldName} not found in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())}");
                return null;
            }

            var propValue = serializedProperty != null &&
                            serializedProperty.propertyType == SerializedPropertyType.ObjectReference
                ? serializedProperty.objectReferenceValue
                : null;
            var fieldValue = field != null ? field.GetValue(comp) : propValue;
            if (fieldValue == null)
            {
                Debug.Log(
                    $"Dynamic field {fieldName} in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())} exists but is null.");
                return null;
            }
            // if (fieldValue == null)
            // {
            //     // Unity 的 VideoPlayer.targetMaterialRenderer 官方行为是：当它为 null 且 renderMode = MaterialOverride 时，会使用当前 GameObject 上的第一个 Renderer。
            //     // Unity 文档也明确写了这一点：https://docs.unity3d.com/ScriptReference/Video.VideoPlayer-targetMaterialRenderer.html
            //     // 但是这涉嫌实验的特殊处理，先注释
            //     // GameObject fallbackObj = GetBuiltinFieldFallbackGameObject(comp, fieldName);
            //     // if (fallbackObj != null)
            //     // {
            //     //     return fallbackObj;
            //     // }
            //
            //     Debug.Log(
            //         $"Dynamic field {fieldName} in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())} exists but is null.");
            //     return null;
            // }

            if (fieldValue is UnityEngine.Object obj && obj == null)
            {
                Debug.Log(
                    $"Dynamic field {fieldName} in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())} exists but references a destroyed or unloaded Unity object.");
                return null;
            }

            if (fieldValue is GameObject go) return go;
            if (fieldValue is Component component) return component.gameObject;
            Debug.LogWarning(
                $"Dynamic field {fieldName} in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())} is of unsupported type {fieldValue.GetType()}");
            return null;
        }

        // private static GameObject GetBuiltinFieldFallbackGameObject(Component comp, string fieldName)
        // {
        //     if (comp is not VideoPlayer videoPlayer) return null;
        //     if (fieldName != "targetMaterialRenderer" && fieldName != "m_TargetMaterialRenderer") return null;
        //     if (videoPlayer.renderMode != VideoRenderMode.MaterialOverride) return null;
        //
        //     Renderer renderer = videoPlayer.targetMaterialRenderer;
        //     if (renderer == null)
        //     {
        //         renderer = comp.GetComponent<Renderer>();
        //     }
        //
        //     if (renderer == null) return null;
        //     Debug.Log(
        //         $"VideoPlayer targetMaterialRenderer is null; using renderer {renderer.name} on {renderer.gameObject.name} by Unity MaterialOverride fallback.");
        //     return renderer.gameObject;
        // }

        private static (List<Type>, int, List<Type>) CollectScriptsNeedsUpdate(Dictionary<string, ScriptDynamicInfo> scriptInfoDict)
        {
            HashSet<Type> scriptsVisit = new();
            List<Type> scriptTypesNeedsUpdate = new();
            List<Type> allScriptTypes = new();
            List<GameObject> allObjects = MetaInfo.CollectAll().Select(x => x.obj).ToList();
            foreach (GameObject obj in allObjects)
            {
                var components = obj.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    if (comp.GetType().Namespace != null &&
                        comp.GetType().Namespace!.StartsWith("SceneFlowTools")) continue;
                    if (IsIgnoredComponentType(comp.GetType())) continue;
                    if (scriptsVisit.Contains(comp.GetType())) continue;
                    scriptsVisit.Add(comp.GetType());
                    allScriptTypes.Add(comp.GetType());
                    string scriptPath = SourceUtils.GetScriptPathOrNameByType(comp.GetType());
                    if (!scriptInfoDict.ContainsKey(scriptPath))
                    {
                        scriptTypesNeedsUpdate.Add(comp.GetType());
                    }
                }
            }

            return (scriptTypesNeedsUpdate, scriptsVisit.Count, allScriptTypes);
        }

        private static void BakeScriptInfo(Dictionary<string, ScriptDynamicInfo> scriptInfoDict,
            List<Type> scriptTypesNeedsUpdate,
            List<Type> allScriptTypes,
            DynamicDetectionMode detectionMode,
            DynamicDetectionBakeOptions options)
        {
            if (detectionMode == DynamicDetectionMode.Agent)
            {
                BakeScriptInfoAgent(scriptInfoDict, scriptTypesNeedsUpdate, allScriptTypes, options);
                return;
            }

            BakeScriptInfoDirect(scriptInfoDict, scriptTypesNeedsUpdate, options);
        }

        private static string GetDetectionModeKey(DynamicDetectionMode detectionMode)
        {
            return detectionMode == DynamicDetectionMode.Agent
                ? AgentDetectionDataVersion
                : DynamicDetectionMode.DirectLLM.ToString();
        }

        private static void BakeScriptInfoDirect(Dictionary<string, ScriptDynamicInfo> scriptInfoDict,
            List<Type> scriptTypesNeedsUpdate,
            DynamicDetectionBakeOptions options)
        {
            double time = EditorApplication.timeSinceStartup;
            int batchSize = Math.Max(1, options.DirectBatchSize);
            Debug.Log($"Detecting scripts dynamic behaviors, batchSize={batchSize}...");

            List<(Type scriptType, string scriptPath, string source)> tasks = scriptTypesNeedsUpdate.Select(x =>
                (x, SourceUtils.GetScriptPathOrNameByType(x), SourceUtils.GetScriptSourceOrFieldsByType(x))
            ).ToList();

            int promptTokens = 0;
            int completionTokens = 0;

            Parallel.ForEach(
                tasks,
                new ParallelOptions()
                {
                    MaxDegreeOfParallelism = batchSize
                },
                task =>
                {
                    Debug.Log($"{DateTime.Now:HH:mm:ss} - Detecting script {task.scriptPath}...");
                    var result = ExternalUtils.DynamicDetection(task.scriptType.Name, task.source, options.UseCache).Result;
                    var scriptDynamicInfo = new ScriptDynamicInfo
                    {
                        ScriptPath = task.scriptPath,
                        Dynamics = result.results.ToArray()
                    };
                    lock (scriptInfoDict)
                    {
                        scriptInfoDict[task.scriptPath] = scriptDynamicInfo;
                        promptTokens += result.promptTokens;
                        completionTokens += result.completionTokens;
                    }
                }
            );

            Debug.Log(
                $"Detecting scripts dynamic behaviors done. time_cost={EditorApplication.timeSinceStartup - time:F2}" +
                $" promptTokens={promptTokens} completionTokens={completionTokens}");
            LastBakeStats.promptTokens += promptTokens;
            LastBakeStats.completionTokens += completionTokens;
            LastBakeStats.scriptAnalysisSeconds += EditorApplication.timeSinceStartup - time;
        }

        private static void BakeScriptInfoAgent(Dictionary<string, ScriptDynamicInfo> scriptInfoDict,
            List<Type> scriptTypesNeedsUpdate,
            List<Type> allScriptTypes,
            DynamicDetectionBakeOptions options)
        {
            double time = EditorApplication.timeSinceStartup;
            Debug.Log("Detecting scripts dynamic behaviors with agent mode...");

            DynamicDetectionAgentRequest request = new()
            {
                sceneId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                scripts = allScriptTypes.Select(SourceUtils.GetDynamicDetectionContextScriptByType).ToList(),
                targetScriptPaths = scriptTypesNeedsUpdate
                    .Select(SourceUtils.GetScriptPathOrNameByType)
                    .ToList(),
                components = allScriptTypes
                    .Select(type => type.FullName ?? type.Name)
                    .Distinct()
                    .ToList(),
                useCache = options.UseCache,
                agentConfig = new DynamicDetectionAgentConfig
                {
                    maxToolCalls = options.AgentMaxToolCalls,
                    enableGlobalScriptSearch = true
                }
            };

            if (request.targetScriptPaths.Count == 0)
            {
                Debug.Log("No scripts need agent dynamic detection.");
                LastBakeStats.scriptAnalysisSeconds += EditorApplication.timeSinceStartup - time;
                return;
            }

            var result = ExternalUtils.DynamicDetectionAgent(request).Result;
            foreach (var scriptResult in result.scripts)
            {
                if (scriptResult.warnings != null)
                {
                    foreach (string warning in scriptResult.warnings)
                    {
                        Debug.LogWarning($"Agent dynamic detection warning for {scriptResult.scriptPath}: {warning}");
                    }
                }

                scriptInfoDict[scriptResult.scriptPath] = new ScriptDynamicInfo
                {
                    ScriptPath = scriptResult.scriptPath,
                    Dynamics = scriptResult.dynamics ?? Array.Empty<DynamicInfo>()
                };
            }

            Debug.Log(
                $"Agent detecting scripts dynamic behaviors done. time_cost={EditorApplication.timeSinceStartup - time:F2}" +
                $" promptTokens={result.promptTokens} completionTokens={result.completionTokens} toolCalls={result.toolCalls}");
            LastBakeStats.promptTokens += result.promptTokens;
            LastBakeStats.completionTokens += result.completionTokens;
            LastBakeStats.toolCalls += result.toolCalls;
            LastBakeStats.scriptAnalysisSeconds += EditorApplication.timeSinceStartup - time;
        }
    }

    [Serializable]
    public class DynamicDetectionBakeStats
    {
        public int promptTokens;
        public int completionTokens;
        public int toolCalls;
        public double scriptAnalysisSeconds;
        public double objectBakeSeconds;
        public double totalBakeSeconds;
    }

    [Serializable]
    public class DynamicDetectionBatchExport
    {
        public string sceneName;
        public string scenePath;
        public string detectionMode;
        public string detectionModeKey;
        public bool propagateDynamicToChildren;
        public int promptTokens;
        public int completionTokens;
        public int toolCalls;
        public double scriptAnalysisSeconds;
        public double objectBakeSeconds;
        public double totalBakeSeconds;
        public List<DynamicDetectionBatchObjectResult> objects = new();
        public List<DynamicDetectionBatchScriptResult> scripts = new();
    }

    [Serializable]
    public class DynamicDetectionBatchObjectResult
    {
        public string objectId;
        public string objectName;
        public string objectPath;
        public string trueLabel;
        public string predictedLabel;
        public bool hasDynamicMarker;
        public bool trueLabelPropagated;
    }

    [Serializable]
    public class DynamicDetectionBatchScriptResult
    {
        public string scriptPath;
        public List<DynamicDetectionBatchDynamicResult> dynamics = new();
    }

    [Serializable]
    public class DynamicDetectionBatchDynamicResult
    {
        public string type;
        public string name;
        public string dynamicType;
    }
}
