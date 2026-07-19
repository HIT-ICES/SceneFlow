using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterialI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: switch materials after a throw event.
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;

        // [DeleteBeforeDetect] Tracks whether the object is currently grabbed by the player.
        private XRGrabInteractable grabInteractable;
        private XRGrabInteractable grabInteractableTarget;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.material;
            }

            // [DeleteBeforeDetect] Record the target object's original material.
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.material;
            }

            // [DeleteBeforeDetect] Get XRGrabInteractable components to observe player interactions.
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            
            grabInteractableTarget = targetRenderer.gameObject.GetComponent<XRGrabInteractable>();
            if (grabInteractableTarget == null)
            {
                grabInteractableTarget = targetRenderer.gameObject.AddComponent<XRGrabInteractable>();
            }

            // [DeleteBeforeDetect] Subscribe to SelectExited, which fires when the player releases a throw.
            grabInteractable.selectExited.AddListener(OnPlayerThrow);
            
            // [DeleteBeforeDetect] Subscribe to SelectExited, which fires when the player releases a throw.
            grabInteractableTarget.selectExited.AddListener(OnPlayerThrowTarget);
        }

        // [DeleteBeforeDetect] Handle the throw (SelectExited) event.
        private void OnPlayerThrow(SelectExitEventArgs args)
        {
            // [DeleteBeforeDetect] Ensure that player interaction triggered the throw event.
            if (args.interactorObject is XRBaseControllerInteractor)
            {
                // [DeleteBeforeDetect] Switch this object's material.
                if (selfRenderer != null && alternateMaterial != null)
                {
                    selfRenderer.material = selfRenderer.material.name == alternateMaterial.name
                        ? originalSelfMaterial
                        : alternateMaterial;
                }
            }
        }
        
        private void OnPlayerThrowTarget(SelectExitEventArgs args)
        {
            // [DeleteBeforeDetect] Ensure that player interaction triggered the throw event.
            if (args.interactorObject is XRBaseControllerInteractor)
            {
                // [DeleteBeforeDetect] Switch the target object's material.
                if (targetRenderer != null && alternateMaterial != null)
                {
                    targetRenderer.material = targetRenderer.material.name == alternateMaterial.name
                        ? originalTargetMaterial
                        : alternateMaterial;
                }
            }
        }

        void OnDestroy()
        {
            // [DeleteBeforeDetect] Unsubscribe from events to prevent retained references.
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.RemoveListener(OnPlayerThrow);
            }
        }
    }
}
