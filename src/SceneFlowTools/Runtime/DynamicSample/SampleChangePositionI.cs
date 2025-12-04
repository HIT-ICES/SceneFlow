using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePositionI : MonoBehaviour
    {

        public Transform targetTransform;


        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;


        private bool isPlayerTouching = false;

        void Update()
        {

            if (isPlayerTouching)
            {

                if (transform != null)
                {
                    transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
                }


                if (targetTransform != null)
                {
                    targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
                }
            }
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerTouching = true;
            }
        }


        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isPlayerTouching = false;
            }
        }
    }
}