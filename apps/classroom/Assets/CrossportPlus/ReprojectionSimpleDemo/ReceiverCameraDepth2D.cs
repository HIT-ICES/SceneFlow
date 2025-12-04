using CrossportPlus.Reprojection;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.ReprojectionSimpleDemo
{
    [RequireComponent(typeof(Camera))]
    public class ReceiverCameraDepth2D : PlusReceiverCamera
    {
        [Tooltip("RGBD纹理")] public RenderTexture overlapTexture;
        [Tooltip("左眼覆盖纹理")] public RenderTexture leftOverlapTexture;
        [Tooltip("右眼覆盖纹理")] public RenderTexture rightOverlapTexture;
        public RenderTexture middleOverlapTexture;
        public ProjectionParams projectionParams;
        public float ipd;
        public bool disableLinearizeDepth = false;

        public override RenderTexture receivedTexture
        {
            get => overlapTexture;
            set => overlapTexture = value;
        }

        private Camera _camera;
        private RenderTexture _cubeTexture;
        private Material _separateEyeBlitMaterial;
        private RenderTexture _depthTexture;
        private ReprojectionTool _reprojectionTool;
        private HoleFillingTool _holeFillingTool;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _separateEyeBlitMaterial = new Material(Resources.Load<Shader>("CrossportPlus/Shaders/SeparateEyeBlit"));
            _reprojectionTool = new ReprojectionTool(projectionParams, ipd)
            {
                disableLinearizeDepth = disableLinearizeDepth
            };
            _holeFillingTool = new HoleFillingTool();
            if (!Mathf.Approximately(ipd, _camera.stereoSeparation))
            {
                Debug.LogWarning($"IPD [{ipd}] is not equal to camera.stereoSeparation [{_camera.stereoSeparation}]");
            }
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if(overlapTexture == null)
            {
                Graphics.Blit(src, dest);
                return;
            }
            CommandBuffer cmd = new CommandBuffer();
            EnsureEyeTexture(ref leftOverlapTexture);
            EnsureEyeTexture(ref rightOverlapTexture);
            EnsureEyeTexture(ref middleOverlapTexture);
            if (!rightOverlapTexture.enableRandomWrite)
                rightOverlapTexture.enableRandomWrite = true;
            if (!middleOverlapTexture.enableRandomWrite)
                middleOverlapTexture.enableRandomWrite = true;
            EnsureMiddleTextures();
            TextureUtils.UnpackRGBDTex2D(cmd, overlapTexture, leftOverlapTexture, _depthTexture);
            _reprojectionTool.Reproject2D(cmd, middleOverlapTexture, leftOverlapTexture, _depthTexture);
            _holeFillingTool.FillHoles(cmd, middleOverlapTexture, rightOverlapTexture);
            // 不能用，global的texture需要在shader中特殊声明
            // cmd.SetGlobalTexture("_LeftTex", leftOverlapTexture);
            // cmd.SetGlobalTexture("_RightTex", rightOverlapTexture);
            _separateEyeBlitMaterial.SetTexture("_LeftTex", leftOverlapTexture);
            _separateEyeBlitMaterial.SetTexture("_RightTex", rightOverlapTexture);
            cmd.Blit(src, dest, _separateEyeBlitMaterial);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        private void EnsureMiddleTextures()
        {
            if (_depthTexture == null)
            {
                _depthTexture = new RenderTexture(overlapTexture.width, overlapTexture.height / 2, 32,
                    overlapTexture.graphicsFormat)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            }
        }

        private void EnsureEyeTexture(ref RenderTexture eyeTexture)
        {
            if (eyeTexture == null || eyeTexture.width != overlapTexture.width ||
                eyeTexture.height != overlapTexture.height / 2)
            {
                Destroy(eyeTexture);
                eyeTexture = new RenderTexture(overlapTexture.width, overlapTexture.height / 2, 24,
                    overlapTexture.graphicsFormat)
                {
                    dimension = TextureDimension.Tex2D,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            }
        }
    }
}