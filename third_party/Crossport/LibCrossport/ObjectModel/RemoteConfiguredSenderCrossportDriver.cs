#nullable enable
using Assets.Scripts.LibCrossport.Settings;
using Unity.RenderStreaming;
using UnityEngine;

namespace Ices.Crossport.ObjectModel
{
    public class RemoteConfiguredSenderCrossportDriver : CrossportDriverBase
    {
        protected override void Configure(CrossportSetting config)
        {
            StartCoroutine
            (
                CrossportClientUtils.FetchAndApplyConfig(config.signaling)
                                    .Then
                                     (
                                         () =>
                                         {
                                             config.Video(nameof(mainCamera))?.Configure(mainCamera, true);

                                             config.Audio(nameof(gameAudio))?.Configure(gameAudio);
                                         },
                                         StartCoroutine
                                     )
            );
        }
#pragma warning disable 0649
        [SerializeField] private AudioStreamSender gameAudio;
        [SerializeField] private VideoStreamSender mainCamera;
#pragma warning restore 0649
    }
}