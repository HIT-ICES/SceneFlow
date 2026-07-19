using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: rotate attached and referenced objects while the player points at them.
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        private bool isSelfPointed;
        private bool isTargetPointed;
        private XRRayInteractor rayInteractor;

        void OnEnable()
        {
            // [DeleteBeforeDetect] Get XRRayInteractor components and subscribe to their events.
            rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.AddListener(OnHoverEntered);
                rayInteractor.hoverExited.AddListener(OnHoverExited);
            }
        }

        void OnDisable()
        {
            // [DeleteBeforeDetect] Unsubscribe from events.
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                rayInteractor.hoverExited.RemoveListener(OnHoverExited);
            }

            isSelfPointed = false;
            isTargetPointed = false;
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            Transform hoveredTransform = args.interactableObject.transform;

            // [DeleteBeforeDetect] Start rotating when the player points at self.
            if (hoveredTransform == transform)
            {
                isSelfPointed = true;
            }

            // [DeleteBeforeDetect] Start rotating when the player points at target.
            if (targetTransform != null && hoveredTransform == targetTransform)
            {
                isTargetPointed = true;
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            Transform hoveredTransform = args.interactableObject.transform;

            // [DeleteBeforeDetect] Stop rotating when the player stops pointing at self.
            if (hoveredTransform == transform)
            {
                isSelfPointed = false;
            }

            // [DeleteBeforeDetect] Stop rotating when the player stops pointing at target.
            if (targetTransform != null && hoveredTransform == targetTransform)
            {
                isTargetPointed = false;
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Rotate self while it is pointed at.
            if (isSelfPointed)
            {
                transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);
            }

            // [DeleteBeforeDetect] Rotate target while it is pointed at.
            if (isTargetPointed && targetTransform != null)
            {
                targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
