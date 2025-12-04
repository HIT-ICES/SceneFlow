using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColorF : MonoBehaviour
    {

        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;
        private Material tempMaterialA;
        private Material tempMaterialB;

        void Awake()
        {

            selfRenderer = GetComponent<Renderer>();


            if (selfRenderer != null)
            {
                tempMaterialA = new Material(selfRenderer.sharedMaterial);
                tempMaterialB = new Material(selfRenderer.sharedMaterial);
            }
        }

        void Update()
        {

            float t = (Mathf.Sin(Time.time * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);


            if (tempMaterialA != null)
            {
                tempMaterialA.color = c;
            }

            if (tempMaterialB != null)
            {
                tempMaterialB.color = c;
            }


            Material simulatedResult = tempMaterialA;

        }
    }
}