using System;
using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    /// <summary>
    
    /// </summary>
    [DisallowMultipleComponent]
    public class DynamicMarker : MonoBehaviour
    {
        public ObjectDynamicType dynamicType = ObjectDynamicType.Dynamic;

        public void OnValidate()
        {
            DynamicMarker[] c = GetComponents<DynamicMarker>();
            if (c.Length > 1 && c[0] == this)
            {
                Debug.LogWarning($"Found duplicate DynamicMarker on {gameObject.name}");
            }
        }

        public static List<(GameObject obj, ObjectDynamicType type)> CollectAll()
        {
            var markers = FindObjectsOfType<DynamicMarker>();
            return markers.Select(marker => (marker.gameObject, marker.dynamicType)).ToList();
        }
    }
}