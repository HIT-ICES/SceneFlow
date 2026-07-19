using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SceneFlowTools.Runtime.Config;
using SceneFlowTools.Runtime.DynamicDetection;
using SceneFlowTools.Runtime.Utils;
using UnityEngine;
using Utils;

namespace SceneFlowTools.Runtime.Service
{
    public enum ServiceAllocationGizmosMode
    {
        Bounds,
        BoundsAndObjects
    }

    public class ServiceAllocation : MonoBehaviour
    {
        public RenderConfigManager renderConfigManager;
        private List<Subscene> Subscenes => renderConfigManager?.sceneConfig?.scenes;

        public double fpsLimit = 60.0;
        public double renderTimeTrangleFactor = 0.00001;
        public int cloudSceneLayerCount = 1;
        public int deviceSceneLayerCount = 1;
        public bool useClientMarker = true;
        public bool deviceSceneNeedsInteractive = false;

        [Tooltip("Optional prefix for generated client/edge/cloud config JSON file names.")]
        public string configPrefix = "";

        public List<ServerInfo> servers;

        public AllocationResult allocationResult;

        public Color[] gizmosColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        public int gizmosEdgeId = -1;
        public ServiceAllocationGizmosMode gizmosMode = ServiceAllocationGizmosMode.Bounds;

        [Tooltip("Color used for subtree scenes of the displayed edge scenes.")]
        public Color gizmosSubtreeColor = Color.white;

        private HashSet<int> _edgeScenes;
        private HashSet<int> _deviceScenes;

        private void Start()
        {
            if (renderConfigManager == null)
            {
                renderConfigManager = FindObjectOfType<RenderConfigManager>();
            }
        }

        public void DoAllocate()
        {
            var objectDynamicType = renderConfigManager.dynamicDetectionManager.data.ObjectsDynamicInfo
                .ToDictionary(x => x.ObjectId, x => x.DynamicType);
            var sceneHasInteractive = new Func<Subscene, bool>((s) =>
            {
                foreach (var objId in s.objectIds)
                {
                    if (objectDynamicType.ContainsKey(objId) &&
                        objectDynamicType[objId] == ObjectDynamicType.DynamicInteractive)
                    {
                        return true;
                    }
                }

                return false;
            });
            List<Subscene> scenes = renderConfigManager.sceneConfig.scenes;
            int maxDepth = scenes.Select((t, i) => SubsceneUtils.GetDepth(scenes, i)).Prepend(0).Max();
            List<int> cloudScenes = scenes.Where(t => SubsceneUtils.GetDepth(scenes, t.id) < cloudSceneLayerCount)
                .Select(t => t.id).ToList();
            if (!useClientMarker)
            {
                _deviceScenes = scenes
                    .Where(t => SubsceneUtils.GetHeight(scenes, t.id) <= deviceSceneLayerCount - 1 &&
                                (!deviceSceneNeedsInteractive || sceneHasInteractive(t)))
                    .Select(t => t.id).ToHashSet();
            }
            else
            {
                ClientConnectMarker[] markers = FindObjectsOfType<ClientConnectMarker>();
                Func<Bounds, bool> containsMarker =
                    bounds => markers.Any(marker => bounds.Contains(marker.transform.position));
                Func<Subscene, bool> isClientScene = s =>
                {
                    bool contains = containsMarker(s.bounds);
                    bool childrenContains = s.subscenes.Select(childId => Subscenes[childId])
                        .Any(child => containsMarker(child.bounds));
                    return contains && !childrenContains;
                };
                _deviceScenes = scenes.Where(s => isClientScene(s)).Select(t => t.id).ToHashSet();
            }

            _edgeScenes = scenes
                .Select(t => t.id)
                .Where(id => !cloudScenes.Contains(id) && !_deviceScenes.Contains(id))
                .ToHashSet();
            var (userCount, serverAlloc) = DoServiceAndServerAllocation();
            if (userCount == null)
            {
                allocationResult = null;
            }

            allocationResult = new AllocationResult
            {
                maxUserCount = userCount!.Value,
                cloudScenes = cloudScenes,
                deviceScenes = _deviceScenes.ToList(),
                edgeServers = serverAlloc
            };
        }

        private (int?, List<ServerAllocationResult>) DoServiceAndServerAllocation()
        {
            List<ServerAllocationResult> allocation = null;
            int? maxUserCount = BinarySearchUtils.FindMax(0, 100000000, (x) =>
            {
                if (!CheckSingleSubsceneOverload(x))
                    return false;
                var alloc = CheckServiceDivisionSubtree(x);
                if (alloc != null)
                    allocation = alloc;
                return alloc != null;
            });
            return (maxUserCount, allocation);
        }

        // Check whether a single subscene is overloaded.
        private bool CheckSingleSubsceneOverload(int userCount)
        {
            foreach (var sceneId in _edgeScenes)
            {
                Subscene scene = Subscenes[sceneId];
                if (EstimateRenderTime(scene, userCount) > 1.0 / fpsLimit)
                    return false;
                if (scene.metrics.gpuMemory > servers[0].gpuMemoryBytes)
                    return false;
            }

            return true;
        }

        // Reduce the problem to subtree partitioning.
        private List<ServerAllocationResult> CheckServiceDivisionSubtree(int userCount)
        {
            Debug.Log($"CheckServiceDivisionSubtree(userCount={userCount})");
            List<int> roots = _edgeScenes
                .Where(id => SubsceneUtils.GetDepth(Subscenes, id) == cloudSceneLayerCount)
                .ToList();
            List<(double gpuMemory, double renderTime, List<int> group)> divisionResults =
                DfsDivisionSubtreeFirstLevel(userCount, roots);

            List<ServerAllocationResult> results = new List<ServerAllocationResult>();
            for (int i = 0; i < divisionResults.Count; i++)
            {
                var group = divisionResults[i];
                ServerAllocationResult result = new ServerAllocationResult
                {
                    rootScenes = group.group.Where(id => roots.Contains(id)).ToList(),
                    scenes = group.group,
                    gpuMemory = group.gpuMemory,
                    renderTime = group.renderTime
                };
                results.Add(result);
            }

            results.Sort((a, b) => (a.gpuMemory, a.renderTime).CompareTo((b.gpuMemory, b.renderTime)));

            CalcServiceDependencies(results);


            // Two services can be merged when they depend on the same service.
            // A service with no dependencies can also be merged into any service.
            // Disabled pending correction of the merge behavior.
            // for (var i = 0; i < results.Count; i++)
            // {
            //     var server = results[i];
            //     if (server.relyOnServerId != -1) continue;
            //     for (var j = 0; j < results.Count; j++)
            //     {
            //         if (i == j) continue;
            //         var target = results[j];
            //         if (target == null) continue;
            //         if (target.relyOnServerId == i) continue;
            //         if (server.relyOnServerId != -1 && server.relyOnServerId != target.relyOnServerId &&
            //             server.relyOnServerId != j) continue;
            //         if (server.gpuMemory + target.gpuMemory <= servers[0].gpuMemory &&
            //             server.renderTime + target.renderTime <= 1.0 / fpsLimit)
            //         {
            //             Debug.Log($"Merging server {i} (-->{server.relyOnServerId}) into server {j} (-->{target.relyOnServerId})");
            //             target.rootScenes.AddRange(server.rootScenes);
            //             target.scenes.AddRange(server.scenes);
            //             target.gpuMemory += server.gpuMemory;
            //             target.renderTime += server.renderTime;
            //             results[i] = null;
            //             break;
            //         }
            //     }
            // }

            results.RemoveAll(x => x == null);

            CalcServiceDependencies(results);

            while (results.Count < servers.Count)
            {
                results.Add(new ServerAllocationResult());
            }

            if (results.Count > servers.Count)
            {
                Debug.Log(
                    $"CheckServiceDivisionSubtree(userCount={userCount}) 不可行，结果服务数 {results.Count} ({string.Join(",", results.Select(x => x.scenes.Count))})");
                // Output dependency relationships.
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < results.Count; i++)
                {
                    var server = results[i];
                    sb.AppendLine(
                        $"Server rely {i} ({server.renderTime}) --> {server.relyOnServerId} ({(server.relyOnServerId == -1 ? "N/A" : results[server.relyOnServerId].renderTime)})");
                }

                Debug.Log(sb.ToString());
                return null;
            }

            foreach (var alloc in results)
            {
                CollectContainedDeviceScenes(alloc);
            }

            return results;
        }

        private void CollectContainedDeviceScenes(ServerAllocationResult alloc)
        {
            List<int> results = SubsceneUtils.CollectSubsceneIds(Subscenes, alloc.scenes);
            results.RemoveAll(x => !_deviceScenes.Contains(x));
            alloc.deviceScenes = results;
        }

        private List<(double gpuMemory, double renderTime, List<int> group)> DfsDivisionSubtreeFirstLevel(int userCount,
            List<int> roots)
        {
            List<(double gpuMemory, double renderTime, List<int> group)> divisionResults = new();
            List<(double gpuMemory, double renderTime, List<int> group)> childrenResults = new();
            foreach (int rootId in roots)
            {
                childrenResults.Add(DfsDivisionSubtree(rootId, userCount, divisionResults));
            }

            childrenResults.Sort((a, b) => (a.gpuMemory, a.renderTime).CompareTo((b.gpuMemory, b.renderTime)));
            double gpuMemory = 0;
            double renderTime = 0;
            List<int> group = new();
            foreach (var child in childrenResults)
            {
                if (gpuMemory + child.gpuMemory <= servers[0].gpuMemoryBytes &&
                    renderTime + child.renderTime <= 1.0 / fpsLimit)
                {
                    // Merge into the current service.
                    gpuMemory += child.gpuMemory;
                    renderTime += child.renderTime;
                    group.AddRange(child.group);
                }
                else
                {
                    // Create a new service instead of merging.
                    divisionResults.Add(child);
                }
            }

            if (group.Count > 0)
                divisionResults.Add((gpuMemory, renderTime, group));
            //
            // // The divisionResults entries can be merged with each other.
            // divisionResults.Sort((a, b) => b.renderTime.CompareTo(a.renderTime));
            // for (int i = 0; i < divisionResults.Count; i++)
            // {
            //     for (int j = divisionResults.Count - 1; j >= 0; j--)
            //     {
            //         if (i == j) break;
            //         var a = divisionResults[i];
            //         var b = divisionResults[j];
            //         if (a.gpuMemory + b.gpuMemory <= servers[0].gpuMemory &&
            //             a.renderTime + b.renderTime <= 1.0 / fpsLimit)
            //         {
            //             // Merge into the current service.
            //             divisionResults[i] = (a.gpuMemory + b.gpuMemory, a.renderTime + b.renderTime,
            //                 a.group.Concat(b.group).ToList());
            //             divisionResults.RemoveAt(j);
            //         }
            //     }
            // }

            return divisionResults;
        }

        private double GetEstimatedGpuMemory(double userCount)
        {
            return userCount * 3840 * 2160 * 2 * 4;
            // return userCount * 32L * 1024 * 1024;
        }

        private (double gpuMemory, double renderTime, List<int> group) DfsDivisionSubtree(int p, int userCount,
            List<(double gpuMemory, double renderTime, List<int> group)> divisionResults)
        {
            Subscene node = Subscenes[p];

            List<(double gpuMemory, double renderTime, List<int> group)> childrenResults = new();
            foreach (int cid in node.subscenes)
            {
                if (!_edgeScenes.Contains(cid)) continue;
                childrenResults.Add(DfsDivisionSubtree(cid, userCount, divisionResults));
            }

            childrenResults.Sort((a, b) => (a.gpuMemory, a.renderTime).CompareTo((b.gpuMemory, b.renderTime)));

            double gpuMemory = node.metrics.gpuMemory + GetEstimatedGpuMemory(node.userProbability * userCount);
            double renderTime = EstimateRenderTime(node, userCount);
            List<int> group = new List<int> { p };
            foreach (var child in childrenResults)
            {
                if (gpuMemory + child.gpuMemory <= servers[0].gpuMemoryBytes &&
                    renderTime + child.renderTime <= 1.0 / fpsLimit)
                {
                    // Merge into the current service.
                    gpuMemory += child.gpuMemory;
                    renderTime += child.renderTime;
                    group.AddRange(child.group);
                }
                else
                {
                    // Create a new service instead of merging.
                    divisionResults.Add(child);
                }
            }

            return (gpuMemory, renderTime, group);
        }

        void CalcServiceDependencies(List<ServerAllocationResult> services)
        {
            foreach (var server in services)
            {
                server.relyOnServerId = -1;
            }

            // Compute dependencies between services.
            for (int i = 0; i < services.Count; i++)
            {
                var server = services[i];
                for (int j = 0; j < services.Count; j++)
                {
                    if (i == j) continue;
                    var otherServer = services[j];
                    if (server.relyOnServerId == j) continue;
                    if (!IsRelyOn(server.scenes.ToHashSet(), otherServer.scenes.ToHashSet())) continue;
                    // Debug.Log($"Server rely: {i} --> {j}");
                    if (server.relyOnServerId != -1)
                        throw new Exception(
                            $"Multiple dependencies detected: {i} --> {server.relyOnServerId} and {i} --> {j}");
                    if (IsRelyOn(otherServer.scenes.ToHashSet(), server.scenes.ToHashSet()))
                        throw new Exception($"Circular dependency detected: {i} <--> {j}");
                    server.relyOnServerId = j;
                }
            }
        }

        // A depends on B iff a scene in A has its parent scene in B.
        private bool IsRelyOn(HashSet<int> scenesA, HashSet<int> scenesB)
        {
            foreach (var sceneId in scenesA)
            {
                var parentId = Subscenes[sceneId].parentId;
                if (parentId == -1) continue;
                if (scenesB.Contains(parentId)) return true;
            }

            return false;
        }

        // Collect every edge scene in subtree p.
        private HashSet<int> CollectSubtreeScenesInEdge(int p)
        {
            if (!_edgeScenes.Contains(p)) throw new Exception("p must be in edge scenes");
            HashSet<int> scenes = new HashSet<int> { p };
            foreach (int childId in Subscenes[p].subscenes)
            {
                if (_edgeScenes.Contains(childId))
                    scenes.UnionWith(CollectSubtreeScenesInEdge(childId));
            }

            return scenes;
        }

        private double EstimateRenderTime(Subscene scene, int userCount)
        {
            return renderTimeTrangleFactor * scene.metrics.triangleCount * scene.userProbability * userCount;
        }

        private double EstimateRenderTime(AllocationNode node, int userCount)
        {
            return renderTimeTrangleFactor * node.metrics.triangleCount * node.userProbability * userCount;
        }

        private double EstimateRenderTimeIncludeChildren(Subscene scene, int userCount)
        {
            return renderTimeTrangleFactor * scene.metricsIncludeChildren.triangleCount * scene.userProbability *
                   userCount;
        }


        private void OnDrawGizmosSelected()
        {
            if (allocationResult?.edgeServers == null || Subscenes == null) return;
            Dictionary<string, GameObject> id2ObjMap = null;
            if (gizmosMode == ServiceAllocationGizmosMode.BoundsAndObjects)
            {
                id2ObjMap = MetaInfo.CollectAllDict();
            }

            List<int> ids = gizmosEdgeId < 0 || gizmosEdgeId >= allocationResult.edgeServers.Count
                ? Enumerable.Range(0, allocationResult.edgeServers.Count).ToList()
                : new List<int> { gizmosEdgeId };
            HashSet<int> displayedEdgeSceneIds = new();
            foreach (int i in ids)
            {
                var server = allocationResult.edgeServers[i];
                Color color = gizmosColors[i % gizmosColors.Length];
                foreach (var sceneId in server.scenes)
                {
                    if (!IsValidSceneId(sceneId)) continue;
                    displayedEdgeSceneIds.Add(sceneId);
                    DrawSceneGizmos(sceneId, color, id2ObjMap);
                }
            }

            HashSet<int> clientSceneIds = new();
            foreach (var sceneId in displayedEdgeSceneIds)
            {
                clientSceneIds.UnionWith(SubsceneUtils.CollectSubsceneIds(Subscenes, new[] { sceneId }));
            }

            clientSceneIds.ExceptWith(displayedEdgeSceneIds);
            clientSceneIds.IntersectWith(allocationResult.deviceScenes);
            foreach (var sceneId in clientSceneIds)
            {
                if (!IsValidSceneId(sceneId)) continue;
                DrawSceneGizmos(sceneId, gizmosSubtreeColor, id2ObjMap);
            }
        }

        private bool IsValidSceneId(int sceneId)
        {
            return sceneId >= 0 && sceneId < Subscenes.Count && Subscenes[sceneId] != null;
        }

        private void DrawSceneGizmos(int sceneId, Color color, Dictionary<string, GameObject> id2ObjMap)
        {
            var scene = Subscenes[sceneId];
            if (id2ObjMap != null && scene.objectIds != null)
            {
                foreach (var objId in scene.objectIds)
                {
                    if (string.IsNullOrEmpty(objId)) continue;
                    if (id2ObjMap.TryGetValue(objId, out var obj))
                    {
                        MyGizmosUtils.GizmosObjectWireMesh(obj, color);
                    }
                }
            }

            Gizmos.color = color;
            Gizmos.DrawWireCube(scene.bounds.center, scene.bounds.size);
        }
    }

    public class AllocationNode
    {
        public int rootId;
        public HashSet<int> scenes = new();
        public double userProbability;
        public SubsceneMetrics metrics = new();
        public List<AllocationNode> children = new();
    }

    [Serializable]
    public class ServerAllocationResult
    {
        public List<int> rootScenes = new(); // Root scenes.
        public List<int> scenes = new(); // Scenes.
        public List<int> deviceScenes = new(); // Device-side scenes.
        public double renderTime;
        public double gpuMemory;
        public int relyOnServerId = -1; // ID of the dependency server; -1 means no dependency.
    }

    [Serializable]
    public class AllocationResult
    {
        public int maxUserCount; // Maximum supported user count.
        public List<int> cloudScenes; // Cloud-hosted scenes.
        public List<int> deviceScenes; // Device-side scenes.

        public List<ServerAllocationResult> edgeServers; // Edge servers.
    }

    /// <summary>
    /// Information about a virtualized logical server.
    /// Each server hosts exactly one microservice.
    /// </summary>
    [Serializable]
    public class ServerInfo
    {
        public string gpuMemory = "6GiB";
        public double renderFactor = 1;
        public long gpuMemoryBytes => MyMathUtils.ParseSizeInBytes(gpuMemory);
    }
}
