using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePosition : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态改变位置（平滑往返移动），同时对挂载对象和引用对象生效
        public Transform targetTransform;

        // [DeleteBeforeDetect] 移动参数
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        void Update()
        {
            // [DeleteBeforeDetect] 对挂载对象：基于正弦波移动
            if (transform != null)
            {
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            // [DeleteBeforeDetect] 对引用对象：如果存在，同步相反方向移动
            if (targetTransform != null)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }
    }
}