using System.Linq;
using SceneFlowTools.Runtime;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor
{
    [CustomEditor(typeof(MetaInfoManager))]
    public class MetaInfoManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            MetaInfoManager manager = (MetaInfoManager)target;

            EditorGUILayout.Space();
            DrawValidationReport(manager.lastValidationReport);

            EditorGUILayout.Space();
            if (GUILayout.Button("Scan ID Status"))
            {
                manager.ValidateNow("Inspector Scan");
                Repaint();
            }

            if (GUILayout.Button("Generate IDs For Objects Without MetaInfo"))
            {
                int changed = MetaInfoManager.GenerateIdsForMissingMetaInfo();
                Debug.Log($"MetaInfoManager: generated MetaInfo IDs for {changed} objects without MetaInfo.");
            }

            if (GUILayout.Button("Generate IDs For Empty MetaInfo"))
            {
                int changed = MetaInfoManager.GenerateIdsForEmptyMetaInfo();
                Debug.Log($"MetaInfoManager: generated IDs for {changed} empty MetaInfo components.");
            }

            if (GUILayout.Button("Regenerate Duplicate IDs"))
            {
                int changed = MetaInfoManager.RegenerateDuplicateIds();
                Debug.Log($"MetaInfoManager: regenerated {changed} duplicate MetaInfo IDs.");
            }

            if (GUILayout.Button("Regenerate All IDs"))
            {
                int changed = MetaInfoManager.RegenerateAllIds();
                Debug.Log($"MetaInfoManager: regenerated {changed} MetaInfo IDs.");
            }
        }

        private static void DrawValidationReport(MetaInfoValidationReport report)
        {
            if (report == null || string.IsNullOrEmpty(report.lastValidationTime))
            {
                EditorGUILayout.HelpBox("No MetaInfo scan has run yet.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Last Scan Time", report.lastValidationTime);
            EditorGUILayout.LabelField("Last Scan Reason", report.lastValidationReason ?? "");
            EditorGUILayout.LabelField("Scanned Objects", report.scannedObjectCount.ToString());
            EditorGUILayout.LabelField("Objects With MetaInfo", report.metaInfoCount.ToString());
            EditorGUILayout.LabelField("Objects Without MetaInfo", report.missingMetaInfoCount.ToString());
            EditorGUILayout.LabelField("Empty IDs", report.emptyIdCount.ToString());
            EditorGUILayout.LabelField("Duplicate ID Groups", report.duplicateGroupCount.ToString());
            EditorGUILayout.LabelField("Duplicate Objects To Fix", report.duplicateObjectCount.ToString());

            if (report.emptyIdCount > 0 || report.duplicateGroupCount > 0)
            {
                EditorGUILayout.HelpBox("MetaInfo IDs need repair before regenerating SceneFlow configs.",
                    MessageType.Warning);
            }

            foreach (MetaInfoDuplicateGroup group in report.duplicateGroups.Take(5))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Duplicate: {group.id}");
                foreach (GameObject obj in group.objects.Take(5))
                {
                    EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
                }
            }
        }
    }
}
