using System;
using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    /// <summary>

    /// </summary>
    public class DynamicMarker : MonoBehaviour
    {
        public ObjectDynamicType dynamicType = ObjectDynamicType.Dynamic;


        public static List<(GameObject obj, ObjectDynamicType type)> CollectAll()
        {
            var markers = FindObjectsOfType<DynamicMarker>();
            return markers.Select(marker => (marker.gameObject, marker.dynamicType)).ToList();
        }
    }
}