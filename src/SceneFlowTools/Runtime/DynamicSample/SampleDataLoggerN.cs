using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleDataLoggerN : MonoBehaviour
    {
        public Rigidbody targetRigidbody;
        public string logTag;
        private float timer;
        private int frameCount;

        void Start()
        {
            timer = 0f;
            frameCount = 0;
        }

        void Update()
        {
            timer += Time.deltaTime;
            frameCount++;
            if (timer >= 1f)
            {
                float velocity = targetRigidbody.velocity.magnitude;
                LogData(logTag, velocity, frameCount);
                timer = 0f;
                frameCount = 0;
            }
        }

        void LogData(string tag, float value, int frames)
        {
            Debug.Log(tag + " velocity:" + value + " frames:" + frames);
        }
    }
}