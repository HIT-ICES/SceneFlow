using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
                DynDetectExpResult result = ExpSample();
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

        DynDetectExpResult ExpSample()
        {
            const int batchSize = 16;
            List<Type> samples = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type =>
                    type.IsClass && !type.IsAbstract && type.Namespace == "SceneFlowTools.Runtime.DynamicSample")
                .Where(t => t.Name.StartsWith("Sample"))
                .Where(t => t.IsPublic)
                .ToList();

            Debug.Log($"Start experiment for {samples.Count} sample scripts.");

            Debug.Log($"Detecting scripts dynamic behaviors, batchSize={batchSize}...");

            List<(Type scriptType, string source)> tasks = samples.Select(x =>
                (x, SourceUtils.GetScriptSourceOrFieldsByType(x))
            ).ToList();

            List<ScriptDynamicInfo> infos = new List<ScriptDynamicInfo>();
            // int promptTokens = 0;
            // int completionTokens = 0;


            Parallel.ForEach(
                tasks,
                new ParallelOptions()
                {
                    MaxDegreeOfParallelism = batchSize
                },
                task =>
                {
                    var result = ExternalUtils.DynamicDetection(task.scriptType.Name, task.source, true).Result;
                    var scriptDynamicInfo = new ScriptDynamicInfo
                    {
                        ScriptPath = task.scriptType.Name,
                        Dynamics = result.results.ToArray()
                    };
                    lock (infos)
                    {
                        infos.Add(scriptDynamicInfo);
                        // promptTokens += result.promptTokens;
                        // completionTokens += result.completionTokens;
                    }
                }
            );

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

            DynamicDetectionExperiment tg = (DynamicDetectionExperiment)target;

            DynDetectExpResult result = new DynDetectExpResult
            {
                sceneName = "DynamicDetectionSampleScripts",
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
    }
}