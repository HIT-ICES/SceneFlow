using System.Collections.Generic;
using SceneFlowTools.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Editor
{
    [UnityEditor.CustomEditor(typeof(VoxelizeFindRoom))]
    public class VoxelizeFindRoomEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 保留原有字段显示
            DrawDefaultInspector();
            // 获取目标对象
            VoxelizeFindRoom myComponent = (VoxelizeFindRoom)target;
            // 添加按钮
            if (GUILayout.Button("Bake"))
            {
                List<MeshFilter> meshFilters = myComponent.CollectMeshes();
                VoxelizeResult result = VoxelizerCPU.Voxelize(myComponent.voxelizeOptions, meshFilters);
                SceneUtils.SaveSceneAsset(SceneManager.GetActiveScene(), "VoxelizeFindRoomData", result);
                Debug.Log("Voxelization complete. Result saved to scene asset.");
                myComponent.voxelizeResult = result;
            }

            if (GUILayout.Button("Clear"))
            {
                SceneUtils.DeleteSceneAsset(SceneManager.GetActiveScene(), "VoxelizeFindRoomData");
            }
            
            GUILayout.Label("Find Regions: " + (myComponent.voxelizeResult?.regions?.Count ?? 0));
            myComponent.showRegionIndex = EditorGUILayout.IntField("Show Region", myComponent.showRegionIndex);
        }
    }
}