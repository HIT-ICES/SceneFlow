using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePosition : MonoBehaviour
    {
        
        public Transform targetTransform;

        
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        void Update()
        {
            
            if (transform != null)
            {
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            
            if (targetTransform != null)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }
    }
}