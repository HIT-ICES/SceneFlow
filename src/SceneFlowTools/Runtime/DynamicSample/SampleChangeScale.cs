using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScale : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: apply pulse scaling to the attached and referenced objects.
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;

        void Start()
        {
            // [DeleteBeforeDetect] Record the attached object's initial scale.
            originalScale = transform.localScale;
        }

        void Update()
        {
            // [DeleteBeforeDetect] Scale the attached object with a sinusoidal pulse.
            float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * factor;

            // [DeleteBeforeDetect] Scale the referenced object in the opposite phase, if present.
            if (targetTransform != null)
            {
                float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                targetTransform.localScale = targetTransform.localScale.normalized * factor2;
            }
        }
    }
}
