using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    [CreateAssetMenu(menuName = "Scene Division/Voxelize Result")]
    public class VoxelizeResult : ScriptableObject, ISerializationCallbackReceiver
    {
        [HideInInspector] public List<string> objectNames;
        [HideInInspector] public VoxelizeOptions options;

        [HideInInspector] public Vector3Int gridSize;

        [NonSerialized] public VoxelGrid[] voxelGrids;
        [HideInInspector] public List<Region> regions;

        [SerializeField] private byte[] voxelGridsRawData;

        public VoxelGrid GetGird(Vector3Int index)
        {
            return voxelGrids[GetGridIndex(index)];
        }

        public void SetGird(Vector3Int index, VoxelGrid grid)
        {
            voxelGrids[GetGridIndex(index)] = grid;
        }

        public int GetVoxel(Vector3Int index)
        {
            (Vector3Int gridIndex, Vector3Int indexInGrid) = GetVoxelIndex(index);
            VoxelGrid grid = GetGird(gridIndex);
            if (grid == null)
            {
                return -1;
            }
            return grid[indexInGrid];
        }
        
        public void SetVoxel(Vector3Int index, int value)
        {
            (Vector3Int gridIndex, Vector3Int indexInGrid) = GetVoxelIndex(index);
            VoxelGrid grid = GetGird(gridIndex);
            if (grid == null)
            {
                grid = new VoxelGrid(options.girdSize);
                SetGird(gridIndex, grid);
            }
            grid[indexInGrid] = value;
        }

        public bool CheckGridIndex(Vector3Int index)
        {
            return index.x >= 0 && index.y >= 0 && index.z >= 0 && index.x < gridSize.x && index.y < gridSize.y &&
                   index.z < gridSize.z;
        }
        
        public bool CheckVoxelIndex(Vector3Int index)
        {
            Vector3Int size = options.bounds.size;
            return index.x >= 0 && index.y >= 0 && index.z >= 0 && index.x < size.x && index.y < size.y &&
                   index.z < size.z;
        }

        private int GetGridIndex(Vector3Int index)
        {
            if (!CheckGridIndex(index))
            {
                throw new IndexOutOfRangeException("Voxel index out of bounds");
            }

            return index.x + index.y * gridSize.x + index.z * gridSize.x * gridSize.y;
        }
        
        public (Vector3Int girdIndex, Vector3Int indexInGrid) GetVoxelIndex(Vector3Int index)
        {
            int girdSize = options.girdSize;
            Vector3Int girdIndex =
                new Vector3Int(
                    index.x / girdSize,
                    index.y / girdSize,
                    index.z / girdSize
                );
            Vector3Int indexInGrid = new Vector3Int(
                index.x % girdSize,
                index.y % girdSize,
                index.z % girdSize
            );
            return (girdIndex, indexInGrid);
        }

        public void OnBeforeSerialize()
        {
            MemoryStream s = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(s))
            {
                for (int i = 0; i < voxelGrids.Length; i++)
                {
                    VoxelGrid.Write(writer, voxelGrids[i]);
                }
            }
            voxelGridsRawData = s.ToArray();
        }

        public void OnAfterDeserialize()
        {
            if (voxelGridsRawData == null || voxelGridsRawData.Length == 0)
            {
                throw new InvalidOperationException("Voxel grids array is null or empty");
            }
            
            MemoryStream s = new MemoryStream(voxelGridsRawData);
            using (BinaryReader reader = new BinaryReader(s))
            {
                voxelGrids = new VoxelGrid[gridSize.x * gridSize.y * gridSize.z];
                for (int i = 0; i < voxelGrids.Length; i++)
                {
                    voxelGrids[i] = VoxelGrid.Read(reader);
                }
            }
            
            voxelGridsRawData = null;
        }

        
    }
}