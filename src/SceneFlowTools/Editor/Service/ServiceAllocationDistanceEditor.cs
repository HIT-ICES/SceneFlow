using System;
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
        private static readonly string[] GizmosConfigLabels =
        {
            "None",
            "Distance",
            "MUCVR-style",
            "Coterie-style"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
            DrawGizmosConfigSelector();

            if (GUILayout.Button("Allocate Service by Distance"))
            {
                double time = EditorApplication.timeSinceStartup;
                ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
                DistanceAllocResult result = AllocateClientEdgeByRadius(tg.edgeDistance, true);
                tg.distanceResult = result;
                tg.gizmosConfig = DistanceGizmosConfig.Distance;
                SaveDistanceConfig(result);
                Debug.Log(
                    $"Distance-based service allocation complete, time_cost = {EditorApplication.timeSinceStartup - time:F2} seconds");
            }

            if (GUILayout.Button("Generate MUCVR-style Configs"))
            {
                double time = EditorApplication.timeSinceStartup;
                ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
                DistanceAllocResult result = AllocateMucvrByRadius(tg.mucvrRadius);
                tg.mucvrResult = result;
                tg.gizmosConfig = DistanceGizmosConfig.MucvrStyle;
                SaveMucvrConfig(result);
                Debug.Log(
                    $"MUCVR-style service configs generated, time_cost = {EditorApplication.timeSinceStartup - time:F2} seconds");
            }

            if (GUILayout.Button("Calculate Coterie Cutoff Radius"))
            {
                ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
                tg.coterieRadius = CalculateCoterieRadius();
                Debug.Log(
                    $"Coterie-style cutoff radius calculated: radius={tg.coterieRadius:F2}, triangle_budget={tg.coterieTriangleBudget}");
                EditorUtility.SetDirty(tg);
            }

            if (GUILayout.Button("Generate Coterie-style Configs"))
            {
                double time = EditorApplication.timeSinceStartup;
                ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
                DistanceAllocResult result = AllocateClientEdgeByRadius(tg.coterieRadius, tg.forceDynamicObjectsToNear);
                tg.coterieResult = result;
                tg.gizmosConfig = DistanceGizmosConfig.CoterieStyle;
                SaveCoterieConfig(result);
                Debug.Log(
                    $"Coterie-style service configs generated, time_cost = {EditorApplication.timeSinceStartup - time:F2} seconds");
            }
        }

        private void DrawGizmosConfigSelector()
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            EditorGUILayout.LabelField("Gizmos Config");
            EditorGUI.BeginChangeCheck();
            int selectedIndex = GUILayout.Toolbar((int)tg.gizmosConfig, GizmosConfigLabels);
            if (!EditorGUI.EndChangeCheck()) return;

            tg.gizmosConfig = (DistanceGizmosConfig)selectedIndex;
            SceneView.RepaintAll();
            Repaint();
        }

        private void SaveDistanceConfig(DistanceAllocResult allocResult)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            string configPath =
                Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "Configs",
                    "ByDistance");
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

        private void SaveMucvrConfig(DistanceAllocResult allocResult)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            string configPath =
                Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "Configs",
                    "MUCVR");
            Directory.CreateDirectory(configPath);

            WriteServiceConfig(
                Path.Combine(configPath, $"mucvr-client-{tg.id}-service_config.json"),
                CreateServiceConfig(false, false, new List<string>(), "thin"));
            WriteServiceConfig(
                Path.Combine(configPath, $"mucvr-edge-{tg.id}-service_config.json"),
                CreateServiceConfig(true, false, allocResult.edgeObjects));
            WriteServiceConfig(
                Path.Combine(configPath, $"mucvr-cloud-{tg.id}-service_config.json"),
                CreateServiceConfig(true, true, allocResult.cloudObjects));
        }

        private void SaveCoterieConfig(DistanceAllocResult allocResult)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            string configPath =
                Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "Configs",
                    "Coterie");
            Directory.CreateDirectory(configPath);

            WriteServiceConfig(
                Path.Combine(configPath, $"coterie-client-{tg.id}-service_config.json"),
                CreateServiceConfig(false, false, allocResult.deviceObjects));
            WriteServiceConfig(
                Path.Combine(configPath, $"coterie-edge-{tg.id}-service_config.json"),
                CreateServiceConfig(true, false, allocResult.edgeObjects));
        }

        private static ServiceConfig CreateServiceConfig(
            bool isService,
            bool isCloud,
            List<string> activeObjects,
            string specialTag = null)
        {
            return new ServiceConfig
            {
                isService = isService,
                isCloud = isCloud,
                specialTag = specialTag,
                activeScenes = new(),
                activeObjects = activeObjects ?? new List<string>()
            };
        }

        private static void WriteServiceConfig(string path, ServiceConfig config)
        {
            File.WriteAllText(path, JsonUtility.ToJson(config, true));
        }

        private DistanceAllocResult AllocateClientEdgeByRadius(double radius, bool forceDynamicObjects)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            DistanceAllocResult result = new DistanceAllocResult();
            HashSet<string> dynamicObjectIds = CollectDynamicObjectIds();
            foreach (var objInfo in CollectObjectInfos())
            {
                bool dynamicNear = forceDynamicObjects && dynamicObjectIds.Contains(objInfo.Id);
                if (IsInsideRadius(objInfo, radius) || dynamicNear)
                {
                    result.deviceObjects.Add(objInfo.Id);
                }
                else
                {
                    result.edgeObjects.Add(objInfo.Id);
                }
            }

            return result;
        }

        private DistanceAllocResult AllocateMucvrByRadius(double radius)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            DistanceAllocResult result = new DistanceAllocResult();
            HashSet<string> dynamicObjectIds = CollectDynamicObjectIds();
            foreach (var objInfo in CollectObjectInfos())
            {
                bool dynamicNear = tg.forceDynamicObjectsToNear && dynamicObjectIds.Contains(objInfo.Id);
                if (IsInsideRadius(objInfo, radius) || dynamicNear)
                {
                    result.edgeObjects.Add(objInfo.Id);
                }
                else
                {
                    result.cloudObjects.Add(objInfo.Id);
                }
            }

            return result;
        }

        private double CalculateCoterieRadius()
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            long triangleBudget = Math.Max(0, tg.coterieTriangleBudget);
            long usedTriangles = 0;
            double radius = 0;
            HashSet<string> dynamicObjectIds = CollectDynamicObjectIds();

            foreach (var objInfo in CollectObjectInfos().OrderBy(GetDistance))
            {
                if (tg.forceDynamicObjectsToNear && dynamicObjectIds.Contains(objInfo.Id))
                {
                    continue;
                }

                long nextTriangles = usedTriangles + objInfo.TriangleCount;
                if (nextTriangles > triangleBudget)
                {
                    break;
                }

                usedTriangles = nextTriangles;
                radius = Math.Max(radius, GetDistance(objInfo));
            }

            Debug.Log(
                $"Coterie cutoff calculation: used_triangles={usedTriangles}, budget={triangleBudget}, radius={radius:F2}");
            return radius;
        }

        private List<ObjectDistanceInfo> CollectObjectInfos()
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            if (tg.meshObjectList == null)
            {
                Debug.LogError("ServiceAllocationDistance.meshObjectList is not assigned.");
                return new List<ObjectDistanceInfo>();
            }

            Dictionary<GameObject, string> obj2IdMap = MetaInfo.CollectAllDictReversed();
            return tg.meshObjectList.meshObjects
                .Where(meshFilter => meshFilter != null && meshFilter.gameObject != null)
                .Select(meshFilter => CreateObjectDistanceInfo(meshFilter, obj2IdMap))
                .Where(info => info != null)
                .ToList();
        }

        private ObjectDistanceInfo CreateObjectDistanceInfo(
            MeshFilter meshFilter,
            Dictionary<GameObject, string> obj2IdMap)
        {
            GameObject obj = meshFilter.gameObject;
            if (!obj2IdMap.TryGetValue(obj, out string id))
            {
                return null;
            }

            Bounds? bounds = BoundsUtils.From(obj);
            if (bounds == null)
            {
                Debug.Log($"{obj.name} has no bounds, using center distance.");
            }

            return new ObjectDistanceInfo
            {
                Id = id,
                Bounds = bounds,
                MeshFilter = meshFilter,
                BoundsDistance = CalculateBoundsDistance(obj, bounds),
                TriangleCount = GetTriangleCount(meshFilter)
            };
        }

        private float CalculateBoundsDistance(GameObject obj, Bounds? bounds)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            Vector3 pos = tg.transform.position;
            if (bounds != null)
            {
                Vector3 closestPoint =
                    bounds.Value.ClosestPoint(new Vector3(pos.x, bounds.Value.center.y, pos.z));
                return Vector2.Distance(
                    new Vector2(pos.x, pos.z),
                    new Vector2(closestPoint.x, closestPoint.z));
            }

            return Vector2.Distance(
                new Vector2(pos.x, pos.z),
                new Vector2(obj.transform.position.x, obj.transform.position.z));
        }

        private float GetDistance(ObjectDistanceInfo objInfo)
        {
            if (objInfo.DistanceCalculated)
            {
                return objInfo.Distance;
            }

            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            objInfo.Distance = tg.distanceMeasureMode switch
            {
                DistanceMeasureMode.MeshSurface => CalculateMeshSurfaceDistance(objInfo),
                _ => objInfo.BoundsDistance
            };
            objInfo.DistanceCalculated = true;
            return objInfo.Distance;
        }

        private float CalculateMeshSurfaceDistance(ObjectDistanceInfo objInfo)
        {
            MeshFilter meshFilter = objInfo.MeshFilter;
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null || mesh.triangles == null || mesh.triangles.Length < 3)
            {
                return objInfo.BoundsDistance;
            }

            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (vertices == null || vertices.Length == 0)
            {
                return objInfo.BoundsDistance;
            }

            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            Vector3 pos = tg.transform.position;
            Vector2 point = new Vector2(pos.x, pos.z);
            Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;
            float minDistance = float.PositiveInfinity;

            for (int i = 0; i + 2 < triangles.Length; i += 3)
            {
                int ia = triangles[i];
                int ib = triangles[i + 1];
                int ic = triangles[i + 2];
                if (ia < 0 || ia >= vertices.Length ||
                    ib < 0 || ib >= vertices.Length ||
                    ic < 0 || ic >= vertices.Length)
                {
                    continue;
                }

                Vector3 a3 = localToWorld.MultiplyPoint3x4(vertices[ia]);
                Vector3 b3 = localToWorld.MultiplyPoint3x4(vertices[ib]);
                Vector3 c3 = localToWorld.MultiplyPoint3x4(vertices[ic]);
                float distance = DistancePointTriangleXZ(
                    point,
                    new Vector2(a3.x, a3.z),
                    new Vector2(b3.x, b3.z),
                    new Vector2(c3.x, c3.z));
                if (distance <= 0f)
                {
                    return 0f;
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return float.IsPositiveInfinity(minDistance) ? objInfo.BoundsDistance : minDistance;
        }

        private static float DistancePointTriangleXZ(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float area2 = Cross(b - a, c - a);
            if (Mathf.Abs(area2) > 1e-6f && IsPointInTriangle(point, a, b, c))
            {
                return 0f;
            }

            return Mathf.Min(
                DistancePointSegment(point, a, b),
                DistancePointSegment(point, b, c),
                DistancePointSegment(point, c, a));
        }

        private static bool IsPointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(point - a, b - a);
            float d2 = Cross(point - b, c - b);
            float d3 = Cross(point - c, a - c);
            bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNegative && hasPositive);
        }

        private static float DistancePointSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = Vector2.Dot(ab, ab);
            if (lengthSq <= 1e-12f)
            {
                return Vector2.Distance(point, a);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSq);
            return Vector2.Distance(point, a + ab * t);
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        private bool IsInsideRadius(ObjectDistanceInfo objInfo, double radius)
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            // Renderer bounds enclose the rendered mesh, so this is a safe lower-bound rejection.
            if (tg.distanceMeasureMode == DistanceMeasureMode.MeshSurface &&
                objInfo.Bounds != null &&
                objInfo.BoundsDistance > radius)
            {
                return false;
            }

            Vector3 pos = tg.transform.position;
            Bounds? objBounds = objInfo.Bounds;
            bool insideBounds = objBounds?.Contains(pos, true, false, true) == true;
            bool containsBounds = objBounds != null &&
                                  new Bounds(pos, new Vector3((float)radius, 10000, (float)radius))
                                      .Contains(objBounds.Value);
            bool inside = !insideBounds || containsBounds;
            return GetDistance(objInfo) <= radius && (tg.deviceIncludeInside || !inside);
        }

        private HashSet<string> CollectDynamicObjectIds()
        {
            ServiceAllocationDistance tg = (ServiceAllocationDistance)target;
            return tg.dynamicDetectionManager?.data?.ObjectsDynamicInfo?
                .Where(x => x.DynamicType.IsDynamic())
                .Select(x => x.ObjectId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet() ?? new HashSet<string>();
        }

        private static int GetTriangleCount(MeshFilter meshFilter)
        {
            return meshFilter.sharedMesh == null ? 0 : meshFilter.sharedMesh.triangles.Length / 3;
        }

        private class ObjectDistanceInfo
        {
            public string Id;
            public Bounds? Bounds;
            public MeshFilter MeshFilter;
            public float BoundsDistance;
            public float Distance;
            public bool DistanceCalculated;
            public int TriangleCount;
        }
    }
}
