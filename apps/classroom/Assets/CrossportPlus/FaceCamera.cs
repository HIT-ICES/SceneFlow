using System;
using UnityEngine;

namespace CrossportPlus
{
    public class FaceCamera  : MonoBehaviour
    {
        public int renderFaceSize = 1024;
        public bool renderDepth = true;
        public CubemapFace face;
        public RenderTexture texture;
        public RenderTexture depthTexture;
        Camera _camera;
        Material _depthCopyMaterial;
        
        private void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.fieldOfView = 90;
            texture = new RenderTexture(renderFaceSize, renderFaceSize, 32, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                autoGenerateMips = false,
                useMipMap = false
            };
            _camera.targetTexture = texture;
            if (renderDepth)
            {
                _camera.depthTextureMode = DepthTextureMode.Depth;
                depthTexture = new RenderTexture(renderFaceSize, renderFaceSize, 32, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear)
                {
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    anisoLevel = 0,
                    dimension = UnityEngine.Rendering.TextureDimension.Tex2D,
                    autoGenerateMips = false,
                    useMipMap = false,
                };
                _depthCopyMaterial = new Material(Shader.Find("Hidden/ExtractDepth"));
            }
        }

        private void LateUpdate()
        {
            // update rotation according to face
            switch (face)
            {
                case CubemapFace.PositiveX:
                    transform.rotation = Quaternion.Euler(0, 90, 0);
                    break;
                case CubemapFace.NegativeX:
                    transform.rotation = Quaternion.Euler(0, -90, 0);
                    break;
                case CubemapFace.PositiveY:
                    transform.rotation = Quaternion.Euler(90, 0, 0);
                    break;
                case CubemapFace.NegativeY:
                    transform.rotation = Quaternion.Euler(-90, 0, 0);
                    break;
                case CubemapFace.PositiveZ:
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                    break;
                case CubemapFace.NegativeZ:
                    transform.rotation = Quaternion.Euler(0, 180, 0);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OnPostRender()
        {
            if (renderDepth)
            {
                Graphics.Blit(texture, depthTexture, _depthCopyMaterial);
            }
        }
    }
}