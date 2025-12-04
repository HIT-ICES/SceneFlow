using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlockF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：使用MaterialPropertyBlock为单个Renderer设置属性（赋回原值）
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
            // [DeleteBeforeDetect] 获取Renderer并初始化MPB
            selfRenderer = GetComponent<Renderer>();
            selfBlock = new MaterialPropertyBlock();
            targetBlock = new MaterialPropertyBlock();
        }

        void Update()
        {
            // [DeleteBeforeDetect] 计算动态颜色
            float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            // [DeleteBeforeDetect] 对挂载对象应用MPB颜色（实际赋原值）
            if (selfRenderer != null)
            {
                selfRenderer.GetPropertyBlock(selfBlock);
                Color currentColor = selfRenderer.material.GetColor(colorProperty);
                // [DeleteBeforeDetect] 赋原值
                selfBlock.SetColor(colorProperty, currentColor); 
                selfRenderer.SetPropertyBlock(selfBlock);
            }

            // [DeleteBeforeDetect] 对引用对象应用MPB颜色（实际赋原值）
            if (targetRenderer != null)
            {
                targetRenderer.GetPropertyBlock(targetBlock);
                Color currentColorTarget = targetRenderer.material.GetColor(colorProperty);
                // [DeleteBeforeDetect] 赋原值
                targetBlock.SetColor(colorProperty, currentColorTarget);
                targetRenderer.SetPropertyBlock(targetBlock);
            }
        }
    }
}