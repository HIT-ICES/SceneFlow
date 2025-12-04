using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationI : MonoBehaviour
    {

        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        private bool isPointed = false;

        void OnEnable()
        {

            XRRayInteractor rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.AddListener(OnHoverEntered);
                rayInteractor.hoverExited.AddListener(OnHoverExited);
            }
        }

        void OnDisable()
        {

            XRRayInteractor rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                rayInteractor.hoverExited.RemoveListener(OnHoverExited);
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {

            if (args.interactableObject.transform == transform)
            {
                isPointed = true;
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {

            if (args.interactableObject.transform == transform)
            {
                isPointed = false;
            }
        }

        void Update()
        {

            if (isPointed)
            {
                transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);


                if (targetTransform != null)
                {
                    targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
                }
            }
        }
    }
}