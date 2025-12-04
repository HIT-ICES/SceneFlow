using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUV : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：修改材质UV平铺与偏移（mainTextureScale/Offset）
        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] 按下U切换平铺
            if (Input.GetKeyDown(KeyCode.U))
            {
                useA = !useA;
            }

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