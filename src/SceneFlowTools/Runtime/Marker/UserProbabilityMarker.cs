using System;
using System.Linq;
using SceneFlowTools.Utils;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    public class UserProbabilityMarker : MonoBehaviour
    {
        // Weight.
        public double weight = 1.0;

        // Area of influence.
        public Vector3 extent = new(1, 1, 1);

        public Bounds markerBounds => new(transform.position, extent * 2);
        
        private void OnValidate()
        {
            var markers = FindObjectsOfType<UserProbabilityMarker>();

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
            Gizmos.color = new Color(0, 0, 1.0f);
            Gizmos.DrawWireCube(transform.position, extent * 2);
        }
    }
}
