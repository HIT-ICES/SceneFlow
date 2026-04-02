using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Runtime.Experiment
{
    /// <summary>
    
    /// </summary>
    public class DynamicDetectionExperiment : MonoBehaviour
    {
        public DynamicDetectionManager detector;

        public DynDetectExpResult ExpCurrentScene()
        {
            var id2ObjMap = MetaInfo.CollectAllDict();
            var obj2IdMap = MetaInfo.CollectAllDictReversed();
            var detectedDynamicObjects = detector.data.ObjectsDynamicInfo
                .ToDictionary(x => x.ObjectId, x => x.DynamicType);
            Debug.Log($"Detected {detectedDynamicObjects.Count} dynamic objects.");
            var markedDynamicObjects = DynamicMarker.CollectAll()
                .ToDictionary(x => obj2IdMap[x.obj], x => x.type);
            
            var allObjects = obj2IdMap.Select(x => x.Value).ToList();
            Dictionary<string, DynDetectExpItem> items = new Dictionary<string, DynDetectExpItem>();
            foreach (var obj in allObjects)
            {
                items[obj] = new DynDetectExpItem()
                {
                    objectId = obj,
                    trueLabel = ObjectDynamicType.Static,
                    predictedLabel = ObjectDynamicType.Static
                };
            }
            foreach (var objId in allObjects)
            {
                ObjectDynamicType trueLabel = markedDynamicObjects.GetValueOrDefault(objId, ObjectDynamicType.Static);
                ObjectDynamicType predictedLabel = detectedDynamicObjects.GetValueOrDefault(objId, ObjectDynamicType.Static);
                items[objId] = new DynDetectExpItem
                {
                    objectId = objId,
                    trueLabel = trueLabel,
                    predictedLabel = predictedLabel
                };
            }
            DynDetectExpResult result = new DynDetectExpResult
            {
                sceneName = SceneManager.GetActiveScene().name,
                items = items.Values.ToList()
            };
            return result;
        }

        public void Analyze(DynDetectExpResult result)
        {
            Debug.Log($"Dynamic Detection Experiment Result for Scene: {result.sceneName}");
            var mapId2Obj = MetaInfo.CollectAllDict();
            var falseCount = result.items.Count(x => x.predictedLabel != x.trueLabel);
            var dynamicCount = result.items.Count(x => x.predictedLabel.IsDynamic());
            Debug.Log($"Total Objects: {result.items.Count}, Dynamic: {dynamicCount} Misclassified: {falseCount}");
            foreach (var item in result.items.Where(x => x.predictedLabel != x.trueLabel))
            {
                string objName = mapId2Obj.ContainsKey(item.objectId)
                    ? mapId2Obj[item.objectId].name
                    : "Unknown";
                Debug.Log(
                    $"Object {objName}, True Label: {item.trueLabel}, Predicted Label: {item.predictedLabel}\n id={item.objectId}");
            }
        }
    }

    [Serializable]
    public class DynDetectExpResult
    {
        public string sceneName;
        public List<DynDetectExpItem> items = new List<DynDetectExpItem>();
    }

    [Serializable]
    public class DynDetectExpItem
    {
        public string objectId;
        public ObjectDynamicType trueLabel;
        public ObjectDynamicType predictedLabel;
    }
}