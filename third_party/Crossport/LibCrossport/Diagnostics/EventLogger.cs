using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ices.Crossport.Diagnostics
{
    public class EventLogger
    {
        public const int STATS_LIMIT = 100;
        public static event Action<TimeSpan>? OnFeedback;
        public static event Action<DateTime>? OnMissedEvent;
        public static Dictionary<long, DateTime> PostedEventDict = new(8192);
        private static readonly List<TimeSpan> _delays = new(STATS_LIMIT);

        public static void LogSend(DateTime now)
        {
            var binary = now.ToBinary();
            //if (0 != binary % 10) return;
            PostedEventDict.TryAdd(binary, now);
        }

        public static Latency Export()
        {
            if (_delays.Count == 0) return new();
            return Latency.FromRaw(_delays.Select(t => t.TotalMilliseconds).ToList());
        }

        public static void Reset() { _delays.Clear(); }

        public static void UseDebugLogger()
        {
            OnFeedback += EventLogger_OnFeedback;
            OnMissedEvent += EventLogger_OnMissedEvent;
        }

        public static void RemoveDebugLogger()
        {
            OnFeedback -= EventLogger_OnFeedback;
            OnMissedEvent -= EventLogger_OnMissedEvent;
        }

        private static void EventLogger_OnMissedEvent(DateTime dateTime)
        {
            ConsoleManager.LogWithDebugWarning($"Missing Posted Event@{dateTime:mm:ss:FFFFFFF}");
        }

        private static void EventLogger_OnFeedback(TimeSpan diff)
        {
            ConsoleManager.LogWithDebug($"Recorded EventDiff {diff.TotalMilliseconds} ms");
        }

        public static void LogFeedback(long time)
        {
            //if (0 != time % 10) return;
            var now = DateTime.UtcNow;
            if (PostedEventDict.Remove(time, out var dateTime))
            {
                var diff = now - dateTime;
                OnFeedback?.Invoke(diff);
            }
            else
            {
                OnMissedEvent?.Invoke(DateTime.FromBinary(time));
            }
        }

        public static void UseStatsLogger() { OnFeedback += EventLogger_Stats_log; }

        private static void EventLogger_Stats_log(TimeSpan obj)
        {
            if (_delays.Count < STATS_LIMIT)
            {
                ConsoleManager.LogWithDebug("time recorded");
                _delays.Add(obj);
            }
        }
    }
}