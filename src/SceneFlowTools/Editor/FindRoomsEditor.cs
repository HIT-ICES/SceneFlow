using System.Collections.Generic;
using System.Linq;
using SceneFlowTools.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace SceneFlowTools.Editor
{
    [CustomEditor(typeof(FindRooms))]
    public class FindRoomsEditor : UnityEditor.Editor
    {
        private List<List<GameObject>> floorObjects = new List<List<GameObject>>();
        private int regionIndex = 0;

        public override void OnInspectorGUI()
        {
            // 保留原有字段显示
            DrawDefaultInspector();

            if (GUILayout.Button("Find Rooms"))
            {
                DoFindRooms();
            }
            // EditorUtility.DisplayProgressBar();
            
            GUILayout.Label("Regions Found: " + floorObjects.Count);
            regionIndex = EditorGUILayout.IntField("Region Index", regionIndex);
            if (GUILayout.Button("Switch Floor"))
            {
                Selection.objects = floorObjects[regionIndex].Cast<Object>().ToArray();
            }
        }

        public void DoFindRooms()
        {
            FindRooms myComponent = (FindRooms)target;
            if (myComponent.navMeshSurface == null)
            {
                Debug.LogError("NavMeshSurface is not assigned.");
                return;
            }

            // 0. 进行NavMesh构建
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                null, // null = 全场景
                myComponent.navMeshSurface.layerMask,
                myComponent.navMeshSurface.useGeometry,
                myComponent.navMeshSurface.defaultArea,
                new List<NavMeshBuildMarkup>(),
                sources
            );

            // 1. 获取NavMesh得到的三角形数据
            NavMeshTriangulation tr = NavMesh.CalculateTriangulation();
            if (tr.vertices.Length == 0 || tr.indices.Length == 0)
            {
                Debug.LogError("No triangulation data found.");
                return;
            }

            Debug.Log("Found " + tr.vertices.Length + " vertices and " + tr.indices.Length / 3 +
                      " triangles in the NavMesh triangulation.");


            // 2. 先找到所有的联通区域
            List<List<int>> edges = new List<List<int>>();
            // 构建图
            for (int i = 0; i < tr.vertices.Length; i++)
            {
                edges.Add(new List<int>());
            }

            for (int i = 0; i < tr.indices.Length; i += 3)
            {
                int v1 = tr.indices[i];
                int v2 = tr.indices[i + 1];
                int v3 = tr.indices[i + 2];

                // 添加边
                edges[v1].Add(v2);
                edges[v2].Add(v1);

                edges[v1].Add(v3);
                edges[v3].Add(v1);

                edges[v2].Add(v3);
                edges[v3].Add(v2);
            }

            // 对图染色
            List<List<int>> regions = new List<List<int>>();
            int[] vColor = new int[tr.vertices.Length];
            for (int i = 0; i < tr.vertices.Length; i++)
            {
                vColor[i] = -1; // -1表示未染色
            }

            for (int i = 0; i < tr.vertices.Length; i++)
            {
                if (vColor[i] != -1) continue;
                // 发现一个未染色的顶点，开始深度优先搜索
                int regionColor = regions.Count;
                // 新建一个区域
                List<int> region = new List<int>();
                // 深度优先搜索
                Stack<int> stack = new Stack<int>();
                stack.Push(i);
                while (stack.Count > 0)
                {
                    int current = stack.Pop();
                    vColor[current] = regionColor;
                    region.Add(current);
                    foreach (int neighbor in edges[current])
                    {
                        if (vColor[neighbor] == -1)
                        {
                            stack.Push(neighbor);
                        }
                    }
                }

                regions.Add(region);
            }
            
            Debug.Log("Finished finding regions. Found " + regions.Count + " regions.");

            List<List<int>> regionTriangles = new List<List<int>>();
            for (int i = 0; i < regions.Count; i++)
            {
                regionTriangles.Add(new List<int>());
            }

            for (int i = 0; i < tr.indices.Length; i += 3)
            {
                int v1 = tr.indices[i];
                int v2 = tr.indices[i + 1];
                int v3 = tr.indices[i + 2];

                // 获取这三个顶点的颜色
                int color1 = vColor[v1];
                int color2 = vColor[v2];
                int color3 = vColor[v3];

                Debug.Assert(color1 == color2 && color1 == color3,
                    $"Triangle vertices {v1}, {v2}, {v3} have different colors: {color1}, {color2}, {color3}");

                regionTriangles[color1].Add(i / 3); // 添加三角形索引
            }

            // 3. 根据NavMesh的数据，找到所有地板
            // 先提取所有Renderer
            floorObjects.Clear();
            for (int i = 0; i < regions.Count; i++)
            {
                floorObjects.Add(new List<GameObject>());
            }

            // 遍历每个区域中的所有三角形，用射线检测找到对应的地板
            for (int i = 0; i < regions.Count; i++)
            {
                foreach (var triIndex in regionTriangles[i])
                {
                    // 获取三角形的顶点
                    int v1 = tr.indices[triIndex * 3];
                    int v2 = tr.indices[triIndex * 3 + 1];
                    int v3 = tr.indices[triIndex * 3 + 2];

                    floorObjects[i].AddRange(TriangleRayCast(
                        tr.vertices[v1],
                        tr.vertices[v2],
                        tr.vertices[v3],
                        myComponent.navMeshSurface.layerMask
                    ));
                }
            }
            
            List<GameObject> hitObjectsList = new List<GameObject>();
            foreach (var t in floorObjects)
            {
                hitObjectsList.AddRange(t);
            }
            Debug.Log("Hit Objects: " + hitObjectsList.Count);
            Selection.objects = hitObjectsList.Cast<Object>().ToArray();

            // foreach (var s in sources)
            // {
            //     if (s.sourceObject is not Mesh mesh) continue;
            //     // 找到对应的区域
            //     int regionIndex = -1;
            //     // 遍历所有区域，判断s在哪个区域中
            //     for (int i = 0; i < regions.Count; i++)
            //     {
            //         // 遍历区域中的所有三角面
            //         foreach (var triIndex in regionTriangles[i])
            //         {
            //             // 获取三角形的顶点
            //             int v1 = tr.indices[triIndex * 3];
            //             int v2 = tr.indices[triIndex * 3 + 1];
            //             int v3 = tr.indices[triIndex * 3 + 2];
            //             
            //             // 
            //         }
            //     }
            // }
        }

        List<GameObject> TriangleRayCast(Vector3 v1, Vector3 v2, Vector3 v3, LayerMask layerMask)
        {
            List<GameObject> hitObjects = new List<GameObject>();
            // 在三角形上均匀取样，每隔0.1f单位取一个点
            // 暂时先用简单的方式
            Vector3 center = (v1 + v2 + v3) / 3;

            var points = new Vector3[4]
            {
                v1,
                v2,
                v3,
                center
            };
            
            

            foreach (var point in points)
            {
                GameObject floorObject = FloorRayCast(point, layerMask);
                if (floorObject != null && !hitObjects.Contains(floorObject))
                {
                    hitObjects.Add(floorObject);
                }
            }

            return hitObjects;
        }

        GameObject FloorRayCast(Vector3 v, LayerMask layerMask)
        {
            Ray ray = new Ray(v, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, 1, layerMask))
            {
                GameObject floorObject = hit.collider.gameObject;
                return floorObject;
            }

            return null;
        }
    }
}