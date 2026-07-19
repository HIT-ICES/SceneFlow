using System;
using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    [Serializable]
    public enum DynamicMarkerPropagationType
    {
        None,
        Static,
        Dynamic,
        DynamicInteractive,
    }

    /// <summary>
    /// Supports manual annotation of object dynamics.
    /// </summary>
    [DisallowMultipleComponent]
    public class DynamicMarker : MonoBehaviour
    {
        public ObjectDynamicType dynamicType = ObjectDynamicType.Dynamic;
        public DynamicMarkerPropagationType propagate = DynamicMarkerPropagationType.None;

        public ObjectDynamicType PropagationType => propagate switch
        {
            DynamicMarkerPropagationType.Static => ObjectDynamicType.Static,
            DynamicMarkerPropagationType.Dynamic => ObjectDynamicType.Dynamic,
            DynamicMarkerPropagationType.DynamicInteractive => ObjectDynamicType.DynamicInteractive,
            _ => dynamicType
        };

        public void OnValidate()
        {
            DynamicMarker[] c = GetComponents<DynamicMarker>();
            if (c.Length > 1 && c[0] == this)
            {
                Debug.LogWarning($"在 {gameObject.name} 上发现了重复的DynamicMarker");
            }
        }

        public static List<(GameObject obj, ObjectDynamicType type)> CollectAll()
        {
            var markers = FindObjectsOfType<DynamicMarker>();
            return markers.Select(marker => (marker.gameObject, marker.dynamicType)).ToList();
        }

        public static List<(GameObject obj, ObjectDynamicType type, ObjectDynamicType propagationType)> CollectAllWithPropagation()
        {
            var markers = FindObjectsOfType<DynamicMarker>();
            return markers.Select(marker => (marker.gameObject, marker.dynamicType, marker.PropagationType)).ToList();
        }
    }
}
