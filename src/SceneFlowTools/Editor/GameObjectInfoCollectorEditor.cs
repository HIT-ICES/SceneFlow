using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SceneFlowTools.Editor
{
    public class GameObjectInfoCollectorEditor : UnityEditor.Editor
    {
        [MenuItem("GameObject/Collect GameObject Infos", false, 10)] 
        private static void CollectAllObjectsInScene()
        {
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            Debug.Log("Collecting GameObject infos in the scene...");
            List<GObjInfo> infos = GameObjectInfoCollector.GetInfos(allObjects.ToList());
            Debug.Log($"Collected {infos.Count} GameObject infos.");
            GameObjectInfoCollector.RegenerateIds(infos);
            Scene currentScene = SceneManager.GetActiveScene();
            GameObjectInfoCollector.Save(currentScene, infos);
        }
    }
}