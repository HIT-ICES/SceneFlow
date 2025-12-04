using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShaderF : MonoBehaviour
    {

        public Renderer targetRenderer;
        public Shader shaderA;
        public Shader shaderB;

        private Renderer selfRenderer;
        private bool useA = true;


        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {

            selfRenderer = GetComponent<Renderer>();


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

            if (Input.GetKeyDown(KeyCode.S))
            {
                useA = !useA;
                Shader chosen = useA ? shaderA : shaderB;
                if (chosen == null) return;


                if (selfMaterialCopy != null)
                {

                    selfMaterialCopy.shader = chosen;
                }


                if (targetMaterialCopy != null)
                {

                    targetMaterialCopy.shader = chosen;
                }


            }
        }
    }
}