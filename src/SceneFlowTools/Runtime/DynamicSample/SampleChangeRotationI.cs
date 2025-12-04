using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：在玩家指向时触发旋转，作用于挂载对象和引用对象
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        private bool isPointed = false; // 用来检测玩家是否正在指向

        void OnEnable()
        {
            // [DeleteBeforeDetect] 获取 XRRayInteractor，并订阅事件
            XRRayInteractor rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.AddListener(OnHoverEntered);
                rayInteractor.hoverExited.AddListener(OnHoverExited);
            }
        }

        void OnDisable()
        {
            // [DeleteBeforeDetect] 取消订阅事件
            XRRayInteractor rayInteractor = FindObjectOfType<XRRayInteractor>();
            if (rayInteractor != null)
            {
                rayInteractor.hoverEntered.RemoveListener(OnHoverEntered);
                rayInteractor.hoverExited.RemoveListener(OnHoverExited);
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // [DeleteBeforeDetect] 如果是玩家指向到这个对象，开始旋转
            if (args.interactableObject.transform == transform)
            {
                isPointed = true;
            }
        }

        private void OnHoverExited(HoverExitEventArgs args)
        {
            // [DeleteBeforeDetect] 当玩家停止指向该对象时，停止旋转
            if (args.interactableObject.transform == transform)
            {
                isPointed = false;
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 只有在玩家指向时才旋转
            if (isPointed)
            {
                transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);

                // [DeleteBeforeDetect] 如果存在引用对象，反向旋转
                if (targetTransform != null)
                {
                    targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
                }
            }
        }
    }
}