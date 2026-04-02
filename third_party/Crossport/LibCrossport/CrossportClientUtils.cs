#nullable enable
using System;
using System.Collections;
using System.Threading;
using Assets.Scripts.LibCrossport.Settings;
using Ices.Crossport.Diagnostics;
using Ices.Crossport.Settings;
using Newtonsoft.Json;
using Unity.RenderStreaming;
using UnityEngine;
using UnityEngine.Networking;

namespace Ices.Crossport
{
    public class CrossportClientUtils
    {
        [Serializable]
        private class ArrayWrapper<T>
        {
            [JsonProperty("array")] public T[] array;

            public static string WrapJson(string json)
                => $"{{\"array\":{json}}}";

            public static ArrayWrapper<T> FromJson(string rawJson)
                => JsonUtility.FromJson<ArrayWrapper<T>>(WrapJson(rawJson));

            public static T[] ParseJsonArray(string rawJson)
                => FromJson(rawJson).array;
        }

        public static IEnumerator RefreshAppList
            (CrossportClientSetting signalingSetting, Action<CrossportClientInfo[]> callback)
        {
            var appUri = signalingSetting.GetFetchAppUrl(); //ConvertUrl(signalUrl);
            var www = UnityWebRequest.Get(appUri);
            ConsoleManager.LogWithDebug($"GET: {appUri}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ConsoleManager.LogWithDebug(www.error);
            }
            else
            {
                callback(ArrayWrapper<CrossportClientInfo>.ParseJsonArray(www.downloadHandler.text));
            }

            yield break;

            static string ConvertUrl(string url)
            {
                var uri = new Uri(url);

                string scheme = uri.Scheme switch
                                {
                                    "ws" => "http",
                                    "wss" => "https",
                                    _ => throw new ArgumentException("Unsupported scheme: " + uri.Scheme)
                                };

                var host = uri.Host;
                var port = uri.Port;
                var path = "/app";

                return string.Format("{0}://{1}:{2}{3}", scheme, host, port, path);
            }
        }

        public static string[] PresetServers =
        {
            "crossport.fers.top",
            "dev.fers.top",
            "10.123.3.1",
            "192.168.1.104:30980",
            "10.123.3.1:30980",
            "localhost",
            "localhost:8080",
            "192.168.139.47"
        };

        public static bool IsExp => ExpProfile == ClientProfile;
        public static void EnableExp() => ClientProfile = ExpProfile;
        public static string ExpProfile { private get; set; } = "exp";
        public static string? ClientProfile { get; private set; }

        public static void ResetStats()
        {
            FrameRateRecorder.ResetStats();
            EventLogger.Reset();
            MTPRecorder.ResetStats();
        }

        public static IEnumerator SubmitStats(CrossportClientSetting clientSetting)
        {
            if (!IsExp)
            {
                ConsoleManager.LogWithDebugWarning("ExpProfile Not Matched. No Stats are submitted.");
                yield break;
            }

            var expUri = clientSetting.GetStatsSubmissionUrl();
            var stats = new ClientExpStats
                        {
                            localFrameRate = FrameRateRecorder.ExportLocal(),
                            remoteFrameRate = FrameRateRecorder.ExportRemote(),
                            netLatency = EventLogger.Export(),
                            mtpLatency = AutomaticMTPRecorder.Export()
                        };
            var questBody = JsonUtility.ToJson(stats);
            var www = UnityWebRequest.Post(expUri, questBody, "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ConsoleManager.LogWithDebug(www.error);
                ConsoleManager.LogWithDebugWarning($"Stats submission failed. Stats was {questBody}.");
            }
            else
            {
                ConsoleManager.LogWithDebugWarning($"Stats submitted: {questBody}");
            }
        }

        public static IEnumerator FetchAndApplyConfig(CrossportClientSetting clientSetting, bool forceExp = false)
        {
            var appUri = clientSetting.GetConfigUrl();
            var www = UnityWebRequest.Get(appUri);
            ConsoleManager.LogWithDebug($"Fetching Application Config, GET: {appUri}");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                ConsoleManager.LogWithDebug(www.error);
                ConsoleManager.LogWithDebugWarning("Application Config fetch failed, using defaults.");
            }
            else
            {
                var appConfig = JsonUtility.FromJson<AppConfig>(www.downloadHandler.text);
                ConsoleManager.LogWithDebug
                (
                    $"Got Config. "
                );
                ConsoleManager.LogWithDebug
                (
                    $"Profile: {ClientProfile}; {appConfig.resolutionX}x{appConfig.resolutionY}@{appConfig.frameRate}; QL: {appConfig.qualityLevel}"
                );
                ClientProfile = appConfig.profile;
                if (forceExp) EnableExp();
                QualityManager.SetResolution(appConfig.resolutionX, appConfig.resolutionY);
                QualityManager.SetTargetFrameRate(appConfig.frameRate);
                QualityManager.SetQualityLevel(appConfig.qualityLevel);
                yield return new WaitForSeconds(1);

                ConsoleManager.LogWithDebug
                (
                    $"Application Config Applied."
                );
                ConsoleManager.LogWithDebug
                (
                    $"Profile: {ClientProfile}{(forceExp ? "(force)" : "")}; {Screen.width}x{Screen.height}@{Application.targetFrameRate}; QL: {QualitySettings.GetQualityLevel()}"
                );
                if (IsExp)
                {
                    ResetStats();
                    ConsoleManager.LogWithDebug
                    (
                        $"Stats Reset."
                    );
                }
            }
        }

        public static IEnumerator UpdateIceConfig
        (
            SignalingManager renderStreaming,
            CrossportSetting setting,
            Action<CrossportSignaling> configureSignalling
        )
        {
            var signalingSetting = setting.signaling;
            if (signalingSetting.FetchIceConfig)
            {
                var appUri = signalingSetting.GetFetchIceUrl();
                var www = UnityWebRequest.Get(appUri);
                ConsoleManager.LogWithDebug($"Fetching ICE Config, GET: {appUri}");
                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    ConsoleManager.LogWithDebug(www.error);
                    ConsoleManager.LogWithDebugWarning("ICE Config fetch failed, using defaults.");
                }
                else
                {
                    var iceConfig = JsonUtility.FromJson<CrossportIceConfig>(www.downloadHandler.text);
                    var newSetting = new WebSocketSignalingSettings
                    (
                        "ws://0.0.0.0",
                        new[]
                        {
                            new IceServer
                            (
                                iceConfig.urls,
                                iceConfig.username,
                                IceCredentialType.Password,
                                iceConfig.credential
                            )
                        }
                    );

                    renderStreaming.SetSignalingSettings(newSetting);
                    ConsoleManager.LogWithDebug($"ICE Configured: {JsonUtility.ToJson(iceConfig)}");
                }

                var signalling = setting.signaling.Create(SynchronizationContext.Current);
                configureSignalling(signalling);
                renderStreaming.Run(signalling);
            }
        }
    }
}