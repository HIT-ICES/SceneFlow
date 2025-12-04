using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotationF : MonoBehaviour
    {

        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        void Update()
        {

            if (transform != null)
            {
                Quaternion currentRotation = transform.rotation;
                Quaternion calculatedRotation = currentRotation * Quaternion.Euler(angularSpeed * Time.deltaTime);
                transform.rotation = currentRotation;
            }


            if (targetTransform != null)
            {
                Quaternion currentRotationTarget = targetTransform.rotation;
                Quaternion calculatedRotationTarget = currentRotationTarget * Quaternion.Euler(-angularSpeed * Time.deltaTime);
                targetTransform.rotation = currentRotationTarget;
            }
        }
    }
}