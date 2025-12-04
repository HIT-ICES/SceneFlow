using System;
using System.Linq;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.ReprojectionSimpleDemo
{
    [RequireComponent(typeof(Camera))]
    public class SourceCameraWithDepth2D : PlusSourceCamera
    {
        public Vector2Int renderSize = new(1920, 1080);
        public RenderTexture sourceTexture;
        public RenderTexture depthTexture;
        public RenderTexture packedTexture;
        public bool disableLinearizeDepth = false;

        private Camera _camera;
        private Material _depthCopyMaterial;
        public override RenderTexture renderedTexture => packedTexture;


        void Start()
        {
            _depthCopyMaterial = new Material(Shader.Find("Hidden/ExtractDepth"));
            if (disableLinearizeDepth)
            {
                _depthCopyMaterial?.EnableKeyword("DISABLE_LINEARIZE_DEPTH");
            }
            else
            {
                _depthCopyMaterial?.DisableKeyword("DISABLE_LINEARIZE_DEPTH");
            }
            DebugUtils.PrintEnabledKeywords(nameof(_depthCopyMaterial), _depthCopyMaterial);
            Debug.Log($"sourceTexture: {sourceTexture}, depthTexture: {depthTexture}, packedTexture: {packedTexture}");

            if (sourceTexture == null)
                sourceTexture = new RenderTexture(renderSize.x, renderSize.y, 32, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            if (depthTexture == null)
                depthTexture = new RenderTexture(renderSize.x, renderSize.y, 32, RenderTextureFormat.ARGBFloat,
                    RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            if (packedTexture == null)
                packedTexture = new RenderTexture(renderSize.x, renderSize.y * 2, 32,
                    RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };


            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;
            _camera.targetTexture = sourceTexture;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
        }
        
        private void OnPostRender()
        {
            Graphics.Blit(sourceTexture, depthTexture, _depthCopyMaterial);
            TextureUtils.PackRGBDTex2D(sourceTexture, depthTexture, packedTexture);
        }
    }
}