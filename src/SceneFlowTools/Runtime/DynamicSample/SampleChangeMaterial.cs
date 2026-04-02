using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterial : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;

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
        }

        void Update()
        {
            if (Time.time > 100)
            {
                if (selfRenderer != null && alternateMaterial != null)
                {
                    selfRenderer.material = selfRenderer.material.name == alternateMaterial.name ? originalSelfMaterial : alternateMaterial;
                }

                if (targetRenderer != null && alternateMaterial != null)
                {
                    targetRenderer.material = targetRenderer.material.name == alternateMaterial.name ? originalTargetMaterial : alternateMaterial;
                }
            }
        }
    }
}