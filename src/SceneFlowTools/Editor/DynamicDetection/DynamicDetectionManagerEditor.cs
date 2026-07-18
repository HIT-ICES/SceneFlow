using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.DynamicDetection;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace SceneFlowTools.Editor.DynamicDetection
{
    [CustomEditor(typeof(DynamicDetectionManager))]
    public class DynamicDetectionManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DynamicDetectionManager ddm = (DynamicDetectionManager)target;
            serializedObject.Update();
            GUIUtils.DataBakeClear(
                serializedObject.FindProperty(nameof(ddm.data)),
                Bake,
                Clear
            );
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ddm.detectionMode)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(ddm.propagateDynamicToChildren)));
            ddm.gizmosDynamicObjects = EditorGUILayout.Toggle("Gizmos Dynamic Objects", ddm.gizmosDynamicObjects);
            serializedObject.ApplyModifiedProperties();
        }

        public void Clear()
        {
            DynamicDetectionManager tg = (DynamicDetectionManager)target;
            if (tg.data is null) return;
            SceneUtils.DeleteSceneAsset(tg.gameObject.scene, "DynamicDetectionResults");
            tg.data = null;
            EditorUtility.SetDirty(tg);
            EditorSceneManager.MarkSceneDirty(tg.gameObject.scene);
        }

        public void Bake()
        {
            DynamicDetectionBaker.Bake((DynamicDetectionManager)target, new DynamicDetectionBakeOptions
            {
                PromptBeforeRun = true
            });
        }
    }
}
