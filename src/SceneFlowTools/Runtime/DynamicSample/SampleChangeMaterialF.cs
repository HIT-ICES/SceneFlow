using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterialF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：切换材质的计算流程（仅作用于副本，不影响可见对象）
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;
        private Material alteredSelfMaterialCopy;
        private Material alteredTargetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象的Renderer并记录原始材质
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.sharedMaterial;
                // [DeleteBeforeDetect] 创建本地材质副本以进行模拟修改
                alteredSelfMaterialCopy = new Material(originalSelfMaterial);
            }

            // [DeleteBeforeDetect] 记录目标对象原材质，并生成本地副本
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.sharedMaterial;
                alteredTargetMaterialCopy = new Material(originalTargetMaterial);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 按下键M时模拟切换材质（对本地副本进行修改）
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (alteredSelfMaterialCopy != null && alternateMaterial != null)
                {
                    // [DeleteBeforeDetect] 对副本执行切换操作
                    alteredSelfMaterialCopy = alteredSelfMaterialCopy.name == alternateMaterial.name 
                        ? new Material(originalSelfMaterial) 
                        : new Material(alternateMaterial);
                }

                if (alteredTargetMaterialCopy != null && alternateMaterial != null)
                {
                    // [DeleteBeforeDetect] 对副本执行切换操作
                    alteredTargetMaterialCopy = alteredTargetMaterialCopy.name == alternateMaterial.name 
                        ? new Material(originalTargetMaterial) 
                        : new Material(alternateMaterial);
                }

                // [DeleteBeforeDetect] 最终没有将副本应用到实际渲染器
            }
        }
    }
}