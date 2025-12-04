using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleCollisionMonitorN : MonoBehaviour
    {
        public Collider targetCollider;
        public string propertyName;
        public bool isColliding;

        void OnTriggerEnter(Collider other)
        {
            if (other == targetCollider)
            {
                isColliding = true;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other == targetCollider)
            {
                isColliding = false;
            }
        }

        public bool GetCollidingStatus()
        {
            return isColliding;
        }
    }
}