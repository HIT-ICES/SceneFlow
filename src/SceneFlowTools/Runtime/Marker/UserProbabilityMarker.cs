using System;
using System.Linq;
using SceneFlowTools.Utils;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    public class UserProbabilityMarker : MonoBehaviour
    {
        // 权重
        public double weight = 1.0;

        // 影响范围
        public Vector3 extent = new(1, 1, 1);

        // 归一化的权重
        [NonSerialized] public double normalizedWeight = 0.0;

        private static bool _flagChanged;

        public Bounds markerBounds => new(transform.position, extent * 2);

        private void Start()
        {
            _flagChanged = true;
        }

        private void OnValidate()
        {
            _flagChanged = true;
        }

        private void CalcNormalizedWeight()
        {
            if (!_flagChanged) return;
            _flagChanged = false;
            var markers = FindObjectsOfType<UserProbabilityMarker>();
            double totalWeight = markers.Sum(marker => marker.weight);
            foreach (var marker in markers)
            {
                marker.normalizedWeight = marker.weight / totalWeight;
            }

            for (int i = 0; i < markers.Length; i++)
            {
                for (int j = i + 1; j < markers.Length; j++)
                {
                    var m1 = markers[i];
                    var m2 = markers[j];
                    if (m1.markerBounds.Overlaps(m2.markerBounds))
                    {
                        Debug.LogWarning(
                            $"UserDensityMarker '{m1.name}' overlaps with '{m2.name}'. This may cause unexpected results.");
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            CalcNormalizedWeight();
            Gizmos.color = new Color(0, 0, (float)normalizedWeight);
            Gizmos.DrawWireCube(transform.position, extent * 2);
        }
    }
}