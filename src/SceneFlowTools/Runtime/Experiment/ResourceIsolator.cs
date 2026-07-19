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
        /// Isolate resources while preserving internal sharing, at the cost of additional GPU memory.
        /// </summary>
        public void IsolateGroupResources()
        {
            // Use two dictionaries as an internal cache.
            // Within this GameObject set, clone each source material or texture only once.
            Dictionary<Material, Material> internalMaterialCache = new Dictionary<Material, Material>();
            Dictionary<Texture, Texture> internalTextureCache = new Dictionary<Texture, Texture>();

            // Get renderers from this object and all of its children.
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer ren in renderers)
            {
                if (ren == null) continue;

                // Read sharedMaterials rather than materials, which would make Unity instantiate materials automatically.
                Material[] originalMats = ren.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];

                for (int i = 0; i < originalMats.Length; i++)
                {
                    Material origMat = originalMats[i];
                    if (origMat == null) continue;

                    // Check whether this material has already been cloned within the isolated set.
                    if (internalMaterialCache.TryGetValue(origMat, out Material cachedMat))
                    {
                        // Preserve internal sharing by reusing the cached clone.
                        newMats[i] = cachedMat;
                    }
                    else
                    {
                        // Isolate from external users by cloning the material on first use.
                        Material clonedMat = new Material(origMat);
                        clonedMat.name = origMat.name + "_GroupIsolated";

                        // Deep-clone the textures referenced by the material.
                        CloneTexturesForMaterial(clonedMat, internalTextureCache);

                        // Add the clone to the internal cache.
                        internalMaterialCache[origMat] = clonedMat;
                        newMats[i] = clonedMat;
                    }
                }

                // Assign the remapped material array back to the Renderer.
                // Continue using sharedMaterials to preserve the intended internal sharing.
                ren.sharedMaterials = newMats;
            }

            Debug.Log($"<color=green>[资源隔离完成]</color> 目标: {gameObject.name}\n" +
                      $"在内部共生成了 {internalMaterialCache.Count} 个独立材质，" +
                      $"{internalTextureCache.Count} 张独立贴图，并已存入显存。");
        }

        /// <summary>
        /// Traverse all texture properties on a material and clone their pixel data.
        /// </summary>
        private void CloneTexturesForMaterial(Material mat, Dictionary<Texture, Texture> textureCache)
        {
            // Get all texture property names defined by the material's shader at runtime.
            string[] texturePropertyNames = mat.GetTexturePropertyNames();

            foreach (string propName in texturePropertyNames)
            {
                Texture originalTex = mat.GetTexture(propName);
                if (originalTex == null) continue;

                // This implementation handles Texture2D instances.
                Texture2D origTex2D = originalTex as Texture2D;
                if (origTex2D == null) continue; // Skip 3D textures and cubemaps for now.

                // Check whether this texture has already been cloned within the isolated set.
                if (textureCache.TryGetValue(origTex2D, out Texture cachedTex))
                {
                    // Preserve internal sharing by referencing the cached texture clone.
                    mat.SetTexture(propName, cachedTex);
                }
                else
                {
                    // Allocate separate GPU memory to isolate a new texture clone from external users.
                    // graphicsFormat preserves the source format and color space (Linear/sRGB).
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

                    // Copy GPU data directly without requiring Read/Write Enabled on the source texture.
                    Graphics.CopyTexture(origTex2D, clonedTex2D);

                    // Assign the cloned texture to the new material.
                    mat.SetTexture(propName, clonedTex2D);

                    // Add the texture clone to the internal cache.
                    textureCache[origTex2D] = clonedTex2D;
                }
            }
        }
    }
}
