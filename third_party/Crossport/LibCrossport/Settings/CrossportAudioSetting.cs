using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.RenderStreaming;
using UnityEngine;

namespace Ices.Crossport.Settings
{
    [Serializable]
    public class CrossportAudioSetting
    {
        public static CrossportAudioSetting Default()
            => new()
            {
                key="default",
                codec= AudioStreamReceiver.GetAvailableCodecs().FirstOrDefault(
                    cc=> cc.mimeType== "audio/opus"
                    && cc.sdpFmtpLine== "minptime=10;sprop-stereo=1;stereo=1;useinbandfec=1"),
                maxBitrate=2000,
                minBitrate=0

            };
        [SerializeField] [Tooltip("key of setting.")]
        public string key;

        [SerializeField] [Tooltip("Audio Codec.")]
        public AudioCodecInfo codec=null;

        [SerializeField] [Tooltip("Min Audio Bit rate.")]
        public uint minBitrate = 0;

        [SerializeField] [Tooltip("Max Audio Bit rate.")]
        public uint maxBitrate = 200;

        public void Configure(AudioStreamSender sender)
        {
            sender.SetBitrate(minBitrate, maxBitrate);
            if (codec != null) sender.SetCodec(codec);
        }

        public void Configure(AudioStreamReceiver receiver)
        {
            if (codec != null) receiver.SetCodec(codec);
        }
        public CrossportAudioSetting()
        {
            key = "(default)";
        }
    }
}
//[Serializable]
//public class CrossportAudioSettingCollection : KeyedCollection<string, CrossportAudioSetting>
//{
//    protected override string GetKeyForItem(CrossportAudioSetting item) => item.key;
//}