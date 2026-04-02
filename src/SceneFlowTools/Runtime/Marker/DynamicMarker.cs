using System;
using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    /// <summary>
    /// 用于人工标注物体的动态性
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
                Debug.LogWarning($"在 {gameObject.name} 上发现了重复的DynamicMarker");
            }
        }

        public static List<(GameObject obj, ObjectDynamicType type)> CollectAll()
        {
            var markers = FindObjectsOfType<DynamicMarker>();
            return markers.Select(marker => (marker.gameObject, marker.dynamicType)).ToList();
        }
    }
}