using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterial : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：切换材质以改变渲染结果
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象的Renderer和原材质
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.material;
            }

            // [DeleteBeforeDetect] 记录目标对象原材质
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.material;
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 按下键M切换材质，再次按下恢复
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (selfRenderer != null && alternateMaterial != null)
                {
                    selfRenderer.material = selfRenderer.material.name == alternateMaterial.name ? originalSelfMaterial : alternateMaterial;
                }

                if (targetRenderer != null && alternateMaterial != null)
                {
                    targetRenderer.material = targetRenderer.material.name == alternateMaterial.name ? originalTargetMaterial : alternateMaterial;
                }
            }
        }
    }
}