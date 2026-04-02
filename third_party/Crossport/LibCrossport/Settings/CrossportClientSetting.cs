// /*
//  * Author: Ferdinand Sukhoi
//  * Email: ${User.Email}
//  * Date: 2 16, 2024
//  *
//  */

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Ices.Crossport.Settings
{
    [Serializable]
    public class CrossportClientSetting : CrossportClientInfo
    {
        [SerializeField]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        [Tooltip("Crossport options. Available: ssl, debug, compatible, no-fetch, private")]
        public List<string>? options;

        [SerializeField]
        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        [Tooltip("Crossport server port")]
        public int? port;

        [SerializeField] [Tooltip("Crossport registry address.")] public string address = CrossportClientUtils.PresetServers.First();

        public bool IsNewFormat => options != null;
        public bool UseSsl => address.Contains("fers.top") || (options?.Contains("ssl") ?? false);
        public bool UseCompatible => options?.Contains("compatible") ?? false;
        public bool UseDebug => options?.Contains("debug") ?? false;

        public Uri GetHttpUrl(string path)
        {
            var builder = new UriBuilder
                          {
                              Scheme = UseSsl ? "https" : "http",
                              Host = address,
                              Port = port ?? (UseSsl ? 443 : 80),
                              Path = path
                          };
            return builder.Uri;
        }

        public Uri GetFetchAppUrl()
            => GetHttpUrl("/app");

        public Uri GetConfigUrl()
            => GetHttpUrl
                ($"/app/{UnityWebRequest.EscapeURL(application)}/{UnityWebRequest.EscapeURL(component)}/config");

        public Uri GetStatsSubmissionUrl()
            => GetHttpUrl($"/exp/{UnityWebRequest.EscapeURL(application)}/stats");

        public Uri GetSignallingUrl(string clientId)
        {
            var builder = new UriBuilder
                          {
                              Scheme = UseSsl ? "wss" : "ws",
                              Host = address,
                              Port = port ?? (UseSsl ? 443 : 80),
                              Path =
                                  $"/sig/{UnityWebRequest.EscapeURL(application)}/{UnityWebRequest.EscapeURL(component)}",
                              Query = string.Join
                              (
                                  "&",
                                  $"id={clientId}",
                                  $"operation={(UseDebug ? "debug" : "standard")}",
                                  $"capacity={capacity}",
                                  $"compatible={UseCompatible.ToString().ToLower()}"
                              )
                          };
            return builder.Uri;
        }
    }
}