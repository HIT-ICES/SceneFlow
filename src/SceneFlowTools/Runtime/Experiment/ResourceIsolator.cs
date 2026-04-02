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
        
        /// </summary>
        public void IsolateGroupResources()
        {
            
            
            Dictionary<Material, Material> internalMaterialCache = new Dictionary<Material, Material>();
            Dictionary<Texture, Texture> internalTextureCache = new Dictionary<Texture, Texture>();

            
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

            foreach (Renderer ren in renderers)
            {
                if (ren == null) continue;

                
                Material[] originalMats = ren.sharedMaterials;
                Material[] newMats = new Material[originalMats.Length];

                for (int i = 0; i < originalMats.Length; i++)
                {
                    Material origMat = originalMats[i];
                    if (origMat == null) continue;

                    
                    if (internalMaterialCache.TryGetValue(origMat, out Material cachedMat))
                    {
                        
                        newMats[i] = cachedMat;
                    }
                    else
                    {
                        
                        Material clonedMat = new Material(origMat);
                        clonedMat.name = origMat.name + "_GroupIsolated";

                        
                        CloneTexturesForMaterial(clonedMat, internalTextureCache);

                        
                        internalMaterialCache[origMat] = clonedMat;
                        newMats[i] = clonedMat;
                    }
                }

                
                
                ren.sharedMaterials = newMats;
            }

            Debug.Log($"<color=green>[Resource Isolation Completed]</color> Target: {gameObject.name}\n" +
                      $"Generated {internalMaterialCache.Count} isolated materials internally, " +
                      $"{internalTextureCache.Count} isolated textures, and uploaded them to GPU memory.");
        }

        /// <summary>
        
        /// </summary>
        private void CloneTexturesForMaterial(Material mat, Dictionary<Texture, Texture> textureCache)
        {
            
            string[] texturePropertyNames = mat.GetTexturePropertyNames();

            foreach (string propName in texturePropertyNames)
            {
                Texture originalTex = mat.GetTexture(propName);
                if (originalTex == null) continue;

                
                Texture2D origTex2D = originalTex as Texture2D;
                if (origTex2D == null) continue; 

                
                if (textureCache.TryGetValue(origTex2D, out Texture cachedTex))
                {
                    
                    mat.SetTexture(propName, cachedTex);
                }
                else
                {
                    
                    
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

                    
                    Graphics.CopyTexture(origTex2D, clonedTex2D);

                    
                    mat.SetTexture(propName, clonedTex2D);

                    
                    textureCache[origTex2D] = clonedTex2D;
                }
            }
        }
    }
}