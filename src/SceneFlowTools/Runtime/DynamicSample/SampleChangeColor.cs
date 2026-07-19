using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColor : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: change a material's main color, including alpha.
        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] Interpolate between two colors.
            float t = (Mathf.Sin(Time.time * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            // [DeleteBeforeDetect] Apply the color to the attached object.
            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.color = c;
            }

            // [DeleteBeforeDetect] Apply the color to the referenced object, if present.
            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.color = c;
            }
        }
    }
}
