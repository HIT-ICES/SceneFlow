using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Runtime
{
    [RequireComponent(typeof(MeshObjectList))]
    public class SceneDivision : MonoBehaviour
    {
        
        public GlobalDivisionParams globalDivisionParams;
        [NonSerialized]
        public bool regenerateSceneInfo = true;
        public GlobalDivisionResult globalDivisionResult;
        [NonSerialized]
        public bool showGlobalDivisionGizmos;
        [NonSerialized]
        public int showGlobalDivisionRegionIndex;


        
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
    }

    [Serializable]
    public class DbscanExtraParams
    {
        public float downsampleStep = 0.5f;
        public float eps = 2.0f; 
        public int minSamples = 10; 
        [Tooltip("KMeans")]
        public int numClusters = 4; 
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