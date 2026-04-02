using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Config;
using SceneFlowTools.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
// using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using Object = UnityEngine.Object;

namespace SceneFlowTools.Editor.Config
{
    [CustomEditor(typeof(RenderConfigManager))]
    public class RenderConfigManagerEditor : UnityEditor.Editor
    {
        private bool _showGizmosNodeSettings = true;

        public override void OnInspectorGUI()
        {
            RenderConfigManager tg = (RenderConfigManager)target;
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RenderConfigManager.allowEmptyNodes)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(RenderConfigManager.sceneDivision)));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(RenderConfigManager.dynamicDetectionManager)));
            EditorGUILayout.PropertyField(
                 serializedObject.FindProperty(nameof(RenderConfigManager.player)));
            GUIUtils.DataBakeClear(serializedObject.FindProperty(nameof(RenderConfigManager.sceneConfig)),
                ServiceConfigBake,
                ServiceConfigClear);
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(RenderConfigManager.startSender)));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(RenderConfigManager.startReceiver)));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(RenderConfigManager.startPlayer)));
            EditorGUILayout.PropertyField(
                serializedObject.FindProperty(nameof(RenderConfigManager.ignore)));


            EditorGUILayout.LabelField("Node Count", tg.sceneConfig?.scenes?.Count.ToString() ?? "None");


            _showGizmosNodeSettings = EditorGUILayout.Foldout(_showGizmosNodeSettings, "Gizmos Node Settings");
            if (_showGizmosNodeSettings)
            {
                tg.nodeGizmosSettings.enabled = EditorGUILayout.Toggle("Enabled", tg.nodeGizmosSettings.enabled);
                tg.nodeGizmosSettings.includeChildren =
                    EditorGUILayout.Toggle("Include Children", tg.nodeGizmosSettings.includeChildren);
                tg.nodeGizmosSettings.showMesh = EditorGUILayout.Toggle("Show Mesh", tg.nodeGizmosSettings.showMesh);
                tg.nodeGizmosSettings.node = GUIUtils.IntPicker("Node ID", tg.nodeGizmosSettings.node, -1,
                    tg.sceneConfig?.scenes?.Count ?? -1, -1);
                if (tg.sceneConfig?.scenes != null && tg.nodeGizmosSettings.node != -1)
                {
                    EditorGUILayout.LabelField("Gizmos Node Pos",
                        Subscene.GetNodePos(tg.sceneConfig.scenes, tg.nodeGizmosSettings.node).ToString());
                    EditorGUILayout.LabelField("Parent Node:",
                        tg.sceneConfig.scenes[tg.nodeGizmosSettings.node].parentId.ToString());
                    EditorGUILayout.LabelField("Child Nodes:",
                        string.Join(", ", tg.sceneConfig.scenes[tg.nodeGizmosSettings.node].subscenes));
                    EditorGUILayout.LabelField("Object Count",
                        tg.sceneConfig.scenes[tg.nodeGizmosSettings.node].objectIds.Count.ToString());
                    EditorGUILayout.LabelField("User Probability",
                        tg.sceneConfig.scenes[tg.nodeGizmosSettings.node].userProbability.ToString("F"));
                }
            }

            if (GUILayout.Button("Output"))
            {
                StringBuilder sb = new StringBuilder();
                Queue<int> sceneIdQueue = new Queue<int>();
                sceneIdQueue.Enqueue(0);
                while (sceneIdQueue.Any())
                {
                    int currentSceneId = sceneIdQueue.Dequeue();
                    Subscene currentScene = tg.sceneConfig.scenes[currentSceneId];
                    foreach (var subsceneId in currentScene.subscenes)
                    {
                        sb.AppendLine($"{currentSceneId} --> {subsceneId}");
                        sceneIdQueue.Enqueue(subsceneId);
                    }
                }

                Debug.Log(sb.ToString());
            }


            serializedObject.ApplyModifiedProperties();
        }

        private void ServiceConfigBake()
        {
            RenderConfigManager tg = (RenderConfigManager)target;
            var globalDivisionResult = tg.sceneDivision?.globalDivisionResult;
            var roomDivisionResult = tg.sceneDivision?.roomDivisionResult;
            var dynamicDetectionData = tg.dynamicDetectionManager?.data;
            if (globalDivisionResult == null)
            {
                Debug.LogError("Please bake Global Division first.");
                return;
            }

            if (roomDivisionResult == null)
            {
                Debug.LogError("Please bake Room Division first.");
                return;
            }

            if (dynamicDetectionData == null)
            {
                Debug.LogError("Please bake Dynamic Detection Data first.");
                return;
            }

            Dictionary<GameObject, string> mapObj2Id = new Dictionary<GameObject, string>();
            Dictionary<string, GameObject> mapId2Obj = new Dictionary<string, GameObject>();
            List<(GameObject gameObject, string id)> objects = MetaInfo.CollectAll();
            foreach (var (gObj, id) in objects)
            {
                mapObj2Id[gObj] = id;
                mapId2Obj[id] = gObj;
            }

            
            
            
            
            
            
            
            
            
            
            List<Subscene> subscenes = new List<Subscene>();
            // root
            subscenes.Add(new Subscene()
            {
                id = 0,
                parentId = -1,
                objectIds = new List<string>(),
                subscenes = new List<int>(),
                bounds = BoundsUtils.From(mapId2Obj.Values)!.Value,
            });
            var meshObjects = tg.sceneDivision.GetMeshObjectList().meshObjects;
            foreach (var group in globalDivisionResult.groups)
            {
                List<string> objIds = group
                    .Select(x => meshObjects[x].gameObject)
                    .Where(x => mapObj2Id.ContainsKey(x))
                    .Select(x => mapObj2Id[x])
                    .ToList();
                Subscene scene = new Subscene()
                {
                    id = subscenes.Count,
                    parentId = -1,
                    objectIds = objIds,
                    subscenes = new List<int>(),
                    bounds = BoundsUtils.From(objIds.Select(x => mapId2Obj[x]))!.Value,
                };
                subscenes.Add(scene);
            }

            GenerateTree(subscenes, false);

            int globalDivisionNodeCount = subscenes.Count;

            foreach (var room in roomDivisionResult.regions)
            {
                List<string> objIds = room.objects.Select(x => mapObj2Id[meshObjects[x].gameObject]).ToList();
                Subscene scene = new Subscene()
                {
                    id = subscenes.Count,
                    parentId = 0,
                    objectIds = objIds,
                    subscenes = new List<int>(),
                    bounds = BoundsUtils.From(objIds.Select(x => mapId2Obj[x]))!.Value,
                };
                bool flag = false;
                for (int i = 1; i < globalDivisionNodeCount; i++)
                {
                    if (!subscenes[i].bounds.Contains(scene.bounds, tg.boundsContainsEps, true, true, true)) continue;
                    flag = true;
                    break;
                }

                if (flag) subscenes.Add(scene);
                else Debug.Log($"Room {room.id} is out of global division bounds, ignored.");
            }

            GenerateTree(subscenes, true);

            CollectChildrenByParentId(subscenes);

            
            DeleteObjectsInChildren(subscenes, 0);
            
            
            if (!tg.allowEmptyNodes)
            {
                SubsceneUtils.DeleteEmptyNodes(subscenes);
            }

            
            // UniqueObjectsInBrothers(subscenes, 0);

            // SubsceneUtils.DeleteEmptyNodes(subscenes, 0);
            // SubsceneUtils.RemoveNulls(subscenes);

            UpdateMetrics(subscenes);

            
            
            // foreach (var scene in subscenes)
            // {
            //     var objs = scene.objectIds
            //         .Where(id => mapId2Obj.ContainsKey(id))
            //         .Select(id => mapId2Obj[id])
            //         .ToList();
            //     var bounds = BoundsUtils.From(objs);
            //     if (bounds.HasValue)
            //     {
            //         scene.bounds = bounds.Value;
            //     }
            //     else
            //     {
            //         Debug.LogWarning($"Subscene {scene.id} has no valid objects to calculate bounds.");
            //         scene.bounds = new Bounds(Vector3.zero, Vector3.zero);
            //     }
            // }

            
            
            // GenerateTree(subscenes, true);
            // CollectChildrenByParentId(subscenes);


            foreach (var scene in subscenes)
            {
                if (scene.objectIds.Count == 0)
                {
                    Debug.LogWarning($"Subscene {scene.id} has no objects.");
                }
            }

            
            tg.sceneConfig = ScriptableObject.CreateInstance<SceneConfig>();
            tg.sceneConfig.scenes = subscenes;
            SceneUtils.SaveSceneAsset(tg.gameObject.scene, SceneConfig.AssetName,
                tg.sceneConfig);
            EditorUtility.SetDirty(tg);
            EditorSceneManager.MarkSceneDirty(tg.gameObject.scene);
        }

        private void CollectChildrenByParentId(List<Subscene> subscenes)
        {
            foreach (var subscene in subscenes)
            {
                subscene.subscenes.Clear();
            }
            
            for (int i = 1; i < subscenes.Count; i++)
            {
                int parentId = subscenes[i].parentId;
                if (parentId == -1)
                {
                    Debug.LogWarning($"Subscene {subscenes[i].id} has no parent, attach to root.");
                    subscenes[0].subscenes.Add(subscenes[i].id);
                    subscenes[i].parentId = 0;
                    continue;
                }

                subscenes[parentId].subscenes.Add(subscenes[i].id);
            }
        }

        
        
        private HashSet<string> DeleteObjectsInChildren(List<Subscene> subscenes, int p)
        {
            Subscene node = subscenes[p];
            HashSet<string> objectIds = new HashSet<string>(node.objectIds);
            HashSet<string> currentObjectIds = new HashSet<string>(node.objectIds);
            foreach (int cid in node.subscenes)
            {
                HashSet<string> childObjectIds = DeleteObjectsInChildren(subscenes, cid);
                currentObjectIds.ExceptWith(childObjectIds);
                objectIds.UnionWith(childObjectIds);
            }

            node.objectIds = currentObjectIds.ToList();
            return objectIds;
        }

        
        private void UniqueObjectsInBrothers(List<Subscene> subscenes, int p)
        {
            Subscene node = subscenes[p];
            HashSet<string> childObjectIds = new HashSet<string>();
            foreach (int cid in node.subscenes)
            {
                Subscene child = subscenes[cid];
                child.objectIds.RemoveAll(x => childObjectIds.Contains(x));
                childObjectIds.UnionWith(child.objectIds);
                UniqueObjectsInBrothers(subscenes, cid);
            }
        }

        private void GenerateTree(List<Subscene> subscenes, bool includeY)
        {
            
            var isUpstream = new Func<int, int, bool>((i, j) =>
            {
                if (i == -1 || j == -1 || subscenes[j].parentId == -1) return false;
                j = subscenes[j].parentId;
                while (i != j && subscenes[j].parentId != -1)
                {
                    j = subscenes[j].parentId;
                }
            
                return i == j;
            });
            bool flag = true;
            while (flag)
            {
                flag = false;
                for (int i = 1; i < subscenes.Count; i++)
                {
                    for (int j = 0; j < subscenes.Count; j++)
                    {
                        if (i == j) continue;
                        if (subscenes[i].parentId == j) continue;
                        if (SubsceneUtils.GetDepth(subscenes, i) > SubsceneUtils.GetDepth(subscenes, j)) continue;
                        if (!subscenes[j].bounds.Contains(subscenes[i].bounds,
                                ((RenderConfigManager)target).boundsContainsEps, true, includeY, true)) continue;
                        if (isUpstream(i, j)) continue;
                        // {
                        //     subscenes[j].parentId = subscenes[i].parentId;
                        // }
                        subscenes[i].parentId = j;
                        flag = true;
                        // break;
                    }
                }
            }
        }

        private void UpdateMetrics(List<Subscene> scenes)
        {
            Dictionary<string, GameObject> objMap = MetaInfo.CollectAllDict();
            List<UserProbabilityMarker> userProbs = FindObjectsOfType<UserProbabilityMarker>().ToList();

            foreach (var scene in scenes)
            {
                scene.metrics = new SubsceneMetrics()
                {
                    vertexCount = 0,
                    triangleCount = 0,
                    gpuMemory = 0,
                };
                foreach (var marker in userProbs)
                {
                    scene.userProbability += marker.weight *
                                             BoundsUtils.Intersect(scene.bounds, marker.markerBounds).Volume();
                }

                HashSet<Texture> texSet = new HashSet<Texture>();
                foreach (var objId in scene.objectIds)
                {
                    if (!objMap.TryGetValue(objId, out var obj)) continue;
                    var metaInfo = obj.GetComponent<MetaInfo>();
                    if (metaInfo == null) continue;
                    MeshFilter mf = obj.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    scene.metrics.vertexCount += mf.sharedMesh.vertexCount;
                    scene.metrics.triangleCount += mf.sharedMesh.triangles.Length / 3;

                    var renderer = obj.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;
                    foreach (var mat in renderer.sharedMaterials)
                    {
                        if (mat == null) continue;
                        foreach (var name in mat.GetTexturePropertyNames())
                        {
                            var tex = mat.GetTexture(name);
                            if (tex != null) texSet.Add(tex);
                        }
                    }
                }

                foreach (var tex in texSet)
                {
                    scene.metrics.gpuMemory += MyTextureUtils.GetStorageMemorySizeLong(tex);
                }
            }

            scenes[0].userProbability = 1;
            UpdateProbabilityAndMetricsInChildren(scenes, 0);
        }

        private void UpdateProbabilityAndMetricsInChildren(List<Subscene> scenes, int p)
        {
            Subscene node = scenes[p];
            double totalWeight = node.subscenes.Sum(cid => scenes[cid].userProbability);
            foreach (int cid in node.subscenes)
            {
                Subscene child = scenes[cid];
                if (totalWeight > 0)
                    child.userProbability *= node.userProbability / totalWeight;
                else
                    child.userProbability = 0;
            }

            foreach (int cid in node.subscenes)
            {
                UpdateProbabilityAndMetricsInChildren(scenes, cid);
            }

            node.metricsIncludeChildren = new SubsceneMetrics();
            foreach (int cid in node.subscenes)
            {
                Subscene child = scenes[cid];
                node.metricsIncludeChildren.gpuMemory += child.metricsIncludeChildren.gpuMemory;
                node.metricsIncludeChildren.vertexCount += child.metricsIncludeChildren.vertexCount;
                node.metricsIncludeChildren.triangleCount += child.metricsIncludeChildren.triangleCount;
            }
        }

        private void ServiceConfigClear()
        {
            RenderConfigManager tg = (RenderConfigManager)target;
            tg.sceneConfig = null;
            EditorUtility.SetDirty(tg);
            EditorSceneManager.MarkSceneDirty(tg.gameObject.scene);
            SceneUtils.DeleteSceneAsset(tg.gameObject.scene, SceneConfig.AssetName);
        }
    }
}