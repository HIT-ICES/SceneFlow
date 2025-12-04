using SceneFlowTools.Runtime;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor
{
    [CustomEditor(typeof(FindPlane))]
    public class FindPlaneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 保留原有字段显示
            DrawDefaultInspector();
            // 获取目标对象
            FindPlane myComponent = (FindPlane)target;
            // 添加按钮
            if (GUILayout.Button("Find Plane"))
            {
                myComponent.DoFindPlane();
            }
        }
    }
}