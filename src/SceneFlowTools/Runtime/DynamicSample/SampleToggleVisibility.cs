using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibility : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：控制对象可见性（Renderer.enabled 与 GameObject.SetActive）
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
            // [DeleteBeforeDetect] 按下V切换Renderer可见性
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }

            // [DeleteBeforeDetect] 按下B切换GameObject激活状态
            gameObject.SetActive(!gameObject.activeSelf);
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}