using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeShaderI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态改变缩放（脉冲式缩放），对挂载对象与引用对象生效
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;
        private bool isGrabbed = false; // [DeleteBeforeDetect] 玩家是否正在抓取

        void Start()
        {
            // [DeleteBeforeDetect] 记录挂载对象初始缩放
            originalScale = transform.localScale;

            // [DeleteBeforeDetect] 获取 XRGrabInteractable 并注册事件
            XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrabStarted);
                grabInteractable.selectExited.AddListener(OnGrabEnded);
            }
            else
            {
                Debug.LogWarning("[SampleChangeScaleI] 需要 XRGrabInteractable 才能检测抓取事件！");
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 只有在玩家抓取时才执行脉冲缩放
            if (isGrabbed)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

                transform.localScale = originalScale * factor;

                if (targetTransform != null)
                {
                    targetTransform.localScale = originalScale * factor;
                }
            }
        }

        // [DeleteBeforeDetect] 玩家开始抓取时调用
        private void OnGrabStarted(SelectEnterEventArgs args)
        {
            isGrabbed = true;
        }

        // [DeleteBeforeDetect] 玩家松开抓取时调用
        private void OnGrabEnded(SelectExitEventArgs args)
        {
            isGrabbed = false;
            transform.localScale = originalScale;
            if (targetTransform != null)
            {
                targetTransform.localScale = originalScale;
            }
        }
    }
}