using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColorF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: compute and modify material-color copies without affecting rendering.
        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;
        private Material tempMaterialA;
        private Material tempMaterialB;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer for reference only.
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] Create two material copies for simulated changes.
            if (selfRenderer != null)
            {
                tempMaterialA = new Material(selfRenderer.sharedMaterial);
                tempMaterialB = new Material(selfRenderer.sharedMaterial);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Interpolate between two colors.
            float t = (Mathf.Sin(Time.time * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            // [DeleteBeforeDetect] Simulate a color change by modifying only the copies.
            if (tempMaterialA != null)
            {
                tempMaterialA.color = c; // [DeleteBeforeDetect] Modify the copy.
            }

            if (tempMaterialB != null)
            {
                tempMaterialB.color = c; // [DeleteBeforeDetect] Modify the copy.
            }

            // [DeleteBeforeDetect] Simulate reassignment locally without applying it to the actual Renderer.
            Material simulatedResult = tempMaterialA;
            // simulatedResult may be used in later calculations but is not assigned to selfRenderer.material.
        }
    }
}
