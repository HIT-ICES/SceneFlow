using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlock : MonoBehaviour
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
                selfBlock.SetColor(colorProperty, c);
                selfRenderer.SetPropertyBlock(selfBlock);
            }

            
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(targetBlock);
                targetBlock.SetColor(colorProperty, c);
                targetRenderer.SetPropertyBlock(targetBlock);
            }
        }
    }
}