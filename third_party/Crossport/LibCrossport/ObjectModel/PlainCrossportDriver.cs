using Assets.Scripts.LibCrossport.Settings;
using Unity.RenderStreaming;
using UnityEngine;

namespace Ices.Crossport.ObjectModel
{
    public class PlainCrossportDriver : CrossportDriverBase
    {
        protected override void Configure(CrossportSetting config)
        {
            config.Video(nameof(mainCamera))?.Configure(mainCamera);
            config.Audio(nameof(gameAudio))?.Configure(gameAudio);
        }
#pragma warning disable 0649
        [SerializeField] private AudioStreamSender gameAudio;
        [SerializeField] private VideoStreamSender mainCamera;
#pragma warning restore 0649
    }
}