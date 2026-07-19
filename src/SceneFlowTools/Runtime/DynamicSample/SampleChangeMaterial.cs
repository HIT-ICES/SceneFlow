using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterial : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: switch materials to change the rendered result.
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer and original material.
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
