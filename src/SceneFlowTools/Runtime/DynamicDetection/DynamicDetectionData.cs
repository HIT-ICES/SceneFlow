using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicDetection
{
    [Serializable]
    public class DynamicDetectionData : ScriptableObject
    {
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
}