using System.Collections.Generic;
using Newtonsoft.Json;
using SceneFlowTools.Editor.Utils;
using SceneFlowTools.Runtime;
using SceneFlowTools.Runtime.Config;
using SceneFlowTools.Runtime.Experiment;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneFlowTools.Editor.Experiment
{
    [CustomEditor(typeof(SceneInfoCollector))]
    public class SceneInfoCollectorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            if (GUILayout.Button("Collect Scene Info"))
            {
                CollectSceneInfo();
                Debug.Log("Scene info collected.");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CollectSceneInfo()
        {
            SceneInfoCollector tg = (SceneInfoCollector)target;
            List<Subscene> scenes = tg.renderConfigManager.sceneConfig.scenes;
            var sid2Obj = MetaInfo.CollectAllDict();
            var tex2Id = new Dictionary<Texture, int>();
            var obj2Id = new Dictionary<GameObject, int>();
            SceneData sceneData = new SceneData()
            {
                name = !string.IsNullOrEmpty(tg.sceneName) ? tg.sceneName : SceneManager.GetActiveScene().name,
                textures = new List<TextureData>(),
                objects = new List<GObjectData>(),
                nodes = new List<SceneNodeData>()
            };
            foreach (var s in scenes)
            {
                SceneNodeData nodeData = new SceneNodeData()
                {
                    id = s.id,
                    parent = s.parentId,
                    objects = new List<int>()
                };
                sceneData.nodes.Add(nodeData);
                foreach (var objSid in s.objectIds)
                {
                    if (!sid2Obj.TryGetValue(objSid, out var obj))
                    {
                        Debug.LogWarning($"Object ID {objSid} not found in MetaInfo collection. Skipping.");
                        continue;
                    }

                    GObjectData gObjectData;
                    if (obj2Id.TryGetValue(obj, out var objId))
                    {
                        gObjectData = sceneData.objects[objId];
                    }
                    else
                    {
                        objId = sceneData.objects.Count;
                        obj2Id[obj] = objId;
                        var (faceCount, vertexCount) = GetMeshCount(obj);
                        gObjectData = new GObjectData()
                        {
                            id = objId,
                            textures = new List<int>(),
                            faceCount = faceCount,
                            vertexCount = vertexCount
                        };
                        sceneData.objects.Add(gObjectData);
                    }

                    nodeData.objects.Add(objId);

                    List<Texture> textures = GetObjectTextures(obj);
                    foreach (var tex in textures)
                    {
                        if (!tex2Id.TryGetValue(tex, out var texId))
                        {
                            texId = sceneData.textures.Count;
                            tex2Id[tex] = texId;
                            TextureData texData = new TextureData()
                            {
                                id = texId,
                                size = MyTextureUtils.GetStorageMemorySizeLong(tex) / 1024.0 / 1024.0
                            };
                            sceneData.textures.Add(texData);
                        }

                        gObjectData.textures.Add(texId);
                    }
                }
            }

            WriteSceneInfo(sceneData);
        }

        private (int faceCount, int vertexCount) GetMeshCount(GameObject obj)
        {
            int faceCount = 0;
            int vertexCount = 0;
            var meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                faceCount += meshFilter.sharedMesh.triangles.Length / 3;
                vertexCount += meshFilter.sharedMesh.vertexCount;
            }

            var skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null)
            {
                faceCount += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
                vertexCount += skinnedMeshRenderer.sharedMesh.vertexCount;
            }

            return (faceCount, vertexCount);
        }

        private List<Texture> GetObjectTextures(GameObject obj)
        {
            List<Texture> textures = new List<Texture>();
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return textures;
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                foreach (var name in mat.GetTexturePropertyNames())
                {
                    var tex = mat.GetTexture(name);
                    if (tex != null && !textures.Contains(tex))
                    {
                        textures.Add(tex);
                    }
                }
            }

            return textures;
        }

        private void WriteSceneInfo(SceneData sceneData)
        {
            string text = JsonConvert.SerializeObject(sceneData, Formatting.Indented, new JsonSerializerSettings()
            {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                {
                    NamingStrategy = new Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy()
                }
            });
            string path = $"Build/scene_info_{sceneData.name}.json";
            System.IO.File.WriteAllText(path, text);
        }
    }

    internal class SceneData
    {
        public string name;
        public List<TextureData> textures;
        public List<GObjectData> objects;
        public List<SceneNodeData> nodes;
    }

    internal class TextureData
    {
        public int id;
        public double size;
    }

    internal class GObjectData
    {
        public int id;
        public List<int> textures;
        public int faceCount;
        public int vertexCount;
    }

    internal class SceneNodeData
    {
        public int id;
        public int parent;
        public List<int> objects;
    }
}