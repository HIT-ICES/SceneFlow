using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.DynamicDetection;
using SceneFlowTools.Runtime.DynamicSample;
using SceneFlowTools.Runtime.Experiment;
using UnityEditor;
using UnityEngine;
using Utils;

namespace SceneFlowTools.Editor.Experiment
{
    [CustomEditor(typeof(DynamicDetectionExperiment))]
    public class DynamicDetectionExperimentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            DynamicDetectionExperiment dde = (DynamicDetectionExperiment)target;
            if (GUILayout.Button("Run Experiment"))
            {
                DynDetectExpResult result = dde.ExpCurrentScene();
                dde.Analyze(result);
                Save(result);
            }

            if (GUILayout.Button("Run Experiment For Sample Scripts"))
            {
                Debug.Log("Run Experiment For Sample Scripts");
                DynDetectExpResult result = ExpSample(
                    dde.sampleDetectionMode,
                    dde.sampleUseCache,
                    dde.sampleAgentMaxToolCalls,
                    dde.sampleDirectBatchSize);
                dde.Analyze(result);
                Save(result);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void Save(DynDetectExpResult result)
        {
            string Path = $"Build/Experiment/DynamicDetection/{result.sceneName}.json";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            System.IO.File.WriteAllText(Path, JsonUtility.ToJson(result, true));
            Debug.Log($"Experiment result saved to {Path}");
        }

        public static DynDetectExpResult ExpSample(
            DynamicDetectionMode detectionMode,
            bool useCache,
            int agentMaxToolCalls,
            int directBatchSize)
        {
            List<Type> samples = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass && !type.IsAbstract && type.Namespace == "SceneFlowTools.Runtime.DynamicSample")
                .Where(t => t.Name.StartsWith("Sample"))
                .Where(t => t.IsPublic)
                .OrderBy(t => t.Name)
                .ToList();

            Debug.Log($"Start experiment for {samples.Count} sample scripts.");

            SampleDetectionStats stats = detectionMode == DynamicDetectionMode.Agent
                ? DetectSampleScriptsAgent(samples, useCache, agentMaxToolCalls)
                : DetectSampleScriptsDirect(samples, useCache, directBatchSize);
            List<ScriptDynamicInfo> infos = stats.infos;

            Debug.Log("Detecting scripts dynamic behaviors done.");
            List<string> all = samples
                .Select(x => x.Name)
                .SelectMany(x => new[]
                {
                    $"{x}-field",
                    $"{x}-this",
                })
                .ToList();
            Dictionary<string, ObjectDynamicType> trueLabels = new();
            Dictionary<string, ObjectDynamicType> predictedLabels = new();


            foreach (var sample in samples)
            {
                string name = sample.Name;
                if (name.EndsWith("I"))
                {
                    trueLabels[$"{name}-field"] = ObjectDynamicType.DynamicInteractive;
                    trueLabels[$"{name}-this"] = ObjectDynamicType.DynamicInteractive;
                }
                else if (name.EndsWith("N") || name.EndsWith("F"))
                {
                    trueLabels[$"{name}-field"] = ObjectDynamicType.Static;
                    trueLabels[$"{name}-this"] = ObjectDynamicType.Static;
                }
                else
                {
                    trueLabels[$"{name}-field"] = ObjectDynamicType.Dynamic;
                    trueLabels[$"{name}-this"] = ObjectDynamicType.Dynamic;
                }
            }

            foreach (var objId in all)
            {
                predictedLabels[objId] = ObjectDynamicType.Static;
            }

            foreach (var info in infos)
            {
                foreach (var dyn in info.Dynamics)
                {
                    if (dyn.Type != "field" && dyn.Type != "this") continue;
                    predictedLabels[$"{info.ScriptPath}-{dyn.Type}"] = dyn.DynamicType;
                }
            }

            DynDetectExpResult result = new DynDetectExpResult
            {
                sceneName = "DynamicDetectionSampleScripts",
                promptTokens = stats.promptTokens,
                completionTokens = stats.completionTokens,
                toolCalls = stats.toolCalls,
                analysisSeconds = stats.analysisSeconds,
            };
            foreach (var objId in all)
            {
                result.items.Add(new DynDetectExpItem
                {
                    objectId = objId,
                    trueLabel = trueLabels[objId],
                    predictedLabel = predictedLabels[objId],
                });
            }

            return result;
        }

        private static SampleDetectionStats DetectSampleScriptsDirect(
            List<Type> samples,
            bool useCache,
            int directBatchSize)
        {
            double time = EditorApplication.timeSinceStartup;
            int batchSize = Math.Max(1, directBatchSize);
            Debug.Log($"Detecting sample scripts dynamic behaviors, mode=DirectLLM, batchSize={batchSize}...");
            List<(Type scriptType, string source)> tasks = samples.Select(x =>
                (x, SourceUtils.GetScriptSourceOrFieldsByType(x))
            ).ToList();
            List<ScriptDynamicInfo> infos = new();
            int promptTokens = 0;
            int completionTokens = 0;

            Parallel.ForEach(
                tasks,
                new ParallelOptions { MaxDegreeOfParallelism = batchSize },
                task =>
                {
                    var result = ExternalUtils.DynamicDetection(task.scriptType.Name, task.source, useCache).Result;
                    lock (infos)
                    {
                        infos.Add(new ScriptDynamicInfo
                        {
                            ScriptPath = task.scriptType.Name,
                            Dynamics = result.results.ToArray()
                        });
                        promptTokens += result.promptTokens;
                        completionTokens += result.completionTokens;
                    }
                }
            );

            return new SampleDetectionStats
            {
                infos = infos,
                promptTokens = promptTokens,
                completionTokens = completionTokens,
                analysisSeconds = EditorApplication.timeSinceStartup - time,
            };
        }

        private static SampleDetectionStats DetectSampleScriptsAgent(
            List<Type> samples,
            bool useCache,
            int agentMaxToolCalls)
        {
            double time = EditorApplication.timeSinceStartup;
            Debug.Log("Detecting sample scripts dynamic behaviors, mode=Agent...");
            List<DynamicDetectionContextScript> contextScripts = samples.Select(GetSampleContextScript).ToList();
            List<ScriptDynamicInfo> infos = new();
            int promptTokens = 0;
            int completionTokens = 0;
            int toolCalls = 0;
            const int batchSize = 8;
            for (int start = 0; start < samples.Count; start += batchSize)
            {
                List<Type> batch = samples.Skip(start).Take(batchSize).ToList();
                DynamicDetectionAgentRequest request = new()
                {
                    sceneId = "DynamicDetectionSampleScripts",
                    scripts = contextScripts,
                    targetScriptPaths = batch.Select(type => type.Name).ToList(),
                    components = samples.Select(type => type.FullName ?? type.Name).ToList(),
                    useCache = useCache,
                    agentConfig = new DynamicDetectionAgentConfig
                    {
                        maxToolCalls = agentMaxToolCalls,
                        enableGlobalScriptSearch = true
                    }
                };
                Debug.Log(
                    $"Agent detecting sample script batch {start / batchSize + 1}/{Math.Ceiling(samples.Count / (double)batchSize)} " +
                    $"targets={string.Join(",", request.targetScriptPaths)}");

                DynamicDetectionAgentResult result = ExternalUtils.DynamicDetectionAgent(request).Result;
                promptTokens += result.promptTokens;
                completionTokens += result.completionTokens;
                toolCalls += result.toolCalls;
                infos.AddRange(result.scripts.Select(scriptResult => new ScriptDynamicInfo
                {
                    ScriptPath = scriptResult.scriptPath,
                    Dynamics = scriptResult.dynamics ?? Array.Empty<DynamicInfo>()
                }));
            }

            return new SampleDetectionStats
            {
                infos = infos,
                promptTokens = promptTokens,
                completionTokens = completionTokens,
                toolCalls = toolCalls,
                analysisSeconds = EditorApplication.timeSinceStartup - time,
            };
        }

        private class SampleDetectionStats
        {
            public List<ScriptDynamicInfo> infos = new();
            public int promptTokens;
            public int completionTokens;
            public int toolCalls;
            public double analysisSeconds;
        }

        private static DynamicDetectionContextScript GetSampleContextScript(Type type)
        {
            string source = SourceUtils.GetScriptSourceOrFieldsByType(type);
            return new DynamicDetectionContextScript
            {
                scriptPath = type.Name,
                className = type.Name,
                sourceHash = Sha1(source),
                source = source,
                fields = SourceUtils.GetSerializeFields(type),
                isBuiltin = false
            };
        }

        private static string Sha1(string text)
        {
            using SHA1 sha1 = SHA1.Create();
            byte[] bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
