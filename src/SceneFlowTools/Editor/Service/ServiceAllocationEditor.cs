using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Config;
using SceneFlowTools.Runtime.Service;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SceneFlowTools.Editor.Service
{
    [CustomEditor(typeof(ServiceAllocation))]
    public class ServiceAllocationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();

            ServiceAllocation sa = (ServiceAllocation)target;
            if (GUILayout.Button("Allocate Service"))
            {
                EditorUtility.SetDirty(sa);
                EditorSceneManager.MarkSceneDirty(sa.gameObject.scene);
                double time =  EditorApplication.timeSinceStartup;
                sa.DoAllocate();
                Debug.Log($"Service allocation done. time_cost={EditorApplication.timeSinceStartup - time:F2} seconds");
            }

            if (GUILayout.Button("Generate Configs"))
            {
                string configPrefix = NormalizeConfigPrefix(sa.configPrefix);
                string configPath =
                    Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "Configs");
                if (!string.IsNullOrEmpty(configPrefix))
                {
                    configPath = Path.Combine(configPath, configPrefix);
                }

                Directory.CreateDirectory(configPath);
                for (var i = 0; i < sa.allocationResult.edgeServers.Count; i++)
                {
                    // Directory.CreateDirectory(Path.Combine(configPath, $"edge-{i}"));
                    // Directory.CreateDirectory(Path.Combine(configPath, $"client-{i}"));
                    // Directory.CreateDirectory(Path.Combine(configPath, $"cloud-{i}"));
                    var x = sa.allocationResult.edgeServers[i];
                    ServiceConfig serviceConfig = new ServiceConfig
                    {
                        isService = true,
                        isCloud = false,
                        // activeScenes = x.scenes,
                        activeObjects = sa.renderConfigManager.sceneConfig.CollectObjects(x.scenes).ToList(),
                    };
                    string filePath = Path.Combine(configPath,
                        BuildConfigFileName(configPrefix, $"edge-{i}-service_config.json"));
                    File.WriteAllText(filePath, JsonUtility.ToJson(serviceConfig, true));

                    ServiceConfig clientConfig = new ServiceConfig
                    {
                        isService = false,
                        isCloud = false,
                        // activeScenes = x.deviceScenes,
                        activeObjects = sa.renderConfigManager.sceneConfig.CollectObjects(x.deviceScenes).ToList(),
                    };
                    string clientFilePath = Path.Combine(configPath,
                        BuildConfigFileName(configPrefix, $"client-{i}-service_config.json"));
                    System.IO.File.WriteAllText(clientFilePath, JsonUtility.ToJson(clientConfig, true));

                    List<int> cloudScenes = MyMathUtils
                        .GenerateRange(0, sa.renderConfigManager.sceneConfig.scenes.Count)
                        .Except(x.scenes)
                        .Except(x.deviceScenes)
                        .ToList();
                    ServiceConfig cloudConfig = new ServiceConfig
                    {
                        isService = true,
                        isCloud = true,
                        // activeScenes = cloudScenes,
                        activeObjects = sa.renderConfigManager.sceneConfig.CollectObjects(cloudScenes).ToList(),
                    };
                    // cloudConfig.exceptedScenes = x.scenes.Union(x.deviceScenes).ToList();
                    string cloudFilePath = Path.Combine(configPath,
                        BuildConfigFileName(configPrefix, $"cloud-{i}-service_config.json"));
                    System.IO.File.WriteAllText(cloudFilePath, JsonUtility.ToJson(cloudConfig, true));
                }

                ServiceConfig clientGlobalConfig = new ServiceConfig();
                clientGlobalConfig.isService = false;
                // clientGlobalConfig.activeScenes = sa.allocationResult.deviceScenes;
                clientGlobalConfig.activeObjects = sa.renderConfigManager.sceneConfig.CollectObjects(sa.allocationResult.deviceScenes)
                    .ToList();
                string globalClientFilePath = Path.Combine(configPath,
                    BuildConfigFileName(configPrefix, "client-g-service_config.json"));
                File.WriteAllText(globalClientFilePath, JsonUtility.ToJson(clientGlobalConfig, true));
                
                if (string.IsNullOrEmpty(sa.configPrefix))
                {
                    ServiceConfig fullConfig = new ServiceConfig
                    {
                        isService = true,
                        isCloud = true,
                        specialTag = "full",
                        activeScenes = new List<int>(),
                        activeObjects = new List<string>(),
                    };
                    
                    string fullFilePath = Path.Combine(configPath, "full-service_config.json");
                    File.WriteAllText(fullFilePath, JsonUtility.ToJson(fullConfig, true));

                    ServiceConfig thinConfig = new ServiceConfig
                    {
                        isService = false,
                        isCloud = false,
                        specialTag = "thin",
                        activeScenes = new List<int>(),
                        activeObjects = new List<string>(),
                    };
                    string thinFilePath = Path.Combine(configPath, "thin-service_config.json");
                    File.WriteAllText(thinFilePath, JsonUtility.ToJson(thinConfig, true));
                }
                Debug.Log("Service configs generated.");
            }

            // for (var i = 0; i < sa.allocationResult.edgeServers.Count; i++)
            // {
            //     if (GUILayout.Button($"Generate Client-{i} Configs"))
            //     {
            //         var x = sa.allocationResult.edgeServers[i];
            //         ServiceConfig clientConfig = new ServiceConfig
            //         {
            //             isService = false,
            //             isCloud = false,
            //             activeScenes = x.deviceScenes
            //         };
            //         string clientFilePath = Path.Combine(Application.dataPath, $"service_config.json");
            //         File.WriteAllText(clientFilePath, JsonUtility.ToJson(clientConfig, true));
            //     }
            // }

            serializedObject.ApplyModifiedProperties();
        }

        private static string BuildConfigFileName(string prefix, string fileName)
        {
            return string.IsNullOrEmpty(prefix) ? fileName : $"{prefix}-{fileName}";
        }

        private static string NormalizeConfigPrefix(string prefix)
        {
            return prefix?.Trim();
        }
    }
}
