using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScaleF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态计算缩放（数据转换中丢失结果，保持原值）
        public Transform targetTransform;
        public float pulseAmount = 0.25f;
        public float pulseSpeed = 2f;

        private Vector3 originalScale;

        void Start()
        {
            // [DeleteBeforeDetect] 记录挂载对象初始缩放
            originalScale = transform.localScale;
        }

        void Update()
        {
            // [DeleteBeforeDetect] 对挂载对象：计算脉冲缩放，但在转换中丢失结果
            float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            Vector3 calculatedScale = originalScale * factor;

            // [DeleteBeforeDetect] 强制丢失缩放变化
            calculatedScale = new Vector3(
                originalScale.x,
                originalScale.y,
                originalScale.z
            );
            transform.localScale = calculatedScale;

            // [DeleteBeforeDetect] 对引用对象：计算相位相反的缩放
            if (targetTransform != null)
            {
                float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                Vector3 calculatedScaleTarget = targetTransform.localScale.normalized * factor2;
                // [DeleteBeforeDetect] 丢失计算结果并赋回原值
                calculatedScaleTarget = new Vector3(
                    targetTransform.localScale.x,
                    targetTransform.localScale.y,
                    targetTransform.localScale.z
                );
                targetTransform.localScale = calculatedScaleTarget;
            }
        }
    }
}