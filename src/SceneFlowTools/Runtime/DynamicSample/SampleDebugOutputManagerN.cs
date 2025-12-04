using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleDebugOutputManagerN: MonoBehaviour
    {
        public Transform targetTransform;
        public float logInterval;
        private float timer;

        void Start()
        {
            timer = 0f;
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= logInterval)
            {
                Debug.Log("Target position:" + targetTransform.position.ToString());
                timer = 0f;
            }
        }
    }
}