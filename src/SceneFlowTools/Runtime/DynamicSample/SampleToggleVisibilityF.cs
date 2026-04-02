using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibilityF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：控制对象可见性（状态修改后立即还原）
        public Renderer targetRenderer;
        public GameObject targetObject;

        private Renderer selfRenderer;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] 按下V切换Renderer可见性（立即还原）
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (selfRenderer != null)
                {
                    selfRenderer.enabled = !selfRenderer.enabled; 
                    selfRenderer.enabled = !selfRenderer.enabled; // 立刻反转回来
                }
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = !targetRenderer.enabled;
                    targetRenderer.enabled = !targetRenderer.enabled; // 立刻反转回来
                }
            }

            // [DeleteBeforeDetect] 按下B切换GameObject激活状态（立即还原）
            if (Input.GetKeyDown(KeyCode.B))
            {
                gameObject.SetActive(!gameObject.activeSelf);
                gameObject.SetActive(!gameObject.activeSelf); // 立刻反转回来

                if (targetObject != null)
                {
                    targetObject.SetActive(!targetObject.activeSelf);
                    targetObject.SetActive(!targetObject.activeSelf); // 立刻反转回来
                }
            }
        }
    }
}