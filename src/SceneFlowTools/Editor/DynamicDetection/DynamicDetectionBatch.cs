using System;
using System.Collections.Generic;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SceneFlowTools.Editor.DynamicDetection
{
    public static class DynamicDetectionBatch
    {
        public static void Run()
        {
            try
            {
                Dictionary<string, string> args = ParseCommandLineArgs();
                string scenePath = GetArg(args, "scenePath", required: true);
                string mode = GetArg(args, "mode", defaultValue: DynamicDetectionMode.Agent.ToString());
                string serviceHost = GetArg(args, "serviceHost", defaultValue: "localhost:8000");
                string outputJson = GetArg(args, "outputJson", required: true);
                bool forceRefresh = ParseBool(GetArg(args, "forceRefresh", defaultValue: "true"));
                bool useCache = ParseBool(GetArg(args, "useCache", defaultValue: "true"));
                bool propagateDynamicToChildren =
                    ParseBool(GetArg(args, "propagateDynamicToChildren", defaultValue: "true"));
                int agentMaxToolCalls = ParseInt(GetArg(args, "agentMaxToolCalls", defaultValue: "6"));
                int directBatchSize = ParseInt(GetArg(args, "directBatchSize", defaultValue: "8"));

                Debug.Log(
                    $"DynamicDetectionBatch.Run scenePath={scenePath} mode={mode} serviceHost={serviceHost} " +
                    $"forceRefresh={forceRefresh} useCache={useCache} " +
                    $"propagateDynamicToChildren={propagateDynamicToChildren} outputJson={outputJson}");

                ExternalUtils.Host = serviceHost;
                EditorSceneManager.OpenScene(scenePath);
                ApplyServiceHost(serviceHost);

                DynamicDetectionManager manager = UnityEngine.Object.FindObjectOfType<DynamicDetectionManager>();
                if (manager == null)
                {
                    throw new Exception($"No DynamicDetectionManager found in scene {scenePath}.");
                }

                if (!Enum.TryParse(mode, ignoreCase: true, out DynamicDetectionMode detectionMode))
                {
                    throw new Exception($"Invalid dynamic detection mode: {mode}");
                }

                manager.detectionMode = detectionMode;
                manager.propagateDynamicToChildren = propagateDynamicToChildren;
                DynamicDetectionBaker.Bake(manager, new DynamicDetectionBakeOptions
                {
                    PromptBeforeRun = false,
                    ForceRefreshScripts = forceRefresh,
                    UseCache = useCache,
                    AgentMaxToolCalls = agentMaxToolCalls,
                    DirectBatchSize = directBatchSize,
                });
                DynamicDetectionBaker.ExportJson(manager, outputJson);
                Debug.Log("DynamicDetectionBatch.Run completed.");
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

        private static string GetArg(Dictionary<string, string> args, string name, string defaultValue = null, bool required = false)
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

        private static void ApplyServiceHost(string serviceHost)
        {
            ExternalUtils.Host = serviceHost;
            foreach (ExternalServiceSettings settings in UnityEngine.Object.FindObjectsOfType<ExternalServiceSettings>(true))
            {
                settings.usePredefinedServiceHost = PredefinedServiceHost.None;
                settings.serviceHost = serviceHost;
                EditorUtility.SetDirty(settings);
            }

            Debug.Log($"External service host set to {serviceHost}");
        }
    }
}
