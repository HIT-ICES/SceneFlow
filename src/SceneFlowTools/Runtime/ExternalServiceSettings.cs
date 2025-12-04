using System;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    [ExecuteAlways]
    public class ExternalServiceSettings: MonoBehaviour
    {
        public PredefinedServiceHost usePredefinedServiceHost = PredefinedServiceHost.Localhost;
        public string serviceHost = "localhost:8000";

        void Awake()
        {
            if (ExternalUtils.Host == serviceHost) return;
            ExternalUtils.Host = serviceHost;
            Debug.Log("ExternalUtils.Host set to: " + ExternalUtils.Host);
        }

        void OnValidate()
        {
            switch (usePredefinedServiceHost)
            {
                case PredefinedServiceHost.None:
                    break;
                case PredefinedServiceHost.Localhost:
                    serviceHost = "localhost:8000";
                    break;
                case PredefinedServiceHost.ExperimentalServer:
                    serviceHost = "localhost:8000";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (ExternalUtils.Host == serviceHost) return;
            ExternalUtils.Host = serviceHost;
            Debug.Log("ExternalUtils.Host set to: " + ExternalUtils.Host);
        }
    }

    public enum PredefinedServiceHost
    {
        None,
        Localhost,
        ExperimentalServer,
    }
}