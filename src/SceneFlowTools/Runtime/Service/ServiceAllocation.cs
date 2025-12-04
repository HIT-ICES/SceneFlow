using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using SceneFlowTools.Runtime.Config;
using UnityEngine;
using Utils;

namespace SceneFlowTools.Runtime.Service
{
    public class ServiceAllocation : MonoBehaviour
    {
        public RenderConfigManager renderConfigManager;
        private List<Subscene> Subscenes => renderConfigManager?.sceneConfig?.scenes;

        public double fpsLimit = 60.0;
        public double renderTimeTrangleFactor = 0.00001;
        public int cloudSceneLayerCount = 1;
        public int deviceSceneLayerCount = 1;
        public List<ServerInfo> servers;

        public AllocationResult allocationResult;

        public Color[] gizmosColors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        public int gizmosEdgeId = -1;

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
            List<Subscene> scenes = renderConfigManager.sceneConfig.scenes;
            int maxDepth = scenes.Select((t, i) => SubsceneUtils.GetDepth(scenes, i)).Prepend(0).Max();
            List<int> cloudScenes = scenes.Where(t => SubsceneUtils.GetDepth(scenes, t.id) < cloudSceneLayerCount)
                .Select(t => t.id).ToList();
            _deviceScenes = scenes
                .Where(t => SubsceneUtils.GetHeight(scenes, t.id) <= deviceSceneLayerCount - 1)
                .Select(t => t.id).ToHashSet();
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

        private List<ServerAllocationResult> CheckServiceDivision(int userCount)
        {
            List<Subscene> sceneList = Subscenes
                .Where(t => _edgeScenes.Contains(t.id))
                .ToList();

            sceneList.Sort((a, b) => EstimateRenderTime(b, userCount).CompareTo(EstimateRenderTime(a, userCount)));
            List<ServerAllocationResult> services = new List<ServerAllocationResult>();
            foreach (ServerInfo server in servers)
            {
                ServerAllocationResult service = new ServerAllocationResult();
                for (var i = 0; i < sceneList.Count; i++)
                {
                    var scene = sceneList[i];
                    if (scene == null) continue;
                    if (EstimateRenderTime(scene, userCount) + service.renderTime > 1.0 / fpsLimit) continue;
                    if (scene.metrics.gpuMemory + service.gpuMemory > servers[0].gpuMemoryBytes) continue;
                    service.scenes.Add(scene.id);
                    service.renderTime += EstimateRenderTime(scene, userCount);
                    service.gpuMemory += scene.metrics.gpuMemory;
                    sceneList[i] = null;
                }

                if (service.scenes.Count > 0)
                {
                    services.Add(service);
                }
            }


            foreach (var scene in sceneList)
            {
                if (scene != null)
                {
                    return null;
                }
            }

            return services;
        }

        private List<ServerAllocationResult> CheckServiceDivision2(int userCount)
        {
            List<List<AllocationNode>> nodeLayers = new List<List<AllocationNode>>();
            nodeLayers.Add(CollectEdgeFirstLayerNodes());
            AssertNodesNotOverlap(nodeLayers);
            bool flagAllInLimit = false;
            while (!flagAllInLimit)
            {
                flagAllInLimit = true;
                for (int i = 0; i < nodeLayers.Count; i++)
                {
                    var nodeLayer = nodeLayers[i];

                    int maxGpuMemoryIndex = FindHeavestNode(nodeLayer, n => n.metrics.gpuMemory).index;
                    if (nodeLayer[maxGpuMemoryIndex].metrics.gpuMemory > servers[0].gpuMemoryBytes)
                    {
                        flagAllInLimit = false;
                        int heaviestId =
                            FindHeavestNodeInChildren(nodeLayer[maxGpuMemoryIndex], n => n.metrics.gpuMemory).nodeId;
                        if (heaviestId == -1)
                        {


                            return null;
                        }

                        var (root, other) = SplitAllocationNode(nodeLayer[maxGpuMemoryIndex], heaviestId);
                        nodeLayer.RemoveAt(maxGpuMemoryIndex);
                        nodeLayer.Add(root);
                        if (i + 1 >= nodeLayers.Count)
                            nodeLayers.Add(new List<AllocationNode>());
                        nodeLayers[i + 1].Add(other);
                    }

                    AssertNodesNotOverlap(nodeLayers);

                    int maxRenderTimeIndex = FindHeavestNode(nodeLayer, n => EstimateRenderTime(n, userCount)).index;
                    // Debug.Log(
                    // $"Heavest render time node: {nodeLayer[maxRenderTimeIndex].rootId}, time: {EstimateRenderTime(nodeLayer[maxRenderTimeIndex], userCount)}");
                    if (EstimateRenderTime(nodeLayer[maxRenderTimeIndex], userCount) > 1.0 / fpsLimit)
                    {
                        // Debug.Log(

                        flagAllInLimit = false;
                        int heaviestId = FindHeavestNodeInChildren(nodeLayer[maxRenderTimeIndex],
                            n => EstimateRenderTime(n, userCount)).nodeId;
                        if (heaviestId == -1)
                        {


                            return null;
                        }

                        var (root, other) = SplitAllocationNode(nodeLayer[maxRenderTimeIndex], heaviestId);
                        nodeLayer.RemoveAt(maxRenderTimeIndex);
                        nodeLayer.Add(root);
                        if (i + 1 >= nodeLayers.Count)
                            nodeLayers.Add(new List<AllocationNode>());
                        nodeLayers[i + 1].Add(other);
                    }

                    AssertNodesNotOverlap(nodeLayers);
                }
            }



            return AllocateNodeToServersGreedy(userCount, nodeLayers,
                (a, b) => EstimateRenderTime(b, userCount).CompareTo(EstimateRenderTime(a, userCount)));
        }

        private void AssertNodesNotOverlap(List<List<AllocationNode>> nodeLayers)
        {
            HashSet<int> allScenes = new HashSet<int>();
            foreach (var layer in nodeLayers)
            {
                foreach (var node in layer)
                {
                    foreach (var scene in node.scenes)
                    {
                        if (!allScenes.Add(scene))
                            throw new Exception("nodes overlap");
                    }
                }
            }
        }

        private List<ServerAllocationResult> AllocateNodeToServersGreedy(int userCount,
            List<List<AllocationNode>> allocNodeLayers, Comparison<AllocationNode> nodeComparison)
        {

            List<List<AllocationNode>> nodeLayers = allocNodeLayers.Select(layer =>
            {
                var newLayer = new List<AllocationNode>(layer);
                newLayer.Sort(nodeComparison);
                Debug.Log(
                    $"Layer: {string.Join(", ", newLayer.Select(n => n.rootId + "(time:" + EstimateRenderTime(n, userCount) + ")"))}");
                return newLayer;
            }).ToList();
            List<ServerAllocationResult> results = new List<ServerAllocationResult>();
            foreach (var server in servers)
            {
                ServerAllocationResult result = new ServerAllocationResult();
                for (int i = 0; i < nodeLayers.Count; i++)
                {
                    var nodeLayer = nodeLayers[i];
                    if (nodeLayer == null) continue;
                    bool allocatedInThisLayer = false;
                    for (int j = 0; j < nodeLayer.Count; j++)
                    {
                        var node = nodeLayer[j];
                        if (node == null) continue;
                        if (EstimateRenderTime(node, userCount) + result.renderTime > 1.0 / fpsLimit) continue;
                        if (node.metrics.gpuMemory + result.gpuMemory > server.gpuMemoryBytes) continue;
                        allocatedInThisLayer = true;
                        result.rootScenes.Add(node.rootId);
                        result.scenes.AddRange(node.scenes);
                        result.renderTime += EstimateRenderTime(node, userCount);
                        result.gpuMemory += node.metrics.gpuMemory;
                        nodeLayer[j] = null;
                    }


                    if (!allocatedInThisLayer) break;

                    if (nodeLayer.All(n => n == null)) nodeLayers[i] = null;

                    if (nodeLayers[i] != null) break;
                }

                results.Add(result);
            }


            if (nodeLayers.Any(layer => layer != null))
            {
                Debug.Log(
                    $"AllocateNodeToServersGreedy(" +
                    $"userCount={userCount}, " +
                    $"layerCount={string.Join(",", allocNodeLayers.Select(x => x.Count))}) " +
                    $"Illegal" +
                    $"results={string.Join(",", results.Select(x => x.scenes.Count))}"
                );
                return null;
            }

            CalcServiceDependencies(results);

            return results;
        }












        private List<ServerAllocationResult> CheckServiceDivision3(int userCount)
        {
            List<AllocationNode> nodes = CollectEdgeFirstLayerNodes();
            foreach (var node in nodes)
            {
                if (!DoSplitNodes(userCount, node))
                {

                    return null;
                }
            }

            return AllocateNodeToServersGreedy3(userCount, nodes,
                (a, b) => EstimateRenderTime(b, userCount).CompareTo(EstimateRenderTime(a, userCount)));
        }


        private List<ServerAllocationResult> AllocateNodeToServersGreedy3(int userCount,
            List<AllocationNode> nodes, Comparison<AllocationNode> nodeComparison)
        {
            throw new NotImplementedException();
        }


        private List<ServerAllocationResult> CheckServiceDivisionSubtree(int userCount)
        {
            Debug.Log($"CheckServiceDivisionSubtree(userCount={userCount})");
            List<int> roots = _edgeScenes
                .Where(id => SubsceneUtils.GetDepth(Subscenes, id) == cloudSceneLayerCount)
                .ToList();
            List<(long gpuMemory, double renderTime, List<int> group)> divisionResults =
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

            results.Sort((a, b) => a.renderTime.CompareTo(b.renderTime));

            CalcServiceDependencies(results);





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
                    $"CheckServiceDivisionSubtree(userCount={userCount}) false, service count {results.Count} ({string.Join(",", results.Select(x => x.scenes.Count))})");

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

        private List<(long gpuMemory, double renderTime, List<int> group)> DfsDivisionSubtreeFirstLevel(int userCount,
            List<int> roots)
        {
            List<(long gpuMemory, double renderTime, List<int> group)> divisionResults = new();
            List<(long gpuMemory, double renderTime, List<int> group)> childrenResults = new();
            foreach (int rootId in roots)
            {
                childrenResults.Add(DfsDivisionSubtree(rootId, userCount, divisionResults));
            }

            childrenResults.Sort((a, b) => a.renderTime.CompareTo(b.renderTime));
            long gpuMemory = 0;
            double renderTime = 0;
            List<int> group = new();
            foreach (var child in childrenResults)
            {
                if (gpuMemory + child.gpuMemory + GetEstimatedGpuMemory(userCount) <= servers[0].gpuMemoryBytes &&
                    renderTime + child.renderTime <= 1.0 / fpsLimit)
                {

                    gpuMemory += child.gpuMemory;
                    renderTime += child.renderTime;
                    group.AddRange(child.group);
                }
                else
                {

                    divisionResults.Add(child);
                }
            }

            if (group.Count > 0)
                divisionResults.Add((gpuMemory, renderTime, group));
            //

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

            //             divisionResults[i] = (a.gpuMemory + b.gpuMemory, a.renderTime + b.renderTime,
            //                 a.group.Concat(b.group).ToList());
            //             divisionResults.RemoveAt(j);
            //         }
            //     }
            // }

            return divisionResults;
        }

        private long GetEstimatedGpuMemory(int userCount)
        {
            return userCount * 32L * 1024 * 1024;
        }

        private (long gpuMemory, double renderTime, List<int> group) DfsDivisionSubtree(int p, int userCount,
            List<(long gpuMemory, double renderTime, List<int> group)> divisionResults)
        {
            Subscene node = Subscenes[p];

            List<(long gpuMemory, double renderTime, List<int> group)> childrenResults = new();
            foreach (int cid in node.subscenes)
            {
                if (!_edgeScenes.Contains(cid)) continue;
                childrenResults.Add(DfsDivisionSubtree(cid, userCount, divisionResults));
            }

            childrenResults.Sort((a, b) => a.renderTime.CompareTo(b.renderTime));

            long gpuMemory = node.metrics.gpuMemory;
            double renderTime = EstimateRenderTime(node, userCount);
            List<int> group = new List<int> { p };
            foreach (var child in childrenResults)
            {
                if (gpuMemory + child.gpuMemory + GetEstimatedGpuMemory(userCount) <= servers[0].gpuMemoryBytes &&
                    renderTime + child.renderTime <= 1.0 / fpsLimit)
                {

                    gpuMemory += child.gpuMemory;
                    renderTime += child.renderTime;
                    group.AddRange(child.group);
                }
                else
                {

                    divisionResults.Add(child);
                }
            }

            return (gpuMemory, renderTime, group);
        }

        private bool DoSplitNodes(int userCount, AllocationNode node)
        {
            while (node.metrics.gpuMemory > servers[0].gpuMemoryBytes)
            {

                int heaviestId = FindHeavestNodeInChildren(node, n => n.metrics.gpuMemory).nodeId;
                if (heaviestId == -1)
                {

                    return false;
                }


                SplitAllocationNodeToChild(node, heaviestId);
            }

            while (EstimateRenderTime(node, userCount) > 1.0 / fpsLimit)
            {

                int heaviestId = FindHeavestNodeInChildren(node, n => EstimateRenderTime(n, userCount)).nodeId;
                if (heaviestId == -1)
                {

                    return false;
                }


                SplitAllocationNodeToChild(node, heaviestId);
            }


            foreach (var child in node.children)
            {
                if (!DoSplitNodes(userCount, child))
                    return false;
            }


            return true;
        }

        void CalcServiceDependencies(List<ServerAllocationResult> services)
        {
            foreach (var server in services)
            {
                server.relyOnServerId = -1;
            }


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

        private (int nodeId, int index) FindHeavestNodeInChildren<T>(AllocationNode node,
            Func<AllocationNode, T> keySelector)
            where T : IComparable
        {
            var childNodes = Subscenes[node.rootId]
                .subscenes
                .Where(x => node.scenes.Contains(x))
                .Select(CollectAllocationNodeInEdge);
            return FindHeavestNode(childNodes, keySelector);
        }

        private (int nodeId, int index) FindHeavestNode<T>(IEnumerable<AllocationNode> nodes,
            Func<AllocationNode, T> keySelector)
            where T : IComparable
        {
            int heaviestId = -1;
            int heaviestIndex = -1;
            int index = 0;
            T heaviestValue = default;
            bool first = true;
            foreach (var n in nodes)
            {
                var value = keySelector(n);
                if (first || heaviestValue.CompareTo(value) < 0)
                {
                    // Debug.Log($"FindHeavestNode: found new heaviest node {n.rootId} with value {value}");
                    heaviestId = n.rootId;
                    heaviestIndex = index;
                    heaviestValue = value;
                    first = false;
                }

                index++;
            }

            return (heaviestId, heaviestIndex);
        }

        private List<AllocationNode> CollectEdgeFirstLayerNodes()
        {
            List<AllocationNode> nodes = new List<AllocationNode>();
            foreach (int sceneId in _edgeScenes)
            {
                Subscene scene = Subscenes[sceneId];
                if (SubsceneUtils.GetDepth(Subscenes, sceneId) != cloudSceneLayerCount) continue;
                nodes.Add(CollectAllocationNodeInEdge(sceneId));
            }

            return nodes;
        }

        private void SplitAllocationNodeToChild(AllocationNode node, int splitId)
        {
            if (node.rootId == splitId) throw new Exception("splitId must not be the rootId");
            if (!node.scenes.Contains(splitId)) throw new Exception($"splitId must be child of node: {splitId}");
            AllocationNode other = CollectAllocationNodeInEdge(splitId);
            node.scenes = node.scenes.Except(other.scenes).ToHashSet();
            node.metrics -= other.metrics;
            node.children.Add(other);
        }

        private (AllocationNode root, AllocationNode other) SplitAllocationNode(AllocationNode node, int splitId)
        {
            if (node.rootId == splitId) throw new Exception("splitId must not be the rootId");
            if (!node.scenes.Contains(splitId)) throw new Exception($"splitId must be child of node: {splitId}");
            AllocationNode other = CollectAllocationNodeInEdge(splitId);
            AllocationNode root = new AllocationNode
            {
                rootId = node.rootId,
                scenes = node.scenes.Except(other.scenes).ToHashSet(),
                userProbability = node.userProbability,
                metrics = node.metrics - other.metrics,
            };
            return (root, other);
        }

        private AllocationNode CollectAllocationNodeInEdge(int p)
        {
            AllocationNode node = new AllocationNode
            {
                rootId = p,
                scenes = CollectSubtreeScenesInEdge(p),
                userProbability = Subscenes[p].userProbability,
                metrics = new SubsceneMetrics()
            };
            foreach (int sceneId in node.scenes)
            {
                node.metrics += Subscenes[sceneId].metrics;
            }

            return node;
        }


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
            if (allocationResult?.edgeServers == null) return;
            List<int> ids = gizmosEdgeId < 0 || gizmosEdgeId >= allocationResult.edgeServers.Count
                ? Enumerable.Range(0, allocationResult.edgeServers.Count).ToList()
                : new List<int> { gizmosEdgeId };
            foreach (int i in ids)
            {
                var server = allocationResult.edgeServers[i];
                Color color = gizmosColors[i % gizmosColors.Length];
                foreach (var sceneId in server.scenes)
                {
                    var scene = Subscenes[sceneId];
                    if (scene == null) continue;
                    var bounds = scene.bounds;
                    Gizmos.color = color;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
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
        public List<int> rootScenes = new();
        public List<int> scenes = new();
        public List<int> deviceScenes = new();
        public double renderTime;
        public long gpuMemory;
        public int relyOnServerId = -1;
    }

    [Serializable]
    public class AllocationResult
    {
        public int maxUserCount;
        public List<int> cloudScenes;
        public List<int> deviceScenes;

        public List<ServerAllocationResult> edgeServers;
    }

    /// <summary>


    /// </summary>
    [Serializable]
    public class ServerInfo
    {
        public string gpuMemory = "6GiB";
        public double renderFactor = 1;
        public long gpuMemoryBytes => MyMathUtils.ParseSizeInBytes(gpuMemory);
    }
}