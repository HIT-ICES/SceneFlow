// /*
//  * Author: Ferdinand Sukhoi
//  * Email: ${User.Email}
//  * Date: 3 4, 2024
//  *
//  */

#nullable enable

using System;
using System.Collections;
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
        private const string VideoSkyboxShaderName = "Skybox/Panoramic";

        private enum VideoSkyboxMode
        {
            DirectSkyboxTexture = 1,
            DoubleBufferedSkybox = 2
        }

        [SerializeField] private AudioStreamReceiver gameAudio;
        [SerializeField] private VideoStreamReceiver mainCamera;
        [SerializeField] private AudioSource targetAudio;
        [SerializeField] private SingleConnection connection;
        [SerializeField] private VideoSkyboxMode videoSkyboxMode = VideoSkyboxMode.DoubleBufferedSkybox;
        public event Action<RemoteConfiguredReceiverCrossportDriver>? OnStart;
        public event Action<RemoteConfiguredReceiverCrossportDriver>? OnStop;

        private Material? material;
        private Texture? receivedSkyboxTexture;
        private RenderTexture? skyboxReadBuffer;
        private RenderTexture? skyboxWriteBuffer;
        private Coroutine? skyboxBufferCoroutine;
        private bool hasBufferedSkyboxFrame;
        private int skyboxBufferWidth;
        private int skyboxBufferHeight;

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
            mainCamera.OnUpdateReceiveTexture = StoreIntoTempTexture;
            gameAudio.OnUpdateReceiveAudioSource += source =>
            {
                source.loop = true;
                source.Play();
            };
        }

        private void StoreIntoTempTexture(Texture texture)
        {
            receivedSkyboxTexture = texture;

            if (texture != null)
            {
                Debug.Log($"Skybox Received: {texture.width}x{texture.height}.");
            }

            var hasSkyboxMaterial = EnsureSkyboxMaterial();

            if (videoSkyboxMode == VideoSkyboxMode.DirectSkyboxTexture && hasSkyboxMaterial)
            {
                ApplyDirectSkyboxTexture(texture);
                return;
            }

            EnsureSkyboxBufferCoroutine();
        }

        private void Update()
        {
            if (videoSkyboxMode == VideoSkyboxMode.DirectSkyboxTexture)
            {
                StopSkyboxBufferCoroutine();

                if (skyboxReadBuffer != null || skyboxWriteBuffer != null)
                {
                    ReleaseSkyboxBuffers();
                }

                if (receivedSkyboxTexture != null &&
                    (RenderSettings.skybox == null ||
                     RenderSettings.skybox.mainTexture != receivedSkyboxTexture) &&
                    EnsureSkyboxMaterial())
                {
                    ApplyDirectSkyboxTexture(receivedSkyboxTexture);
                }

                return;
            }

            if (receivedSkyboxTexture == null) return;
            if (!EnsureSkyboxMaterial()) return;
            EnsureSkyboxBufferCoroutine();

            if (hasBufferedSkyboxFrame && RenderSettings.skybox.mainTexture != skyboxReadBuffer)
            {
                RenderSettings.skybox.mainTexture = skyboxReadBuffer;
            }
        }

        private IEnumerator CopySkyboxTextureAtEndOfFrame()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            while (true)
            {
                yield return waitForEndOfFrame;

                if (videoSkyboxMode != VideoSkyboxMode.DoubleBufferedSkybox) continue;

                var source = receivedSkyboxTexture;
                if (source == null) continue;
                if (!EnsureSkyboxMaterial()) continue;
                if (!EnsureSkyboxBuffers(source)) continue;
                if (skyboxWriteBuffer == null) continue;

                Graphics.Blit(source, skyboxWriteBuffer);
                SwapSkyboxBuffers();
                hasBufferedSkyboxFrame = true;
            }
        }

        private void EnsureSkyboxBufferCoroutine()
        {
            if (skyboxBufferCoroutine != null) return;
            skyboxBufferCoroutine = StartCoroutine(CopySkyboxTextureAtEndOfFrame());
        }

        private void StopSkyboxBufferCoroutine()
        {
            if (skyboxBufferCoroutine == null) return;
            StopCoroutine(skyboxBufferCoroutine);
            skyboxBufferCoroutine = null;
        }

        private void ApplyDirectSkyboxTexture(Texture texture)
        {
            ReleaseSkyboxBuffers();

            // Old direct path, kept as a selectable mode for profiling comparison. This makes the
            // skybox sample the WebRTC plugin texture directly and can cause a read/write synchronization stall.
            RenderSettings.skybox.mainTexture = texture;
        }

        private bool EnsureSkyboxMaterial()
        {
            if (RenderSettings.skybox != null &&
                RenderSettings.skybox.shader != null &&
                RenderSettings.skybox.shader.name == VideoSkyboxShaderName)
            {
                material = RenderSettings.skybox;
                return true;
            }

            var shader = Shader.Find(VideoSkyboxShaderName);
            if (shader == null)
            {
                Debug.LogError($"Unable to find skybox shader: {VideoSkyboxShaderName}");
                return false;
            }

            material = new Material(shader);
            RenderSettings.skybox = material;
            Debug.Log("Skybox Material Changed.");
            return true;
        }

        private bool EnsureSkyboxBuffers(Texture source)
        {
            if (source.width <= 0 || source.height <= 0) return false;
            if (skyboxReadBuffer != null &&
                skyboxWriteBuffer != null &&
                skyboxBufferWidth == source.width &&
                skyboxBufferHeight == source.height)
            {
                return true;
            }

            ReleaseSkyboxBuffers();

            skyboxBufferWidth = source.width;
            skyboxBufferHeight = source.height;
            skyboxReadBuffer = CreateSkyboxBuffer("Crossport Video Skybox Buffer A", source.width, source.height);
            skyboxWriteBuffer = CreateSkyboxBuffer("Crossport Video Skybox Buffer B", source.width, source.height);
            hasBufferedSkyboxFrame = false;
            Debug.Log($"Created double-buffered skybox textures: {source.width}x{source.height}.");
            return true;
        }

        private static RenderTexture CreateSkyboxBuffer(string bufferName, int width, int height)
        {
            var buffer = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = bufferName,
                useMipMap = false,
                autoGenerateMips = false,
                filterMode = FilterMode.Bilinear
            };
            buffer.wrapModeU = TextureWrapMode.Repeat;
            buffer.wrapModeV = TextureWrapMode.Clamp;
            buffer.Create();
            return buffer;
        }

        private void SwapSkyboxBuffers()
        {
            var previousReadBuffer = skyboxReadBuffer;
            skyboxReadBuffer = skyboxWriteBuffer;
            skyboxWriteBuffer = previousReadBuffer;
        }

        private void OnDestroy()
        {
            StopSkyboxBufferCoroutine();
            ReleaseSkyboxBuffers();
        }

        private void ReleaseSkyboxBuffers()
        {
            if (RenderSettings.skybox != null &&
                (RenderSettings.skybox.mainTexture == skyboxReadBuffer ||
                 RenderSettings.skybox.mainTexture == skyboxWriteBuffer))
            {
                RenderSettings.skybox.mainTexture = null;
            }

            ReleaseSkyboxBuffer(ref skyboxReadBuffer);
            ReleaseSkyboxBuffer(ref skyboxWriteBuffer);
            hasBufferedSkyboxFrame = false;
            skyboxBufferWidth = 0;
            skyboxBufferHeight = 0;
        }

        private static void ReleaseSkyboxBuffer(ref RenderTexture? buffer)
        {
            if (buffer == null) return;
            buffer.Release();
            UnityEngine.Object.Destroy(buffer);
            buffer = null;
        }
    }
}
