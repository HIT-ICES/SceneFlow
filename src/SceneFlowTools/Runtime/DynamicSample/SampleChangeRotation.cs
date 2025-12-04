using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotation : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：动态改变旋转（持续旋转），同时对挂载对象和引用对象生效
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        void Update()
        {
            // [DeleteBeforeDetect] 对挂载对象：持续旋转
            transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);

            // [DeleteBeforeDetect] 对引用对象：如果存在，持续反向旋转
            if (targetTransform != null)
            {
                targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}