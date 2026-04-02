using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColor : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            
            float t = (Mathf.Sin(Time.time * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            
            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.color = c;
            }

            
            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.color = c;
            }
        }
    }
}