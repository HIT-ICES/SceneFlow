using System;
using System.Linq;
using CrossportPlus.Reprojection;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.ComponentDemo
{
    // 组件化渲染的摄像机
    // 负责渲染一部分画面
    [RequireComponent(typeof(Camera))]
    public class ComponentCamera : MonoBehaviour
    {
        public Vector2Int renderSize = new(1920, 1080);
        [Tooltip("摄像机渲染的RGB纹理")]
        public RenderTexture sourceTexture;
        [Tooltip("摄像机渲染的深度纹理")]
        public RenderTexture depthTexture;
        [Tooltip("组合出的RGB-D纹理")]
        public RenderTexture packedTexture;
        public bool disableLinearizeDepth = false;
        
        private Camera _camera;
        private DepthExtractTool _depthExtractTool;


        void Start()
        {
            _depthExtractTool = new DepthExtractTool()
            {
                disableLinearizeDepth = disableLinearizeDepth
            };
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
                depthTexture = new RenderTexture(renderSize.x, renderSize.y, 32, RenderTextureFormat.ARGB32,
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
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
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
            _depthExtractTool.ExtractCurrentCameraDepth(depthTexture);
            TextureUtils.PackRGBDTex2D(sourceTexture, depthTexture, packedTexture);
        }
    }
}