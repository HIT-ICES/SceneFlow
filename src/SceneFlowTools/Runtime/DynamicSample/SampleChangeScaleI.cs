using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScaleI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：玩家按XR右手手柄A键时触发脉冲式缩放
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalSelfScale;
        private Vector3 originalTargetScale;
        private bool isScaling;
        private bool isSelfGrabbed;
        private bool isTargetGrabbed;
        private XRGrabInteractable selfGrabInteractable;
        private XRGrabInteractable targetGrabInteractable;

        // [DeleteBeforeDetect] XR右手设备引用
        public InputDevice rightHand;

        void Awake()
        {
            // [DeleteBeforeDetect] 记录挂载对象初始缩放
            originalSelfScale = transform.localScale;
            if (targetTransform != null)
            {
                originalTargetScale = targetTransform.localScale;
            }

            // [DeleteBeforeDetect] self 监听抓取事件
            selfGrabInteractable = GetComponent<XRGrabInteractable>();
            if (selfGrabInteractable == null)
            {
                selfGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            selfGrabInteractable.selectEntered.AddListener(OnSelfGrabStarted);
            selfGrabInteractable.selectExited.AddListener(OnSelfGrabEnded);

            // [DeleteBeforeDetect] target 监听抓取事件
            if (targetTransform != null)
            {
                targetGrabInteractable = targetTransform.GetComponent<XRGrabInteractable>();
                if (targetGrabInteractable == null)
                {
                    targetGrabInteractable = targetTransform.gameObject.AddComponent<XRGrabInteractable>();
                }

                if (targetGrabInteractable != selfGrabInteractable)
                {
                    targetGrabInteractable.selectEntered.AddListener(OnTargetGrabStarted);
                    targetGrabInteractable.selectExited.AddListener(OnTargetGrabEnded);
                }
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 没有抓取前置时，不允许触发缩放
            if (!isSelfGrabbed && !isTargetGrabbed)
            {
                isScaling = false;
                return;
            }

            // [DeleteBeforeDetect] 检查XR右手A键是否按下（primaryButton）
            EnsureRightHandDevice();
            bool aPressed = false;
            if (rightHand.isValid)
            {
                rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out aPressed);
            }

            if (aPressed && !isScaling)
            {
                isScaling = true;
            }
            else if (!aPressed && isScaling)
            {
                isScaling = false;
                ResetScales();
            }

            // [DeleteBeforeDetect] 只有在玩家触发时才执行脉冲缩放逻辑
            if (isScaling)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                if (isSelfGrabbed)
                {
                    transform.localScale = originalSelfScale * factor;
                }

                // [DeleteBeforeDetect] 对引用对象：如果存在，做相位相反的缩放
                if (isTargetGrabbed && targetTransform != null)
                {
                    float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                    targetTransform.localScale = originalTargetScale * factor2;
                }
            }
        }

        private void EnsureRightHandDevice()
        {
            if (rightHand.isValid)
            {
                return;
            }

            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid)
            {
                rightHand = device;
            }
        }

        private void OnSelfGrabStarted(SelectEnterEventArgs args)
        {
            isSelfGrabbed = true;
        }

        private void OnSelfGrabEnded(SelectExitEventArgs args)
        {
            isSelfGrabbed = false;
            transform.localScale = originalSelfScale;
        }

        private void OnTargetGrabStarted(SelectEnterEventArgs args)
        {
            isTargetGrabbed = true;
        }

        private void OnTargetGrabEnded(SelectExitEventArgs args)
        {
            isTargetGrabbed = false;
            if (targetTransform != null)
            {
                targetTransform.localScale = originalTargetScale;
            }
        }

        private void ResetScales()
        {
            transform.localScale = originalSelfScale;
            if (targetTransform != null)
            {
                targetTransform.localScale = originalTargetScale;
            }
        }

        void OnDestroy()
        {
            if (selfGrabInteractable != null)
            {
                selfGrabInteractable.selectEntered.RemoveListener(OnSelfGrabStarted);
                selfGrabInteractable.selectExited.RemoveListener(OnSelfGrabEnded);
            }

            if (targetGrabInteractable != null && targetGrabInteractable != selfGrabInteractable)
            {
                targetGrabInteractable.selectEntered.RemoveListener(OnTargetGrabStarted);
                targetGrabInteractable.selectExited.RemoveListener(OnTargetGrabEnded);
            }
        }
    }
}