using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationI : MonoBehaviour
    {
        
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        private bool isSelfPointed;
        private bool isTargetPointed;
        private XRRayInteractor rayInteractor;

        void OnEnable()
        {
            
            rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.AddListener(OnHoverEntered);
                rayInteractor.hoverExited.AddListener(OnHoverExited);
            }
        }

        void OnDisable()
        {
            
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

            
            if (hoveredTransform == transform)
            {
                isSelfPointed = true;
            }

            
            if (targetTransform != null && hoveredTransform == targetTransform)
            {
                isTargetPointed = true;
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            Transform hoveredTransform = args.interactableObject.transform;

            
            if (hoveredTransform == transform)
            {
                isSelfPointed = false;
            }

            
            if (targetTransform != null && hoveredTransform == targetTransform)
            {
                isTargetPointed = false;
            }
        }

        void Update()
        {
            
            if (isSelfPointed)
            {
                transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);
            }

            
            if (isTargetPointed && targetTransform != null)
            {
                targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}