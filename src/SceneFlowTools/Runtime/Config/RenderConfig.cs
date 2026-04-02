using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SceneFlowTools.Runtime.Config
{
    
    [Serializable]
    public class SceneConfig : ScriptableObject
    {
        public static string AssetName = "SceneConfigData";
        public List<Subscene> scenes;

        public List<int> GetParentChain(int sceneId)
        {
            List<int> chain = new List<int>();
            Subscene currentScene = scenes[sceneId];
            Assert.AreEqual(currentScene.id, sceneId);
            while (currentScene.parentId != -1)
            {
                chain.Add(currentScene.parentId);
                currentScene = scenes[currentScene.parentId];
            }

            return chain;
        }

        public List<int> CollectSubscenes(IEnumerable<int> activeSceneIds)
        {
            Queue<int> sceneIdQueue = new Queue<int>(activeSceneIds);
            HashSet<int> visitedSceneIds = new HashSet<int>();
            while (sceneIdQueue.Any())
            {
                int currentSceneId = sceneIdQueue.Dequeue();
                if (!visitedSceneIds.Add(currentSceneId)) continue;
                Subscene currentScene = scenes[currentSceneId];
                foreach (var subsceneId in currentScene.subscenes)
                {
                    sceneIdQueue.Enqueue(subsceneId);
                }
            }

            return visitedSceneIds.ToList();
        }

        public HashSet<string> CollectObjects(IEnumerable<int> activeSceneIds)
        {
            HashSet<string> objectsToRender = new HashSet<string>();

            foreach (var activeSceneId in activeSceneIds)
            {
                Subscene s = scenes[activeSceneId];
                foreach (var objId in s.objectIds)
                {
                    objectsToRender.Add(objId);
                }
            }

            return objectsToRender;
            // Queue<int> sceneIdQueue = new Queue<int>(activeSceneIds);
            // HashSet<int> visitedSceneIds = new HashSet<int>();
            // while (sceneIdQueue.Any())
            // {
            //     int currentSceneId = sceneIdQueue.Dequeue();
            //     if (!visitedSceneIds.Add(currentSceneId)) continue;
            //     Subscene currentScene = scenes.Find(s => s.id == currentSceneId);
            //     if (currentScene == null)
            //     {
            //         Debug.LogWarning($"Scene ID {currentSceneId} not found in scene config.");
            //         continue;
            //     }
            //
            //     foreach (var objId in currentScene.objectIds)
            //     {
            //         objectsToRender.Add(objId);
            //     }
            //
            //     foreach (var subsceneId in currentScene.subscenes)
            //     {
            //         sceneIdQueue.Enqueue(subsceneId);
            //     }
            // }
            //
            // return objectsToRender;
        }

        public int GetDepth(int nodeId)
        {
            int depth = 0;
            Subscene currentScene = scenes[nodeId];
            Assert.AreEqual(currentScene.id, nodeId);
            while (currentScene.parentId != -1)
            {
                depth++;
                if (currentScene.parentId > scenes.Count)
                {
                    Debug.LogError($"Scene ID {currentScene.parentId} not found in scene config.");
                }

                currentScene = scenes[currentScene.parentId];
            }

            return depth;
        }
    }

    
    [Serializable]
    public class ServiceConfig
    {
        public bool isService;
        public bool isCloud;
        public List<int> activeScenes;
        public List<int> exceptedScenes;
        public List<string> activeObjects;
    }


    [Serializable]
    public class Subscene
    {
        
        public int id;

        
        public int parentId;

        
        public List<string> objectIds;

        
        public List<int> subscenes;

        
        public Bounds bounds;

        
        public SubsceneMetrics metrics;

        
        public SubsceneMetrics metricsIncludeChildren;

        
        public double userProbability;

        public static List<int> GetLeafNodes(List<Subscene> scenes)
        {
            return (from scene in scenes where scene.subscenes.Count == 0 select scene.id).ToList();
        }

        public static Vector2Int GetNodePos(List<Subscene> scenes, int p)
        {
            Dictionary<int, Vector2Int> map = GetNodePosMap(scenes);
            return map[p];
        }

        public static Dictionary<int, Vector2Int> GetNodePosMap(List<Subscene> scenes)
        {
            Dictionary<int, Vector2Int> map = new Dictionary<int, Vector2Int>();
            GetNodePosMap(map, scenes, 0, 0, 0);
            return map;
        }

        private static void GetNodePosMap(Dictionary<int, Vector2Int> map, List<Subscene> scenes, int p, int left,
            int depth)
        {
            map[p] = new Vector2Int(left, depth);
            int childCount = scenes[p].subscenes.Count;
            for (int i = 0; i < childCount; i++)
            {
                int cid = scenes[p].subscenes[i];
                GetNodePosMap(map, scenes, cid, left + i, depth + 1);
            }
        }
    }

    [Serializable]
    public class SubsceneMetrics
    {
        public int vertexCount;
        public int triangleCount;
        public long gpuMemory; // in bytes

        public static SubsceneMetrics operator +(SubsceneMetrics a, SubsceneMetrics b)
        {
            return new SubsceneMetrics
            {
                vertexCount = a.vertexCount + b.vertexCount,
                triangleCount = a.triangleCount + b.triangleCount,
                gpuMemory = a.gpuMemory + b.gpuMemory
            };
        }

        public static SubsceneMetrics operator -(SubsceneMetrics a, SubsceneMetrics b)
        {
            return new SubsceneMetrics
            {
                vertexCount = a.vertexCount - b.vertexCount,
                triangleCount = a.triangleCount - b.triangleCount,
                gpuMemory = a.gpuMemory - b.gpuMemory
            };
        }
    }

    public static class SubsceneUtils
    {
        
        public static void DeleteEmptyNodes(List<Subscene> subscenes)
        {
            AssertValid(subscenes);
            DeleteEmptyNodesImpl(subscenes, 0);
            if (subscenes[0].objectIds.Count == 0 && subscenes[0].subscenes.Count == 1)
            {
                int onlyChildId = subscenes[0].subscenes[0];
                Subscene onlyChild = subscenes[onlyChildId];
                onlyChild.id = 0;
                onlyChild.parentId = -1;
                subscenes[0] = onlyChild;
                subscenes[onlyChildId] = null;
                foreach (var onlyChildSubscene in onlyChild.subscenes)
                {
                    subscenes[onlyChildSubscene].parentId = 0;
                }
            }
            RemoveNulls(subscenes);
        }

        private static void DeleteEmptyNodesImpl(List<Subscene> subscenes, int p)
        {
            Subscene node = subscenes[p];
            List<int> toRemove = new List<int>();
            foreach (int cid in node.subscenes)
            {
                DeleteEmptyNodesImpl(subscenes, cid);
                if (subscenes[cid].objectIds.Count == 0)
                {
                    Debug.Log($"Remove empty node {cid}");
                    toRemove.Add(cid);
                }
            }

            foreach (int cid in toRemove)
            {
                node.subscenes.Remove(cid);
                node.subscenes.AddRange(subscenes[cid].subscenes);
                foreach (int gcid in subscenes[cid].subscenes)
                {
                    Assert.AreEqual(subscenes[gcid].id, gcid);
                    Assert.AreEqual(subscenes[gcid].parentId, cid);
                    subscenes[gcid].parentId = p;
                }

                subscenes[cid] = null;
            }
        }

        
        public static void RemoveNulls(List<Subscene> scenes)
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i] != null)
                    Assert.AreEqual(scenes[i].id, i);
            }

            scenes.RemoveAll(x => x == null);

            Dictionary<int, int> map = new Dictionary<int, int>();
            for (int i = 0; i < scenes.Count; i++)
            {
                map[scenes[i].id] = i;
                scenes[i].id = i;
            }

            foreach (var scene in scenes)
            {
                scene.parentId = scene.parentId == -1 ? -1 : map[scene.parentId];
                for (int i = 0; i < scene.subscenes.Count; i++)
                {
                    scene.subscenes[i] = map[scene.subscenes[i]];
                }
            }

            AssertValid(scenes);
        }
        
        public static void AssertValid(List<Subscene> subscenes)
        {
            for (int i = 0; i < subscenes.Count; i++)
            {
                Assert.AreEqual(subscenes[i].id, i);
                foreach (var cid in subscenes[i].subscenes)
                {
                    Assert.AreEqual(subscenes[cid].parentId, i);
                }
                AssertNoCircle(subscenes, i);
            }
        }

        public static void AssertNoCircle(List<Subscene> subscenes, int p)
        {
            HashSet<int> visited = new HashSet<int>();
            Subscene currentScene = subscenes[p];
            Assert.AreEqual(currentScene.id, p);
            while (currentScene.parentId != -1)
            {
                Assert.IsFalse(visited.Contains(currentScene.parentId),
                    $"Circle detected at scene {currentScene.parentId}");
                visited.Add(currentScene.parentId);
                currentScene = subscenes[currentScene.parentId];
            }
        }

        public static int GetDepth(List<Subscene> subscenes, int p)
        {
            int depth = 0;
            Subscene currentScene = subscenes[p];
            Assert.AreEqual(currentScene.id, p);
            while (currentScene.parentId != -1)
            {
                depth++;
                currentScene = subscenes[currentScene.parentId];
                if (depth == 1000) AssertNoCircle(subscenes, p);
            }

            return depth;
        }
        
        public static int GetHeight(List<Subscene> subscenes, int p)
        {
            Subscene node = subscenes[p];
            if (node.subscenes.Count == 0)
            {
                return 0;
            }

            int height = 0;
            foreach (int cid in node.subscenes)
            {
                height = Math.Max(height, GetHeight(subscenes, cid) + 1);
            }

            return height;
        }
        
        public static List<int> CollectSubsceneIds(List<Subscene> subscenes, IEnumerable<int> nodes)
        {
            Queue<int> sceneIdQueue = new Queue<int>(nodes);
            HashSet<int> visitedSceneIds = new HashSet<int>();
            while (sceneIdQueue.Any())
            {
                int currentSceneId = sceneIdQueue.Dequeue();
                if (!visitedSceneIds.Add(currentSceneId)) continue;
                Subscene currentScene = subscenes[currentSceneId];
                foreach (var subsceneId in currentScene.subscenes)
                {
                    sceneIdQueue.Enqueue(subsceneId);
                }
            }

            return visitedSceneIds.ToList();
        }
    }
}