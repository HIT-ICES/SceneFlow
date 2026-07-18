using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicDetection
{
    [Serializable]
    public class DynamicDetectionData : ScriptableObject
    {
        public string DetectionMode;
        public List<ScriptDynamicInfo> ScriptsDynamicInfo = new();
        public List<GObjectDynamicInfo> ObjectsDynamicInfo = new();

        public ScriptDynamicInfo Find(string scriptPath) =>
            ScriptsDynamicInfo?.Find(info => info.ScriptPath == scriptPath);
    }

    [Serializable]
    public enum ObjectDynamicType
    {
        Static,
        Dynamic,
        DynamicInteractive,
    }

    [Serializable]
    public enum DynamicDetectionMode
    {
        DirectLLM,
        Agent,
    }

    public static class ObjectDynamicTypeExtensions
    {
        public static bool IsDynamic(this ObjectDynamicType type)
        {
            return type == ObjectDynamicType.Dynamic || type == ObjectDynamicType.DynamicInteractive;
        }
    }

    [Serializable]
    public class GObjectDynamicInfo
    {
        public string ObjectId;
        public string ObjectName;
        public ObjectDynamicType DynamicType;
        public ObjectDynamicType MarkedType;
    }

    [Serializable]
    public class ScriptDynamicInfo
    {
        public string ScriptPath;
        public DynamicInfo[] Dynamics;
    }

    [Serializable]
    public class DynamicInfo
    {
        public string Type;
        public ObjectDynamicType DynamicType;
        public string Name;


        // public string Description;
        // public string ExternMethods;
    }

    [Serializable]
    public class DynamicDetectionResult
    {
        public List<DynamicInfo> results;
        public int promptTokens;
        public int completionTokens;
    }

    [Serializable]
    public class DynamicDetectionContextScript
    {
        public string scriptPath;
        public string className;
        public string sourceHash;
        public string source;
        public List<string> fields = new();
        public bool isBuiltin;
    }

    [Serializable]
    public class DynamicDetectionAgentConfig
    {
        public int maxToolCalls = 6;
        public bool enableGlobalScriptSearch = true;
    }

    [Serializable]
    public class DynamicDetectionAgentRequest
    {
        public string sceneId;
        public List<DynamicDetectionContextScript> scripts = new();
        public List<string> targetScriptPaths = new();
        public List<string> components = new();
        public bool useCache = true;
        public DynamicDetectionAgentConfig agentConfig = new();
    }

    [Serializable]
    public class DynamicDetectionAgentScriptResult
    {
        public string scriptPath;
        public DynamicInfo[] dynamics;
        public List<string> warnings;
    }

    [Serializable]
    public class DynamicDetectionAgentResult
    {
        public List<DynamicDetectionAgentScriptResult> scripts = new();
        public int promptTokens;
        public int completionTokens;
        public int toolCalls;
    }
}
