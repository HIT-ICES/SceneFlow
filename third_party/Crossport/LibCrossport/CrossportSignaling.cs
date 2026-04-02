using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using Ices.Crossport.Settings;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

namespace Ices.Crossport
{
    [Serializable]
    public class RoutedMessage<T>
    {
        public T data;
        public string from;
        public string to;
        public string type;
    }

    [Serializable]
    public class SignalingMessage
    {
        public string candidate;
        public string connectionId;
        public string message;
        public bool polite;
        public string sdp;
        public string sdpMid;
        public int sdpMLineIndex;
        public string sessionId;
        public string status;
        public string type;
    }

    public class CrossportSignaling : ISignaling, IDisposable
    {
        private readonly AutoResetEvent m_wsCloseEvent;
        private Guid m_id;
        private readonly SynchronizationContext m_mainThreadContext;
        private bool m_running;
        private Thread m_signalingThread;
        private readonly Queue<object> m_sendQueue;
        private readonly SemaphoreSlim m_sendLock;
        private WebSocket m_webSocket;

        public CrossportSignaling(CrossportSignalingSetting info, SynchronizationContext mainThreadContext)
        {
            Interval = info.interval;
            m_mainThreadContext = mainThreadContext;
            m_wsCloseEvent = new AutoResetEvent(false);
            m_id = Guid.NewGuid();
            Url = info.GetSignallingUrl(m_id.ToString()).ToString();
            m_sendLock = new SemaphoreSlim(1, 1);
            m_sendQueue = new Queue<object>();
        }

        public float Interval { get; }

        public string Url { get; }

        public void Start()
        {
            if (m_running)
                throw new InvalidOperationException("This object is already started.");
            m_running = true;
            m_signalingThread = new Thread(WSManage);
            m_signalingThread.Start();
        }


        public void Stop()
        {
            if (m_running)
            {
                m_running = false;
                m_webSocket?.Close();

                if (m_signalingThread.ThreadState == ThreadState.WaitSleepJoin)
                    m_signalingThread.Abort();
                else
                    m_signalingThread.Join(1000);
                m_signalingThread = null;
            }
        }

        public event OnStartHandler OnStart;
        public event OnConnectHandler OnCreateConnection;
        public event OnDisconnectHandler OnDestroyConnection;
        public event OnOfferHandler OnOffer;
#pragma warning disable 0067
        // this event is never used in this class
        public event OnAnswerHandler OnAnswer;
#pragma warning restore 0067
        public event OnIceCandidateHandler OnIceCandidate;

        public void SendOffer(string connectionId, RTCSessionDescription offer)
        {
            var data = new DescData
                       {
                           connectionId = connectionId,
                           sdp = offer.sdp,
                           type = "offer"
                       };

            var routedMessage = new RoutedMessage<DescData>
                                {
                                    from = connectionId,
                                    data = data,
                                    type = "offer"
                                };

            WSSend(routedMessage);
        }

        public void SendAnswer(string connectionId, RTCSessionDescription answer)
        {
            var data = new DescData
                       {
                           connectionId = connectionId,
                           sdp = answer.sdp,
                           type = "answer"
                       };

            var routedMessage = new RoutedMessage<DescData>
                                {
                                    from = connectionId,
                                    data = data,
                                    type = "answer"
                                };

            WSSend(routedMessage);
        }

        public void SendCandidate(string connectionId, RTCIceCandidate candidate)
        {
            var data = new CandidateData
                       {
                           connectionId = connectionId,
                           candidate = candidate.Candidate,
                           sdpMLineIndex = candidate.SdpMLineIndex.GetValueOrDefault(0),
                           sdpMid = candidate.SdpMid
                       };

            var routedMessage = new RoutedMessage<CandidateData>
                                {
                                    from = connectionId,
                                    data = data,
                                    type = "candidate"
                                };

            WSSend(routedMessage);
        }

        public void OpenConnection(string connectionId)
        {
            WSSend($"{{\"type\":\"connect\", \"connectionId\":\"{connectionId}\"}}");
        }

        public void CloseConnection(string connectionId)
        {
            WSSend($"{{\"type\":\"disconnect\", \"connectionId\":\"{connectionId}\"}}");
        }

        ~CrossportSignaling() { Dispose(false); }

        private void WSManage()
        {
            while (m_running)
            {
                WSCreate();

                try
                {
                    m_wsCloseEvent.WaitOne();

                    Thread.Sleep((int)(Interval * 1000));
                }
                catch (ThreadAbortException)
                {
                    // Thread.Abort() called from main thread. Ignore
                    return;
                }
            }

            ConsoleManager.LogWithDebug("Signaling: WS managing thread ended");
        }

        private void WSCreate()
        {
            m_webSocket = new WebSocket(Url);
            if (Url.StartsWith("wss"))
                m_webSocket.SslConfiguration.EnabledSslProtocols =
                    SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            m_webSocket.EnableRedirection = true;
            m_webSocket.OnOpen += WSConnected;
            m_webSocket.OnMessage += WSProcessMessage;
            m_webSocket.OnError += WSError;
            m_webSocket.OnClose += WSClosed;

            Monitor.Enter(m_webSocket);

            ConsoleManager.LogWithDebug($"Signaling: Connecting WS {Url}");
            m_webSocket.ConnectAsync();
            m_webSocket.OnError += WebSocket_OnError;
        }

        private static void WebSocket_OnError(object sender, ErrorEventArgs e)
        {
            ConsoleManager.LogWithDebugError($"WebSocket Error: {e.Exception}");
        }

        private void WSProcessMessage(object sender, MessageEventArgs e)
        {
            var content = Encoding.UTF8.GetString(e.RawData);
            // ConsoleManager.LogWithDebug($"Signaling: Receiving message: {content}");

            try
            {
                var routedMessage = JsonUtility.FromJson<RoutedMessage<SignalingMessage>>(content);
                m_mainThreadContext.Post((x) =>
                {
                    ConsoleManager.LogWithDebug($"Signaling: Receiving [{routedMessage.type}] message: {content}");
                },null);
                

                SignalingMessage msg;
                msg = !string.IsNullOrEmpty(routedMessage.type)
                          ? routedMessage.data
                          : JsonUtility.FromJson<SignalingMessage>(content);

                if (string.IsNullOrEmpty(routedMessage.type)) return;
                switch (routedMessage.type)
                {
                case "connect":
                    msg = JsonUtility.FromJson<SignalingMessage>(content);
                    m_mainThreadContext.Post
                    (
                        d => OnCreateConnection?.Invoke(this, msg.connectionId, msg.polite),
                        null
                    );
                    break;
                case "disconnect":
                    msg = JsonUtility.FromJson<SignalingMessage>(content);
                    m_mainThreadContext.Post(d => OnDestroyConnection?.Invoke(this, msg.connectionId), null);
                    break;
                case "offer":
                {
                    var offer = new DescData
                                {
                                    connectionId = routedMessage.from,
                                    sdp = msg.sdp,
                                    polite = msg.polite
                                };
                    m_mainThreadContext.Post(d => OnOffer?.Invoke(this, offer), null);
                    break;
                }
                case "answer":
                {
                    var answer = new DescData
                                 {
                                     connectionId = routedMessage.from,
                                     sdp = msg.sdp
                                 };
                    m_mainThreadContext.Post(d => OnAnswer?.Invoke(this, answer), null);
                    break;
                }
                case "candidate":
                {
                    var candidate = new CandidateData
                                    {
                                        connectionId = routedMessage.from,
                                        candidate = msg.candidate,
                                        sdpMLineIndex = msg.sdpMLineIndex,
                                        sdpMid = msg.sdpMid
                                    };
                    m_mainThreadContext.Post(d => OnIceCandidate?.Invoke(this, candidate), null);
                    break;
                }
                case "error":
                    msg = JsonUtility.FromJson<SignalingMessage>(content);
                    m_mainThreadContext.Post((x) => { ConsoleManager.LogWithDebugError(msg.message); }, null);
                    break;
                }
            }
            catch (Exception ex)
            {
                m_mainThreadContext.Post((x) =>
                {
                    ConsoleManager.LogWithDebugError("Signaling: Failed to parse message: " + ex);
                }, null);
            }
        }

        private void WSConnected(object sender, EventArgs e)
        {
            ConsoleManager.LogWithDebug("Signaling: WS connected.");
            //RegisterCrossport();
            m_mainThreadContext.Post(d => OnStart?.Invoke(this), null);
        }


        private void WSError(object sender, ErrorEventArgs e)
        {
            ConsoleManager.LogWithDebugError($"Signaling: WS connection error: {e.Message}");
        }

        private void WSClosed(object sender, CloseEventArgs e)
        {
            ConsoleManager.LogWithDebug($"Signaling: WS connection closed, code: {e.Code}");

            m_wsCloseEvent.Set();
            m_webSocket = null;
        }

        private void WSSendInternal(object data)
        {
            if (data is string s)
            {
                ConsoleManager.LogWithDebug("Signaling: Sending WS data: " + s);
                m_webSocket.Send(s);
            }
            else
            {
                var str = JsonUtility.ToJson(data);
                ConsoleManager.LogWithDebug("Signaling: Sending WS data: " + str);
                m_webSocket.Send(str);
            }
        }

        private void WSSend(object data)
        {
            m_sendLock.Wait();
            if (m_webSocket is not { ReadyState: WebSocketState.Open })
            {
                ConsoleManager.LogWithDebugWarning("Signaling: WS is not connected. Message is enqueued.");
                m_sendQueue.Enqueue(data);
                m_sendLock.Release();
                return;
            }

            while (m_sendQueue.Any())
            {
                var msg = m_sendQueue.Dequeue();
                WSSendInternal(msg);
            }

            WSSendInternal(data);

            m_sendLock.Release();
        }

        [Serializable]
        private class CrossportRegisterMessage
        {
            public CrossportClientInfo data;
            public string id;
            public string type;
        }

        //private void ReleaseUnmanagedResources()
        //{
        //    // TODO release unmanaged resources here
        //}

        private void Dispose(bool disposing)
        {
            //ReleaseUnmanagedResources();
            if (disposing)
            {
                m_wsCloseEvent?.Dispose();
                m_sendLock?.Dispose();
                ((IDisposable)m_webSocket)?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}