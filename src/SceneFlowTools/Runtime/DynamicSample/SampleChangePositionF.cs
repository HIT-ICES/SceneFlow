using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePositionF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态计算位置变化（受条件控制而不执行）
        public Transform targetTransform;

        // [DeleteBeforeDetect] 移动参数
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        // [DeleteBeforeDetect] 控制修改执行的条件
        // [DeleteBeforeDetect] 永远为 false
        private bool allowMove = false;

        void Update()
        {
            // [DeleteBeforeDetect] 对挂载对象：计算平滑往返位置，但条件不满足时不应用
            if (transform != null && allowMove)
            {
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            // [DeleteBeforeDetect] 对引用对象：条件不满足，不应用位移
            if (targetTransform != null && allowMove)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }
    }
}