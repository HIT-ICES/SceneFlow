using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePositionF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: compute position changes without applying them when the guard is false.
        public Transform targetTransform;

        // [DeleteBeforeDetect] Movement parameters.
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        // [DeleteBeforeDetect] Guard that controls whether changes are applied.
        // [DeleteBeforeDetect] This guard is always false.
        private bool allowMove = false;

        void Update()
        {
            // [DeleteBeforeDetect] Compute smooth back-and-forth movement but do not apply it while the guard is false.
            if (transform != null && allowMove)
            {
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            // [DeleteBeforeDetect] Do not move the referenced object while the guard is false.
            if (targetTransform != null && allowMove)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }
    }
}
