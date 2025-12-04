using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class Polycounter : Editor
{
    [MenuItem("GameObject/Count Polygons")]
    public static void CountPolygons()
    {
        int totalVertices = 0;
        int polyCount = 0;
        foreach (GameObject go in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (go.activeInHierarchy)
            {
                MeshFilter[] filters = go.GetComponentsInChildren<MeshFilter>();
                foreach (MeshFilter filter in filters)
                {
                    totalVertices += filter.sharedMesh.vertexCount;
                    polyCount += filter.sharedMesh.triangles.Length / 3;
                }
            }
        }

        Debug.Log("Total vertices in all active GameObjects: " + totalVertices);
        Debug.Log("Total Tris in all active GameObjects: " + polyCount);

    }
}