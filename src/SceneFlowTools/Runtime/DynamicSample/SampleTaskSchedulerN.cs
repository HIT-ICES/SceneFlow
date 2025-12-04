using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleTaskSchedulerN : MonoBehaviour
    {
        public float interval;
        private Queue<Action> tasks = new Queue<Action>();
        private float elapsed;

        void Start()
        {
            elapsed = 0f;
        }
        
        void AddTask(Action task)
        {
            tasks.Enqueue(task);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= interval)
            {
                if (tasks.Count > 0)
                {
                    Action task = tasks.Dequeue();
                    task.Invoke();
                }
                elapsed = 0f;
            }
        }
    }
}