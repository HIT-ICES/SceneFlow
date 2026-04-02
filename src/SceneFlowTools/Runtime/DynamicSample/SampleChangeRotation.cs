using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeRotation : MonoBehaviour
    {
        
        public Transform targetTransform;
        public Vector3 angularSpeed = new Vector3(0f, 90f, 0f);

        void Update()
        {
            
            transform.Rotate(angularSpeed * Time.deltaTime, Space.Self);

            
            if (targetTransform != null)
            {
                targetTransform.Rotate(-angularSpeed * Time.deltaTime, Space.Self);
            }
        }
    }
}