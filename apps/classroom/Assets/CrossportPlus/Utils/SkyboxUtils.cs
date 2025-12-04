using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.Utils
{
    public static class SkyboxUtils
    {
        public static void SetToSkybox(Texture texture)
        {
            switch (texture.dimension)
            {
                case TextureDimension.Cube:
                    SetCubemapToSkybox(texture);
                    break;
                default:
                case TextureDimension.Tex2D:
                    SetTexture2DToSkybox(texture);
                    break;
            }
        }

        public static void SetCubemapToSkybox(Texture texture)
        {
            Material skyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));
            skyboxMaterial.SetTexture("_Tex", texture);
            RenderSettings.skybox = skyboxMaterial;
        }

        public static void SetTexture2DToSkybox(Texture texture)
        {
            RenderSettings.skybox.mainTexture = texture;
        }
    }
}