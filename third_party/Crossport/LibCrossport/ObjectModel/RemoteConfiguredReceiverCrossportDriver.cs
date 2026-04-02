// /*
//  * Author: Ferdinand Sukhoi
//  * Email: ${User.Email}
//  * Date: 3 4, 2024
//  *
//  */

#nullable enable

using System;
using System.Threading;
using Assets.Scripts.LibCrossport.Settings;
using Ices.Crossport;
using Ices.Crossport.ObjectModel;
using Unity.RenderStreaming;
using UnityEngine;

namespace Ices.Crossport.ObjectModel
{
    public class RemoteConfiguredReceiverCrossportDriver : CrossportDriverBase
    {
        [SerializeField] private AudioStreamReceiver gameAudio;
        [SerializeField] private VideoStreamReceiver mainCamera;
        [SerializeField] private AudioSource targetAudio;
        [SerializeField] private SingleConnection connection;
        public event Action<RemoteConfiguredReceiverCrossportDriver>? OnStart;
        public event Action<RemoteConfiguredReceiverCrossportDriver>? OnStop;

        private Material material;

        protected override void ConfigureSignalling(CrossportSignaling signaling)
        {
            signaling.OnStart += signalingOnStart;
            signaling.OnDestroyConnection += signalingOnDestroyConnection;

            return;


            void signalingOnDestroyConnection(Unity.RenderStreaming.Signaling.ISignaling signaling, string connectionId)
            {
                OnStop?.Invoke(this);
                Thread.Sleep(1000);
                connection.CreateConnection(Guid.NewGuid().ToString("N"));
            }

            void signalingOnStart(Unity.RenderStreaming.Signaling.ISignaling signaling)
            {
                Debug.Log("signalingOnStart called.");
                connection.CreateConnection(Guid.NewGuid().ToString("N"));
                gameAudio.targetAudioSource = targetAudio;
                OnStart?.Invoke(this);
            }
        }

        protected override void Configure(CrossportSetting config)
        {
            config.Video
                   (nameof(mainCamera))
                  .Configure(mainCamera);
            config.Audio(nameof(gameAudio)).Configure(gameAudio);
            mainCamera.OnUpdateReceiveTexture = storeIntoTempTexture;
            gameAudio.OnUpdateReceiveAudioSource += source =>
            {
                source.loop = true;
                source.Play();
            };
            return;

            static void storeIntoTempTexture(Texture texture)
            {
                if (texture != null)
                {
                    Debug.Log("Skybox Received.");
                }
                if (RenderSettings.skybox.shader.name!="Skybox/Panoramic")
                {
                    RenderSettings.skybox = new Material(Shader.Find("Skybox/Panoramic"));
                    Debug.Log("Skybox Material Changed.");
                }

                RenderSettings.skybox.mainTexture = texture;
            }
        }
    }
}