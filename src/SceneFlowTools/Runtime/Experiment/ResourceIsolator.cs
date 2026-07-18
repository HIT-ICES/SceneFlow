using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace SceneFlowTools.Runtime.Experiment
{
    public class ResourceIsolator : MonoBehaviour
    {
        void Awake()
        {
            IsolateGroupResources();
        }

        /// <summary>
        /// 执行资源隔离逻辑：对内共享，对外独立，额外消耗显存
        /// </summary>
        public void IsolateGroupResources()
        {
            // 核心：两个字典作为“内部缓存池”
            // 保证在这个 GameObject 集合内部，相同的原始材质/贴图只会克隆一次
            Dictionary<Material, Material> internalMaterialCache = new Dictionary<Material, Material>();
            Dictionary<Texture, Texture> internalTextureCache = new Dictionary<Texture, Texture>();

            // 获取当前物体及所有子物体的渲染器
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer ren in renderers)
            {
                if (ren == null) continue;

                // 注意：必须读取 sharedMaterials，不能读取 materials（否则 Unity 会自动破坏共享）
                Material[] originalMats = ren.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];

                for (int i = 0; i < originalMats.Length; i++)
                {
                    Material origMat = originalMats[i];
                    if (origMat == null) continue;

                    // 检查缓存：这个材质在我们的“孤岛”内部是否已经被克隆过？
                    if (internalMaterialCache.TryGetValue(origMat, out Material cachedMat))
                    {
                        // 对内共享：直接使用已经克隆好的材质
                        newMats[i] = cachedMat;
                    }
                    else
                    {
                        // 对外隔离：这是第一次遇到该材质，彻底克隆它
                        Material clonedMat = new Material(origMat);
                        clonedMat.name = origMat.name + "_GroupIsolated";

                        // 进一步深入：深度克隆该材质引用的贴图
                        CloneTexturesForMaterial(clonedMat, internalTextureCache);

                        // 记录到内部缓存池中
                        internalMaterialCache[origMat] = clonedMat;
                        newMats[i] = clonedMat;
                    }
                }

                // 将重新分配好引用的新材质数组赋值回 Renderer
                // 注意：依然使用 sharedMaterials 赋值，维持我们设定的内部共享关系
                ren.sharedMaterials = newMats;
            }

            Debug.Log($"<color=green>[资源隔离完成]</color> 目标: {gameObject.name}\n" +
                      $"在内部共生成了 {internalMaterialCache.Count} 个独立材质，" +
                      $"{internalTextureCache.Count} 张独立贴图，并已存入显存。");
        }

        /// <summary>
        /// 遍历材质上的所有贴图属性，并执行像素级克隆
        /// </summary>
        private void CloneTexturesForMaterial(Material mat, Dictionary<Texture, Texture> textureCache)
        {
            // 获取该材质 Shader 中所有定义的贴图属性名称（支持运行时调用）
            string[] texturePropertyNames = mat.GetTexturePropertyNames();

            foreach (string propName in texturePropertyNames)
            {
                Texture originalTex = mat.GetTexture(propName);
                if (originalTex == null) continue;

                // 大多数情况我们处理的都是 Texture2D
                Texture2D origTex2D = originalTex as Texture2D;
                if (origTex2D == null) continue; // 如果是 3D纹理或Cubemap 则暂时跳过

                // 检查缓存：这张贴图在“孤岛”内部是否已经被克隆过？
                if (textureCache.TryGetValue(origTex2D, out Texture cachedTex))
                {
                    // 对内共享：直接把材质上的贴图指针指向已缓存的克隆体
                    mat.SetTexture(propName, cachedTex);
                }
                else
                {
                    // 对外隔离：彻底在显存中开辟空间，克隆一张新贴图
                    // 使用 graphicsFormat 可以完美继承原图的格式和色彩空间 (Linear/sRGB)
                    Texture2D clonedTex2D = new Texture2D(
                        origTex2D.width,
                        origTex2D.height,
                        origTex2D.graphicsFormat,
                        origTex2D.mipmapCount,
                        UnityEngine.Experimental.Rendering.TextureCreationFlags.None
                    );

                    clonedTex2D.name = origTex2D.name + "_GroupIsolated";
                    clonedTex2D.filterMode = origTex2D.filterMode;
                    clonedTex2D.wrapMode = origTex2D.wrapMode;
                    clonedTex2D.anisoLevel = origTex2D.anisoLevel;

                    // 核心：硬件级显存数据拷贝，速度极快，且不需要原图开启 Read/Write Enabled
                    Graphics.CopyTexture(origTex2D, clonedTex2D);

                    // 赋值给新材质
                    mat.SetTexture(propName, clonedTex2D);

                    // 加入内部缓存池
                    textureCache[origTex2D] = clonedTex2D;
                }
            }
        }
    }
}