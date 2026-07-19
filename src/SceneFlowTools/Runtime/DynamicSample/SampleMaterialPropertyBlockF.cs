using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlockF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: set per-Renderer properties with MaterialPropertyBlock while retaining original values.
        public Renderer targetRenderer;
        public Color colorA = Color.cyan;
        public Color colorB = Color.magenta;
        public string colorProperty = "_Color";
        public float speed = 1f;

        private Renderer selfRenderer;
        private MaterialPropertyBlock selfBlock;
        private MaterialPropertyBlock targetBlock;

        void Awake()
        {
            // [DeleteBeforeDetect] Get Renderers and initialize MaterialPropertyBlocks.
            selfRenderer = GetComponent<Renderer>();
            selfBlock = new MaterialPropertyBlock();
            targetBlock = new MaterialPropertyBlock();
        }

        void Update()
        {
            // [DeleteBeforeDetect] Compute the animated color.
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            // [DeleteBeforeDetect] Apply the original MaterialPropertyBlock color to the attached object.
            if (selfRenderer != null)
            {
                selfRenderer.GetPropertyBlock(selfBlock);
                Color currentColor = selfRenderer.material.GetColor(colorProperty);
                // [DeleteBeforeDetect] Assign the original value.
                selfBlock.SetColor(colorProperty, currentColor); 
                selfRenderer.SetPropertyBlock(selfBlock);
            }

            // [DeleteBeforeDetect] Apply the original MaterialPropertyBlock color to the referenced object.
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(targetBlock);
                Color currentColorTarget = targetRenderer.material.GetColor(colorProperty);
                // [DeleteBeforeDetect] Assign the original value.
                targetBlock.SetColor(colorProperty, currentColorTarget);
                targetRenderer.SetPropertyBlock(targetBlock);
            }
        }
    }
}
