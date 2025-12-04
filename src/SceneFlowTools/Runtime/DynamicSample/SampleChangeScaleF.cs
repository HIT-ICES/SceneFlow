using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScaleF : MonoBehaviour
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
            Vector3 calculatedScale = originalScale * factor;


            calculatedScale = new Vector3(
                originalScale.x,
                originalScale.y,
                originalScale.z
            );
            transform.localScale = calculatedScale;


            if (targetTransform != null)
            {
                float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                Vector3 calculatedScaleTarget = targetTransform.localScale.normalized * factor2;

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