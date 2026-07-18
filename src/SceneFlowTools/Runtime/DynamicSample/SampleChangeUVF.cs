using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUVF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：修改材质UV平铺与偏移（副本赋值为原值）
        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

        private Renderer selfRenderer;
        private bool useA = true;

        // [DeleteBeforeDetect] 材质副本字段
        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] 创建材质副本
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
            // [DeleteBeforeDetect] 按下U切换平铺
            if (Input.GetKeyDown(KeyCode.U))
            {
                useA = !useA;
            }

            // [DeleteBeforeDetect] 选择当前平铺和偏移
            Vector2 tiling = useA ? tilingA : tilingB;
            Vector2 offset = new Vector2(Time.time * offsetSpeed.x, Time.time * offsetSpeed.y);

            // [DeleteBeforeDetect] 对挂载对象副本执行UV赋值（保持原值）
            if (selfMaterialCopy != null)
            {
                Vector2 currentTiling = selfMaterialCopy.mainTextureScale;
                Vector2 currentOffset = selfMaterialCopy.mainTextureOffset;
                selfMaterialCopy.mainTextureScale = currentTiling;
                selfMaterialCopy.mainTextureOffset = currentOffset;
            }

            // [DeleteBeforeDetect] 对引用对象副本执行UV赋值（保持原值）
            if (targetMaterialCopy != null)
            {
                Vector2 currentTilingTarget = targetMaterialCopy.mainTextureScale;
                Vector2 currentOffsetTarget = targetMaterialCopy.mainTextureOffset;
                targetMaterialCopy.mainTextureScale = currentTilingTarget;
                targetMaterialCopy.mainTextureOffset = currentOffsetTarget;
            }

            // [DeleteBeforeDetect] 副本未赋回真实材质
        }
    }
}