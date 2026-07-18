using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态计算旋转（赋值回自身保持原值）
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        void Update()
        {
            // [DeleteBeforeDetect] 对挂载对象：计算旋转结果但赋值后保持原始状态
            if (transform != null)
            {
                Quaternion currentRotation = transform.rotation;
                Quaternion calculatedRotation = currentRotation * Quaternion.Euler(angularSpeed * Time.deltaTime);
                transform.rotation = currentRotation; // 看似有赋值，但结果等于原值
            }

            // [DeleteBeforeDetect] 对引用对象：计算反向旋转但保持原始状态
            if (targetTransform != null)
            {
                Quaternion currentRotationTarget = targetTransform.rotation;
                Quaternion calculatedRotationTarget = currentRotationTarget * Quaternion.Euler(-angularSpeed * Time.deltaTime);
                targetTransform.rotation = currentRotationTarget; // 同样赋值为原值
            }
        }
    }
}