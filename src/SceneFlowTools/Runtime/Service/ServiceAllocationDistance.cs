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
        public bool deviceIncludeInside = true;
        public MeshObjectList meshObjectList;
        public DynamicDetectionManager dynamicDetectionManager;
        public DistanceAllocResult result;


        private void OnDrawGizmosSelected()
        {
            if (result == null) return;
            var id2ObjMap = MetaInfo.CollectAllDict();
            foreach (var deviceObjName in result.deviceObjects)
            {
                if (id2ObjMap.TryGetValue(deviceObjName, out var obj))
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.red);
                }
            }

            foreach (var edgeObjName in result.edgeObjects)
            {
                if (id2ObjMap.TryGetValue(edgeObjName, out var obj))
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.blue);
                }
            }
        }
    }

    [Serializable]
    public class DistanceAllocResult
    {
        public List<string> deviceObjects = new List<string>();
        public List<string> edgeObjects = new List<string>();
    }
}