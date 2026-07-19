using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace SceneFlowTools.Runtime
{
    [RequireComponent(typeof(MeshObjectList))]
    public class Voxelizer : MonoBehaviour
    {
        public VoxelizeOptions options;
        public VoxelizeResult result;
    }


    [Serializable]
    public class VoxelizeOptions
    {
        public float voxelSize = 0.2f; // Voxel size in world units.

        public int girdSize = 10; // Storage grid size in voxels.

        public float roomSizeThreshold = 5f; // Room-size threshold in world units.

        public BoundsInt bounds;

        public VoxelizeOptions Clone()
        {
            return MemberwiseClone() as VoxelizeOptions;
        }

        public Bounds worldBounds
        {
            get
            {
                Vector3 min = VoxelizerCPU.VoxelToWorldPos(voxelSize, bounds.min);
                Vector3 max = VoxelizerCPU.VoxelToWorldPos(voxelSize, bounds.max);
                return new Bounds((min + max) * 0.5f, max - min);
            }
        }
    }

    [Serializable]
    public class VoxelGrid
    {
        [SerializeField] private int size;
        [SerializeField] private int[] voxelObjectIndex;

        public VoxelGrid(int size)
        {
            this.size = size;
            voxelObjectIndex = new int[size * size * size];
            for (int i = 0; i < voxelObjectIndex.Length; i++)
            {
                voxelObjectIndex[i] = -1; // -1 means unoccupied.
            }
        }

        public int this[Vector3Int index]
        {
            get => voxelObjectIndex[GetIndex(index)];
            set => voxelObjectIndex[GetIndex(index)] = value;
        }

        private int GetIndex(Vector3Int index)
        {
            if (index.x < 0 || index.y < 0 || index.z < 0 || index.x >= size || index.y >= size || index.z >= size)
            {
                throw new IndexOutOfRangeException("Voxel index out of bounds");
            }

            return index.x + index.y * size + index.z * size * size;
        }

        public static VoxelGrid Read(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            int size = reader.ReadInt32();
            VoxelGrid grid = new VoxelGrid(size);
            for (int i = 0; i < size * size * size; i++)
            {
                grid.voxelObjectIndex[i] = reader.ReadInt32();
            }

            return grid;
        }

        public static void Write(BinaryWriter writer, VoxelGrid grid)
        {
            if (grid == null)
            {
                writer.Write(false);
                return;
            }

            writer.Write(true);
            writer.Write(grid.size);
            for (int i = 0; i < grid.size * grid.size * grid.size; i++)
            {
                writer.Write(grid.voxelObjectIndex[i]);
            }
        }
    }

    // [Serializable]
    // public struct Voxel
    // {
    //     public int objectIndex; // Index of the occupying object; -1 means unoccupied.
    //     public int regionId; // ID of the containing region.
    // }

    [Serializable]
    public class Region
    {
        public int id; // Region ID.
        public int sizeInVoxels;
        public List<int> objects; // Indices of objects in this region.
        public Vector3Int startVoxel; // Starting voxel coordinate of the region.
    }

    public static class VoxelizerCPU
    {
        public static VoxelizeResult Voxelize(VoxelizeOptions options, List<MeshFilter> meshes)
        {
            Debug.Log(
                $"Voxelize: {meshes.Count} meshes, voxel size: {options.voxelSize}, grid size: {options.girdSize}, room size threshold: {options.roomSizeThreshold}");
            options = options.Clone();

            VoxelizeResult result = ScriptableObject.CreateInstance<VoxelizeResult>();
            result.objectNames = meshes.Select(m => SceneUtils.GetPathInScene(m.transform)).ToList();
            result.options = options;

            // Vector3 minMeshBounds = VoxelToWorldPos(options.voxelSize, options.minBounds);
            // Vector3 maxMeshBounds = VoxelToWorldPos(options.voxelSize, options.minBounds);
            Vector3 minMeshBounds = Vector3.positiveInfinity;
            Vector3 maxMeshBounds = Vector3.negativeInfinity;
            foreach (MeshFilter mesh in meshes)
            {
                if (!mesh.TryGetComponent(out MeshRenderer r))
                {
                    Debug.LogError($"Mesh {mesh.name} is missing a MeshRenderer component");
                }

                minMeshBounds = Vector3.Min(minMeshBounds, r.bounds.min);
                maxMeshBounds = Vector3.Max(maxMeshBounds, r.bounds.max);
            }

            Debug.Log($"Mesh bounds: min {minMeshBounds}, max {maxMeshBounds}");

            options.bounds.min = WorldPosToVoxel(options.voxelSize, minMeshBounds);
            options.bounds.max = WorldPosToVoxel(options.voxelSize, maxMeshBounds);

            result.gridSize = options.bounds.size / options.girdSize + Vector3Int.one;
            Debug.Log($"Voxelization bounds: {options.bounds.min} to {options.bounds.max}, size: {result.gridSize}");
            result.voxelGrids = new VoxelGrid[result.gridSize.x * result.gridSize.y * result.gridSize.z];

            for (var mIndex = 0; mIndex < meshes.Count; mIndex++)
            {
                var mesh = meshes[mIndex];
                Vector3[] vertices = mesh.sharedMesh.vertices;
                int[] triangles = mesh.sharedMesh.triangles;

                // Transform vertices from local space to world space.
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = mesh.transform.TransformPoint(vertices[i]);
                }

                // Voxelize the triangle.
                List<Vector3Int> occupiedVoxels = new List<Vector3Int>();
                Parallel.For(0, triangles.Length / 3, i =>
                {
                    Vector3 v0 = vertices[triangles[i * 3]];
                    Vector3 v1 = vertices[triangles[i * 3 + 1]];
                    Vector3 v2 = vertices[triangles[i * 3 + 2]];
                    var voxels = VoxelizeTriangle(options.voxelSize, v0, v1, v2);
                    lock (occupiedVoxels)
                    {
                        occupiedVoxels.AddRange(voxels);
                    }
                });

                // Map occupied voxels into the voxel grid.
                foreach (Vector3Int voxel in occupiedVoxels)
                {
                    Vector3Int index = voxel - options.bounds.min; // Convert to a voxel index.
                    result.SetVoxel(index, mIndex);
                }
            }

            FindRoomsBasic(result);

            // FindRoomsMultiThread(result);


            return result;
        }

        public static void FindRoomsBasic(VoxelizeResult r)
        {
            Vector3Int size = r.options.bounds.size;
            BitArray visit = new BitArray(size.x * size.y * size.z);
            BitArray girdVisit = new BitArray(r.gridSize.x * r.gridSize.y * r.gridSize.z);
            Debug.Log($"total: {visit.Count} voxels to visit");
            Debug.Log($"needs {visit.Count / 8.0 / 1024.0 / 1024.0} MB memory for visit array");
            r.regions = new List<Region>();
            // Reuse this queue across searches.
            Queue<Vector3Int> queue = new();
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int index = new Vector3Int(x, y, z);
                        if (visit[MyMathUtils.GetIndex(size, index)]) continue;
                        if (r.GetVoxel(index) != -1) continue;
                        // Start a region search from an unvisited voxel not occupied by a triangle.

                        Region region = new Region
                        {
                            id = r.regions.Count,
                            objects = new List<int>(),
                            startVoxel = index
                        };

                        HashSet<int> objectsInRegion = new HashSet<int>();
                        // Traverse the region with depth-first search.
                        queue.Clear();
                        queue.Enqueue(index);
                        visit[MyMathUtils.GetIndex(size, index)] = true;
                        region.sizeInVoxels++;
                        // Ignore regions connected to the outer boundary.
                        // bool flagIsOuter = false;
                        while (queue.Count > 0)
                        {
                            Vector3Int current = queue.Dequeue();

                            // Mark the voxel as visited.
                            // visit[MyMathUtils.GetIndex(size, current)] = true;
                            if (!visit[MyMathUtils.GetIndex(size, current)])
                            {
                                Debug.LogError("Voxel not marked: " + current);
                                return;
                            }

                            // region.sizeInVoxels++;
                            Vector3Int currentGirdIndex = r.GetVoxelIndex(current).girdIndex;
                            if (r.GetGird(currentGirdIndex) == null &&
                                !girdVisit[MyMathUtils.GetIndex(r.gridSize, currentGirdIndex)])
                            {
                                girdVisit[MyMathUtils.GetIndex(r.gridSize, currentGirdIndex)] = true;
                                // The current grid cell is completely empty.
                                // Mark its interior as visited, leaving the surface layer for the regular search.
                                Vector3Int girdMin = r.GetVoxelIndex(current).girdIndex * r.options.girdSize;
                                Vector3Int girdMax = Vector3Int.Min(girdMin + Vector3Int.one * r.options.girdSize,
                                    size);
                                for (int gx = girdMin.x+1; gx < girdMax.x -1; gx++)
                                {
                                    for (int gy = girdMin.y+1; gy < girdMax.y -1; gy++)
                                    {
                                        for (int gz = girdMin.z+1; gz < girdMax.z -1; gz++)
                                        {
                                            Vector3Int gVoxel = new Vector3Int(gx, gy, gz);
                                            if (visit[MyMathUtils.GetIndex(size, gVoxel)]) continue;
                                            visit[MyMathUtils.GetIndex(size, gVoxel)] = true;
                                            region.sizeInVoxels++;
                                        }
                                    }
                                }
                            }

                            // Examine adjacent voxels.
                            foreach (var dir in MyMathUtils.Directions6)
                            {
                                Vector3Int neighbor = current + dir;
                                if (!r.CheckVoxelIndex(neighbor)) continue;
                                if (visit[MyMathUtils.GetIndex(size, neighbor)]) continue;
                                int neighborVoxelObjIdx = r.GetVoxel(neighbor);
                                // Collect indices of objects on the boundary.
                                if (neighborVoxelObjIdx != -1)
                                {
                                    objectsInRegion.Add(neighborVoxelObjIdx);
                                    continue;
                                }

                                // Push unvisited voxels that are not walls onto the stack.
                                queue.Enqueue(neighbor);
                                visit[MyMathUtils.GetIndex(size, neighbor)] = true;
                                region.sizeInVoxels++;
                            }
                        }

                        region.objects = objectsInRegion.ToList();

                        if (region.sizeInVoxels * Math.Pow(r.options.voxelSize, 3) >=
                            r.options.roomSizeThreshold)
                        {
                            r.regions.Add(region);
                        }
                    }
                }
            }

            r.regions.Sort((a, b) => b.sizeInVoxels.CompareTo(a.sizeInVoxels));
            for (int i = 0; i < r.regions.Count; i++)
            {
                r.regions[i].id = i; // Assign the region ID.
            }
        }

        public static void FindRoomsMultiThread(VoxelizeResult r)
        {
            int threadCount = Math.Min(Environment.ProcessorCount / 2, r.gridSize.z);
            int minGridLength = r.gridSize.z / threadCount;
            Debug.Log($"FindRoomsMultiThread: Using {threadCount} threads, min grid length: {minGridLength}");
            SliceRegionResult x = _FindRoomsMultiThread(r, minGridLength, 0, r.gridSize.z);
            r.regions = x.regions.Where(v => v.sizeInVoxels * Math.Pow(r.options.voxelSize, 3) >=
                                             r.options.roomSizeThreshold).ToList();
            r.regions.Sort((a, b) => b.sizeInVoxels.CompareTo(a.sizeInVoxels));
            for (int i = 0; i < r.regions.Count; i++)
            {
                r.regions[i].id = i; // Assign the region ID.
            }
        }

        private static SliceRegionResult _FindRoomsMultiThread(VoxelizeResult r,
            int minGridLength,
            int gridStart,
            int gridLength)
        {
            if (gridLength <= minGridLength)
            {
                return FindRoomsSlice(r, gridStart * r.options.girdSize, gridLength * r.options.girdSize);
            }

            int leftLength = gridLength / 2;

            Task<SliceRegionResult> leftTask =
                Task.Run(() => _FindRoomsMultiThread(r, minGridLength, gridStart, leftLength));

            Task<SliceRegionResult> rightTask = Task.Run(() => _FindRoomsMultiThread(r, minGridLength,
                gridStart + leftLength, gridLength - leftLength));

            SliceRegionResult leftResult = leftTask.Result;
            SliceRegionResult rightResult = rightTask.Result;

            return CombineSlice(r, leftResult, rightResult);
        }

        public class SliceRegionResult
        {
            public int start, length;
            public HashSet<Region> regions;
            public Region[] positiveFace;
            public Region[] negativeFace;
            public int[] positiveFaceObjectId;
            public int[] negativeFaceObjectId;

            public SliceRegionResult()
            {
            }

            public SliceRegionResult(int start, int length, int faceSize)
            {
                regions = new();
                positiveFace = new Region[faceSize];
                negativeFace = new Region[faceSize];
                positiveFaceObjectId = new int[faceSize];
                negativeFaceObjectId = new int[faceSize];
                for (int i = 0; i < faceSize; i++)
                {
                    positiveFaceObjectId[i] = -1;
                    negativeFaceObjectId[i] = -1;
                }
            }
        }

        public static SliceRegionResult FindRoomsSlice(VoxelizeResult r, int start, int length)
        {
            Debug.Log($"FindRoomsSlice: start={start}, length={length}");
            Vector3Int size = r.options.bounds.size;
            SliceRegionResult result =
                new SliceRegionResult(start, length, size.x * size.y);
            BitArray visit = new BitArray(size.x * size.y * length);
            Vector3Int visitSize = new Vector3Int(size.x, size.y, length);
            var getVisitIndex = new Func<Vector3Int, int>(pos =>
                MyMathUtils.GetIndex(visitSize, new Vector3Int(pos.x, pos.y, pos.z - start)));
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = start; z < start + length; z++)
                    {
                        Vector3Int index = new Vector3Int(x, y, z);
                        if (visit[getVisitIndex(index)]) continue;
                        if (r.GetVoxel(index) != -1) continue;

                        // Start a region search from an unvisited voxel not occupied by a triangle.
                        Region region = new Region
                        {
                            id = -1,
                            objects = new List<int>(),
                            startVoxel = index
                        };

                        HashSet<int> objectsInRegion = new HashSet<int>();
                        // Traverse the region with depth-first search.
                        Stack<Vector3Int> stack = new Stack<Vector3Int>();
                        stack.Push(index);
                        while (stack.Count > 0)
                        {
                            Vector3Int current = stack.Pop();

                            // Mark the voxel as visited.
                            visit[getVisitIndex(current)] = true;

                            region.sizeInVoxels++;
                            // Update the regions on the positive and negative faces.
                            if (current.z == start + length - 1)
                            {
                                result.positiveFace[current.x + current.y * size.x] = region;
                            }
                            else if (current.z == start)
                            {
                                result.negativeFace[current.x + current.y * size.x] = region;
                            }

                            // Examine adjacent voxels.
                            foreach (var dir in MyMathUtils.Directions6)
                            {
                                Vector3Int neighbor = current + dir;
                                if (!r.CheckVoxelIndex(neighbor)) continue;
                                if (neighbor.z < start || neighbor.z >= start + length) continue;
                                if (visit[getVisitIndex(neighbor)]) continue;
                                int neighborVoxelObjIdx = r.GetVoxel(neighbor);
                                // Collect indices of objects on the boundary.
                                if (neighborVoxelObjIdx != -1)
                                {
                                    objectsInRegion.Add(neighborVoxelObjIdx);
                                    if (neighbor.z == start + length - 1)
                                    {
                                        result.positiveFaceObjectId[current.x + current.y * size.x] =
                                            neighborVoxelObjIdx;
                                    }
                                    else if (neighbor.z == start)
                                    {
                                        result.negativeFaceObjectId[current.x + current.y * size.x] =
                                            neighborVoxelObjIdx;
                                    }

                                    continue;
                                }

                                // Push unvisited voxels that are not walls onto the stack.
                                stack.Push(neighbor);
                            }
                        }

                        region.objects = objectsInRegion.ToList();
                        result.regions.Add(region);
                    }
                }
            }

            return result;
        }

        public static SliceRegionResult CombineSlice(VoxelizeResult r, SliceRegionResult a, SliceRegionResult b)
        {
            if (a.start > b.start)
            {
                (a, b) = (b, a);
            }

            if (a.start + a.length != b.start)
            {
                throw new ArgumentException("Slices must be contiguous");
            }

            SliceRegionResult result = new SliceRegionResult();
            result.start = a.start;
            result.length = a.length + b.length;
            result.regions = new HashSet<Region>();
            result.regions.UnionWith(a.regions);
            result.regions.UnionWith(b.regions);
            result.negativeFace = a.negativeFace;
            result.negativeFaceObjectId = a.negativeFaceObjectId;
            result.positiveFace = b.positiveFace;
            result.positiveFaceObjectId = b.positiveFaceObjectId;
            UnionFind<Region> u = new UnionFind<Region>();
            Dictionary<Region, Region> regionMap = new Dictionary<Region, Region>();

            foreach (var region in result.regions)
            {
                regionMap[region] = region;
            }

            for (int i = 0; i < a.positiveFace.Length; i++)
            {
                Region af = a.positiveFace[i];
                Region bf = b.negativeFace[i];

                if (af == null && bf == null) continue;

                if (af != null)
                    u.Add(af);
                if (bf != null)
                    u.Add(bf);

                if (af == null)
                {
                    if (a.positiveFaceObjectId[i] != -1)
                    {
                        Region region = regionMap[u.Find(bf)];
                        region.objects.Add(a.positiveFaceObjectId[i]);
                    }

                    continue;
                }

                if (bf == null)
                {
                    if (b.negativeFaceObjectId[i] != -1)
                    {
                        Region region = regionMap[u.Find(af)];
                        region.objects.Add(b.negativeFaceObjectId[i]);
                    }

                    continue;
                }

                if (!a.regions.Contains(af))
                    throw new Exception("!!!!! a");
                if (!b.regions.Contains(bf))
                    throw new Exception("!!!!! b");

                af = u.Find(af);
                bf = u.Find(bf);

                if (af == bf) continue;

                result.regions.Remove(regionMap[u.Find(af)]);
                result.regions.Remove(regionMap[u.Find(bf)]);

                Region cr = CombineRegion(af, bf);
                u.Union(af, bf);
                regionMap[u.Find(af)] = cr;
                result.regions.Add(cr);
            }

            for (int i = 0; i < a.positiveFace.Length; i++)
            {
                if (a.negativeFace[i] != null)
                {
                    if (u.Contains(a.negativeFace[i]))
                        a.negativeFace[i] = regionMap[u.Find(a.negativeFace[i])];
                    if (!result.regions.Contains(a.negativeFace[i]))
                        throw new Exception("!!!!!! 2a");
                }

                if (b.positiveFace[i] != null)
                {
                    if (u.Contains(b.positiveFace[i]))
                        b.positiveFace[i] = regionMap[u.Find(b.positiveFace[i])];
                    if (!result.regions.Contains(b.positiveFace[i]))
                        throw new Exception("!!!!!! 2b");
                }
            }


            return result;
        }

        public static Region CombineRegion(Region a, Region b)
        {
            if (a == null || b == null) throw new ArgumentException("Cannot combine null regions");

            HashSet<int> combinedObjects = new HashSet<int>(a.objects);
            combinedObjects.UnionWith(b.objects);

            return new Region
            {
                id = -1, // Assign a new region ID as needed.
                sizeInVoxels = a.sizeInVoxels + b.sizeInVoxels,
                objects = combinedObjects.ToList(),
                startVoxel = Vector3Int.Min(a.startVoxel, b.startVoxel)
            };
        }

        // public class GridStepResult
        // {
        //     public List<GridRegion> gridRegions; // Regions represented by grid cells.
        //     public int[] regionIndexOfGrid; // Region index for each grid cell.
        //
        //     public GridStepResult(int gridCount)
        //     {
        //         gridRegions = new List<GridRegion>();
        //         regionIndexOfGrid = new int[gridCount];
        //         for (int i = 0; i < gridCount; i++)
        //         {
        //             gridRegions.Add(new GridRegion());
        //             regionIndexOfGrid[i] = -1; // -1 means no region is assigned.
        //         }
        //     }
        // }
        //
        // public class GridRegion
        // {
        //     public List<Vector3Int> emptyGrids = new(); // Empty grid cells.
        //     public HashSet<Vector3Int> edgeVoxels = new(); // Boundary voxels.
        //     public HashSet<int> objects = new(); // Indices of objects associated with boundary voxels.
        // }
        //
        // public static GridStepResult FindRoomsGridStep(VoxelizeResult r)
        // {
        //     Vector3Int gridSize = r.gridSize;
        //     int gridCount = gridSize.x * gridSize.y * gridSize.z;
        //     GridStepResult result = new GridStepResult(gridCount);
        //     BitArray visit = new BitArray(gridCount);
        //     for (int x = 0; x < gridSize.x; x++)
        //     {
        //         for (int y = 0; y < gridSize.y; y++)
        //         {
        //             for (int z = 0; z < gridSize.z; z++)
        //             {
        //                 Vector3Int gridIndex = new Vector3Int(x, y, z);
        //                 if (visit[MyMathUtils.GetIndex(gridSize, gridIndex)]) continue;
        //                 if (r.GetGird(gridIndex) != null) continue;
        //
        //                 // Start a region search from an unvisited empty grid cell.
        //                 GridRegion region = new();
        //                 result.gridRegions.Add(region);
        //                 Stack<Vector3Int> stack = new Stack<Vector3Int>();
        //                 stack.Push(gridIndex);
        //                 while (stack.Count > 0)
        //                 {
        //                     Vector3Int currentGrid = stack.Pop();
        //
        //                     // Mark the grid cell as visited.
        //                     visit[MyMathUtils.GetIndex(gridSize, currentGrid)] = true;
        //                     result.regionIndexOfGrid[MyMathUtils.GetIndex(gridSize, currentGrid)] =
        //                         result.gridRegions.Count;
        //                     region.emptyGrids.Add(currentGrid);
        //
        //                     // Examine adjacent grid cells.
        //                     foreach (var dir in MyMathUtils.Directions6)
        //                     {
        //                         Vector3Int neighbor = currentGrid + dir;
        //                         if (!r.CheckGridIndex(neighbor)) continue;
        //                         if (visit[MyMathUtils.GetIndex(gridSize, neighbor)]) continue;
        //                         VoxelGrid neighborGrid = r.GetGird(neighbor);
        //                         if (neighborGrid == null)
        //                         {
        //                             // Push unvisited empty cells onto the stack.
        //                             stack.Push(neighbor);
        //                             continue;
        //                         }
        //
        //                         // For nonempty cells, record boundary voxels for further searching.
        //                         // Examine voxels on the boundary face.
        //                         Vector3Int offsetMin = Vector3Int.zero;
        //                         Vector3Int offsetMax = Vector3Int.one * (r.options.girdSize - 1);
        //                         for (int i = 0; i < 3; i++)
        //                         {
        //                             if (dir[i] == 1)
        //                             {
        //                                 offsetMin[i] = offsetMax[i] = r.options.girdSize;
        //                             }
        //                             else if (dir[i] == -1)
        //                             {
        //                                 offsetMin[i] = offsetMax[i] = -1;
        //                             }
        //                         }
        //
        //                         for (int ex = offsetMin.x; ex <= offsetMax.x; ex++)
        //                         {
        //                             for (int ey = offsetMin.y; ey <= offsetMax.y; ey++)
        //                             {
        //                                 for (int ez = offsetMin.z; ez <= offsetMax.z; ez++)
        //                                 {
        //                                     Vector3Int edgeVoxel = new Vector3Int(
        //                                         gridIndex.x * r.options.girdSize + ex,
        //                                         gridIndex.y * r.options.girdSize + ey,
        //                                         gridIndex.z * r.options.girdSize + ez
        //                                     );
        //
        //                                     if (!r.CheckVoxelIndex(edgeVoxel)) continue;
        //
        //                                     int voxelObjIdx = r.GetVoxel(edgeVoxel);
        //                                     if (voxelObjIdx == -1) // Add empty voxels to the boundary.
        //                                     {
        //                                         region.edgeVoxels.Add(edgeVoxel);
        //                                     }
        //                                     else // Record the object occupying a boundary voxel.
        //                                     {
        //                                         region.objects.Add(voxelObjIdx);
        //                                     }
        //                                 }
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        //
        //     return result;
        // }
        //
        // // Step 2.
        // // Run DFS from each nonempty grid cell to find connected components.
        // // When DFS reaches an empty cell, add the entire region found in step 1 to the traversal.
        // public static void FindRoomsVoxelStep(VoxelizeResult r, GridStepResult step1Result)
        // {
        //     r.regions = new List<Region>();
        //     Vector3Int gridSize = r.gridSize;
        //     bool[] gridRegionVisit = new bool[step1Result.gridRegions.Count];
        //     Vector3Int size = r.options.bounds.size;
        //     BitArray visitGrid = new BitArray(size.x * size.y * size.z);
        //     BitArray[] visitVoxel = new BitArray[gridSize.x * gridSize.y * gridSize.z];
        //     for (int x = 0; x < gridSize.x; x++)
        //     {
        //         for (int y = 0; y < gridSize.y; y++)
        //         {
        //             for (int z = 0; z < gridSize.z; z++)
        //             {
        //                 Vector3Int gridIndex = new Vector3Int(x, y, z);
        //                 if (r.GetGird(gridIndex) == null) continue;
        //                 // if (visit[MyMathUtils.GetIndex(gridSize, gridIndex)]) continue;
        //
        //                 visitVoxel[MyMathUtils.GetIndex(gridSize, gridIndex)] ??=
        //                     new BitArray(r.options.girdSize * r.options.girdSize * r.options.girdSize);
        //                 
        //                 // Start searching from a nonempty grid cell.
        //                 
        //                 BitArray currentGridVisit = visitVoxel[MyMathUtils.GetIndex(gridSize, gridIndex)];
        //
        //                 for (int vx = 0; vx < r.options.girdSize; vx++)
        //                 {
        //                     for (int vy = 0; vy < r.options.girdSize; vy++)
        //                     {
        //                         for (int vz = 0; vz < r.options.girdSize; vz++)
        //                         {
        //                             
        //                             Vector3Int index = new Vector3Int(x, y, z);
        //                             if (currentGridVisit[MyMathUtils.GetIndex(size, index)]) continue;
        //                             if (r.GetVoxel(index) != -1) continue;
        //                             // Start a region search from an unvisited voxel not occupied by a triangle.
        //                             
        //                             Region region = new Region
        //                             {
        //                                 id = r.regions.Count,
        //                                 objects = new List<int>(),
        //                                 startVoxel = index
        //                             };
        //                             r.regions.Add(region);
        //                             Stack<Vector3Int> stack = new Stack<Vector3Int>();
        //                             stack.Push(gridIndex);
        //                             
        //                             
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }


        // Determine whether a point lies inside the prism formed by the triangle projection, using barycentric coordinates.
        static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // Compute edge and point vectors.
            Vector3 v0 = c - a;
            Vector3 v1 = b - a;
            Vector3 v2 = p - a;

            // Compute dot products.
            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);

            // Compute barycentric coordinates.
            float denom = dot00 * dot11 - dot01 * dot01;
            if (denom == 0) return false;
            float u = (dot11 * dot02 - dot01 * dot12) / denom;
            float v = (dot00 * dot12 - dot01 * dot02) / denom;

            return (u >= 0) && (v >= 0) && (u + v <= 1);
        }

        // Compute the distance from a point to the triangle plane.
        static float PointTrianglePlaneDistance(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            return Mathf.Abs(Vector3.Dot(p - a, n));
        }

        // Input triangle vertices in local or world space.
        // Return occupied coordinates in voxel space.
        public static List<Vector3Int> VoxelizeTriangle(float voxelSize, Vector3 v0, Vector3 v1, Vector3 v2)
        {
            List<Vector3Int> occupiedVoxels = new List<Vector3Int>();

            // Compute the bounds and expand them slightly to account for numerical error.
            Vector3 min = Vector3.Min(v0, Vector3.Min(v1, v2)) - Vector3.one * (voxelSize * 0.5f);
            Vector3 max = Vector3.Max(v0, Vector3.Max(v1, v2)) + Vector3.one * (voxelSize * 0.5f);

            Vector3Int minVoxel = WorldPosToVoxel(voxelSize, min);
            Vector3Int maxVoxel = WorldPosToVoxel(voxelSize, max);

            // Normal of the triangle plane.
            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;

            for (int x = minVoxel.x; x <= maxVoxel.x; x++)
            {
                for (int y = minVoxel.y; y <= maxVoxel.y; y++)
                {
                    for (int z = minVoxel.z; z <= maxVoxel.z; z++)
                    {
                        Vector3 voxelCenter = VoxelToWorldPos(voxelSize, new Vector3Int(x, y, z));

                        // Point-to-plane distance threshold: half the voxel size.
                        float dist = PointTrianglePlaneDistance(voxelCenter, v0, v1, v2);
                        if (dist > voxelSize * 0.5f) continue;

                        // Project the point onto the triangle plane.
                        Vector3 projectedPoint = voxelCenter - dist * normal;

                        // Determine whether the projected point lies inside the triangle.
                        if (PointInTriangle(projectedPoint, v0, v1, v2))
                        {
                            occupiedVoxels.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

            return occupiedVoxels;
        }

        // Convert world coordinates to voxel coordinates by rounding down.
        public static Vector3Int WorldPosToVoxel(float voxelSize, Vector3 pos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(pos.x / voxelSize),
                Mathf.FloorToInt(pos.y / voxelSize),
                Mathf.FloorToInt(pos.z / voxelSize));
        }

        // Convert voxel coordinates to the world-space center of the voxel.
        public static Vector3 VoxelToWorldPos(float voxelSize, Vector3Int voxel)
        {
            return new Vector3(
                (voxel.x + 0.5f) * voxelSize,
                (voxel.y + 0.5f) * voxelSize,
                (voxel.z + 0.5f) * voxelSize);
        }
    }
}
