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

        private enum PointedTarget
        {
            None,
            Self,
            Target
        }

        // [DeleteBeforeDetect] 用于标记玩家当前指向对象
        private PointedTarget pointedTarget;

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
            if (pointedTarget != PointedTarget.None && Input.GetButtonDown("Fire1"))
            {
                if (pointedTarget == PointedTarget.Self)
                {
                    ToggleSelfVisibility();
                    ToggleSelfActiveState();
                }
                else if (pointedTarget == PointedTarget.Target)
                {
                    ToggleTargetVisibility();
                    ToggleTargetActiveState();
                }
            }
        }

        // [DeleteBeforeDetect] 检查玩家射线是否指向本物体
        private void CheckPlayerPointing()
        {
            pointedTarget = PointedTarget.None;
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    pointedTarget = PointedTarget.Self;
                    return;
                }

                if (targetRenderer != null)
                {
                    Transform targetRendererTransform = targetRenderer.transform;
                    if (hitTransform == targetRendererTransform || hitTransform.IsChildOf(targetRendererTransform))
                    {
                        pointedTarget = PointedTarget.Target;
                        return;
                    }
                }

                if (targetObject != null)
                {
                    Transform targetObjectTransform = targetObject.transform;
                    if (hitTransform == targetObjectTransform || hitTransform.IsChildOf(targetObjectTransform))
                    {
                        pointedTarget = PointedTarget.Target;
                    }
                }
            }
        }

        // [DeleteBeforeDetect] 切换 self Renderer 可见性
        private void ToggleSelfVisibility()
        {
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }
        }

        // [DeleteBeforeDetect] 切换 target Renderer 可见性
        private void ToggleTargetVisibility()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }
        }

        // [DeleteBeforeDetect] 切换 self GameObject 激活状态
        private void ToggleSelfActiveState()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        // [DeleteBeforeDetect] 切换 target GameObject 激活状态
        private void ToggleTargetActiveState()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}