using System;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor.Utils
{
    public static class GUIUtils
    {
        public static GUIIdentGroup Group(string label = null, float space = 15f)
        {
            return new GUIIdentGroup(label, space);
        }

        public static DataBakeClearResult DataBakeClear(SerializedProperty property)
        {
            DataBakeClearResult result = DataBakeClearResult.None;
            GUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, true);
            if (GUILayout.Button("Bake", GUILayout.ExpandWidth(false)))
            {
                result = DataBakeClearResult.Bake;
            }

            if (GUILayout.Button("Clear", GUILayout.ExpandWidth(false)))
            {
                result = DataBakeClearResult.Clear;
            }

            GUILayout.EndHorizontal();

            return result;
        }

        public static void DataBakeClear(SerializedProperty property, Action bakeAction, Action clearAction)
        {
            switch (DataBakeClear(property))
            {
                case DataBakeClearResult.Bake:
                    bakeAction?.Invoke();
                    break;
                case DataBakeClearResult.Clear:
                    clearAction?.Invoke();
                    break;
                case DataBakeClearResult.None:
                default:
                    break;
            }
        }

        public static int IntPicker(string label, int value, int min, int max, int defaultValue)
        {
            GUILayout.BeginHorizontal();

            value = EditorGUILayout.IntField(label, value);

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                value--;
            }

            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                value++;
            }

            if (GUILayout.Button("x", GUILayout.ExpandWidth(false)))
            {
                value = defaultValue;
            }

            GUILayout.EndHorizontal();

            value = Mathf.Clamp(value, min, max);

            return value;
        }
    }

    public enum DataBakeClearResult
    {
        None,
        Bake,
        Clear
    }

    public class GUIIdentGroup : IDisposable
    {
        public GUIIdentGroup(string label, float space)
        {
            if (label != null)
                GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            GUILayout.Space(space);
            GUILayout.BeginVertical();
        }

        public void Dispose()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
    }
}