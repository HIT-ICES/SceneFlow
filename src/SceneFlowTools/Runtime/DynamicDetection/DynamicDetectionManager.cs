using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicDetection
{
    [ExecuteAlways]
    public class DynamicDetectionManager : MonoBehaviour
    {
        public DynamicDetectionData data;
        public DynamicDetectionMode detectionMode = DynamicDetectionMode.DirectLLM;
        public bool propagateDynamicToChildren = true;
        [NonSerialized] public bool gizmosDynamicObjects;

        [NonSerialized]
        public List<GameObject> cachedGizmosDynamic, cachedGizmosInteractive, cachedGizmosWrong, cachedGizmosError;

        // private void Start()
        // {
        //     if (data != null) return;
        //     var sceneData = SceneUtils.LoadSceneAsset<DynamicDetectionData>(gameObject.scene, "DynamicDetectionResults");
        //     if (sceneData == null) return;
        //     data = sceneData;
        // }


        private void HighlightObject(GameObject obj, Color color)
        {
            Gizmos.color = color;
            var meshes = obj.GetComponentsInChildren<MeshFilter>();
            foreach (var mesh in meshes)
            {
                if (mesh == null || mesh.sharedMesh == null) continue;
                Gizmos.DrawWireMesh(mesh.sharedMesh,
                    mesh.transform.position,
                    mesh.transform.rotation, mesh.transform.lossyScale);
            }
        }

        private void OnDrawGizmos()
        {
            if (!gizmosDynamicObjects || data == null || data.ObjectsDynamicInfo == null) return;
            if (cachedGizmosDynamic == null)
            {
                UpdateCachedGizmosObjects();
            }

            foreach (var obj in cachedGizmosDynamic)
            {
                HighlightObject(obj, Color.green);
            }

            foreach (var obj in cachedGizmosInteractive)
            {
                HighlightObject(obj, Color.blue);
            }

            foreach (var obj in cachedGizmosWrong)
            {
                HighlightObject(obj, Color.yellow);
            }

            foreach (var obj in cachedGizmosError)
            {
                HighlightObject(obj, Color.red);
            }
        }

        private void UpdateCachedGizmosObjects()
        {
            Dictionary<string, GameObject> allObjects = MetaInfo.CollectAllDict();
            Dictionary<string, ObjectDynamicType> detectedResults =
                data.ObjectsDynamicInfo.ToDictionary(info => info.ObjectId, info => info.DynamicType);
            var markers = DynamicMarker.CollectAllWithPropagation()
                .Where(x => x.obj != null && x.obj.TryGetComponent(out MetaInfo metaInfo) &&
                            !string.IsNullOrEmpty(metaInfo.uid))
                .ToList();
            Dictionary<string, ObjectDynamicType> truthResults = markers
                .ToDictionary(x => x.obj.GetComponent<MetaInfo>().uid, x => x.type);
            if (propagateDynamicToChildren)
            {
                detectedResults = PropagateDynamicTypes(detectedResults, allObjects);
                truthResults = PropagateMarkerDynamicTypes(markers);
            }

            cachedGizmosDynamic = new List<GameObject>();
            cachedGizmosInteractive = new List<GameObject>();
            cachedGizmosWrong = new List<GameObject>();
            cachedGizmosError = new List<GameObject>();
            // foreach (var x in data.ObjectsDynamicInfo)
            // {
            //     if (x.DynamicType != x.MarkedType)
            //         cachedGizmosWrong.Add(allObjects[x.ObjectId]);
            //     else if (x.DynamicType == ObjectDynamicType.DynamicInteractive)
            //         cachedGizmosInteractive.Add(allObjects[x.ObjectId]);
            //     else if (x.DynamicType == ObjectDynamicType.Dynamic)
            //         cachedGizmosDynamic.Add(allObjects[x.ObjectId]);
            // }
            foreach (var obj in detectedResults.Keys.Union(truthResults.Keys))
            {
                if (allObjects[obj] == null)
                {
                    Debug.LogWarning($"Object with id {obj} not found in the scene.");
                    continue;
                }
                ObjectDynamicType detected = detectedResults.GetValueOrDefault(obj, ObjectDynamicType.Static);
                ObjectDynamicType truth = truthResults.GetValueOrDefault(obj, ObjectDynamicType.Static);
                if (detected != truth)
                    if (detected > truth && detected - truth == 1)
                        cachedGizmosWrong.Add(allObjects[obj]);
                    else
                        cachedGizmosError.Add(allObjects[obj]);
                else if (detected == ObjectDynamicType.DynamicInteractive)
                    cachedGizmosInteractive.Add(allObjects[obj]);
                else if (detected == ObjectDynamicType.Dynamic)
                    cachedGizmosDynamic.Add(allObjects[obj]);
            }
        }

        private static Dictionary<string, ObjectDynamicType> PropagateDynamicTypes(
            Dictionary<string, ObjectDynamicType> source,
            Dictionary<string, GameObject> allObjects)
        {
            Dictionary<string, ObjectDynamicType> propagated = new(source);
            foreach (var kv in source)
            {
                if (!kv.Value.IsDynamic()) continue;
                if (!allObjects.TryGetValue(kv.Key, out GameObject obj) || obj == null) continue;
                foreach (MetaInfo childMeta in obj.GetComponentsInChildren<MetaInfo>())
                {
                    if (childMeta == null || string.IsNullOrEmpty(childMeta.uid)) continue;
                    ObjectDynamicType current = propagated.GetValueOrDefault(childMeta.uid, ObjectDynamicType.Static);
                    if (current < kv.Value)
                    {
                        propagated[childMeta.uid] = kv.Value;
                    }
                }
            }

            return propagated;
        }

        private static Dictionary<string, ObjectDynamicType> PropagateMarkerDynamicTypes(
            List<(GameObject obj, ObjectDynamicType type, ObjectDynamicType propagationType)> markers)
        {
            Dictionary<string, ObjectDynamicType> propagated = markers
                .ToDictionary(x => x.obj.GetComponent<MetaInfo>().uid, x => x.type);
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
    }
}
