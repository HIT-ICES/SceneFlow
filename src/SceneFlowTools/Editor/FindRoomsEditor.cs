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


            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(
                null,
                myComponent.navMeshSurface.layerMask,
                myComponent.navMeshSurface.useGeometry,
                myComponent.navMeshSurface.defaultArea,
                new List<NavMeshBuildMarkup>(),
                sources
            );


            NavMeshTriangulation tr = NavMesh.CalculateTriangulation();
            if (tr.vertices.Length == 0 || tr.indices.Length == 0)
            {
                Debug.LogError("No triangulation data found.");
                return;
            }

            Debug.Log("Found " + tr.vertices.Length + " vertices and " + tr.indices.Length / 3 +
                      " triangles in the NavMesh triangulation.");



            List<List<int>> edges = new List<List<int>>();

            for (int i = 0; i < tr.vertices.Length; i++)
            {
                edges.Add(new List<int>());
            }

            for (int i = 0; i < tr.indices.Length; i += 3)
            {
                int v1 = tr.indices[i];
                int v2 = tr.indices[i + 1];
                int v3 = tr.indices[i + 2];


                edges[v1].Add(v2);
                edges[v2].Add(v1);

                edges[v1].Add(v3);
                edges[v3].Add(v1);

                edges[v2].Add(v3);
                edges[v3].Add(v2);
            }


            List<List<int>> regions = new List<List<int>>();
            int[] vColor = new int[tr.vertices.Length];
            for (int i = 0; i < tr.vertices.Length; i++)
            {
                vColor[i] = -1;
            }

            for (int i = 0; i < tr.vertices.Length; i++)
            {
                if (vColor[i] != -1) continue;

                int regionColor = regions.Count;

                List<int> region = new List<int>();

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


                int color1 = vColor[v1];
                int color2 = vColor[v2];
                int color3 = vColor[v3];

                Debug.Assert(color1 == color2 && color1 == color3,
                    $"Triangle vertices {v1}, {v2}, {v3} have different colors: {color1}, {color2}, {color3}");

                regionTriangles[color1].Add(i / 3);
            }



            floorObjects.Clear();
            for (int i = 0; i < regions.Count; i++)
            {
                floorObjects.Add(new List<GameObject>());
            }


            for (int i = 0; i < regions.Count; i++)
            {
                foreach (var triIndex in regionTriangles[i])
                {

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

            //     int regionIndex = -1;

            //     for (int i = 0; i < regions.Count; i++)
            //     {

            //         foreach (var triIndex in regionTriangles[i])
            //         {

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