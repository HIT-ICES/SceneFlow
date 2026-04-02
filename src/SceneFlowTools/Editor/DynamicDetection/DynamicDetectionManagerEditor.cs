using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEditor;
using UnityEngine;
using Unity.EditorCoroutines.Editor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;
using Utils;


namespace SceneFlowTools.Editor.DynamicDetection
{
    [CustomEditor(typeof(DynamicDetectionManager))]
    public class DynamicDetectionManagerEditor : UnityEditor.Editor
    {
        // Common Unity built-in types that are ignored in dynamic detection
        public readonly HashSet<Type> IgnoredComponentTypes = new()
        {
            // basic
            typeof(Transform),
            typeof(RectTransform),
            typeof(MeshFilter),

            // render
            typeof(MeshRenderer),
            typeof(SkinnedMeshRenderer),
            typeof(SpriteRenderer),
            typeof(ParticleSystemRenderer),
            typeof(LineRenderer),
            typeof(TrailRenderer),
            typeof(CanvasRenderer),

            // Audio
            typeof(AudioSource),
            typeof(AudioListener),

            // light (light is ignored)
            typeof(Light),
            typeof(LightProbeGroup),
            typeof(ReflectionProbe),

            // physics
            typeof(Rigidbody),
            typeof(Collider),
            typeof(Joint),

            // UI
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(EventSystem),
            typeof(GraphicRaycaster),

            // dynamic but ignored
            typeof(CharacterController),

            // others
            typeof(Camera),
        };

        private bool IsIgnoredComponentType(Type type)
        {
            if (IgnoredComponentTypes.Contains(type)) return true;
            foreach (var item in IgnoredComponentTypes)
            {
                if (type.IsSubclassOf(item)) return true;
            }

            // skip ugui components
            if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine.UI")) return true;
            return false;
        }

        public override void OnInspectorGUI()
        {
            DynamicDetectionManager ddm = (DynamicDetectionManager)target;
            serializedObject.Update();
            // EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ddm.meshObjectList)));
            GUIUtils.DataBakeClear(
                serializedObject.FindProperty(nameof(ddm.data)),
                Bake,
                Clear
            );
            ddm.gizmosDynamicObjects = EditorGUILayout.Toggle("Gizmos Dynamic Objects", ddm.gizmosDynamicObjects);

            // if (GUILayout.Button("test"))
            // {
            //     EditorCoroutineUtility.StartCoroutineOwnerless(TestCoroutine());
            // }

            serializedObject.ApplyModifiedProperties();
        }

        public void Clear()
        {
            DynamicDetectionManager tg = (DynamicDetectionManager)target;
            if (tg.data is null) return;
            SceneUtils.DeleteSceneAsset(tg.gameObject.scene, "DynamicDetectionResults");
            tg.data = null;
            EditorUtility.SetDirty(tg);
            EditorSceneManager.MarkSceneDirty(tg.gameObject.scene);
        }

        public void Bake()
        {
            DynamicDetectionManager tg = (DynamicDetectionManager)target;
            tg.cachedGizmosDynamic = null;

            if (tg.data == null)
            {
                tg.data = ScriptableObject.CreateInstance<DynamicDetectionData>();
                tg.data.ObjectsDynamicInfo = new List<GObjectDynamicInfo>();
                tg.data.ScriptsDynamicInfo = new List<ScriptDynamicInfo>();
                SceneUtils.SaveSceneAsset(tg.gameObject.scene, "DynamicDetectionResults", tg.data);
                EditorUtility.SetDirty(tg);
                EditorSceneManager.MarkSceneDirty(tg.gameObject.scene);
            }

            Debug.Log("Detecting dynamic behaviors in the scene...");
            Dictionary<string, ScriptDynamicInfo> scriptInfoDict = new();
            foreach (var scriptDynamicInfo in tg.data.ScriptsDynamicInfo)
            {
                scriptInfoDict[scriptDynamicInfo.ScriptPath] = scriptDynamicInfo;
                Debug.Log($"Detected script info: {scriptDynamicInfo.ScriptPath}");
            }


            var (scriptTypesNeedsUpdate, totalScripts) = CollectScriptsNeedsUpdate(scriptInfoDict);
            Debug.Log(
                $"{scriptTypesNeedsUpdate.Count} scripts need update. ({scriptTypesNeedsUpdate.Count}/{totalScripts})");
            StringBuilder sb = new();
            sb.AppendLine(
                $"{scriptTypesNeedsUpdate.Count} scripts need update. ({scriptTypesNeedsUpdate.Count}/{totalScripts})\n");
            sb.AppendLine($"Do you want to proceed?");
            foreach (var type in scriptTypesNeedsUpdate)
            {
                sb.AppendLine($" - {SourceUtils.GetScriptPathOrNameByType(type)}");
            }

            if (!EditorUtility.DisplayDialog("Dynamic Detection", sb.ToString(), "Ok", "Cancel"))
            {
                Debug.Log("Dynamic detection cancelled by user.");
                return;
            }

            BakeScriptInfo(scriptInfoDict, scriptTypesNeedsUpdate);
            Dictionary<string, GObjectDynamicInfo> objectInfoDict = BakeObjectInfo(scriptInfoDict);

            int dynamicObjCount = objectInfoDict.Values.Count(info => info.DynamicType.IsDynamic());
            Debug.Log($"Dynamic detection done. {objectInfoDict.Count} objects detected, {dynamicObjCount} dynamic.");
            // LogDiffs(data.ObjectsDynamicInfo.ToDictionary(info => info.ObjectId), objectInfoDict);

            tg.data.ScriptsDynamicInfo = scriptInfoDict.Values.ToList();
            tg.data.ObjectsDynamicInfo = objectInfoDict.Values
                .Where(info => info.DynamicType != ObjectDynamicType.Static).ToList();

            MetaInfo.AssertExistsIds(tg.data.ObjectsDynamicInfo.Select(x => x.ObjectId));

            EditorUtility.SetDirty(tg.data);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Dictionary<string, GObjectDynamicInfo> BakeObjectInfo(
            Dictionary<string, ScriptDynamicInfo> scriptInfoDict)
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
                    if (comp is Behaviour { enabled: false }) continue; // skip disabled components
                    string scriptPath = SourceUtils.GetScriptPathOrNameByType(comp.GetType());
                    if (scriptPath == null) continue;
                    if (!scriptInfoDict.ContainsKey(scriptPath)) continue;
                    var dynamicInfos = scriptInfoDict[scriptPath].Dynamics;
                    if (dynamicInfos == null || dynamicInfos.Length == 0) continue;
                    foreach (var dynamicInfo in dynamicInfos)
                    {
                        if (dynamicInfo.Type == "this")
                        {
                            // 过滤掉没有Renderer的物体
                            if (obj.GetComponentsInChildren<Renderer>().Length == 0) continue;
                            SetObjectDynamicType(objectInfoDict, obj, dynamicInfo.DynamicType);
                        }
                        else if (dynamicInfo.Type == "field")
                        {
                            GameObject fieldObj = GetFieldRelatedGameObject(comp, dynamicInfo.Name);
                            // Debug.Log(
                                // $"reference: {comp.name} on {obj.name} --{dynamicInfo.Name}--> {fieldObj?.name}");
                            if (fieldObj == null) continue;
                            // 过滤掉未激活的物体
                            if (!fieldObj.activeInHierarchy) continue;
                            // 过滤掉没有Renderer的物体
                            if (fieldObj.GetComponentsInChildren<Renderer>().Length == 0) continue;
                            Debug.Log(
                                $"Object {obj.name} field {dynamicInfo.Name} references dynamic object {fieldObj.name} (id={fieldObj.GetComponent<MetaInfo>()?.uid})");
                            SetObjectDynamicType(objectInfoDict, fieldObj, dynamicInfo.DynamicType);
                        }
                    }
                }
            }

            Debug.Log($"Baking object dynamic info done. time_cost={EditorApplication.timeSinceStartup - time:F2}");
            return objectInfoDict;
        }

        private void LogDiffs(Dictionary<string, GObjectDynamicInfo> oldDict,
            Dictionary<string, GObjectDynamicInfo> newDict)
        {
            List<GameObject> allObjects = MetaInfo.CollectAll().Select(x => x.obj).ToList();
            Dictionary<string, GameObject> idToObj = new();

            foreach (GameObject obj in allObjects)
            {
                if (obj.TryGetComponent(out MetaInfo metaInfo))
                {
                    idToObj[metaInfo.uid] = obj;
                }
            }

            foreach (var kv in newDict)
            {
                if (oldDict.TryGetValue(kv.Key, out var oldInfo))
                {
                    if (oldInfo.DynamicType != kv.Value.DynamicType)
                    {
                        Debug.Log(
                            $"Object {idToObj[kv.Key]} dynamic type changed from {oldInfo.DynamicType} to {kv.Value.DynamicType}");
                    }
                }
                else
                {
                    Debug.Log($"Object {idToObj[kv.Key]} added with dynamic type {kv.Value.DynamicType}");
                }
            }

            foreach (var kv in oldDict)
            {
                if (!newDict.ContainsKey(kv.Key))
                {
                    Debug.Log($"Object {idToObj[kv.Key]} removed");
                }
            }
        }

        private void SetObjectDynamicType(Dictionary<string, GObjectDynamicInfo> objectInfoDict, GameObject obj,
            ObjectDynamicType dynamicType)
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
                // Debug.Log($"Object {obj.name} ({objectId}) dynamic type updated to {dynamicType}");
            }
            else
            {
                objectInfoDict[objectId] = new GObjectDynamicInfo
                {
                    ObjectId = objectId,
                    ObjectName = obj.name,
                    DynamicType = dynamicType
                };
                // Debug.Log($"Object {obj.name} ({objectId}) dynamic type set to {dynamicType}");
            }

            // record marked type, for analysis
            objectInfoDict[objectId].MarkedType =
                obj.GetComponent<DynamicMarker>()?.dynamicType ?? ObjectDynamicType.Static;
        }

        private string GetCommonPropertyName(string s)
        {
            return "m_" + char.ToUpper(s[0]) + s.Substring(1);
        }

        private GameObject GetFieldRelatedGameObject(Component comp, string fieldName)
        {
            // fieldName 也可能被SerializeField属性覆盖
            // if (comp is VideoPlayer videoPlayer)
            // {
            //     var targetRenderer = videoPlayer.targetMaterialRenderer;
            //     Debug.Log(
            //         $"Special case VideoPlayer field {fieldName} in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())}, targetRenderer={targetRenderer}");
            // }
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

            var propValue = serializedProperty.propertyType == SerializedPropertyType.ObjectReference
                ? serializedProperty.objectReferenceValue
                : null;
            var fieldValue = field != null ? field.GetValue(comp) : propValue;
            // Debug.Log(
            // $"gameobj={comp.gameObject} comp={comp.GetType().Name} fieldName={fieldName} field={field} prop={serializedProperty} value={fieldValue} value==null?{fieldValue == null}");
            if (fieldValue == null) return null;
            if (fieldValue is UnityEngine.Object obj && obj == null) return null; // handle destroyed UnityEngine.Object
            if (fieldValue is GameObject go) return go;
            if (fieldValue is Component component) return component.gameObject;
            // if (fieldValue is IEnumerable<Component> compEnumerable)
            // {
            //     return compEnumerable.Select(c => c.gameObject).FirstOrDefault();
            // }
            // if (fieldValue is IEnumerable<GameObject> goEnumerable)
            // {
            //     return goEnumerable.FirstOrDefault();
            // }
            Debug.LogWarning(
                $"Dynamic field {fieldName} in script {SourceUtils.GetScriptPathOrNameByType(comp.GetType())} is of unsupported type {fieldValue.GetType()}");
            return null;
        }

        private (List<Type>, int) CollectScriptsNeedsUpdate(Dictionary<string, ScriptDynamicInfo> scriptInfoDict)
        {
            HashSet<Type> scriptsVisit = new();
            List<Type> scriptTypesNeedsUpdate = new();
            List<GameObject> allObjects = MetaInfo.CollectAll().Select(x => x.obj).ToList();
            foreach (GameObject obj in allObjects)
            {
                var components = obj.GetComponents<Component>();
                foreach (var comp in components)
                {
                    if (comp == null) continue;
                    // skip scripts in this package
                    if (comp.GetType().Namespace != null &&
                        comp.GetType().Namespace!.StartsWith("SceneFlowTools")) continue;
                    // skip ignored types
                    if (IsIgnoredComponentType(comp.GetType())) continue;
                    if (scriptsVisit.Contains(comp.GetType())) continue;
                    scriptsVisit.Add(comp.GetType());
                    string scriptPath = SourceUtils.GetScriptPathOrNameByType(comp.GetType());
                    if (!scriptInfoDict.ContainsKey(scriptPath))
                    {
                        scriptTypesNeedsUpdate.Add(comp.GetType());
                    }
                }
            }

            return (scriptTypesNeedsUpdate, scriptsVisit.Count);
        }

        private void BakeScriptInfo(Dictionary<string, ScriptDynamicInfo> scriptInfoDict,
            List<Type> scriptTypesNeedsUpdate)
        {
            double time = EditorApplication.timeSinceStartup;
            const int batchSize = 8;
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
                    var result = ExternalUtils.DynamicDetection(task.scriptType.Name, task.source).Result;
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
        }

        // public IEnumerator TestCoroutine()
        // {
        //     Debug.Log("Coroutine start");
        //     for (int i = 0; i < 10; i++)
        //     {
        //         bool cancel = EditorUtility.DisplayCancelableProgressBar("TestCoroutine", "TestCoroutine is running...", i/10f);
        //         if (cancel) break;
        //         yield return new EditorWaitForSeconds(1f);
        //     }
        //     EditorUtility.ClearProgressBar();
        //     Debug.Log("Coroutine end clear");
        // }
    }
}