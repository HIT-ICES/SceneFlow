using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    public class SceneTreeUtils
    {
        public Bounds CalculateBounds(List<MeshFilter> meshFilters)
        {
            if (meshFilters == null || meshFilters.Count == 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Bounds bounds = new Bounds(meshFilters[0].transform.position, Vector3.zero);

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    bounds.Encapsulate(meshFilter.sharedMesh.bounds);
                }
            }

            return bounds;
        }

        public bool SceneContains(Bounds a, Bounds b)
        {
            // Require containment on the XZ plane and at least an intersection on the Y axis.
            return a.min.x <= b.min.x && a.max.x >= b.max.x &&
                   a.min.z <= b.min.z && a.max.z >= b.max.z &&
                   a.min.y <= b.max.y && a.max.y >= b.min.y;
        }

        public SceneTreeNode BuildTree(List<List<MeshFilter>> meshFilters)
        {
            List<Bounds> bounds = meshFilters.Select(CalculateBounds).ToList();
            List<SceneTreeNode> nodes = meshFilters.Select(v => new SceneTreeNode(v)).ToList();
            SceneTreeNode root = new SceneTreeNode(new List<MeshFilter>());
            for (int i = 0; i < meshFilters.Count; i++)
            {
                int parent = -1;
                for (int j = 0; j < i; j++)
                {
                    if (i == j) continue;
                    if (!SceneContains(bounds[j], bounds[i])) continue;
                    if (parent == -1 || SceneContains(bounds[parent], bounds[j]))
                    {
                        parent = j;
                    }
                }
                if (parent != -1)
                {
                    nodes[parent].AddChild(nodes[i]);
                    nodes[i].Parent = nodes[parent];
                }
                else
                {
                    root.AddChild(nodes[i]);
                    nodes[i].Parent = root;
                }
            }
            return root;
        }
    }

    public class SceneTreeNode
    {
        public List<MeshFilter> Objects;
        public SceneTreeNode Parent;
        public List<SceneTreeNode> Children;

        public SceneTreeNode(List<MeshFilter> objects)
        {
            Objects = objects;
            Children = new List<SceneTreeNode>();
        }

        public void AddChild(SceneTreeNode child)
        {
            Children.Add(child);
        }
    }
}
