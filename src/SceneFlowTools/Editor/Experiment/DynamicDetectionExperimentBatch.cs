using System;
using System.Collections.Generic;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.DynamicDetection;
using SceneFlowTools.Runtime.Experiment;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor.Experiment
{
    public static class DynamicDetectionExperimentBatch
    {
        public static void Run()
        {
            try
            {
                Dictionary<string, string> args = ParseCommandLineArgs();
                string mode = GetArg(args, "mode", defaultValue: DynamicDetectionMode.Agent.ToString());
                string serviceHost = GetArg(args, "serviceHost", defaultValue: "localhost:8000");
                string outputJson = GetArg(args, "outputJson", required: true);
                bool useCache = ParseBool(GetArg(args, "useCache", defaultValue: "true"));
                int agentMaxToolCalls = ParseInt(GetArg(args, "agentMaxToolCalls", defaultValue: "6"));
                int directBatchSize = ParseInt(GetArg(args, "directBatchSize", defaultValue: "16"));

                if (!Enum.TryParse(mode, ignoreCase: true, out DynamicDetectionMode detectionMode))
                {
                    throw new Exception($"Invalid dynamic detection mode: {mode}");
                }

                ExternalUtils.Host = serviceHost;
                Debug.Log(
                    $"DynamicDetectionExperimentBatch.Run mode={mode} serviceHost={serviceHost} " +
                    $"useCache={useCache} outputJson={outputJson}");

                DynDetectExpResult result = DynamicDetectionExperimentEditor.ExpSample(
                    detectionMode,
                    useCache,
                    agentMaxToolCalls,
                    directBatchSize);

                string fullPath = System.IO.Path.GetFullPath(outputJson);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
                System.IO.File.WriteAllText(fullPath, JsonUtility.ToJson(result, true));
                Debug.Log($"Dynamic detection sample experiment result exported to {fullPath}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static Dictionary<string, string> ParseCommandLineArgs()
        {
            string[] rawArgs = Environment.GetCommandLineArgs();
            Dictionary<string, string> args = new();
            for (int i = 0; i < rawArgs.Length; i++)
            {
                string key = rawArgs[i];
                if (!key.StartsWith("-")) continue;
                key = key.TrimStart('-');
                if (i + 1 >= rawArgs.Length || rawArgs[i + 1].StartsWith("-"))
                {
                    args[key] = "true";
                    continue;
                }

                args[key] = rawArgs[++i];
            }

            return args;
        }

        private static string GetArg(Dictionary<string, string> args, string name, string defaultValue = null,
            bool required = false)
        {
            if (args.TryGetValue(name, out string value))
            {
                return value;
            }

            if (required)
            {
                throw new ArgumentException($"Missing required command line argument -{name}");
            }

            return defaultValue;
        }

        private static bool ParseBool(string value)
        {
            return value != null && bool.TryParse(value, out bool result) && result;
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(value, out int result) ? result : 0;
        }
    }
}
