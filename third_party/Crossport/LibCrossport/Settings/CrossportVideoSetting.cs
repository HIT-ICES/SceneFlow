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
    public class CrossportVideoSetting
    {
        public static CrossportVideoSetting Default()
            => new()
               {
                   key = "default",
                   codec = VideoStreamReceiver.GetAvailableCodecs()
                                              .FirstOrDefault
                                               (
                                                   cc => cc.mimeType == "video/H264"
                                               ),
                   maxBitrate = 10000,
                   minBitrate = 0,
                   frameRate = 30.0f,
                   resolutionX = 2560,
                   resolutionY = 1440
               };

        [SerializeField] [Tooltip("key of setting.")] public string key;

        [SerializeField] [Tooltip("Video Codec.")] public VideoCodecInfo codec = null;

        [SerializeField] [Tooltip("Min Video Bit rate.")] public uint minBitrate = 0;

        [SerializeField] [Tooltip("Max Video Bit rate.")] public uint maxBitrate = 100000;


        [SerializeField] [Tooltip("Max Video frame rate (fps).")] public float frameRate = 30;

        [SerializeField] [Tooltip("Video resolution.")] public int resolutionX = 2560;

        [SerializeField] [Tooltip("Video resolution.")] public int resolutionY = 1440;

        public static void LogAvailableCodecs()
        {
            string codecToExpression(VideoCodecInfo codec)
            {
                switch (codec)
                {
                case H264CodecInfo h264Codec:
                    return
                        $"{h264Codec.mimeType} {h264Codec.profile} {h264Codec.level.ToString().Insert(1, ".")} {h264Codec.codecImplementation}: {h264Codec.name} [{codec.sdpFmtpLine}]";
                case VP9CodecInfo V9Codec:
                    return
                        $"{V9Codec.mimeType} {V9Codec.profile} {V9Codec.codecImplementation}: {V9Codec.name} [{codec.sdpFmtpLine}]";
                case AV1CodecInfo av1Codec:
                    return
                        $"{av1Codec.mimeType} {av1Codec.profile} {av1Codec.codecImplementation}: {av1Codec.name} [{codec.sdpFmtpLine}]";
                default:
                    return $"{codec.mimeType} {codec.codecImplementation}: {codec.name} [{codec.sdpFmtpLine}]";
                }
            }

            var receiverCodecs =
                string.Join
                (
                    "\n",
                    VideoStreamReceiver.GetAvailableCodecs().Select(codecToExpression)
                );
            var senderCodecs =
                string.Join
                (
                    "\n",
                    VideoStreamSender.GetAvailableCodecs().Select(codecToExpression)
                );
            ConsoleManager.LogWithDebug
                ($"Video System:\nReceiver Codecs: \n{receiverCodecs}; \nSender Codecs: \n {senderCodecs}");
        }

        public void Configure(VideoStreamSender sender, bool @override = false)
        {
            if (@override)
            {
                sender.SetTextureSize(new(Screen.width, Screen.height));
                sender.SetFrameRate(Application.targetFrameRate);
                sender.SetBitrate(0u, 100000u);
            }
            else
            {
                sender.SetBitrate(minBitrate, maxBitrate);
                sender.SetTextureSize(new(resolutionX, resolutionY));
                sender.SetFrameRate(frameRate);
            }

            if (codec != null) sender.SetCodec(codec);
        }

        public void Configure(VideoStreamReceiver receiver)
        {
            if (codec != null) receiver.SetCodec(codec);
        }

        public CrossportVideoSetting()
        {
            key = "(default)";
            codec = VideoStreamReceiver.GetAvailableCodecs()
                                       .FirstOrDefault
                                        (
                                            cc => cc.mimeType == "video/H264"
                                        );
        }
    }

    //[Serializable]
    //public class CrossportVideoSettingCollection : KeyedCollection<string, CrossportVideoSetting>
    //{
    //    protected override string GetKeyForItem(CrossportVideoSetting item) => item.key;
    //}
}