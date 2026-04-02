using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTexture : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {
            
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