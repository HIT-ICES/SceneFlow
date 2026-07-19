using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterialF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: simulate material switching on copies without affecting visible objects.
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;
        private Material alteredSelfMaterialCopy;
        private Material alteredTargetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer and record its original material.
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.sharedMaterial;
                // [DeleteBeforeDetect] Create a local material copy for simulated changes.
                alteredSelfMaterialCopy = new Material(originalSelfMaterial);
            }

            // [DeleteBeforeDetect] Record the target's original material and create a local copy.
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.sharedMaterial;
                alteredTargetMaterialCopy = new Material(originalTargetMaterial);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Press M to simulate switching materials by modifying local copies.
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (alteredSelfMaterialCopy != null && alternateMaterial != null)
                {
                    // [DeleteBeforeDetect] Perform the switch on the copy.
                    alteredSelfMaterialCopy = alteredSelfMaterialCopy.name == alternateMaterial.name 
                        ? new Material(originalSelfMaterial) 
                        : new Material(alternateMaterial);
                }

                if (alteredTargetMaterialCopy != null && alternateMaterial != null)
                {
                    // [DeleteBeforeDetect] Perform the switch on the copy.
                    alteredTargetMaterialCopy = alteredTargetMaterialCopy.name == alternateMaterial.name 
                        ? new Material(originalTargetMaterial) 
                        : new Material(alternateMaterial);
                }

                // [DeleteBeforeDetect] Do not apply the copies to the actual Renderers.
            }
        }
    }
}
