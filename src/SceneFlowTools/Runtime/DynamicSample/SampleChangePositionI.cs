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

        // [DeleteBeforeDetect] 用于判断 self/target 是否由玩家触发
        private bool isSelfPlayerTouching;
        private bool isTargetPlayerTouching;
        private SampleChangePositionITargetTriggerRelay targetTriggerRelay;

        void Awake()
        {
            if (targetTransform != null && targetTransform != transform)
            {
                targetTriggerRelay = targetTransform.GetComponent<SampleChangePositionITargetTriggerRelay>();
                if (targetTriggerRelay == null)
                {
                    targetTriggerRelay = targetTransform.gameObject.AddComponent<SampleChangePositionITargetTriggerRelay>();
                }

                targetTriggerRelay.Initialize(this);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 只有在玩家正在触碰 self 时才执行 self 位置变化
            if (isSelfPlayerTouching)
            {
                // [DeleteBeforeDetect] 对挂载对象：基于正弦波移动
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            // [DeleteBeforeDetect] 只有在玩家正在触碰 target 时才执行 target 位置变化
            if (isTargetPlayerTouching && targetTransform != null)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }

        // [DeleteBeforeDetect] 当检测到玩家进入触碰范围时触发
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) // 确保是玩家
            {
                isSelfPlayerTouching = true;
            }
        }

        // [DeleteBeforeDetect] 当玩家离开触碰范围时停止移动
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isSelfPlayerTouching = false;
            }
        }

        public void NotifyTargetTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isTargetPlayerTouching = true;
            }
        }

        public void NotifyTargetTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isTargetPlayerTouching = false;
            }
        }

        void OnDestroy()
        {
            if (targetTriggerRelay != null)
            {
                targetTriggerRelay.ClearOwner(this);
            }
        }
    }

    class SampleChangePositionITargetTriggerRelay : MonoBehaviour
    {
        private SampleChangePositionI owner;

        public void Initialize(SampleChangePositionI ownerScript)
        {
            owner = ownerScript;
        }

        public void ClearOwner(SampleChangePositionI ownerScript)
        {
            if (owner == ownerScript)
            {
                owner = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null)
            {
                owner.NotifyTargetTriggerEnter(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (owner != null)
            {
                owner.NotifyTargetTriggerExit(other);
            }
        }
    }
}