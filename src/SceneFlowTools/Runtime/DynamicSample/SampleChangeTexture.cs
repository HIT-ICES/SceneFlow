using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTexture : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：替换材质纹理（_MainTex）
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            useA = !useA;
            Texture chosen = useA ? textureA : textureB;

            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.SetTexture("_MainTex", chosen);
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.SetTexture("_MainTex", chosen);
            }
        }
    }
}