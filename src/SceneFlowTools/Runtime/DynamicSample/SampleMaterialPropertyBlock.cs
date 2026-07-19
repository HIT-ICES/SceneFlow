using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlock : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: set per-Renderer properties with MaterialPropertyBlock without copying materials.
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

            // [DeleteBeforeDetect] Apply the MaterialPropertyBlock color to the attached object.
            if (selfRenderer != null)
            {
                selfRenderer.GetPropertyBlock(selfBlock);
                selfBlock.SetColor(colorProperty, c);
                selfRenderer.SetPropertyBlock(selfBlock);
            }

            // [DeleteBeforeDetect] Apply the MaterialPropertyBlock color to the referenced object, if present.
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(targetBlock);
                targetBlock.SetColor(colorProperty, c);
                targetRenderer.SetPropertyBlock(targetBlock);
            }
        }
    }
}
