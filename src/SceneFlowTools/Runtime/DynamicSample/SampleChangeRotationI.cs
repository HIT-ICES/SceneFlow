using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：在玩家指向时触发旋转，作用于挂载对象和引用对象
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        private bool isSelfPointed;
        private bool isTargetPointed;
        private XRRayInteractor rayInteractor;

        void OnEnable()
        {
            // [DeleteBeforeDetect] 获取 XRRayInteractor，并订阅事件
            rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.AddListener(OnHoverEntered);
                rayInteractor.hoverExited.AddListener(OnHoverExited);
            }
        }

        void OnDisable()
        {
            // [DeleteBeforeDetect] 取消订阅事件
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                rayInteractor.hoverExited.RemoveListener(OnHoverExited);
            }

            isSelfPointed = false;
            isTargetPointed = false;
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            Transform hoveredTransform = args.interactableObject.transform;

            // [DeleteBeforeDetect] 如果是玩家指向到 self 对象，开始旋转
            if (hoveredTransform == transform)
            {
                isSelfPointed = true;
            }

            // [DeleteBeforeDetect] 如果是玩家指向到 target 对象，开始旋转
            if (targetTransform != null && hoveredTransform == targetTransform)
            {
                isTargetPointed = true;
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            Transform hoveredTransform = args.interactableObject.transform;

            // [DeleteBeforeDetect] 当玩家停止指向 self 对象时，停止旋转
            if (hoveredTransform == transform)
            {
                isSelfPointed = false;
            }

            // [DeleteBeforeDetect] 当玩家停止指向 target 对象时，停止旋转
            if (targetTransform != null && hoveredTransform == targetTransform)
            {
                isTargetPointed = false;
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] self 被指向时旋转
            if (isSelfPointed)
            {
                transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);
            }

            // [DeleteBeforeDetect] target 被指向时旋转
            if (isTargetPointed && targetTransform != null)
            {
                targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}