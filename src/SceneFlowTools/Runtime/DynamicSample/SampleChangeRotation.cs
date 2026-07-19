using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotation : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: continuously rotate the attached and referenced objects.
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        void Update()
        {
            // [DeleteBeforeDetect] Continuously rotate the attached object.
            transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);

            // [DeleteBeforeDetect] Continuously rotate the referenced object in reverse, if present.
            if (targetTransform != null)
            {
                targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}
