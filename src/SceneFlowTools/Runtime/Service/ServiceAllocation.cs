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

        // 检查单个子场景是否超载
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
            // 按照渲染时间从大到小排序
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

            // 如果还有场景没有分配完，说明不可行
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
                    // 如果gpu内存超过限制，则拆分
                    int maxGpuMemoryIndex = FindHeavestNode(nodeLayer, n => n.metrics.gpuMemory).index;
                    if (nodeLayer[maxGpuMemoryIndex].metrics.gpuMemory > servers[0].gpuMemoryBytes)
                    {
                        flagAllInLimit = false;
                        int heaviestId =
                            FindHeavestNodeInChildren(nodeLayer[maxGpuMemoryIndex], n => n.metrics.gpuMemory).nodeId;
                        if (heaviestId == -1)
                        {
                            // Debug.Log($"CheckServiceDivision2(userCount={userCount}) 没有子节点了，无法拆分");
                            // 说明没有子节点了，无法拆分
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
                    // 如果渲染时间超过限制，则拆分
                    int maxRenderTimeIndex = FindHeavestNode(nodeLayer, n => EstimateRenderTime(n, userCount)).index;
                    // Debug.Log(
                    // $"Heavest render time node: {nodeLayer[maxRenderTimeIndex].rootId}, time: {EstimateRenderTime(nodeLayer[maxRenderTimeIndex], userCount)}");
                    if (EstimateRenderTime(nodeLayer[maxRenderTimeIndex], userCount) > 1.0 / fpsLimit)
                    {
                        // Debug.Log(
                        // $"CheckServiceDivision2(userCount={userCount}) 节点 {nodeLayer[maxRenderTimeIndex].rootId} 渲染时间 {EstimateRenderTime(nodeLayer[maxRenderTimeIndex], userCount)} 超过限制 {1.0 / fpsLimit}，需要拆分");
                        flagAllInLimit = false;
                        int heaviestId = FindHeavestNodeInChildren(nodeLayer[maxRenderTimeIndex],
                            n => EstimateRenderTime(n, userCount)).nodeId;
                        if (heaviestId == -1)
                        {
                            // 说明没有子节点了，无法拆分
                            // Debug.Log($"CheckServiceDivision2(userCount={userCount}) 没有子节点了，无法拆分");
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

            // 这是一个多重约束多背包问题
            // 暂时使用贪心法求解
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
            // 拷贝一份，防止修改原数据，并且每一层排序
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

                    // 如果这一层没有任何节点被分配，则不能在后续层分配，直接跳出
                    if (!allocatedInThisLayer) break;
                    // 如果一整层都分配了，给当前层设为null
                    if (nodeLayer.All(n => n == null)) nodeLayers[i] = null;
                    // 如果当前这层没分配完，后续层也不能分配，直接跳出
                    if (nodeLayers[i] != null) break;
                }

                results.Add(result);
            }

            // 如果还有节点没有分配完，说明不可行
            if (nodeLayers.Any(layer => layer != null))
            {
                Debug.Log(
                    $"AllocateNodeToServersGreedy(" +
                    $"userCount={userCount}, " +
                    $"layerCount={string.Join(",", allocNodeLayers.Select(x => x.Count))}) " +
                    $"不可行" +
                    $"results={string.Join(",", results.Select(x => x.scenes.Count))}"
                );
                return null;
            }

            CalcServiceDependencies(results);

            return results;
        }

        // 对于位于edge的场景森林，可以看作以下问题：
        // 树上的分组覆盖问题：
        // - 每个叶节点可以合并到兄弟节点或者父节点，问能否最终合并为n个满足约束的节点 （这里忽略了森林的特性，不完全等价）（完全不等价，如不能处理按深度分组的情况）
        // - 将每个节点分配到一个组，要求每个组满足性能约束，并且每个组要么不与其他组联通，要么仅有一个联通，问能否划分为n个组
        // 似乎是NP-hard的
        // 这里先使用贪心法求解
        // 先把整个树“合并”为一个超节点，每次挑选x最大（或者最合适）的子节点，将这个节点和子树分裂出去，直到当前超节点满足约束
        // 然后递归地处理当前超节点的子节点，使得最终构建一棵超树，其中每个节点都满足约束
        // （这样一来，每个节点不可能与父节点合并，只用处理兄弟合并的情况）
        // 最后，将每一层的超节点尽可能合并
        // 好像也不对，也无法处理按深度分组的情况
        private List<ServerAllocationResult> CheckServiceDivision3(int userCount)
        {
            List<AllocationNode> nodes = CollectEdgeFirstLayerNodes();
            foreach (var node in nodes)
            {
                if (!DoSplitNodes(userCount, node))
                {
                    // 说明无法拆分到满足要求
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

        // 将问题进一步限制为子树划分问题来求解
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


            // 如果两个服务依赖同一个服务，则可以合并
            // 或者一个服务不依赖任何其他服务，也可以合并到任意一个服务
            // 有问题，先注释掉
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
                // 输出依赖关系
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
                    // 合并
                    gpuMemory += child.gpuMemory;
                    renderTime += child.renderTime;
                    group.AddRange(child.group);
                }
                else
                {
                    // 不合并，形成一个新的服务
                    divisionResults.Add(child);
                }
            }

            if (group.Count > 0)
                divisionResults.Add((gpuMemory, renderTime, group));
            //
            // // divisionResults 彼此之间可以合并
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
            //             // 合并
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
                    // 合并
                    gpuMemory += child.gpuMemory;
                    renderTime += child.renderTime;
                    group.AddRange(child.group);
                }
                else
                {
                    // 不合并，形成一个新的服务
                    divisionResults.Add(child);
                }
            }

            return (gpuMemory, renderTime, group);
        }

        private bool DoSplitNodes(int userCount, AllocationNode node)
        {
            while (node.metrics.gpuMemory > servers[0].gpuMemoryBytes)
            {
                // 如果gpu内存超过限制，则拆分
                int heaviestId = FindHeavestNodeInChildren(node, n => n.metrics.gpuMemory).nodeId;
                if (heaviestId == -1)
                {
                    // 说明没有子节点了，无法拆分
                    return false;
                }

                // 拆出开销最大的子节点，形成一个新的节点
                SplitAllocationNodeToChild(node, heaviestId);
            }

            while (EstimateRenderTime(node, userCount) > 1.0 / fpsLimit)
            {
                // 如果渲染时间超过限制，则拆分
                int heaviestId = FindHeavestNodeInChildren(node, n => EstimateRenderTime(n, userCount)).nodeId;
                if (heaviestId == -1)
                {
                    // 说明没有子节点了，无法拆分
                    return false;
                }

                // 拆出渲染时间最大的子节点，形成一个新的节点
                SplitAllocationNodeToChild(node, heaviestId);
            }

            // 当前节点已经满足要求，递归处理子节点
            foreach (var child in node.children)
            {
                if (!DoSplitNodes(userCount, child))
                    return false;
            }

            // 成功，使所有节点都满足要求
            return true;
        }

        void CalcServiceDependencies(List<ServerAllocationResult> services)
        {
            foreach (var server in services)
            {
                server.relyOnServerId = -1;
            }

            // 计算服务间依赖关系
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

        // A依赖B，当且仅当存在A中的场景的父场景在B中
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

        // 收集子树p中的所有edge上的场景
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
        public List<int> rootScenes = new(); // 根场景
        public List<int> scenes = new(); // 场景
        public List<int> deviceScenes = new(); // 设备场景
        public double renderTime;
        public long gpuMemory;
        public int relyOnServerId = -1; // 依赖的服务器ID，-1表示不依赖
    }

    [Serializable]
    public class AllocationResult
    {
        public int maxUserCount; // 支持的最大用户数
        public List<int> cloudScenes; // 云端场景
        public List<int> deviceScenes; // 设备场景

        public List<ServerAllocationResult> edgeServers; // 边缘服务器
    }

    /// <summary>
    /// 服务器信息（指的是虚拟化后的逻辑服务器）
    /// 每个服务器只承载一个微服务
    /// </summary>
    [Serializable]
    public class ServerInfo
    {
        public string gpuMemory = "6GiB";
        public double renderFactor = 1;
        public long gpuMemoryBytes => MyMathUtils.ParseSizeInBytes(gpuMemory);
    }
}