using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePositionI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态改变位置（平滑往返移动），仅在玩家触碰时触发
        public Transform targetTransform;

        // [DeleteBeforeDetect] 移动参数
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        // [DeleteBeforeDetect] 用于判断是否由玩家触发
        private bool isPlayerTouching = false;

        void Update()
        {
            // [DeleteBeforeDetect] 只有在玩家正在触碰时才执行位置变化
            if (isPlayerTouching)
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

        // [DeleteBeforeDetect] 当检测到玩家进入触碰范围时触发
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) // 确保是玩家
            {
                isPlayerTouching = true;
            }
        }

        // [DeleteBeforeDetect] 当玩家离开触碰范围时停止移动
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerTouching = false;
            }
        }
    }
}