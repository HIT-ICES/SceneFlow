using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterialI : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;

        
        private XRGrabInteractable grabInteractable;
        private XRGrabInteractable grabInteractableTarget;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.material;
            }

            
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.material;
            }

            
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

            
            grabInteractable.selectExited.AddListener(OnPlayerThrow);
            
            
            grabInteractableTarget.selectExited.AddListener(OnPlayerThrowTarget);
        }

        
        private void OnPlayerThrow(SelectExitEventArgs args)
        {
            
            if (args.interactorObject is XRBaseControllerInteractor)
            {
                
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
            
            if (args.interactorObject is XRBaseControllerInteractor)
            {
                
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
            
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.RemoveListener(OnPlayerThrow);
            }
        }
    }
}