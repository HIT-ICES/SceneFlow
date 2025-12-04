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

            DrawDefaultInspector();

            FindPlane myComponent = (FindPlane)target;

            if (GUILayout.Button("Find Plane"))
            {
                myComponent.DoFindPlane();
            }
        }
    }
}