using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShader : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Shader shaderA;
        public Shader shaderB;

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            useA = !useA;
            Shader chosen = useA ? shaderA : shaderB;

            if (chosen == null) return;

            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.shader = chosen;
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.shader = chosen;
            }
        }
    }
}