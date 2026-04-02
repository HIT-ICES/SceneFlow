using System;
using System.Linq;
using Assets.Scripts.LibCrossport.Settings;
#nullable enable
using Ices.Crossport.Settings;
using Unity.RenderStreaming;
using UnityEngine;

namespace Ices.Crossport.ObjectModel
{
    public class CrossportDriverBase : MonoBehaviour
    {
        [SerializeField] private SignalingManager renderStreaming;
        [SerializeField] private string defaultConfigName;
        [SerializeField] private CrossportSetting defaultConfig;
        public CrossportSetting? setting = null;
        public bool autoRun;

        protected virtual void Configure(CrossportSetting config) { }
        protected virtual void ConfigureSignalling(CrossportSignaling signalling) { }

        public void RideOn()
        {
            if (setting is null) return;
            setting.signaling.TryUpgrade();
            Configure(setting);
            StartCoroutine(CrossportClientUtils.UpdateIceConfig(renderStreaming, setting, ConfigureSignalling));
        }

        public void RideOff() { renderStreaming.Stop(); }

        public virtual void Awake()
        {
            renderStreaming ??= GetComponent<SignalingManager>();

            if (renderStreaming.runOnAwake)
                renderStreaming.Stop();
            var args = Environment.GetCommandLineArgs().ToList();
            var cFlag = args.IndexOf($"-c:{defaultConfigName}");
            var phFlag = args.IndexOf("-ph") == -1;
            var configName = cFlag == -1 ? defaultConfigName : args[cFlag + 1];
            setting =
                phFlag
                    ? CrossportConfigurationManager.GetSetting(configName)
                    : CrossportConfigurationManager.GetPlaceHoldenSetting(configName);
            CrossportVideoSetting.LogAvailableCodecs();
            setting ??= defaultConfig;
            Debug.Log($"!!!!! setting: {setting}");
            if (autoRun) RideOn();
        }
    }
}