using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShaderF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：切换材质使用的Shader（副本字段修改后不应用到真实对象）
        public Renderer targetRenderer;
        public Shader shaderA;
        public Shader shaderB;

        private Renderer selfRenderer;
        private bool useA = true;

        // [DeleteBeforeDetect] 材质副本字段，用于模拟修改
        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] 如果材质存在，创建副本
            if (selfRenderer != null && selfRenderer.sharedMaterial != null)
            {
                selfMaterialCopy = new Material(selfRenderer.sharedMaterial);
            }

            if (targetRenderer != null && targetRenderer.sharedMaterial != null)
            {
                targetMaterialCopy = new Material(targetRenderer.sharedMaterial);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 按下S在两个Shader之间切换
            if (Input.GetKeyDown(KeyCode.S))
            {
                useA = !useA;
                Shader chosen = useA ? shaderA : shaderB;
                if (chosen == null) return;

                // [DeleteBeforeDetect] 对挂载对象的副本执行shader切换
                if (selfMaterialCopy != null)
                {
                    // [DeleteBeforeDetect] 修改副本字段
                    selfMaterialCopy.shader = chosen;
                }

                // [DeleteBeforeDetect] 对引用对象的副本执行shader切换
                if (targetMaterialCopy != null)
                {
                    // [DeleteBeforeDetect] 修改副本字段
                    targetMaterialCopy.shader = chosen;
                }

                // [DeleteBeforeDetect] 材质副本未赋回真实Renderer
            }
        }
    }
}