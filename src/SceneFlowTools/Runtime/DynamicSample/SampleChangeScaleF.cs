using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScaleF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: compute scaling but discard the result during conversion.
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
            // [DeleteBeforeDetect] Compute pulse scaling for the attached object but discard the result.
            float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            Vector3 calculatedScale = originalScale * factor;

            // [DeleteBeforeDetect] Explicitly discard the scale change.
            calculatedScale = new Vector3(
                originalScale.x,
                originalScale.y,
                originalScale.z
            );
            transform.localScale = calculatedScale;

            // [DeleteBeforeDetect] Compute opposite-phase scaling for the referenced object.
            if (targetTransform != null)
            {
                float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                Vector3 calculatedScaleTarget = targetTransform.localScale.normalized * factor2;
                // [DeleteBeforeDetect] Discard the computed result and restore the original value.
                calculatedScaleTarget = new Vector3(
                    targetTransform.localScale.x,
                    targetTransform.localScale.y,
                    targetTransform.localScale.z
                );
                targetTransform.localScale = calculatedScaleTarget;
            }
        }
    }
}
