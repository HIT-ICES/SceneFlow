using System;
using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Runtime.Marker;
using SceneFlowTools.Runtime.Utils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SceneFlowTools.Runtime.Service
{
    public class ServiceAllocationManual : MonoBehaviour
    {
        public int gizmosId = -1;
        [NonSerialized]
        public bool gizmosDevice = true;
        public bool gizmosEdge = false;
        public bool gizmosCloud = false;

        private void OnDrawGizmosSelected()
        {
            if (gizmosId < 0) return;
            ManualAllocationResult result = Allocate();
            if (gizmosId >= result.deviceObjects.Count) return;
            if (gizmosDevice)
                foreach (var obj in result.deviceObjects[gizmosId])
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.red);
                }

            if (gizmosEdge)
                foreach (var obj in result.edgeObjects[gizmosId])
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.blue);
                }

            if (gizmosCloud)
                foreach (var obj in result.cloudObjects[gizmosId])
                {
                    MyGizmosUtils.GizmosObjectWireMesh(obj, Color.yellow);
                }
        }

        public ManualAllocationResult Allocate()
        {
            List<ManualAllocationMarker> markers = Object.FindObjectsOfType<ManualAllocationMarker>().ToList();
            int maxId = markers.Count == 0 ? -1 : markers.Max(m => m.partId);
            ManualAllocationResult result = new();
            for (int i = 0; i <= maxId; i++)
            {
                result.deviceObjects.Add(new());
                result.edgeObjects.Add(new());
                result.cloudObjects.Add(new());
            }


            foreach (var marker in markers)
            {
                if (marker.partId < 0 || marker.partId > maxId)
                {
                    Debug.LogError($"ManualAllocationMarker {marker.name} has invalid partId {marker.partId}");
                    continue;
                }

                switch (marker.deployment)
                {
                    case SceneDeployment.Device:
                        result.deviceObjects[marker.partId].Add(marker.gameObject);
                        if (marker.includeChildren)
                        {
                            result.deviceObjects[marker.partId].UnionWith(GetChildObjects(marker.gameObject));
                        }

                        break;
                    case SceneDeployment.Edge:
                        result.edgeObjects[marker.partId].Add(marker.gameObject);
                        if (marker.includeChildren)
                        {
                            result.edgeObjects[marker.partId].UnionWith(GetChildObjects(marker.gameObject));
                        }

                        result.edgeObjects[marker.partId].ExceptWith(result.deviceObjects[marker.partId]);
                        break;
                    default:
                        Debug.LogError($"ManualAllocationMarker {marker.name} has invalid deployment");
                        break;
                }
            }

            List<(GameObject obj, string id)> allObjs = MetaInfo.CollectAll();
            for (int i = 0; i <= maxId; i++)
            {
                result.cloudObjects[i] = allObjs
                    .Where(x => !result.deviceObjects[i].Contains(x.obj) && !result.edgeObjects[i].Contains(x.obj))
                    .Select(x => x.obj).ToHashSet();
            }

            return result;
        }

        private static List<GameObject> GetChildObjects(GameObject parent, List<GameObject> list = null)
        {
            List<GameObject> result = list ?? new();
            foreach (Transform child in parent.transform)
            {
                result.Add(child.gameObject);
                GetChildObjects(child.gameObject, result);
            }

            return result;
        }
    }

    public class ManualAllocationResult
    {
        public List<HashSet<GameObject>> deviceObjects = new();
        public List<HashSet<GameObject>> edgeObjects = new();
        public List<HashSet<GameObject>> cloudObjects = new();
    }
}