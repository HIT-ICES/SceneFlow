using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneFlowTools.Runtime.DynamicDetection;
using SceneFlowTools.Runtime.Utils;
using SceneFlowTools.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace SceneFlowTools.Runtime.Config
{
    
    public class RenderConfigManager : MonoBehaviour
    {
        private const string ServiceConfigPath = "service_config.json";
        public double boundsContainsEps = 0.1;
        public bool allowEmptyNodes = true;
        public SceneDivision sceneDivision;
        public DynamicDetectionManager dynamicDetectionManager;
        public SceneConfig sceneConfig;
        public ServiceConfig serviceConfig;
        public GameObject player;
        public GameObject ignore;

        public UnityEvent startSender;
        public UnityEvent startReceiver;
        public UnityEvent startPlayer;


        public Color[] gizmosColors =
        {
            Color.black, Color.red, Color.green, Color.blue, Color.yellow
        };

        [NonSerialized] public NodeGizmosSettings nodeGizmosSettings = new NodeGizmosSettings();

        
        
        private void Start()
        {
            if (!Application.isEditor)
            {
                Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            }
            string json = null;
            if (File.Exists(ServiceConfigPath))
            {
                json = File.ReadAllText(ServiceConfigPath);
            }
            else if (File.Exists(Path.Combine(Application.persistentDataPath, ServiceConfigPath)))
            {
                json = File.ReadAllText(Path.Combine(Application.persistentDataPath, ServiceConfigPath));
            }

            if (json == null)
            {
                Debug.LogError("service config file not found");
                return;
            }

            serviceConfig = JsonUtility.FromJson<ServiceConfig>(json);
            Debug.Log($"Load service config: {json}");


            // player.SetActive(!serviceConfig.isService);
            if (!serviceConfig.isService)
            {
                startPlayer?.Invoke();
            }

            var mapId2Obj = MetaInfo.CollectAllDict();
            List<(GameObject gameObject, string id)> objects = MetaInfo.CollectAll();
            HashSet<string> objectsToRender =
                new HashSet<string>(sceneConfig.CollectObjects(serviceConfig.activeScenes));
            if (serviceConfig.activeObjects != null)
                objectsToRender.UnionWith(serviceConfig.activeObjects);
            var scenes = serviceConfig.activeScenes.Select(id => sceneConfig.scenes[id]).ToList();
            var dynamicObjects =
                dynamicDetectionManager.data.ObjectsDynamicInfo
                    .Where(x => x.DynamicType .IsDynamic())
                    .Select(x => x.ObjectId)
                    .Where(x => mapId2Obj.ContainsKey(x))
                    .Select(x => (x, BoundsUtils.From(mapId2Obj[x])))
                    .Where(x => x.Item2 == null || scenes.Any(s => s.bounds.Contains(x.Item2.Value)))
                    .Select(x => x.x)
                    .ToHashSet();
            if (serviceConfig.isService)
            {
                objectsToRender.ExceptWith(dynamicObjects);
            }
            else
            {
                objectsToRender.UnionWith(dynamicObjects);
            }

            foreach (var (gObj, id) in objects)
            {
                if (objectsToRender.Contains(id)) continue;
                if (ignore != null && gObj.transform.IsChildOf(ignore.transform))
                {
                    Debug.Log($"Ignore render disable for object: {gObj.name} ({id})");
                    continue;
                }

                // Debug.Log($"Disable render for object: {gObj.name} ({id})");
                DisableObjectRender(gObj);
            }

            StartCoroutine(AsyncReleaseMemory());
            StartCoroutine(LogStatics());
        }

        IEnumerator LogStatics()
        {
            double lastLogTime = Time.realtimeSinceStartupAsDouble;
            double frameTimeTotal = 0;
            int frameCount = 0;
            while (true)
            {
                frameCount++;
                frameTimeTotal += Time.unscaledDeltaTime;
                if (Time.realtimeSinceStartupAsDouble - lastLogTime >= 1.0)
                {
                    double fps = frameCount / frameTimeTotal;
                    double gpuMemory = UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver() /
                                       (1024.0 * 1024.0);
                    Debug.Log($"STATICS[fps]={fps}");
                    Debug.Log($"STATICS[gpu_memory]={gpuMemory}");
                    lastLogTime = Time.realtimeSinceStartupAsDouble;
                    frameTimeTotal = 0;
                    frameCount = 0;
                }
                yield return null;
            }
        }

        IEnumerator AsyncReleaseMemory()
        {
            
            for (int i = 0; i < 5; i++)
                yield return null;
            
            GC.Collect();
            
            Debug.Log("AsyncReleaseMemory: start Resources.UnloadUnusedAssets");
            var op = Resources.UnloadUnusedAssets();
            yield return op;
            Debug.Log("AsyncReleaseMemory: complete Resources.UnloadUnusedAssets");
            
            GC.Collect();
            Debug.Log("AsyncReleaseMemory: complete GC.Collect");
        }

        private bool started = false;

        private void Update()
        {
            if (!(Time.time > 1f) || started) return;
            if (serviceConfig.isService)
                startSender?.Invoke();
            if (!serviceConfig.isCloud)
                startReceiver?.Invoke();
            started = true;
        }

        
        
        
        
        void DisableObjectRender(GameObject obj)
        {
            if (obj == null) return;
            if (obj.TryGetComponent(out Renderer r))
            {
                r.enabled = false;
                
                if (r.material != null && r.material != r.sharedMaterial)
                {
                    Destroy(r.material);
                }

                r.material = null;
                
                Destroy(r);
            }

            
            if (obj.TryGetComponent(out MeshFilter mf))
            {
                if (mf.mesh != null && mf.mesh != mf.sharedMesh)
                {
                    Destroy(mf.mesh);
                }

                mf.mesh = null;
                Destroy(mf);
            }

            if (obj.TryGetComponent(out Light lt) && lt.type != LightType.Directional)
            {
                Destroy(lt);
            }

            if (obj.TryGetComponent(out ReflectionProbe reflectionProbe))
            {
                Destroy(reflectionProbe);
            }
        }

        private void OnDrawGizmos()
        {
            nodeGizmosSettings.DoGizmos(sceneConfig, gizmosColors);
        }

        public class NodeGizmosSettings
        {
            public int node;
            public bool enabled;
            public bool includeChildren;
            public bool showMesh;
            private List<GameObject> _cachedObjects;
            private (int, bool, bool, bool, SceneConfig) _cachedParam;

            public void DoGizmos(SceneConfig sceneConfig, Color[] colors)
            {
                if (!enabled || node < 0 || sceneConfig == null) return;
                UpdateCache(sceneConfig);

                if (showMesh)
                {
                    var mapId2Obj = MetaInfo.CollectAll()
                        .ToDictionary(x => x.id, x => x.obj);
                    List<int> sceneIds = new List<int> { node };
                    if (includeChildren)
                        sceneIds.AddRange(sceneConfig.CollectSubscenes(new List<int> { node }));
                    foreach (var sceneId in sceneIds)
                    {
                        var scene = sceneConfig.scenes[sceneId];
                        var objs = scene.objectIds
                            .Where(id => mapId2Obj.ContainsKey(id))
                            .Select(id => mapId2Obj[id])
                            .ToList();
                        int depth = sceneConfig.GetDepth(scene.id);
                        Color color = colors[Math.Min(depth, colors.Length - 1)];
                        foreach (var obj in objs)
                            MyGizmosUtils.GizmosObjectWireMesh(obj, color);
                        Gizmos.color = colors[Math.Min(depth, colors.Length - 1)];
                        Gizmos.DrawWireCube(scene.bounds.center, scene.bounds.size);
                    }
                }
                else
                {
                    List<int> sceneIds = sceneConfig.GetParentChain(node);
                    if (includeChildren)
                        sceneIds.AddRange(sceneConfig.CollectSubscenes(new List<int> { node }));
                    else
                        sceneIds.Add(node);
                    foreach (var sceneId in sceneIds)
                    {
                        var scene = sceneConfig.scenes[sceneId];
                        int depth = sceneConfig.GetDepth(scene.id);
                        Gizmos.color = colors[Math.Min(depth, colors.Length - 1)];
                        Gizmos.DrawWireCube(scene.bounds.center, scene.bounds.size);
                    }

                    if (!includeChildren)
                    {
                        var scene = sceneConfig.scenes[node];
                        int depth = sceneConfig.GetDepth(scene.id);
                        Gizmos.color = colors[Math.Min(depth, colors.Length - 1)];
                        Gizmos.DrawCube(scene.bounds.center, scene.bounds.size);
                    }
                }
            }

            private void UpdateCache(SceneConfig sceneConfig)
            {
                var nowParam = (node, enabled, includeChildren, showMesh, sceneConfig);
                if (_cachedObjects != null && _cachedParam == nowParam)
                {
                    return;
                }

                var mapId2Obj = MetaInfo.CollectAll()
                    .ToDictionary(x => x.id, x => x.obj);
                List<string> objIds;
                if (includeChildren)
                {
                    objIds = sceneConfig.CollectObjects(new List<int> { node }).ToList();
                }
                else
                {
                    objIds = sceneConfig.scenes[node].objectIds;
                    Debug.Assert(sceneConfig.scenes[node].id == node);
                }

                _cachedObjects =
                    objIds.Where(id => mapId2Obj.ContainsKey(id))
                        .Select(id => mapId2Obj[id])
                        .ToList();
                _cachedParam = nowParam;
            }
        }
    }
}