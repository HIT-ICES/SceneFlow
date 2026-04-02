using System;
using Ices.Crossport.Settings;
using Newtonsoft.Json;
using UnityEngine;

namespace Ices.Crossport
{
    [Serializable]
    public class CrossportClientInfo
    {
        [SerializeField] [Tooltip("Application name.")] public string application;

        [SerializeField]
        [Tooltip
            ("Character this application plays, 0 for consumer, n > 0 for provider. n is maximum connection allowed")]
        public int capacity;

        [SerializeField] [Tooltip("Component id.")] public string component;
    }
}