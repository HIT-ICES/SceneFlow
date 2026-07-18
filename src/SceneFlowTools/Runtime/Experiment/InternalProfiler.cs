using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace SceneFlowTools.Runtime.Experiment
{
    [AddComponentMenu("SceneFlow/Experiment/Internal Profiler")]
    public class InternalProfiler : MonoBehaviour
    {
        private const string LogPrefix = "STATICS_PROFILE ";

        [Min(0.1f)]
        public float logIntervalSeconds = 1.0f;

        public bool enableBinaryProfilerLog;
        public string binaryProfilerLogPath = "";
        public int profilerBufferSizeMb = 256;

        private ProfilerRecorder mainThreadTime;
        private ProfilerRecorder renderThreadTime;
        private ProfilerRecorder batchesCount;
        private ProfilerRecorder setPassCallsCount;
        private ProfilerRecorder drawCallsCount;
        private ProfilerRecorder trianglesCount;
        private ProfilerRecorder verticesCount;
        private ProfilerRecorder gfxUsedMemory;
        private ProfilerRecorder gcUsedMemory;
        private ProfilerRecorder totalReservedMemory;

        private readonly FrameTiming[] frameTimings = new FrameTiming[16];
        private double intervalStartTime;
        private double frameTimeTotal;
        private int frameCount;

        private void OnEnable()
        {
            intervalStartTime = Time.realtimeSinceStartupAsDouble;
            frameTimeTotal = 0;
            frameCount = 0;

            mainThreadTime = StartRecorder(ProfilerCategory.Internal, "Main Thread", 120);
            renderThreadTime = StartRecorder(ProfilerCategory.Internal, "Render Thread", 120);
            batchesCount = StartRecorder(ProfilerCategory.Render, "Batches Count", 120);
            setPassCallsCount = StartRecorder(ProfilerCategory.Render, "SetPass Calls Count", 120);
            drawCallsCount = StartRecorder(ProfilerCategory.Render, "Draw Calls Count", 120);
            trianglesCount = StartRecorder(ProfilerCategory.Render, "Triangles Count", 120);
            verticesCount = StartRecorder(ProfilerCategory.Render, "Vertices Count", 120);
            gfxUsedMemory = StartRecorder(ProfilerCategory.Memory, "Gfx Used Memory", 120);
            gcUsedMemory = StartRecorder(ProfilerCategory.Memory, "GC Used Memory", 120);
            totalReservedMemory = StartRecorder(ProfilerCategory.Memory, "Total Reserved Memory", 120);

            if (enableBinaryProfilerLog)
            {
                StartBinaryProfilerLog();
            }
        }

        private void OnDisable()
        {
            DisposeRecorders();

            if (enableBinaryProfilerLog)
            {
                Profiler.enabled = false;
                Profiler.logFile = "";
            }
        }

        private void Update()
        {
            FrameTimingManager.CaptureFrameTimings();

            frameCount++;
            frameTimeTotal += Time.unscaledDeltaTime;

            double now = Time.realtimeSinceStartupAsDouble;
            if (now - intervalStartTime < logIntervalSeconds)
            {
                return;
            }

            Debug.Log(BuildProfileLog(now));
            ResetRecorders();

            intervalStartTime = now;
            frameTimeTotal = 0;
            frameCount = 0;
        }

        private static ProfilerRecorder StartRecorder(ProfilerCategory category, string statName, int sampleCount)
        {
            try
            {
                return ProfilerRecorder.StartNew(category, statName, sampleCount);
            }
            catch (Exception)
            {
                return default;
            }
        }

        private void StartBinaryProfilerLog()
        {
            if (profilerBufferSizeMb > 0)
            {
                Profiler.maxUsedMemory = profilerBufferSizeMb * 1024 * 1024;
            }

            string logPath = binaryProfilerLogPath;
            if (string.IsNullOrWhiteSpace(logPath))
            {
                string fileName = $"internal_profiler_{DateTime.UtcNow:yyyyMMdd_HHmmss}.raw";
                logPath = Path.Combine(Application.persistentDataPath, fileName);
            }

            Profiler.logFile = logPath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
            Debug.Log($"STATICS_PROFILE_BINARY_LOG {logPath}");
        }

        private string BuildProfileLog(double now)
        {
            uint timingCount = FrameTimingManager.GetLatestTimings((uint)frameTimings.Length, frameTimings);
            double cpuFrameMs = AverageFrameTiming(timingCount, timing => timing.cpuFrameTime);
            double gpuFrameMs = AverageFrameTiming(timingCount, timing => timing.gpuFrameTime);
            double fps = frameTimeTotal > 0 ? frameCount / frameTimeTotal : 0;

            var json = new StringBuilder(512);
            json.Append(LogPrefix);
            json.Append('{');
            AppendNumber(json, "time", now);
            AppendNumber(json, "frame", Time.frameCount);
            AppendNumber(json, "intervalSeconds", now - intervalStartTime);
            AppendNumber(json, "fps", fps);
            AppendNumber(json, "frameMs", fps > 0 ? 1000.0 / fps : 0);
            AppendNumberOrNull(json, "mainThreadMs", AverageRecorderMs(mainThreadTime));
            AppendNumberOrNull(json, "renderThreadMs", AverageRecorderMs(renderThreadTime));
            AppendNumberOrNull(json, "cpuFrameMs", cpuFrameMs);
            AppendNumberOrNull(json, "gpuFrameMs", gpuFrameMs);
            AppendNumberOrNull(json, "batches", AverageRecorderValue(batchesCount));
            AppendNumberOrNull(json, "setPassCalls", AverageRecorderValue(setPassCallsCount));
            AppendNumberOrNull(json, "drawCalls", AverageRecorderValue(drawCallsCount));
            AppendNumberOrNull(json, "triangles", AverageRecorderValue(trianglesCount));
            AppendNumberOrNull(json, "vertices", AverageRecorderValue(verticesCount));
            AppendNumberOrNull(json, "gfxUsedMemoryMB", RecorderBytesToMb(gfxUsedMemory));
            AppendNumberOrNull(json, "gcUsedMemoryMB", RecorderBytesToMb(gcUsedMemory));
            AppendNumberOrNull(json, "totalReservedMemoryMB", RecorderBytesToMb(totalReservedMemory));
            AppendNumber(json, "graphicsDriverMemoryMB",
                Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024.0 * 1024.0));
            AppendNumber(json, "screenWidth", Screen.width);
            AppendNumber(json, "screenHeight", Screen.height);
            AppendNumber(json, "targetFrameRate", Application.targetFrameRate);
            AppendNumber(json, "vSyncCount", QualitySettings.vSyncCount);
            json.Append('}');
            return json.ToString();
        }

        private double AverageFrameTiming(uint count, Func<FrameTiming, double> selector)
        {
            if (count == 0)
            {
                return double.NaN;
            }

            double total = 0;
            for (int i = 0; i < count; i++)
            {
                total += selector(frameTimings[i]);
            }

            return total / count;
        }

        private static double AverageRecorderMs(ProfilerRecorder recorder)
        {
            double value = AverageRecorderValue(recorder);
            return double.IsNaN(value) ? double.NaN : value / 1_000_000.0;
        }

        private static double RecorderBytesToMb(ProfilerRecorder recorder)
        {
            double value = AverageRecorderValue(recorder);
            return double.IsNaN(value) ? double.NaN : value / (1024.0 * 1024.0);
        }

        private static double AverageRecorderValue(ProfilerRecorder recorder)
        {
            if (!recorder.Valid || recorder.Count == 0)
            {
                return double.NaN;
            }

            ProfilerRecorderSample[] samples = recorder.ToArray();
            if (samples == null || samples.Length == 0)
            {
                return double.NaN;
            }

            double total = 0;
            int count = 0;
            foreach (ProfilerRecorderSample sample in samples)
            {
                total += sample.Value;
                count++;
            }

            return count > 0 ? total / count : double.NaN;
        }

        private void DisposeRecorders()
        {
            DisposeRecorder(ref mainThreadTime);
            DisposeRecorder(ref renderThreadTime);
            DisposeRecorder(ref batchesCount);
            DisposeRecorder(ref setPassCallsCount);
            DisposeRecorder(ref drawCallsCount);
            DisposeRecorder(ref trianglesCount);
            DisposeRecorder(ref verticesCount);
            DisposeRecorder(ref gfxUsedMemory);
            DisposeRecorder(ref gcUsedMemory);
            DisposeRecorder(ref totalReservedMemory);
        }

        private void ResetRecorders()
        {
            ResetRecorder(ref mainThreadTime);
            ResetRecorder(ref renderThreadTime);
            ResetRecorder(ref batchesCount);
            ResetRecorder(ref setPassCallsCount);
            ResetRecorder(ref drawCallsCount);
            ResetRecorder(ref trianglesCount);
            ResetRecorder(ref verticesCount);
            ResetRecorder(ref gfxUsedMemory);
            ResetRecorder(ref gcUsedMemory);
            ResetRecorder(ref totalReservedMemory);
        }

        private static void ResetRecorder(ref ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
            {
                return;
            }

            recorder.Reset();
            recorder.Start();
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }

        private static void AppendNumber(StringBuilder builder, string name, double value)
        {
            AppendName(builder, name);
            builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        private static void AppendNumber(StringBuilder builder, string name, int value)
        {
            AppendName(builder, name);
            builder.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendNumberOrNull(StringBuilder builder, string name, double value)
        {
            AppendName(builder, name);
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                builder.Append("null");
            }
            else
            {
                builder.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        private static void AppendName(StringBuilder builder, string name)
        {
            if (builder[builder.Length - 1] != '{')
            {
                builder.Append(',');
            }

            builder.Append('"');
            builder.Append(name);
            builder.Append("\":");
        }
    }
}
