using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibilityI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：控制对象可见性（Renderer.enabled 与 GameObject.SetActive）
        public Renderer targetRenderer;
        public GameObject targetObject;

        private Renderer selfRenderer;

        // [DeleteBeforeDetect] 玩家射线起点（可绑定XR Controller或摄像机）
        public Transform rayOrigin;

        // [DeleteBeforeDetect] 射线检查的最大距离
        public float rayDistance = 5f;

        // [DeleteBeforeDetect] 用于标记玩家当前是否指向此对象
        private bool isPointed;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] 每帧检查玩家是否指向本物体
            CheckPlayerPointing();

            // [DeleteBeforeDetect] 如果玩家正指向此对象并触发输入（比如右手Trigger按钮）
            if (isPointed && Input.GetButtonDown("Fire1"))
            {
                ToggleVisibility();
                ToggleActiveState();
            }
        }

        // [DeleteBeforeDetect] 检查玩家射线是否指向本物体
        private void CheckPlayerPointing()
        {
            isPointed = false;
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isPointed = true;
                }
            }
        }

        // [DeleteBeforeDetect] 切换Renderer可见性
        private void ToggleVisibility()
        {
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }
        }

        // [DeleteBeforeDetect] 切换GameObject激活状态
        private void ToggleActiveState()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}