using UnityEngine;
using UnityEngine.XR;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScaleI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：玩家按XR右手手柄A键时触发脉冲式缩放
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;
        private bool isScaling = false; // [DeleteBeforeDetect] 缩放状态

        // [DeleteBeforeDetect] XR右手设备引用
        public InputDevice rightHand;

        void Start()
        {
            // [DeleteBeforeDetect] 记录挂载对象初始缩放
            originalScale = transform.localScale;
        }

        void Update()
        {
            // [DeleteBeforeDetect] 检查XR右手A键是否按下（primaryButton）
            if (rightHand != null)
            {
                if (rightHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed))
                {
                    if (aPressed && !isScaling)
                    {
                        isScaling = true; // [DeleteBeforeDetect] 按下开始缩放
                    }
                    else if (!aPressed && isScaling)
                    {
                        isScaling = false; // [DeleteBeforeDetect] 松开恢复
                        transform.localScale = originalScale;
                        if (targetTransform != null)
                            targetTransform.localScale = originalScale;
                    }
                }
            }

            // [DeleteBeforeDetect] 只有在玩家触发时才执行脉冲缩放逻辑
            if (isScaling)
            {
                float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                transform.localScale = originalScale * factor;

                // [DeleteBeforeDetect] 对引用对象：如果存在，做相位相反的缩放
                if (targetTransform != null)
                {
                    float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                    targetTransform.localScale = originalScale * factor2;
                }
            }
        }
    }
}