using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: compute rotation but retain the original value on assignment.
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        void Update()
        {
            // [DeleteBeforeDetect] Compute a rotation for the attached object but retain its original state.
            if (transform != null)
            {
                Quaternion currentRotation = transform.rotation;
                Quaternion calculatedRotation = currentRotation * Quaternion.Euler(angularSpeed * Time.deltaTime);
                transform.rotation = currentRotation; // The assigned value is the original rotation.
            }

            // [DeleteBeforeDetect] Compute reverse rotation for the referenced object but retain its original state.
            if (targetTransform != null)
            {
                Quaternion currentRotationTarget = targetTransform.rotation;
                Quaternion calculatedRotationTarget = currentRotationTarget * Quaternion.Euler(-angularSpeed * Time.deltaTime);
                targetTransform.rotation = currentRotationTarget; // Assign the original value here as well.
            }
        }
    }
}
