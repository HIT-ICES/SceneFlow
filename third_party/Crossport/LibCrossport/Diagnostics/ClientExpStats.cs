using System;
using System.Collections.Generic;
using System.Linq;
using Ices.Crossport.Settings;
using Newtonsoft.Json;

namespace Ices.Crossport.Diagnostics
{
    [Serializable]
    public class Latency
    {
        [JsonProperty] public double min;
        [JsonProperty] public double max;
        [JsonProperty] public double average;
        [JsonProperty] public List<double> raw;

        public static Latency FromRaw(List<double> raw)
        {
            if (raw.Count == 0) return new();
            return new()
                   {
                       min = raw.Min(),
                       max = raw.Max(),
                       average = raw.Average(),
                       raw = raw
                   };
        }

        public static Latency FromRawAndAvg(List<double> raw, double average)
        {
            if (raw.Count == 0) return new();
            return new()
                   {
                       min = raw.Min(),
                       max = raw.Max(),
                       average = average,
                       raw = raw
                   };
        }
    }

    [Serializable]
    public class FrameRate
    {
        [JsonProperty] public double min;
        [JsonProperty] public double max;
        [JsonProperty] public List<double> raw;
        [JsonProperty] public uint frameCount;
        [JsonProperty] public long timeDiff;

        public static FrameRate FromRaw(List<double> raw, uint frameCount, long timeDiff)
        {
            if (raw.Count == 0) return new();
            return new()
                   {
                       min = raw.Min(),
                       max = raw.Max(),
                       raw = raw,
                       frameCount = frameCount,
                       timeDiff = timeDiff
                   };
        }
    }

    [Serializable]
    public class ClientExpStats
    {
        [JsonProperty] public Latency netLatency;
        [JsonProperty] public Latency mtpLatency;
        [JsonProperty] public FrameRate localFrameRate;
        [JsonProperty] public FrameRate remoteFrameRate;
    }
}