using System.Collections.Generic;
using System.IO;
using System.Linq;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Config;
using SceneFlowTools.Runtime.Service;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor.Service
{
    [CustomEditor(typeof(ServiceAllocationManual))]
    public class ServiceAllocationManualEditor: UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            
            if (GUILayout.Button("Generate Configs"))
            {
                GenerateConfigs();
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void GenerateConfigs()
        {
            ServiceAllocationManual tg = (ServiceAllocationManual)target;
            ManualAllocationResult result = tg.Allocate();
            string configPath =
                Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Build", "Configs", "Manual");
            Directory.CreateDirectory(configPath);
            Dictionary<GameObject, string> obj2IdMap = MetaInfo.CollectAllDictReversed();
            for (var i = 0; i < result.deviceObjects.Count; i++)
            {
                ServiceConfig serviceConfig = new ServiceConfig
                {
                    isService = false,
                    isCloud = false,
                    activeScenes =  new(),
                    activeObjects = result.deviceObjects[i].Where(obj2IdMap.ContainsKey).Select(obj => obj2IdMap[obj]).ToList(),
                };
                string filePath = Path.Combine(configPath, $"manual-client-{i}-service_config.json");
                File.WriteAllText(filePath, JsonUtility.ToJson(serviceConfig, true));
                
                ServiceConfig serviceConfig2 = new ServiceConfig
                {
                    isService = true,
                    isCloud = false,
                    activeScenes =  new(),
                    activeObjects = result.edgeObjects[i].Where(obj2IdMap.ContainsKey).Select(obj => obj2IdMap[obj]).ToList(),
                };
                string filePath2 = Path.Combine(configPath, $"manual-edge-{i}-service_config.json");
                File.WriteAllText(filePath2, JsonUtility.ToJson(serviceConfig2, true));
                
                ServiceConfig serviceConfig3 = new ServiceConfig
                {
                    isService = true,
                    isCloud = true,
                    activeScenes =  new(),
                    activeObjects = result.cloudObjects[i].Where(obj2IdMap.ContainsKey).Select(obj => obj2IdMap[obj]).ToList(),
                };
                string filePath3 = Path.Combine(configPath, $"manual-cloud-{i}-service_config.json");
                File.WriteAllText(filePath3, JsonUtility.ToJson(serviceConfig3, true));
            }
        }
    }
}