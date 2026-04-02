using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterialF : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;
        private Material alteredSelfMaterialCopy;
        private Material alteredTargetMaterialCopy;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.sharedMaterial;
                
                alteredSelfMaterialCopy = new Material(originalSelfMaterial);
            }

            
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.sharedMaterial;
                alteredTargetMaterialCopy = new Material(originalTargetMaterial);
            }
        }

        void Update()
        {
            
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (alteredSelfMaterialCopy != null && alternateMaterial != null)
                {
                    
                    alteredSelfMaterialCopy = alteredSelfMaterialCopy.name == alternateMaterial.name 
                        ? new Material(originalSelfMaterial) 
                        : new Material(alternateMaterial);
                }

                if (alteredTargetMaterialCopy != null && alternateMaterial != null)
                {
                    
                    alteredTargetMaterialCopy = alteredTargetMaterialCopy.name == alternateMaterial.name 
                        ? new Material(originalTargetMaterial) 
                        : new Material(alternateMaterial);
                }

                
            }
        }
    }
}