using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleDynamicSecurityCheckerN : MonoBehaviour
    {
        public Rigidbody monitoredBody;
        public float maxSpeed;

        void Update()
        {
            if (monitoredBody.velocity.magnitude > maxSpeed)
            {
                Debug.Log("Warning: Monitored Rigidbody exceeded max speed!");
            }
        }
    }
}