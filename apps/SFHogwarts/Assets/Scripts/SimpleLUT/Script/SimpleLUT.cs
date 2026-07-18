using System;
using UnityEngine;

namespace DigitalRuby.SimpleLUT
{
    [ExecuteInEditMode]
    public class SimpleLUT : MonoBehaviour
    {
        [Range(0, 1)] [Tooltip("Lerp between original (0) and corrected color (1)")]
        public float Amount = 1.0f;

        [Range(-1, 1)] [Tooltip("Brightness")] public float Brightness = 0.0f;

        [Range(-1, 1)] [Tooltip("Contrast")] public float Contrast = 0.0f;

        private Texture3D converted3DLut;

        [Range(0, 360)] [Tooltip("Hue")] public float Hue = 0.0f;

        [Tooltip(
            "Texture to use for the lookup. Make sure it has read/write enabled and mipmaps disabled. The height must equal the square root of the width.")]
        public Texture2D LookupTexture;

        private int lutSize;

        private Material material;
        private Shader previousShader;
        private Texture2D previousTexture;

        [Range(-1, 1)] [Tooltip("Saturation")] public float Saturation = 0.0f;

        [Tooltip("Shader to use for the lookup.")]
        public Shader Shader;

        [Range(0, 1)] [Tooltip("Sharpness")] public float Sharpness = 0.0f;

        [Tooltip("Tint color, applied to the final pixel")]
        public Color TintColor = Color.white;

        private void CreateMaterial()
        {
            if (Shader == null)
            {
                material = null;
                Debug.LogError("Must set a shader to use LUT");
                return;
            }

            material = new Material(Shader);
            material.hideFlags = HideFlags.DontSave;
        }

        private void OnEnable()
        {
            if (GetComponent<Camera>() == null) Debug.LogError("This script must be attached to a Camera");
        }

        private void Update()
        {
            if (Shader != previousShader)
            {
                previousShader = Shader;
                CreateMaterial();
            }

            if (LookupTexture != previousTexture)
            {
                previousTexture = LookupTexture;
                Convert(LookupTexture);
            }
        }

        private void OnDestroy()
        {
            if (converted3DLut != null) DestroyImmediate(converted3DLut);
            converted3DLut = null;
        }

        public void SetIdentityLut()
        {
            if (!SystemInfo.supports3DTextures)
                return;
            if (converted3DLut != null) DestroyImmediate(converted3DLut);

            var dim = 16;
            var newC = new Color[dim * dim * dim];
            var oneOverDim = 1.0f / (1.0f * dim - 1.0f);

            for (var i = 0; i < dim; i++)
            for (var j = 0; j < dim; j++)
            for (var k = 0; k < dim; k++)
                newC[i + j * dim + k * dim * dim] = new Color(i * 1.0f * oneOverDim, j * 1.0f * oneOverDim,
                    k * 1.0f * oneOverDim, 1.0f);

            converted3DLut = new Texture3D(dim, dim, dim, TextureFormat.ARGB32, false);
            converted3DLut.SetPixels(newC);
            converted3DLut.Apply();
            lutSize = converted3DLut.width;
            converted3DLut.wrapMode = TextureWrapMode.Clamp;
        }

        public bool ValidDimensions(Texture2D tex2d)
        {
            if (tex2d == null) return false;

            var h = tex2d.height;
            if (h != Mathf.FloorToInt(Mathf.Sqrt(tex2d.width))) return false;
            return true;
        }

        internal bool Convert(Texture2D lookupTexture)
        {
            if (!SystemInfo.supports3DTextures)
            {
                Debug.LogError("System does not support 3D textures");
                return false;
            }

            if (lookupTexture == null)
            {
                SetIdentityLut();
            }
            else
            {
                if (converted3DLut != null) DestroyImmediate(converted3DLut);

                if (lookupTexture.mipmapCount > 1)
                {
                    Debug.LogError("Lookup texture must not have mipmaps");
                    return false;
                }

                try
                {
                    var dim = lookupTexture.width * lookupTexture.height;
                    dim = lookupTexture.height;

                    if (!ValidDimensions(lookupTexture))
                    {
                        Debug.LogError(
                            "Lookup texture dimensions must be a power of two. The height must equal the square root of the width.");
                        return false;
                    }

                    var c = lookupTexture.GetPixels();
                    var newC = new Color[c.Length];

                    for (var i = 0; i < dim; i++)
                    for (var j = 0; j < dim; j++)
                    for (var k = 0; k < dim; k++)
                    {
                        var j_ = dim - j - 1;
                        newC[i + j * dim + k * dim * dim] = c[k * dim + i + j_ * dim * dim];
                    }

                    converted3DLut = new Texture3D(dim, dim, dim, TextureFormat.ARGB32, false);
                    converted3DLut.SetPixels(newC);
                    converted3DLut.Apply();
                    lutSize = converted3DLut.width;
                    converted3DLut.wrapMode = TextureWrapMode.Clamp;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Unable to convert texture to LUT texture, make sure it is read/write. Error: " +
                                   ex);
                }
            }

            return true;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (converted3DLut == null) SetIdentityLut();

            if (converted3DLut == null || material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetTexture("_MainTex", source);
            material.SetTexture("_ClutTex", converted3DLut);
            material.SetFloat("_Amount", Amount);
            material.SetColor("_TintColor", TintColor);
            material.SetFloat("_Hue", Hue);
            material.SetFloat("_Saturation", Saturation + 1.0f);
            material.SetFloat("_Brightness", Brightness + 1.0f);
            material.SetFloat("_Contrast", Contrast + 1.0f);
            material.SetFloat("_Scale", (lutSize - 1) / (1.0f * lutSize));
            material.SetFloat("_Offset", 1.0f / (2.0f * lutSize));

            var actualSharpness = Sharpness * 4.0f * 0.2f;
            material.SetFloat("_SharpnessCenterMultiplier", 1.0f + 4.0f * actualSharpness);
            material.SetFloat("_SharpnessEdgeMultiplier", actualSharpness);
            material.SetVector("_ImageWidthFactor", new Vector4(1.0f / source.width, 0.0f, 0.0f, 0.0f));
            material.SetVector("_ImageHeightFactor", new Vector4(0.0f, 1.0f / source.height, 0.0f, 0.0f));

            Graphics.Blit(source, destination, material, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
        }
    }
}