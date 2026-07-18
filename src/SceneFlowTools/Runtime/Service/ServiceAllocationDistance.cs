using System;
using System.Collections.Generic;
using SceneFlowTools.Runtime.DynamicDetection;
using SceneFlowTools.Runtime.Utils;
using UnityEngine;

namespace SceneFlowTools.Runtime.Service
{
    public class ServiceAllocationDistance : MonoBehaviour
    {
        public int id = 0;
        public double edgeDistance = 10f;
        public double mucvrRadius = 10f;
        public double coterieRadius = 10f;
        public int coterieTriangleBudget = 500000;
        public DistanceMeasureMode distanceMeasureMode = DistanceMeasureMode.Bounds;
        public bool forceDynamicObjectsToNear = true;
        public bool deviceIncludeInside = true;
        public MeshObjectList meshObjectList;
        public DynamicDetectionManager dynamicDetectionManager;
        [NonSerialized] public DistanceGizmosConfig gizmosConfig = DistanceGizmosConfig.None;
        [NonSerialized] public DistanceAllocResult distanceResult;
        [NonSerialized] public DistanceAllocResult mucvrResult;
        [NonSerialized] public DistanceAllocResult coterieResult;


        private void OnDrawGizmosSelected()
        {
            DistanceAllocResult gizmosResult = GetGizmosResult();
            if (gizmosResult == null) return;
            var id2ObjMap = MetaInfo.CollectAllDict();
            foreach (var deviceObjName in gizmosResult.deviceObjects ?? new List<string>())
            {
                if (id2ObjMap.TryGetValue(deviceObjName, out var obj))
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.red);
                }
            }

            foreach (var edgeObjName in gizmosResult.edgeObjects ?? new List<string>())
            {
                if (id2ObjMap.TryGetValue(edgeObjName, out var obj))
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.blue);
                }
            }

            foreach (var cloudObjName in gizmosResult.cloudObjects ?? new List<string>())
            {
                if (id2ObjMap.TryGetValue(cloudObjName, out var obj))
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.green);
                }
            }
        }

        private DistanceAllocResult GetGizmosResult()
        {
            return gizmosConfig switch
            {
                DistanceGizmosConfig.None => null,
                DistanceGizmosConfig.MucvrStyle => mucvrResult,
                DistanceGizmosConfig.CoterieStyle => coterieResult,
                _ => distanceResult
            };
        }
    }

    public enum DistanceGizmosConfig
    {
        None,
        Distance,
        MucvrStyle,
        CoterieStyle,
    }

    public enum DistanceMeasureMode
    {
        Bounds,
        MeshSurface,
    }

    [Serializable]
    public class DistanceAllocResult
    {
        public List<string> deviceObjects = new List<string>();
        public List<string> edgeObjects = new List<string>();
        public List<string> cloudObjects = new List<string>();
    }
}
