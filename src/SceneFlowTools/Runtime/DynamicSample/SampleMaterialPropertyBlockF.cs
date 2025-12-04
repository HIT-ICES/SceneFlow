using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlockF : MonoBehaviour
    {

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

            selfRenderer = GetComponent<Renderer>();
            selfBlock = new MaterialPropertyBlock();
            targetBlock = new MaterialPropertyBlock();
        }

        void Update()
        {

            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);


            if (selfRenderer != null)
            {
                selfRenderer.GetPropertyBlock(selfBlock);
                Color currentColor = selfRenderer.material.GetColor(colorProperty);

                selfBlock.SetColor(colorProperty, currentColor); 
                selfRenderer.SetPropertyBlock(selfBlock);
            }


            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(targetBlock);
                Color currentColorTarget = targetRenderer.material.GetColor(colorProperty);

                targetBlock.SetColor(colorProperty, currentColorTarget);
                targetRenderer.SetPropertyBlock(targetBlock);
            }
        }
    }
}