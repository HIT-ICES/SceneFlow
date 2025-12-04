using System;
using System.Linq;
using CrossportPlus.Reprojection;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus
{
    [RequireComponent(typeof(Camera))]
    public class ReprojectionTest : MonoBehaviour
    {
        public RenderTexture sourceTexture;
        public RenderTexture depthTexture;
        public RenderTexture reprojectionTexture;
        public RenderTexture anotherEyeTexture;
        public ComputeShader reprojectionShader;


        public bool linearizeDepth
        {
            get => _linearizeDepth;
            set
            {
                _linearizeDepth = value;
                updateShaderKeywords();
            }
        }

        [SerializeField] private bool _linearizeDepth = true;

        private Camera _camera;
        private Camera _cameraAnotherEye;

        private Material _depthCopyMaterial;
        private ReprojectionTool _reprojectionTool;


        // Start is called before the first frame update
        void Start()
        {
            _depthCopyMaterial = new Material(Shader.Find("Hidden/ExtractDepth"));


            if (reprojectionTexture == null)
                reprojectionTexture = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32)
                {
                    enableRandomWrite = true,
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            if (anotherEyeTexture == null)
                anotherEyeTexture = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            if (sourceTexture == null)
                sourceTexture = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
            if (depthTexture == null)
                depthTexture = new RenderTexture(1024, 1024, 32, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear)
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

            _cameraAnotherEye = new GameObject("AnotherEye").AddComponent<Camera>();
            _cameraAnotherEye.CopyFrom(_camera);
            _cameraAnotherEye.transform.SetParent(_camera.transform);
            _cameraAnotherEye.transform.localPosition = new Vector3(0.1f, 0, 0);
            _cameraAnotherEye.targetTexture = anotherEyeTexture;
            
            Debug.Log($"reprojectionShader enabled keywords: {DebugUtils.DebugString(reprojectionShader.enabledKeywords.Select((x)=>x.name))}");
            
            _reprojectionTool = new ReprojectionTool(reprojectionShader, ProjectionParams.FromCamera(_camera), 0.1f);

            // reprojectionShader.SetTexture(0, "ResultTexture", tmpTexture);
            // reprojectionShader.Dispatch(0, 1024 / 8, 1024 / 8, 1);

            // Matrix4x4 mat1 = Matrix4x4.Perspective(90, 1, 0.1f, 100f);
            // Matrix4x4 matReproj = mat1 * Matrix4x4.Translate(new Vector3(-0.1f, 0, 0)) * mat1.inverse;
            // Vector4 v1 = new Vector4(0f, 0f, 0.2f, 1f);
            // Debug.Log($"matReproj:\n{matReproj}");
            // Debug.Log($"matReproj*v1: {matReproj * v1}");
            updateShaderKeywords();
            // Debug.Log($"_depthCopyMaterial keywords: {DebugUtils.DebugString(_depthCopyMaterial.shaderKeywords)}");
            // Debug.Log($"_depthCopyMaterial enabled keywords: {DebugUtils.DebugString(_depthCopyMaterial.enabledKeywords.Select((x)=>x.name))}");
        }

        private void updateShaderKeywords()
        {
            if (!_linearizeDepth)
            {
                _depthCopyMaterial?.EnableKeyword("DISABLE_LINEARIZE_DEPTH");
            }
            else
            {
                _depthCopyMaterial?.DisableKeyword("DISABLE_LINEARIZE_DEPTH");
            }
            _reprojectionTool.disableLinearizeDepth = !_linearizeDepth;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                updateShaderKeywords();
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination);
        }

        private void OnPostRender()
        {
            Graphics.Blit(sourceTexture, depthTexture, _depthCopyMaterial);
            _reprojectionTool.Reproject2D(reprojectionTexture, sourceTexture, depthTexture);
        }
    }
}