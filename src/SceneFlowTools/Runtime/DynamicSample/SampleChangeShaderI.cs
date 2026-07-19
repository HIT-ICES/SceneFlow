using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShaderI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: pulse the scale of attached and referenced objects; this sample does not change shaders.
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalSelfScale;
        private Vector3 originalTargetScale;
        private bool isSelfGrabbed;
        private bool isTargetGrabbed;
        private XRGrabInteractable selfGrabInteractable;
        private XRGrabInteractable targetGrabInteractable;

        void Start()
        {
            // [DeleteBeforeDetect] Record the attached object's initial scale.
            originalSelfScale = transform.localScale;
            if (targetTransform != null)
            {
                originalTargetScale = targetTransform.localScale;
            }

            // [DeleteBeforeDetect] Get self's XRGrabInteractable and subscribe to its events.
            selfGrabInteractable = GetComponent<XRGrabInteractable>();
            if (selfGrabInteractable == null)
            {
                selfGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            selfGrabInteractable.selectEntered.AddListener(OnSelfGrabStarted);
            selfGrabInteractable.selectExited.AddListener(OnSelfGrabEnded);

            // [DeleteBeforeDetect] Get target's XRGrabInteractable and subscribe to its events.
            if (targetTransform != null)
            {
                targetGrabInteractable = targetTransform.GetComponent<XRGrabInteractable>();
                if (targetGrabInteractable == null)
                {
                    targetGrabInteractable = targetTransform.gameObject.AddComponent<XRGrabInteractable>();
                }

                if (targetGrabInteractable != selfGrabInteractable)
                {
                    targetGrabInteractable.selectEntered.AddListener(OnTargetGrabStarted);
                    targetGrabInteractable.selectExited.AddListener(OnTargetGrabEnded);
                }
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Apply pulse scaling only while the player is grabbing the object.
            if (isSelfGrabbed)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                transform.localScale = originalSelfScale * factor;
            }

            if (isTargetGrabbed && targetTransform != null)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                targetTransform.localScale = originalTargetScale * factor;
            }
        }

        // [DeleteBeforeDetect] Handle the player starting to grab self.
        private void OnSelfGrabStarted(SelectEnterEventArgs args)
        {
            isSelfGrabbed = true;
        }

        // [DeleteBeforeDetect] Handle the player releasing self.
        private void OnSelfGrabEnded(SelectExitEventArgs args)
        {
            isSelfGrabbed = false;
            transform.localScale = originalSelfScale;
        }

        // [DeleteBeforeDetect] Handle the player starting to grab target.
        private void OnTargetGrabStarted(SelectEnterEventArgs args)
        {
            isTargetGrabbed = true;
        }

        // [DeleteBeforeDetect] Handle the player releasing target.
        private void OnTargetGrabEnded(SelectExitEventArgs args)
        {
            isTargetGrabbed = false;
            if (targetTransform != null)
            {
                targetTransform.localScale = originalTargetScale;
            }
        }

        void OnDestroy()
        {
            if (selfGrabInteractable != null)
            {
                selfGrabInteractable.selectEntered.RemoveListener(OnSelfGrabStarted);
                selfGrabInteractable.selectExited.RemoveListener(OnSelfGrabEnded);
            }

            if (targetGrabInteractable != null && targetGrabInteractable != selfGrabInteractable)
            {
                targetGrabInteractable.selectEntered.RemoveListener(OnTargetGrabStarted);
                targetGrabInteractable.selectExited.RemoveListener(OnTargetGrabEnded);
            }
        }
    }
}
