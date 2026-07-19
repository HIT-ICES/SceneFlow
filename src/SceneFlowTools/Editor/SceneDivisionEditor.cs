using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SceneFlowTools.Editor
{
    [CustomEditor(typeof(SceneDivision))]
    public class SceneDivisionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector fields.
            // DrawDefaultInspector();
            serializedObject.Update();
            InspectorGUIGlobalDivision();
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
            InspectorGUIRoomDivision();
            serializedObject.ApplyModifiedProperties();

            // if (GUILayout.Button("test"))
            // {
            //     Progress.Start()
            //     // EditorUtility.DisplayProgressBar("title", "info", 0.5f);
            //     EditorUtility.ClearProgressBar();
            // }
        }

        private void InspectorGUIGlobalDivision()
        {
            SceneDivision sd = (SceneDivision)target;
            using var _ = GUIUtils.Group("Global Division:");
            DrawGlobalDivisionParams(serializedObject.FindProperty(nameof(sd.globalDivisionParams)));
            sd.regenerateSceneInfo = EditorGUILayout.Toggle("Regenerate SceneInfo", sd.regenerateSceneInfo);
            GUIUtils.DataBakeClear(serializedObject.FindProperty(nameof(sd.globalDivisionResult)), GlobalDivisionBake,
                GlobalDivisionClear);
            sd.showGlobalDivisionGizmos = EditorGUILayout.Toggle("Gizmos", sd.showGlobalDivisionGizmos);
            GUILayout.Label("Find Regions: " + (sd.globalDivisionResult?.groups?.Count ?? 0));
            int maxIndex = sd.globalDivisionResult?.groups?.Count - 1 ?? -1;
            sd.showGlobalDivisionRegionIndex =
                GUIUtils.IntPicker("Show Region", sd.showGlobalDivisionRegionIndex, -1, maxIndex, -1);
        }

        private static void DrawGlobalDivisionParams(SerializedProperty property)
        {
            SerializedProperty methodProperty = property.FindPropertyRelative(nameof(GlobalDivisionParams.method));
            SerializedProperty extraProperty =
                property.FindPropertyRelative(nameof(GlobalDivisionParams.dbscanExtraParams));

            EditorGUILayout.PropertyField(methodProperty);
            EditorGUILayout.PropertyField(extraProperty.FindPropertyRelative(nameof(DbscanExtraParams.downsampleStep)));

            GlobalDivisionMethod method = (GlobalDivisionMethod)methodProperty.enumValueIndex;
            if (IsDbscanMethod(method))
            {
                DrawDbscanParams(extraProperty);
                return;
            }

            if (IsKMeansMethod(method))
            {
                EditorGUILayout.PropertyField(
                    extraProperty.FindPropertyRelative(nameof(DbscanExtraParams.numClusters)));
            }
        }

        private static void DrawDbscanParams(SerializedProperty extraProperty)
        {
            SerializedProperty autoCalibrationProperty =
                extraProperty.FindPropertyRelative(nameof(DbscanExtraParams.autoCalibration));

            EditorGUILayout.PropertyField(autoCalibrationProperty, new GUIContent("Auto DBSCAN Calibration"));
            if (autoCalibrationProperty.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "SceneFlowService will select eps and minSamples with its default calibration strategy. Manual DBSCAN values are ignored while auto calibration is enabled.",
                    MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(autoCalibrationProperty.boolValue))
            {
                EditorGUILayout.PropertyField(extraProperty.FindPropertyRelative(nameof(DbscanExtraParams.eps)));
                EditorGUILayout.PropertyField(extraProperty.FindPropertyRelative(nameof(DbscanExtraParams.minSamples)));
            }
        }

        private static bool IsDbscanMethod(GlobalDivisionMethod method)
        {
            return method == GlobalDivisionMethod.Dbscan || method == GlobalDivisionMethod.VoxelDbscan;
        }

        private static bool IsKMeansMethod(GlobalDivisionMethod method)
        {
            return method == GlobalDivisionMethod.KMeans || method == GlobalDivisionMethod.VoxelKMeans;
        }

        private void GlobalDivisionBake()
        {
            SceneDivision sd = (SceneDivision)target;
            double time = EditorApplication.timeSinceStartup;

            Debug.Log("Global division bake start...");
            string infoPath;
            if (sd.regenerateSceneInfo)
            {
                double t = EditorApplication.timeSinceStartup;
                Debug.Log("Global division: collecting mesh objects...");
                var objList = sd.GetMeshObjectList().meshObjects
                    .Select(v => GameObjectInfoCollector.GetInfo(v.gameObject)).ToList();
                GameObjectInfoCollector.RegenerateIds(objList);

                Debug.Log($"Global division: collected [{objList.Count}] mesh objects.");
                infoPath = GameObjectInfoCollector.Save(sd.gameObject.scene, objList);
                Debug.Log(
                    $"Global division: saved mesh object info to [{infoPath}], time_cost={EditorApplication.timeSinceStartup - t:F2}");
            }
            else
            {
                infoPath = GameObjectInfoCollector.GetSavePath(sd.gameObject.scene);
                Debug.Log("Global division: using existing mesh object info at [" + infoPath + "]");
            }

            Debug.Log("Global division: call external scene division...");
            string path = ExternalUtils.AssetPathToFullPath(infoPath);
            string content = System.IO.File.ReadAllText(path);
            var rawResult = ExternalUtils.SceneDivisionWithContent(
                content,
                sd.globalDivisionParams.method.ToString(),
                sd.globalDivisionParams.ToRequestExtra()
            ).Result;
            if (rawResult == null) return;

            var result = rawResult.Select(x => x.Select(int.Parse).ToList()).ToList();
            Debug.Log($"Global division find [{result.Count}] groups");

            sd.globalDivisionResult = ScriptableObject.CreateInstance<GlobalDivisionResult>();
            sd.globalDivisionResult.groups = result;
            SceneUtils.SaveSceneAsset(sd.gameObject.scene, "GlobalDivisionData",
                sd.globalDivisionResult);
            EditorUtility.SetDirty(sd);
            EditorSceneManager.MarkSceneDirty(sd.gameObject.scene);
            Debug.Log(
                $"Global division bake complete, time_cost = {EditorApplication.timeSinceStartup - time:F2} seconds.");
        }

        private void GlobalDivisionClear()
        {
            SceneDivision sd = (SceneDivision)target;
            sd.globalDivisionResult = null;
            SceneUtils.DeleteSceneAsset(sd.gameObject.scene, "GlobalDivisionData");
            EditorUtility.SetDirty(sd);
            EditorSceneManager.MarkSceneDirty(sd.gameObject.scene);
        }


        private void InspectorGUIRoomDivision()
        {
            SceneDivision sd = (SceneDivision)target;
            using var _ = GUIUtils.Group("Room Division:");
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(sd.roomDivisionParams)), true);
            GUIUtils.DataBakeClear(serializedObject.FindProperty(nameof(sd.roomDivisionResult)), RoomDivisionBake,
                RoomDivisionClear);
            sd.showRoomDivisionGizmos = EditorGUILayout.Toggle("Gizmos", sd.showRoomDivisionGizmos);
            GUILayout.Label("Find Regions: " + (sd.roomDivisionResult?.regions?.Count ?? 0));
            int maxIndex = sd.roomDivisionResult?.regions?.Count - 1 ?? -1;
            sd.showRoomDivisionRegionIndex =
                GUIUtils.IntPicker("Show Region", sd.showRoomDivisionRegionIndex, -1, maxIndex, -1);
        }

        private void RoomDivisionBake()
        {
            double time = EditorApplication.timeSinceStartup;
            SceneDivision sd = (SceneDivision)target;
            sd.roomDivisionResult =
                VoxelizerCPU.Voxelize(sd.roomDivisionParams, sd.GetMeshObjectList().meshObjects);
            for (int i = 0; i < sd.roomDivisionResult.regions.Count; i++)
            {
                var x = sd.roomDivisionResult.regions[i];
                var xobjs = new HashSet<int>(x.objects);
                for (int j = 0; j < i; j++)
                {
                    var y = sd.roomDivisionResult.regions[j];
                    if (y == null) continue;
                    var yobjs = new HashSet<int>(y.objects);
                    if (xobjs.SetEquals(yobjs))
                    {
                        sd.roomDivisionResult.regions[j] = null;
                    }
                }
            }

            sd.roomDivisionResult.regions = sd.roomDivisionResult.regions.Where(r => r != null).ToList();
            sd.roomDivisionResult.regions.Sort((a, b) => b.objects.Count - a.objects.Count);
            SceneUtils.SaveSceneAsset(sd.gameObject.scene, "VoxelizeFindRoomData",
                sd.roomDivisionResult);
            EditorUtility.SetDirty(sd);
            EditorSceneManager.MarkSceneDirty(sd.gameObject.scene);
            Debug.Log($"Voxelization complete, time_cost = {EditorApplication.timeSinceStartup - time:F2} seconds.");
        }

        private void RoomDivisionClear()
        {
            SceneDivision sd = (SceneDivision)target;
            sd.roomDivisionResult = null;
            SceneUtils.DeleteSceneAsset(sd.gameObject.scene, "VoxelizeFindRoomData");
            EditorUtility.SetDirty(sd);
            EditorSceneManager.MarkSceneDirty(sd.gameObject.scene);
        }
    }
}
