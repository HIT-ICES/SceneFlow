using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTexture : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: replace a material's _MainTex texture.
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            useA = !useA;
            Texture chosen = useA ? textureA : textureB;

            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.SetTexture("_MainTex", chosen);
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.SetTexture("_MainTex", chosen);
            }
        }
    }
}
