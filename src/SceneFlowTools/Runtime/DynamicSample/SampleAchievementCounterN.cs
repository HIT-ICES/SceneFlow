using System.Collections.Generic;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleAchievementCounterN: MonoBehaviour
    {
        public bool achievementGained;
        public string updateMethod;
        private int counter;

        void Start()
        {
            counter = 0;
        }

        public void Increment()
        {
            counter++;
            if (counter >= 10)
            {
                achievementGained = true;
                counter = 0;
            }
        }

        public int GetCount()
        {
            return counter;
        }
    }
}