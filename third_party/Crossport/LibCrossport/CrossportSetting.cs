using System;
using System.Collections.Generic;
using System.Linq;
using Ices.Crossport.Settings;
using UnityEngine;

namespace Assets.Scripts.LibCrossport.Settings
{
    [Serializable]
    public class CrossportSetting //:ISerializationCallbackReceiver
    {
        [SerializeField] public List<CrossportVideoSetting> video = new();
        [SerializeField] public List<CrossportAudioSetting> audio = new();
        [SerializeField] public CrossportSignalingSetting signaling = new();

        public CrossportVideoSetting Video(string key) => video.FirstOrDefault(v => v.key == key);
        public CrossportAudioSetting Audio(string key) => audio.FirstOrDefault(v => v.key == key);

        public CrossportSetting()
        {
            video.Add(new());
            audio.Add(new());
        }
    }
}