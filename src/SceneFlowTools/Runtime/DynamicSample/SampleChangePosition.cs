using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePosition : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: move the attached and referenced objects smoothly back and forth.
        public Transform targetTransform;

        // [DeleteBeforeDetect] Movement parameters.
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        void Update()
        {
            // [DeleteBeforeDetect] Move the attached object along a sine wave.
            if (transform != null)
            {
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            // [DeleteBeforeDetect] Move the referenced object in the opposite direction, if present.
            if (targetTransform != null)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }
    }
}
