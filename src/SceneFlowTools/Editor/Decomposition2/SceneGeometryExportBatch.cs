using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SceneFlowTools.Editor
{
    public static class SceneGeometryExportBatch
    {
        public static void Run()
        {
            try
            {
                Dictionary<string, string> args = ParseCommandLineArgs();
                string scenePath = GetArg(args, "scenePath", required: true);
                string outputJson = GetArg(args, "outputJson", required: true);
                bool includeInactive = ParseBool(GetArg(args, "includeInactive", defaultValue: "false"));
                bool meshOnly = ParseBool(GetArg(args, "meshOnly", defaultValue: "true"));

                Debug.Log(
                    $"SceneGeometryExportBatch.Run scenePath={scenePath} outputJson={outputJson} " +
                    $"includeInactive={includeInactive} meshOnly={meshOnly}");

                EditorSceneManager.OpenScene(scenePath);

                GeometrySceneExport export = BuildExport(scenePath, includeInactive, meshOnly);
                string fullPath = Path.GetFullPath(outputJson);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllText(fullPath, JsonConvert.SerializeObject(export, Formatting.None));

                Debug.Log($"Scene geometry exported to {fullPath}, objects={export.objects.Count}");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        private static GeometrySceneExport BuildExport(string scenePath, bool includeInactive, bool meshOnly)
        {
            List<MeshFilter> meshFilters = Object.FindObjectsOfType<MeshFilter>(includeInactive)
                .Where(m => m != null && m.sharedMesh != null)
                .Where(m => includeInactive || m.gameObject.activeInHierarchy)
                .Where(m => !meshOnly || m.GetComponent<Renderer>() != null)
                .OrderBy(m => GetDebugPath(m.transform), StringComparer.Ordinal)
                .ToList();

            List<GeometryObjectExport> objects = new();
            bool hasBounds = false;
            Bounds sceneBounds = new Bounds(Vector3.zero, Vector3.zero);

            for (int i = 0; i < meshFilters.Count; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                Renderer renderer = meshFilter.GetComponent<Renderer>();
                Bounds bounds = renderer != null
                    ? renderer.bounds
                    : CalculateWorldBounds(meshFilter);

                if (!hasBounds)
                {
                    sceneBounds = bounds;
                    hasBounds = true;
                }
                else
                {
                    sceneBounds.Encapsulate(bounds);
                }

                Mesh mesh = meshFilter.sharedMesh;
                Vector3[] rawVertices = mesh.vertices;
                float[][] vertices = new float[rawVertices.Length][];
                for (int v = 0; v < rawVertices.Length; v++)
                {
                    vertices[v] = ToArray(meshFilter.transform.TransformPoint(rawVertices[v]));
                }

                objects.Add(new GeometryObjectExport
                {
                    id = i.ToString(),
                    debugPath = GetDebugPath(meshFilter.transform),
                    localToWorld = ToArray(meshFilter.transform.localToWorldMatrix),
                    bounds = ToExportBounds(bounds),
                    vertices = vertices,
                    triangles = mesh.triangles,
                });
            }

            return new GeometrySceneExport
            {
                exporterVersion = 1,
                scenePath = scenePath,
                sceneBounds = hasBounds ? ToExportBounds(sceneBounds) : ToExportBounds(new Bounds(Vector3.zero, Vector3.zero)),
                objects = objects,
            };
        }

        private static Bounds CalculateWorldBounds(MeshFilter meshFilter)
        {
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            if (vertices.Length == 0)
            {
                return new Bounds(meshFilter.transform.position, Vector3.zero);
            }

            Bounds bounds = new Bounds(meshFilter.transform.TransformPoint(vertices[0]), Vector3.zero);
            for (int i = 1; i < vertices.Length; i++)
            {
                bounds.Encapsulate(meshFilter.transform.TransformPoint(vertices[i]));
            }

            return bounds;
        }

        private static string GetDebugPath(Transform transform)
        {
            Stack<string> names = new Stack<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        private static ExportBounds ToExportBounds(Bounds bounds)
        {
            return new ExportBounds
            {
                center = ToArray(bounds.center),
                extents = ToArray(bounds.extents),
                min = ToArray(bounds.min),
                max = ToArray(bounds.max),
            };
        }

        private static float[] ToArray(Vector3 v)
        {
            return new[] { v.x, v.y, v.z };
        }

        private static float[] ToArray(Matrix4x4 m)
        {
            return new[]
            {
                m.m00, m.m01, m.m02, m.m03,
                m.m10, m.m11, m.m12, m.m13,
                m.m20, m.m21, m.m22, m.m23,
                m.m30, m.m31, m.m32, m.m33,
            };
        }

        private static Dictionary<string, string> ParseCommandLineArgs()
        {
            string[] rawArgs = Environment.GetCommandLineArgs();
            Dictionary<string, string> args = new();
            for (int i = 0; i < rawArgs.Length; i++)
            {
                string key = rawArgs[i];
                if (!key.StartsWith("-")) continue;
                key = key.TrimStart('-');
                if (i + 1 >= rawArgs.Length || rawArgs[i + 1].StartsWith("-"))
                {
                    args[key] = "true";
                    continue;
                }

                args[key] = rawArgs[++i];
            }

            return args;
        }

        private static string GetArg(
            Dictionary<string, string> args,
            string name,
            string defaultValue = null,
            bool required = false)
        {
            if (args.TryGetValue(name, out string value))
            {
                return value;
            }

            if (required)
            {
                throw new ArgumentException($"Missing required command line argument -{name}");
            }

            return defaultValue;
        }

        private static bool ParseBool(string value)
        {
            return value != null && bool.TryParse(value, out bool result) && result;
        }
    }

    [Serializable]
    public class GeometrySceneExport
    {
        public int exporterVersion;
        public string scenePath;
        public ExportBounds sceneBounds;
        public List<GeometryObjectExport> objects;
    }

    [Serializable]
    public class GeometryObjectExport
    {
        public string id;
        public string debugPath;
        public float[] localToWorld;
        public ExportBounds bounds;
        public float[][] vertices;
        public int[] triangles;
    }

    [Serializable]
    public class ExportBounds
    {
        public float[] center;
        public float[] extents;
        public float[] min;
        public float[] max;
    }
}
