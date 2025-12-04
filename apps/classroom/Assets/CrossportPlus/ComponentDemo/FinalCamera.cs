using System;
using System.Collections.Generic;
using CrossportPlus.Reprojection;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CrossportPlus.ComponentDemo
{
    /// <summary>
    /// 用户端的最终相机
    /// 本身也进行渲染（双眼），同时合并所有ComponentCamera的RGBD纹理
    /// 组合所有ComponentCamera，实现最终效果
    /// +重投影
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FinalCamera : MonoBehaviour
    {
        [Tooltip("渲染的分辨率，要与ComponentCamera的分辨率一致")]
        public Vector2Int renderSize = new(1920, 1080);

        [Tooltip("各个组合所有ComponentCamera的RGBD纹理")]
        public List<Texture> componentRgbdTextures;

        [Tooltip("debug用，不要修改或复制")] public RenderTexture finalTexture;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture leftEyeTexture;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture rightEyeTexture;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture rightEyeTextureUnFillled;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture leftEyeTextureUnFillled;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture currentDepthTexture;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture testTex;
        [Tooltip("debug用，不要修改或复制")] public RenderTexture unpackedRgbTex;

        public ProjectionParams projectionParams;
        public float ipd;
        public bool disableLinearizeDepth = false;


        private Camera _camera;
        private Material _separateEyeBlitMaterial;
        private Material _finalCameraBlendMaterial;
        private RenderTexture _depthTexture;
        private ReprojectionTool _reprojectionTool;
        private HoleFillingTool _holeFillingTool;
        private DepthBlendTool _depthBlendTool;
        private DepthExtractTool _depthExtractTool;
        
        public Vector3 prevPosition;
        public Quaternion prevRotation;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;
            
            prevPosition = _camera.transform.position;
            prevRotation = _camera.transform.rotation;
            
            // _camera.cullingMask = LayerMask.GetMask("Nothing");
            // _camera.clearFlags = CameraClearFlags.SolidColor;
            // _camera.backgroundColor = Color.green;

            _separateEyeBlitMaterial = new Material(Resources.Load<Shader>("CrossportPlus/Shaders/SeparateEyeBlit"));

            _finalCameraBlendMaterial = new Material(Resources.Load<Shader>("CrossportPlus/Shaders/FinalCameraBlend"));
            Debug.Log($"_finalCameraBlendMaterial.shader={_finalCameraBlendMaterial.shader.name}");

            _reprojectionTool = new ReprojectionTool(projectionParams, ipd)
            {
                disableLinearizeDepth = disableLinearizeDepth
            };

            _holeFillingTool = new HoleFillingTool();

            _depthExtractTool = new DepthExtractTool()
            {
                disableLinearizeDepth = disableLinearizeDepth
            };

            if (!Mathf.Approximately(ipd, _camera.stereoSeparation))
            {
                Debug.LogWarning($"IPD [{ipd}] is not equal to camera.stereoSeparation [{_camera.stereoSeparation}]");
            }

            _depthBlendTool = new DepthBlendTool();

            finalTexture = GetRenderTexture(renderSize.x, renderSize.y * 2);
            leftEyeTexture = GetRenderTexture(renderSize.x, renderSize.y);
            rightEyeTexture = GetRenderTexture(renderSize.x, renderSize.y);
            rightEyeTextureUnFillled = GetRenderTexture(renderSize.x, renderSize.y);
            leftEyeTextureUnFillled = GetRenderTexture(renderSize.x, renderSize.y);
            currentDepthTexture = GetRenderTexture(renderSize.x, renderSize.y);
            unpackedRgbTex  = GetRenderTexture(renderSize.x, renderSize.y);
            testTex = GetRenderTexture(renderSize.x, renderSize.y);
            Debug.Log(
                $"rightEyeTexture=${rightEyeTexture}, width={rightEyeTexture.width}, height={rightEyeTexture.height}");
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            _depthExtractTool.ExtractCurrentCameraDepth(currentDepthTexture);

            // 没有ComponentCamera的RGBD纹理，直接输出
            if (componentRgbdTextures.Count == 0)
            {
                Graphics.Blit(src, dest);
                return;
            }

            CommandBuffer cmd = new CommandBuffer();
            // 把所有ComponentCamera的RGBD纹理合成为左眼纹理
            _depthBlendTool.BlendRGBD(cmd, componentRgbdTextures, finalTexture);

            
            // 重投影左眼
            _reprojectionTool.position = new Vector3(0, 0, 0);
            _reprojectionTool.rotation = Quaternion.Inverse(prevRotation) * _camera.transform.rotation;
            _reprojectionTool.Reproject2DWithRgbd(cmd, leftEyeTextureUnFillled, unpackedRgbTex, finalTexture, true,
                Color.green);
            _holeFillingTool.FillHoles(cmd, leftEyeTextureUnFillled, leftEyeTexture);
            
            // 重投影右眼
            _reprojectionTool.position = new Vector3(0.065f, 0, 0);
            _reprojectionTool.rotation =  Quaternion.Inverse(prevRotation) * _camera.transform.rotation;
            _reprojectionTool.Reproject2DWithRgbd(cmd, rightEyeTextureUnFillled, unpackedRgbTex, finalTexture, true,
                Color.green);
            _holeFillingTool.FillHoles(cmd, rightEyeTextureUnFillled, rightEyeTexture);
            

            // 两眼分开显示，与当前相机渲染的内容混合
            cmd.SetGlobalTexture("_LeftTex", leftEyeTexture);
            cmd.SetGlobalTexture("_RightTex", rightEyeTexture);
            // _finalCameraBlendMaterial.SetTexture("_LeftTex", leftEyeTexture);
            // _finalCameraBlendMaterial.SetTexture("_RightTex", rightEyeTextureUnFillled);
            // cmd.Blit(src, dest);
            cmd.Blit(src, dest, _finalCameraBlendMaterial);

            // _separateEyeBlitMaterial.SetTexture("_LeftTex", leftEyeTexture);
            // _separateEyeBlitMaterial.SetTexture("_RightTex", rightEyeTextureUnFillled);
            // cmd.Blit(src, dest, _separateEyeBlitMaterial);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        private void OnPostRender()
        {
            // _depthExtractTool.ExtractCurrentCameraDepth(currentDepthTexture);
        }

        private RenderTexture GetRenderTexture(int width, int height, bool randomWrite = true)
        {
            RenderTexture rt = new RenderTexture(width, height, 32, GraphicsFormat.R8G8B8A8_UNorm)
            {
                dimension = TextureDimension.Tex2D,
                autoGenerateMips = false,
                useMipMap = false,
                enableRandomWrite = randomWrite,
                depthStencilFormat = GraphicsFormat.D32_SFloat,
            };
            rt.Create();
            if (!rt.IsCreated())
                Debug.LogError($"RenderTexture not created: {rt}");
            return rt;
        }
    }
}