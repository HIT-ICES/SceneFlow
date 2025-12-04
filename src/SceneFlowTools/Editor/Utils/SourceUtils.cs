using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

namespace SceneFlowTools.Editor.Utils
{
    public class SourceUtils
    {
        private static Dictionary<Type, MonoScript> monoScriptDict;

        public static MonoScript GetMonoScriptByType(Type type)
        {
            if (monoScriptDict == null)
            {
                monoScriptDict = new Dictionary<Type, MonoScript>();
                string[] guids = AssetDatabase.FindAssets("t:MonoScript");
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    var candidate = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (candidate == null) continue;
                    // Debug.Log($"Script [{path}], class: [{candidate.GetClass()}]");
                    var cls = candidate.GetClass();
                    if (cls == null) continue;
                    monoScriptDict.TryAdd(cls, candidate);
                }
            }

            monoScriptDict.TryGetValue(type, out var script);
            return script;
        }

        public static string GetScriptSourceOrFieldsByType(Type type)
        {
            var script = GetMonoScriptByType(type);
            if (script != null)
            {
                string path = AssetDatabase.GetAssetPath(script);
                string text = System.IO.File.ReadAllText(path);
                if (text.Contains("[DeleteBeforeDetect]"))
                {
                    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                    List<string> filteredLines = lines.Where(line => !line.Contains("[DeleteBeforeDetect]")).ToList();
                    text = string.Join("\r\n", filteredLines);
                }

                return text;
            }

            List<String> fields = GetSerializeFields(type);
            return $"Unity Builtin Script: {type.FullName}\n"
                   + "Serialize Fields:" + string.Join(", ", fields);
        }

        public static string GetScriptPathOrNameByType(Type type)
        {
            var script = GetMonoScriptByType(type);
            if (script != null) return AssetDatabase.GetAssetPath(script);
            return type.FullName;
        }

        public static List<String> GetSerializeFields(Type type)
        {
            var fields = new List<String>();
            var allFields = type.GetFields(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            foreach (var field in allFields)
            {
                if (field.IsPublic || Attribute.IsDefined(field, typeof(SerializeField)))
                {
                    fields.Add(field.Name);
                }
            }

            return fields;
        }
    }
}