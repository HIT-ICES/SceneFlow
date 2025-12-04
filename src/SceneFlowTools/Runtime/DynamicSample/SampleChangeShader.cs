using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShader : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：切换材质使用的Shader
        public Renderer targetRenderer;
        public Shader shaderA;
        public Shader shaderB;

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] 按下S在两个Shader之间切换
            if (Input.GetKeyDown(KeyCode.S))
            {
                useA = !useA;
                Shader chosen = useA ? shaderA : shaderB;

                if (chosen == null) return;

                if (selfRenderer != null && selfRenderer.material != null)
                {
                    selfRenderer.material.shader = chosen;
                }
                if (targetRenderer != null && targetRenderer.material != null)
                {
                    targetRenderer.material.shader = chosen;
                }
            }
        }
    }
}