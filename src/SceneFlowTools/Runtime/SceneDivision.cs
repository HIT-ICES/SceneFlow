using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Runtime
{
    [RequireComponent(typeof(MeshObjectList))]
    public class SceneDivision : MonoBehaviour
    {
        // 第一步，进行全局的场景划分
        public GlobalDivisionParams globalDivisionParams;
        [NonSerialized]
        public bool regenerateSceneInfo = true;
        public GlobalDivisionResult globalDivisionResult;
        [NonSerialized]
        public bool showGlobalDivisionGizmos;
        [NonSerialized]
        public int showGlobalDivisionRegionIndex;


        // 第二步，进行包围结构的划分
        public VoxelizeOptions roomDivisionParams;
        public VoxelizeResult roomDivisionResult;
        [NonSerialized]
        public bool showRoomDivisionGizmos;
        [NonSerialized]
        public int showRoomDivisionRegionIndex = -1;


        public MeshObjectList GetMeshObjectList()
        {
            return GetComponent<MeshObjectList>();
        }

        private void DrawRoomGizmos()
        {
            if (roomDivisionResult == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(roomDivisionResult.options.worldBounds.center,
                roomDivisionResult.options.worldBounds.size);
            if (showRoomDivisionRegionIndex >= 0 && showRoomDivisionRegionIndex < roomDivisionResult.regions.Count)
            {
                foreach (int oid in roomDivisionResult.regions[showRoomDivisionRegionIndex].objects)
                {
                    string name = roomDivisionResult.objectNames[oid];
                    MeshFilter meshFilter = GetMeshObjectList().meshObjects[oid];
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireMesh(meshFilter.sharedMesh, meshFilter.transform.position,
                        meshFilter.transform.rotation, meshFilter.transform.lossyScale);
                }
            }
        }

        private void DrawGlobalGizmos()
        {
            if (globalDivisionResult == null) return;
            if (showGlobalDivisionRegionIndex >= 0 && showGlobalDivisionRegionIndex < globalDivisionResult.groups.Count)
            {
                foreach (int oid in globalDivisionResult.groups[showGlobalDivisionRegionIndex])
                {
                    MeshFilter meshFilter = GetMeshObjectList().meshObjects[oid];
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireMesh(meshFilter.sharedMesh, meshFilter.transform.position,
                        meshFilter.transform.rotation, meshFilter.transform.lossyScale);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (showRoomDivisionGizmos)
            {
                DrawRoomGizmos();
            }

            if (showGlobalDivisionGizmos)
            {
                DrawGlobalGizmos();
            }
        }
    }

    [Serializable]
    public class GlobalDivisionParams
    {
        public GlobalDivisionMethod method = GlobalDivisionMethod.Dbscan;
        public DbscanExtraParams dbscanExtraParams = new DbscanExtraParams();

        public Dictionary<string, object> ToRequestExtra()
        {
            return dbscanExtraParams.ToRequestExtra();
        }
    }

    [Serializable]
    public class DbscanExtraParams
    {
        public float downsampleStep = 0.5f;
        public float eps = 2.0f; // 邻域半径
        public int minSamples = 10; // 最小点数
        [Tooltip("Let SceneFlowService select DBSCAN eps and min_samples automatically.")]
        public bool autoCalibration;
        [Tooltip("KMeans使用")]
        public int numClusters = 4; // 簇数量

        public Dictionary<string, object> ToRequestExtra()
        {
            var extra = new Dictionary<string, object>
            {
                { "downsample_step", downsampleStep },
                { "num_clusters", numClusters }
            };

            if (autoCalibration)
            {
                extra["auto_calibration"] = true;
                return extra;
            }

            extra["eps"] = eps;
            extra["min_samples"] = minSamples;
            return extra;
        }
    }

    [Serializable]
    public enum GlobalDivisionMethod
    {
        Dbscan,
        VoxelDbscan,
        KMeans,
        VoxelKMeans,
        VoxelGrid,
    }
}
