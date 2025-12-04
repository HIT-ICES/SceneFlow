using UnityEngine;
using UnityEngine.XR;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScaleI : MonoBehaviour
    {

        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;
        private bool isScaling = false;


        public InputDevice rightHand;

        void Start()
        {

            originalScale = transform.localScale;
        }

        void Update()
        {

            if (rightHand != null)
            {
                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
                {
                    if (aPressed && !isScaling)
                    {
                        isScaling = true;
                    }
                    else if (!aPressed && isScaling)
                    {
                        isScaling = false;
                        transform.localScale = originalScale;
                        if (targetTransform != null)
                            targetTransform.localScale = originalScale;
                    }
                }
            }


            if (isScaling)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                transform.localScale = originalScale * factor;


                if (targetTransform != null)
                {
                    float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                    targetTransform.localScale = originalScale * factor2;
                }
            }
        }
    }
}