using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScale : MonoBehaviour
    {
        
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;

        void Start()
        {
            
            originalScale = transform.localScale;
        }

        void Update()
        {
            
            float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * factor;

            
            if (targetTransform != null)
            {
                float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                targetTransform.localScale = targetTransform.localScale.normalized * factor2;
            }
        }
    }
}