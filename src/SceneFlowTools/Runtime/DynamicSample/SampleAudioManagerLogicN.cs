using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleAudioManagerLogicN : MonoBehaviour
    {
        public AudioSource audioSource;
        public float interval;
        private float timer;
        private bool toggle;

        void Start()
        {
            timer = 0f;
            toggle = false;
        }

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                if (toggle)
                    audioSource.Play();
                else
                    audioSource.Stop();
                toggle = !toggle;
                timer = 0f;
            }
        }
    }
}