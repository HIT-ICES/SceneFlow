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
            // Draw the default inspector fields.
            DrawDefaultInspector();
            // Get the target object.
            FindPlane myComponent = (FindPlane)target;
            // Add the action button.
            if (GUILayout.Button("Find Plane"))
            {
                myComponent.DoFindPlane();
            }
        }
    }
}
