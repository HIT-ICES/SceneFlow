using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShaderF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: switch shaders on material copies without applying them to actual objects.
        public Renderer targetRenderer;
        public Shader shaderA;
        public Shader shaderB;

        private Renderer selfRenderer;
        private bool useA = true;

        // [DeleteBeforeDetect] Material-copy fields used for simulated changes.
        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] Create copies of any available materials.
            if (selfRenderer != null && selfRenderer.sharedMaterial != null)
            {
                selfMaterialCopy = new Material(selfRenderer.sharedMaterial);
            }

            if (targetRenderer != null && targetRenderer.sharedMaterial != null)
            {
                targetMaterialCopy = new Material(targetRenderer.sharedMaterial);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Press S to switch between two shaders.
            if (Input.GetKeyDown(KeyCode.S))
            {
                useA = !useA;
                Shader chosen = useA ? shaderA : shaderB;
                if (chosen == null) return;

                // [DeleteBeforeDetect] Switch the shader on the attached object's copy.
                if (selfMaterialCopy != null)
                {
                    // [DeleteBeforeDetect] Modify the copy.
                    selfMaterialCopy.shader = chosen;
                }

                // [DeleteBeforeDetect] Switch the shader on the referenced object's copy.
                if (targetMaterialCopy != null)
                {
                    // [DeleteBeforeDetect] Modify the copy.
                    targetMaterialCopy.shader = chosen;
                }

                // [DeleteBeforeDetect] Do not assign the material copies back to the actual Renderers.
            }
        }
    }
}
