using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Config;
using SceneFlowTools.Runtime.DynamicDetection;
using SceneFlowTools.Runtime.Service;
using SceneFlowTools.Utils;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor.Service
{
    [CustomEditor(typeof(ServiceAllocationDistance))]
    public class ServiceAllocationDistanceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (GUILayout.Button("Allocate Service by Distance"))
            {
                ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
                tg.result = DoAlloc();
                SaveConfig(tg.result);
                EditorUtility.SetDirty(tg);
            }


            serializedObject.ApplyModifiedProperties();
        }

        private void SaveConfig(DistanceAllocResult allocResult)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            string configPath =
                Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "Configs",
                    $"ByDistance[{tg.edgeDistance:.00}]");
            Directory.CreateDirectory(configPath);
            string filePathDevice = Path.Combine(configPath, $"distance-client-{tg.id}-service_config.json");
            ServiceConfig deviceConfig = new ServiceConfig
            {
                isService = false,
                isCloud = false,
                activeScenes = new(),
                activeObjects = allocResult.deviceObjects
            };
            File.WriteAllText(filePathDevice, JsonUtility.ToJson(deviceConfig, true));
            string filePathEdge = Path.Combine(configPath, $"distance-edge-{tg.id}-service_config.json");
            ServiceConfig edgeConfig = new ServiceConfig
            {
                isService = true,
                isCloud = true,
                activeScenes = new(),
                activeObjects = allocResult.edgeObjects
            };
            File.WriteAllText(filePathEdge, JsonUtility.ToJson(edgeConfig, true));
            Debug.Log("Distance-based service configs generated.");
        }

        private DistanceAllocResult DoAlloc()
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            DistanceAllocResult result = new DistanceAllocResult();
            List<GameObject> meshObjects = tg.meshObjectList.meshObjects.Select(x => x.gameObject).ToList();
            var obj2IdMap = MetaInfo.CollectAllDictReversed();
            Vector3 pos = tg.transform.position;
            foreach (var obj in meshObjects)
            {
                float dist = Vector2.Distance(
                    new Vector2(pos.x, pos.z),
                    new Vector2(obj.transform.position.x, obj.transform.position.z));
                Bounds? objBounds = BoundsUtils.From(obj);
                if (objBounds != null)
                {
                    Vector3 closestPoint =
                        objBounds.Value.ClosestPoint(new Vector3(pos.x, objBounds.Value.center.y, pos.z));
                    dist = Vector2.Distance(
                        new Vector2(pos.x, pos.z),
                        new Vector2(closestPoint.x, closestPoint.z));
                }
                else
                {
                    Debug.Log($"{obj.name} has no bounds, using center distance.");
                }

                bool insideBounds = objBounds?.Contains(pos, true, false, true) == true;
                bool containsBounds = objBounds != null && new Bounds(pos, new Vector3((float)tg.edgeDistance, 10000, (float)tg.edgeDistance)).Contains(objBounds.Value);
                bool inside = !insideBounds || containsBounds;
                if (dist <= tg.edgeDistance && (tg.deviceIncludeInside || !inside))
                {
                    // Debug.Log($"{obj.name} assigned to edge (distance: {dist:F2})");
                    if (obj2IdMap.TryGetValue(obj, out var id))
                        result.deviceObjects.Add(id);
                }
                else
                {
                    if (obj2IdMap.TryGetValue(obj, out var id))
                        result.edgeObjects.Add(id);
                }
            }

            var dynamicObjs = tg.dynamicDetectionManager.data.ObjectsDynamicInfo
                .Where(x => x.DynamicType .IsDynamic())
                .Select(x => x.ObjectId).ToList();
            result.deviceObjects = result.deviceObjects.Union(dynamicObjs).ToList();
            result.edgeObjects.RemoveAll(x => dynamicObjs.Contains(x));


            return result;
        }
    }
}