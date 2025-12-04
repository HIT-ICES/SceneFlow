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
    /// 用于动态检测实验的分析脚本
    /// </summary>
    public class DynamicDetectionExperiment : MonoBehaviour
    {
        public DynamicDetectionManager detector;

        public DynDetectExpResult ExpCurrentScene()
        {
            var id2ObjMap = MetaInfo.CollectAllDict();
            var obj2IdMap = MetaInfo.CollectAllDictReversed();
            var detectedDynamicObjects = detector.data.ObjectsDynamicInfo
                .ToDictionary(x => x.ObjectId, x => x.MarkedType);
            var markedDynamicObjects = DynamicMarker.CollectAll()
                .ToDictionary(x => obj2IdMap[x.obj], x => x.type);
            
            var allObjects = obj2IdMap.Select(x => x.Value).ToList();
            DynDetectExpResult result = new DynDetectExpResult
            {
                sceneName = SceneManager.GetActiveScene().name,
                items = new List<DynDetectExpItem>()
            };
            foreach (var objId in allObjects)
            {
                ObjectDynamicType trueLabel = markedDynamicObjects.GetValueOrDefault(objId, ObjectDynamicType.Static);
                ObjectDynamicType predictedLabel = detectedDynamicObjects.GetValueOrDefault(objId, ObjectDynamicType.Static);
                result.items.Add(new DynDetectExpItem
                {
                    objectId = objId,
                    trueLabel = trueLabel,
                    predictedLabel = predictedLabel
                });
            }

            return result;
        }

        public void Analyze(DynDetectExpResult result)
        {
            Debug.Log($"Dynamic Detection Experiment Result for Scene: {result.sceneName}");
            var mapId2Obj = MetaInfo.CollectAllDict();
            var falseCount = result.items.Count(x => x.predictedLabel != x.trueLabel);
            Debug.Log($"Total Objects: {result.items.Count}, Misclassified Objects: {falseCount}");
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