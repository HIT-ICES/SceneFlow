using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SceneFlowTools.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SceneFlowTools.Runtime.Utils
{
    public static class GameObjectInfoCollector
    {
        public static List<GObjInfo> GetInfos(List<GameObject> objs)
        {
            List<GObjInfo> result = objs.Select(GetBasicInfo).ToList();
            Parallel.For(0, result.Count, i => { FillOtherInfos(result[i]); });
            return result;
        }

        public static GObjInfo GetInfo(GameObject obj)
        {
            GObjInfo info = GetBasicInfo(obj);
            FillOtherInfos(info);
            return info;
        }

        private static GObjInfo GetBasicInfo(GameObject obj)
        {
            GObjBounds? bounds = null;
            if (obj.TryGetComponent<Renderer>(out var renderer))
            {
                bounds = new GObjBounds
                {
                    center = renderer.bounds.center,
                    extents = renderer.bounds.extents
                };
            }

            Vector3[] vertices = null;
            List<int[]> triangles = null;
            if (obj.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh != null)
            {
                vertices = meshFilter.sharedMesh.vertices;
                // Convert local vertices to world space
                if (vertices != null && vertices.Length > 0)
                {
                    // Matrix4x4 localToWorld = obj.transform.localToWorldMatrix;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] = meshFilter.transform.TransformPoint(vertices[i]);
                    }
                }


                int[] rawTriangles = meshFilter.sharedMesh.triangles;
                if (rawTriangles.Length % 3 != 0)
                {
                    Debug.LogError(
                        $"Mesh {meshFilter.sharedMesh.name} has an invalid triangle count: {rawTriangles.Length}. It should be a multiple of 3.");
                    throw new Exception();
                }

                triangles = new List<int[]>(rawTriangles.Length / 3);
                for (int i = 0; i < rawTriangles.Length; i += 3)
                {
                    triangles.Add(new[] { rawTriangles[i], rawTriangles[i + 1], rawTriangles[i + 2] });
                }
            }


            GObjInfo info = new GObjInfo
            {
                id = obj.GetInstanceID().ToString(),
                parentId = obj.transform.parent?.GetInstanceID().ToString(),
                pathInScene = SceneUtils.GetPathInScene(obj.transform),
                name = obj.name,
                position = obj.transform.position,
                rotation = obj.transform.rotation.eulerAngles,
                scale = obj.transform.localScale,
                active = obj.activeInHierarchy,
                bounds = bounds,
                vertices = vertices,
                triangles = triangles,
                voxels = null
            };
            return info;
        }

        private static void FillOtherInfos(GObjInfo info)
        {
            var triangles = info.triangles;
            var vertices = info.vertices;
            if (triangles == null || vertices == null)
            {
                return;
            }

            HashSet<Vector3Int> voxelSet = new HashSet<Vector3Int>();
            Parallel.For(0, triangles.Count, i =>
            {
                var tr = triangles[i];
                var v = VoxelizerCPU.VoxelizeTriangle(0.2f, vertices[tr[0]], vertices[tr[1]],
                    vertices[tr[2]]);
                lock (voxelSet)
                {
                    voxelSet.UnionWith(v);
                }
            });
            // foreach (var triangle in triangles)
            // {
            //     var v = VoxelizerCPU.VoxelizeTriangle(0.2f, vertices[triangle[0]], vertices[triangle[1]],
            //         vertices[triangle[2]]);
            //     voxelSet.UnionWith(v);
            // }
            info.voxels = voxelSet.Select(v => VoxelizerCPU.VoxelToWorldPos(0.2f, v)).ToArray();
        }

        public static void RegenerateIds(List<GObjInfo> infos)
        {
            for (var i = 0; i < infos.Count; i++)
            {
                infos[i].id = i.ToString();
            }
        }

        public static string GetSavePath(Scene currentScene)
        {
            string folder = SceneUtils.GetSceneAssetFolderPath(currentScene);
            string filePath = $"{folder}/{currentScene.name}.gobj_info.json";
            return filePath;
        }

        public static string Save(Scene currentScene, List<GObjInfo> infos)
        {
            string folder = SceneUtils.GetSceneAssetFolderPath(currentScene, true);
            string filePath = $"{folder}/{currentScene.name}.gobj_info.json";
            using (var fs = File.Create(filePath))
            using (var sw = new StreamWriter(fs))
            using (var jw = new JsonTextWriter(sw))
            {
                var serializer = new JsonSerializer();
                serializer.Converters.Add(new JsonConverterForVector());
                serializer.Serialize(jw, new
                {
                    sceneName = currentScene.name,
                    objects = infos
                });
            }

            // using (var fs = File.Create(filePath))
            // using (var sw = new StreamWriter(fs))
            // using (var jw = new JsonTextWriter(sw))
            // {
            //     jw.WriteState
            //     string[] buff = new string[100]; 
            //     for (int i = 0; i < infos.Count; i += 100)
            //     {
            //         int end = Mathf.Min(i + 100, infos.Count);
            //         Parallel.For(i, end, j =>
            //         {
            //             buff[j] = JsonConvert.SerializeObject(infos[j]);
            //         });
            //     }
            //     var serializer = new JsonSerializer();
            //     serializer.Converters.Add(new JsonConverterForVector());
            //     JsonConvert.SerializeObject()
            //     serializer.Serialize(jw, new
            //     {
            //         sceneName = currentScene.name,
            //         objects = infos
            //     });
            // }

            // Debug.Log($"write to {filePath}");
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return filePath;
        }

        public static void Delete(Scene currentScene)
        {
            string folder = SceneUtils.GetSceneAssetFolderPath(currentScene, true);
            string filePath = $"{folder}/{currentScene.name}.gobj_info.json";
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                // Debug.Log($"Deleted GameObject info file: {filePath}");
            }
        }
    }

    public class JsonConverterForVector : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (value)
            {
                case Vector3 v:
                    writer.WriteStartArray();
                    writer.WriteValue(v.x);
                    writer.WriteValue(v.y);
                    writer.WriteValue(v.z);
                    writer.WriteEndArray();
                    break;
                case Vector3Int v:
                    writer.WriteStartArray();
                    writer.WriteValue(v.x);
                    writer.WriteValue(v.y);
                    writer.WriteValue(v.z);
                    writer.WriteEndArray();
                    break;
                default:
                    throw new JsonSerializationException($"Expected Vector3 object value, got {value?.GetType()}.");
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            throw new NotImplementedException("Deserialization is not implemented for Vector types in this converter.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3) || objectType == typeof(Vector3Int);
        }
    }

    [Serializable]
    public class GObjInfo
    {
        public string id;
        public string parentId;
        public string name;
        public string pathInScene;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;
        public bool active;
        public GObjBounds? bounds;
        public Vector3[] vertices;
        public List<int[]> triangles;
        public Vector3[] voxels;
    }

    [Serializable]
    public struct GObjBounds
    {
        public Vector3 center;
        public Vector3 extents;
    }
}