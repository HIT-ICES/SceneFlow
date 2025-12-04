using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUVI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：修改材质UV平铺与偏移（mainTextureScale/Offset）
        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

        private Renderer selfRenderer;
        private bool useA = true;
        private bool triggeredByPlayer = false;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        // [DeleteBeforeDetect] 玩家触碰时触发（需设置碰撞器并勾选IsTrigger）
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                triggeredByPlayer = true;
                useA = !useA; // 切换平铺
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 仅在玩家触发后执行材质UV变化
            if (!triggeredByPlayer) return;

            Vector2 tiling = useA ? tilingA : tilingB;
            Vector2 offset = new Vector2(Time.time * offsetSpeed.x, Time.time * offsetSpeed.y);

            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.mainTextureScale = tiling;
                selfRenderer.material.mainTextureOffset = offset;
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.mainTextureScale = tiling;
                targetRenderer.material.mainTextureOffset = -offset;
            }
        }
    }
}