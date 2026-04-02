using System;
using System.IO;
using System.Linq;
using Ices.Crossport.Settings;
using Unity.RenderStreaming;
using UnityEngine;

//using UnityEditor.VersionControl;
namespace Ices.Crossport
{

    [Serializable]
    public class CrossportReceiverSetting //:ISerializationCallbackReceiver
    {
        [SerializeField] [Tooltip("Crossport registry address.")]
        public string address = "ws://localhost";

        [SerializeField] [Tooltip("Application name.")]
        public string application = "";

        [SerializeField] [Tooltip("Receiver Audio Codec.")]
        public AudioCodecInfo audioCodec;

        [SerializeField] [Tooltip("Component id.")]
        public string component = "";

        [SerializeField] [Tooltip("Time interval for polling from signaling server.")]
        public float interval = 5.0f;

        [SerializeField] [Tooltip("Receiver Video Codec.")]
        public VideoCodecInfo videoCodec;


        public static CrossportReceiverSetting FromFile()
        {
#if !UNITY_EDITOR
        var configFile = Environment.GetCommandLineArgs().FirstOrDefault(c => c.EndsWith(".cpcfg.json"));
        if (configFile is null) return null;
        try
        {
            JsonUtility.FromJson<CrossportReceiverSetting>(File.ReadAllText(configFile));
        }
        catch (Exception)
        {
            return null;
        }
#else
            var configFile = "editor_cache.cpcfg.json";
#endif
            try
            {
                return JsonUtility.FromJson<CrossportReceiverSetting>(File.ReadAllText(configFile));
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        public void ToFile()
        {
#if !UNITY_EDITOR
        var configFile = Environment.GetCommandLineArgs().FirstOrDefault(c => c.EndsWith(".cpcfg.json"));
        if (configFile is null) return;
        try
        {
            JsonUtility.FromJson<CrossportReceiverSetting>(File.ReadAllText(configFile));
        }
        catch (Exception)
        {
            return;
        }
#else
            var configFile = "editor_cache.cpcfg.json";
#endif
            try
            {
                File.WriteAllText(configFile, JsonUtility.ToJson(this));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public CrossportSignalingSetting Export()
        {
            return new()
            {
                application = application,
                component = component,
                capacity = 0,
                address = address,
                interval = interval
            };
        }
        //public void OnBeforeSerialize()
        //{

        //}

        //public void OnAfterDeserialize()
        //{
        //    videoCodec = VideoStreamReceiver.GetAvailableCodecs().First();
        //    audioCodec = AudioStreamReceiver.GetAvailableCodecs().First();
        //}
    }
}