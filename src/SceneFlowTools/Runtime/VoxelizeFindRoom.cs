using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    [ExecuteAlways]
    public class VoxelizeFindRoom : MonoBehaviour
    {
        public VoxelizeOptions voxelizeOptions;
        public VoxelizeResult voxelizeResult;
        public MeshObjectList meshObjectList;
        [NonSerialized] public int showRegionIndex = -1; 
        [NonSerialized] public Dictionary<string, MeshFilter> meshFilters = new();
        
        private void OnEnable()
        {
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
#endif
            UpdateMeshes();
        }

        private void OnHierarchyChanged()
        {
            UpdateMeshes();
        }

        private void UpdateMeshes()
        {
            List<MeshFilter> newMeshFilters = CollectMeshes();
            meshFilters.Clear();
            foreach (var m in newMeshFilters)
            {
                meshFilters[SceneUtils.GetPathInScene(m.transform)] = m;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (voxelizeResult == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(voxelizeResult.options.worldBounds.center, voxelizeResult.options.worldBounds.size);
            if (showRegionIndex >= 0 && showRegionIndex < voxelizeResult.regions.Count)
            {
                foreach (int oid in voxelizeResult.regions[showRegionIndex].objects)
                {
                    string name = voxelizeResult.objectNames[oid];
                    if (!meshFilters.TryGetValue(name, out MeshFilter meshFilter))
                    {
                        Debug.LogWarning($"MeshFilter for {name} not found in meshFilters dictionary.");
                        continue;
                    }

                    Gizmos.color = Color.red;
                    Gizmos.DrawWireMesh(meshFilter.sharedMesh, meshFilter.transform.position,
                        meshFilter.transform.rotation, meshFilter.transform.lossyScale);
                }
            }
        }

        public List<MeshFilter> CollectMeshes()
        {
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            
            foreach (var meshFilter in FindObjectsOfType<MeshFilter>())
            {
                if (meshFilter.gameObject.activeInHierarchy && meshFilter.sharedMesh != null)
                {
                    meshFilters.Add(meshFilter);
                }
            }

            Debug.Log($"bake: {meshFilters.Count} mesh filters found.");
            return meshFilters;
        }
    }
}