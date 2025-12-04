using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleAIDecisionManagerN : MonoBehaviour
    {
        public AiUtils ai;
        public string decisionFunction;
        public float decisionInterval;
        private float lastDecisionTime;

        void Start()
        {
            lastDecisionTime = 0f;
        }

        void Update()
        {
            if (Time.time - lastDecisionTime >= decisionInterval)
            {
                ai.Disition();
                lastDecisionTime = Time.time;
            }
        }
    }

    public class AiUtils
    {
        public void Disition()
        {
            
        }
    }
}