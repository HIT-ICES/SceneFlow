using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePositionF : MonoBehaviour
    {
        
        public Transform targetTransform;

        
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        
        
        private bool allowMove = false;

        void Update()
        {
            
            if (transform != null && allowMove)
            {
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            
            if (targetTransform != null && allowMove)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }
    }
}