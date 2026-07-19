using SceneFlowTools.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SceneFlowTools.Editor
{
    [CustomEditor(typeof(MeshObjectList))]
    public class MeshObjectListEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector fields.
            DrawDefaultInspector();

            // EditorUtility.DisplayProgressBar();

            var myTarget = (MeshObjectList)target;

            GUILayout.Label("MeshObjects: " + myTarget.meshObjects.Count);
            if (myTarget.isOutdated)
            {
                GUILayout.Label("MeshObjects are outdated. Should update.");
            }
            if (GUILayout.Button("Update"))
            {
                myTarget.UpdateMeshObjects();
                EditorUtility.SetDirty(myTarget);
                EditorSceneManager.MarkSceneDirty(myTarget.gameObject.scene);
            }
        }
    }
}
