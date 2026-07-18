using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.RenderStreaming;
using Unity.WebRTC;
using UnityEngine;

namespace Ices.Crossport.Diagnostics
{
    public class FrameRateRecorder : MonoBehaviour
    {
        const int RAW_DATA_SIZE_LIMIT = 600;
        [SerializeField] private VideoStreamReceiver? activeReceiver;

        public static List<double> localRawFps = new();
        public static List<double> remoteRawFps = new();
        private static FrameRateRecorder _instance;
        private uint localframeCount;
        private double localDurations;

        private int lastRecordDuration;

        private uint remoteInitFrameCount;
        private long remoteInitTimestamp;
        private uint remoteLastFrameCount;
        private long remoteLastTimestamp;

        private bool ignoreNextLocalFrame;
        public static void IgnoreNextLocalFrame()
        {
            if (_instance != null)
            {
                _instance.ignoreNextLocalFrame = true;
            }
        }
        
        public static void ResetStats()
        {
            localRawFps.Clear();
            remoteRawFps.Clear();
            _instance?.InstanceResetStats();
        }

        public void InstanceResetStats()
        {
            lastRecordDuration = -1;
            localframeCount = 0;
            localDurations = 0;
            remoteInitTimestamp = 0;
            remoteInitFrameCount = 0;
            remoteLastTimestamp = 0;
            remoteLastFrameCount = 0;
        }

        public static FrameRate ExportLocal()
            => FrameRate.FromRaw
            (
                localRawFps,
                _instance.localframeCount,
                (long)(_instance.localDurations * 1000000L)
            );

        public static FrameRate ExportRemote()
            => FrameRate.FromRaw
            (
                remoteRawFps,
                _instance.remoteLastFrameCount - _instance.remoteInitFrameCount,
                _instance.remoteLastTimestamp - _instance.remoteInitTimestamp
            );

        private void Awake() { _instance = this; }

        private void Start()
        {
            ResetStats();
            if (activeReceiver != null)
            {
                StartCoroutine(CollectStats());
                ConsoleManager.LogWithDebug("Collecting Receiver Framerate");
            }

            ConsoleManager.LogWithDebug("Collecting Local Framerate");
        }

        void AddLocalFps(double fps)
        {
            // var currentTime = (int)Math.Floor(localDurations);
            // if (currentTime == lastRecordDuration) return; // Wait for Next Second
            // lastRecordDuration = currentTime;
            if (localRawFps.Count <= RAW_DATA_SIZE_LIMIT) localRawFps.Add(fps);
        }

        void AddRemoteFps(double fps)
        {
            if (remoteRawFps.Count <= RAW_DATA_SIZE_LIMIT) remoteRawFps.Add(fps);
        }

        private void Update()
        {
            if (ignoreNextLocalFrame)
            {
                ignoreNextLocalFrame = false;
                return;
            }
            // Local Framerate
            var dt = Time.unscaledDeltaTime;
            localDurations += dt;
            localframeCount++;
            var fps = 1.0 / dt;
            AddLocalFps(fps);
        }

        private void OnDestroy() { }

        private IEnumerator CollectStats()
        {
            var waitSec = new WaitForSeconds(1);

            while (true)
            {
                yield return waitSec;
                if (activeReceiver?.Transceiver?.Receiver == null) continue;
                yield return StartCoroutine(UpdateStats(activeReceiver.Transceiver.Receiver));
            }
        }

        private int statsLastFrameCount;
        private float statsLastTime;
        private IEnumerator UpdateStats(RTCRtpReceiver receiver)
        {
            var op = receiver.GetStats();
            yield return new WaitUntilWithTimeout(() => op.IsDone, 3f);

            if (op.IsError || !op.IsDone) yield break;

            var report = op.Value;
            if (report == null) yield break;

            // Update Report
            foreach (var stats in report.Stats.Values)
            {
                if (stats is not RTCInboundRTPStreamStats inboundStats) continue;
                if (inboundStats.kind != "video") continue;
                
                float time = Time.realtimeSinceStartup;
                int frameDiff = inboundStats.framesReceived - statsLastFrameCount;
                float timeDiff = time - statsLastTime;
                statsLastFrameCount = inboundStats.framesReceived;
                statsLastTime = time;
                Debug.Log(
                    $"[FrameRateRecorder] Inbound Stats: framesReceived={inboundStats.framesReceived}, timestamp={inboundStats.Timestamp}, fps={inboundStats.framesPerSecond}");
                Debug.Log(
                    $"[FrameRateRecorder] Stats: fps={frameDiff / timeDiff:0.00}");
                if (frameDiff != 0 && timeDiff != time)
                    AddRemoteFps(frameDiff / timeDiff);

                // if (remoteInitTimestamp == 0)
                // {
                //     remoteInitFrameCount = inboundStats.framesDecoded;
                //     remoteInitTimestamp = inboundStats.Timestamp;
                // }
                // else
                // {
                //     var currentTimeDiff = (double)(inboundStats.Timestamp - remoteLastTimestamp) / 1000000L;
                //     var fps = (inboundStats.framesDecoded - remoteLastFrameCount) / currentTimeDiff;
                //     AddRemoteFps(fps);
                // }
                //
                // remoteLastFrameCount = inboundStats.framesDecoded;
                // remoteLastTimestamp = inboundStats.Timestamp;
            }

            report.Dispose();
        }
    }

    internal class WaitUntilWithTimeout : CustomYieldInstruction
    {
        private readonly Func<bool> predicate;

        private readonly float timeoutTime;

        public WaitUntilWithTimeout(Func<bool> predicate, float timeout)
        {
            timeoutTime = Time.realtimeSinceStartup + timeout;
            this.predicate = predicate;
        }

        public bool IsCompleted { get; private set; }

        public override bool keepWaiting
        {
            get
            {
                IsCompleted = predicate();
                if (IsCompleted) return false;

                return !(Time.realtimeSinceStartup >= timeoutTime);
            }
        }
    }
}