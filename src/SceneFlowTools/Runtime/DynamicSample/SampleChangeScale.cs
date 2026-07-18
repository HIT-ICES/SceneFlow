using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeScale : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态改变缩放（脉冲式缩放），对挂载对象与引用对象生效
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
            // [DeleteBeforeDetect] 对挂载对象：以正弦波做脉冲缩放
            float factor = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * factor;

            // [DeleteBeforeDetect] 对引用对象：如果存在，做相位相反的缩放
            if (targetTransform != null)
            {
                float factor2 = 1f + Mathf.Sin(Time.time * pulseSpeed + Mathf.PI) * pulseAmount;
                targetTransform.localScale = targetTransform.localScale.normalized * factor2;
            }
        }
    }
}