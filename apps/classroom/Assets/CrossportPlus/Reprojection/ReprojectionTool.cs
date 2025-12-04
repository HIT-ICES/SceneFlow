using System;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;

namespace CrossportPlus.Reprojection
{
    public class ReprojectionTool
    {
        private const int SHADER_THREAD_GROUP_X = 8;
        private const int SHADER_THREAD_GROUP_Y = 8;

        public bool disableLinearizeDepth
        {
            get => _disableLinearizeDepth;
            set
            {
                _disableLinearizeDepth = value;
                if (_disableLinearizeDepth)
                {
                    _reprojectionShader.EnableKeyword("DISABLE_LINEARIZE_DEPTH");
                }
                else
                {
                    _reprojectionShader.DisableKeyword("DISABLE_LINEARIZE_DEPTH");
                }
            }
        }

        public ProjectionParams projectionParams { get; set; }

        public float ipd
        {
            get => position.x;
            set => position = new Vector3(value, 0, 0);
        }

        public Vector3 position { get; set; }
        public Quaternion rotation { get; set; }

        private readonly ComputeShader _reprojectionShader;
        private bool _disableLinearizeDepth;

        public ReprojectionTool(ComputeShader reprojectionShader, ProjectionParams projectionParams, float ipd)
        {
            _reprojectionShader = reprojectionShader;
            this.projectionParams = projectionParams;
            this.ipd = ipd;
            DebugUtils.PrintEnabledKeywords(nameof(reprojectionShader), reprojectionShader);
        }

        public ReprojectionTool(ProjectionParams projectionParams, float ipd) : this(
            Resources.Load<ComputeShader>("CrossportPlus/Shaders/Reprojection"), projectionParams, ipd)
        {
        }

        public void Reproject2D(RenderTexture targetTex, RenderTexture rgbTex, RenderTexture depthTex,
            bool clear = true, Color? clearColor = null)
        {
            CommandBuffer cmd = new CommandBuffer();
            Reproject2D(cmd, targetTex, rgbTex, depthTex, clear, clearColor);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        public void Reproject2DWithRgbd(CommandBuffer cmd, RenderTexture targetTex, RenderTexture unpackedRgbTex,
            RenderTexture rgbdTex,
            bool clear = true, Color? clearColor = null)
        {
            Assert.AreEqual((targetTex.width, targetTex.height * 2), (rgbdTex.width, rgbdTex.height));
            int depthTexId = Shader.PropertyToID("UnpackRGBDTex2D_depthTex");
            cmd.GetTemporaryRT(depthTexId, targetTex.descriptor);
            TextureUtils.UnpackRGBDTex2D(cmd, rgbdTex, unpackedRgbTex, depthTexId);
            _Reproject2D(cmd, targetTex.width, targetTex.height, targetTex, unpackedRgbTex, depthTexId, clear,
                clearColor);
            cmd.ReleaseTemporaryRT(depthTexId);
        }

        public void Reproject2D(CommandBuffer cmd, RenderTexture targetTex, RenderTexture rgbTex,
            RenderTexture depthTex,
            bool clear = true, Color? clearColor = null)
        {
            Assert.AreEqual((targetTex.width, targetTex.height), (rgbTex.width, rgbTex.height));
            Assert.AreEqual((targetTex.width, targetTex.height), (depthTex.width, depthTex.height));
            _Reproject2D(cmd, targetTex.width, targetTex.height, targetTex, rgbTex, depthTex, clear, clearColor);
        }

        public void _Reproject2D(CommandBuffer cmd, int width, int height, RenderTargetIdentifier targetTex,
            RenderTargetIdentifier rgbTex,
            RenderTargetIdentifier depthTex,
            bool clear, Color? clearColor)
        {
            // Debug.Log("Reproject2D before clear");
            if (clear)
            {
                // do not use ClearRenderTarget
                cmd.Blit(Texture2D.blackTexture, targetTex);
            }
            // Debug.Log("Reproject2D after clear");
            // cmd.SetRenderTarget(targetTex);
            // cmd.SetRenderTarget(BuiltinRenderTextureType.None);


            //Matrix4x4.Translate(position * -2) *
            Matrix4x4 reprojectionMatrix = Matrix4x4.Rotate(rotation).inverse *
                                           projectionParams.PerspectiveMatrix() *
                                           Matrix4x4.Translate(-2 * position) *
                                           projectionParams.PerspectiveMatrix().inverse;
            // Matrix4x4 reprojectionMatrix = Matrix4x4.Rotate(rotation).inverse;
            // Matrix4x4 reprojectionMatrix =  Matrix4x4.Rotate(Quaternion.Euler(-30, 0, 0));

            int kernel = _reprojectionShader.FindKernel("CSMain");

            cmd.SetComputeTextureParam(_reprojectionShader, kernel, "DepthTexture", depthTex);
            cmd.SetComputeTextureParam(_reprojectionShader, kernel, "SourceTexture", rgbTex);
            cmd.SetComputeTextureParam(_reprojectionShader, kernel, "TargetTexture", targetTex);
            cmd.SetComputeMatrixParam(_reprojectionShader, "ReprojectionMatrix", reprojectionMatrix);
            cmd.SetComputeIntParam(_reprojectionShader, "Width", width);
            cmd.SetComputeIntParam(_reprojectionShader, "Height", height);
            cmd.SetComputeVectorParam(_reprojectionShader, "ProjectionParams", projectionParams.ToVector4());


            cmd.DispatchCompute(_reprojectionShader, kernel, width / SHADER_THREAD_GROUP_X,
                height / SHADER_THREAD_GROUP_Y, 1);
            // cmd.SetRenderTarget(targetTex);
            // Debug.Log("Reproject2D after DispatchCompute");
        }
    }
}