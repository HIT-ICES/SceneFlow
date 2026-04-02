using Ices.Crossport.Core.DualHandling.InputSystem;
using System;
using Ices.Crossport.Diagnostics;
using Unity.RenderStreaming;
using Unity.RenderStreaming.InputSystem;
using Unity.WebRTC;
using UnityEngine;
using InputRemoting = Ices.Crossport.Core.DualHandling.InputSystem.InputRemoting;

namespace Ices.Crossport.Core.DualHandling
{
    /// <summary>
    /// 
    /// </summary>
    public class DualHandlingInputSender : DataChannelBase
    {
        private DualHandlingSenderCore sender;
        private InputRemoting senderInput;

        private IDisposable suscriberDisposer;

        /// <summary>
        ///
        /// </summary>
        /// <param name="track"></param>
        public override void SetChannel(string connectionId, RTCDataChannel channel)
        {
            if (channel == null)
            {
                Dispose();
            }
            else
            {
                sender = new DualHandlingSenderCore();
                senderInput = new InputRemoting(sender);
                suscriberDisposer = senderInput.Subscribe(new Observer(channel));
                channel.OnOpen += OnOpen;
                channel.OnClose += OnClose;
                channel.OnMessage += NewOnMessage;
            }

            base.SetChannel(connectionId, channel);
        }

        void NewOnMessage(byte[] bytes)
        {
            const int k_timeStampSize = 8;
            MessageSerializer.Deserialize(bytes, out var msg);
            if (msg.type is not InputRemoting.MessageType.NewEventFeedback) return;
            var time = BitConverter.ToInt64(msg.data[..k_timeStampSize]);
            EventLogger.LogFeedback(time);
            base.OnMessage(bytes);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="size">Texture Size.</param>
        /// <param name="region">Region of the texture in world coordinate system.</param>
        public void CalculateInputResion(Rect region, Vector2Int size)
        {
            sender.CalculateInputRegion(region, new Rect(Vector2.zero, size));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        public void EnableInputPositionCorrection(bool enabled) { sender.EnableInputPositionCorrection = enabled; }

        void OnOpen()
        {
            Debug.Log($"OnOpen;");
            senderInput.StartSending();
        }

        void OnClose() { senderInput.StopSending(); }

        protected virtual void OnDestroy() { this.Dispose(); }

        protected void Dispose()
        {
            senderInput?.StopSending();
            suscriberDisposer?.Dispose();
            sender?.Dispose();
            sender = null;
        }
    }
}