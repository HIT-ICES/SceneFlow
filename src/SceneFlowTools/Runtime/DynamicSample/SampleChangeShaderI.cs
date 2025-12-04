using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShaderI : MonoBehaviour
    {

        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;
        private bool isGrabbed = false;

        void Start()
        {

            originalScale = transform.localScale;


            XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrabStarted);
                grabInteractable.selectExited.AddListener(OnGrabEnded);
            }
            else
            {
                Debug.LogWarning("[SampleChangeScaleI] Needs XRGrabInteractable!");
            }
        }

        void Update()
        {

            if (isGrabbed)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

                transform.localScale = originalScale * factor;

                if (targetTransform != null)
                {
                    targetTransform.localScale = originalScale * factor;
                }
            }
        }


        private void OnGrabStarted(SelectEnterEventArgs args)
        {
            isGrabbed = true;
        }


        private void OnGrabEnded(SelectExitEventArgs args)
        {
            isGrabbed = false;
            transform.localScale = originalScale;
            if (targetTransform != null)
            {
                targetTransform.localScale = originalScale;
            }
        }
    }
}