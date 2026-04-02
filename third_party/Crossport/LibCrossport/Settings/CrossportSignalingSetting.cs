#nullable enable
using System;
using System.Threading;
using Unity.RenderStreaming;
using UnityEngine;

namespace Ices.Crossport.Settings
{
    [Serializable]
    public class CrossportSignalingSetting : CrossportClientSetting
    {
        public bool FetchIceConfig => !(options?.Contains("no-fetch") ?? false);
        public bool IsPrivateNode => options?.Contains("private") ?? false;

        [SerializeField] [Tooltip("Time interval for polling from signaling server.")] public float interval = 5.0f;

        public CrossportClientInfo AsInfo()
        {
            return new()
                   {
                       application = application,
                       capacity = capacity,
                       component = component
                   };
        }

        public void TryUpgrade()
        {
            if (IsNewFormat) return;
            var oldUrl = new Uri(address);
            options = new();
            if (oldUrl.Scheme == "wss") options.Add("ssl");
            address = oldUrl.Host;
            port = oldUrl.Port;
        }

        public Uri GetFetchIceUrl()
            => GetHttpUrl($"/ice/{(IsPrivateNode ? "private" : "public")}");

        public CrossportSignaling Create(SynchronizationContext mainThreadContext) => new(this, mainThreadContext);
    }
}