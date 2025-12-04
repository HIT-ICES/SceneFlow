using System;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.Reprojection
{
    public class DepthExtractTool
    {
        private readonly Material _depthCopyMaterial;
        
        public bool disableLinearizeDepth
        {
            get => _disableLinearizeDepth;
            set
            {
                _disableLinearizeDepth = value;
                if (_disableLinearizeDepth)
                {
                    _depthCopyMaterial.EnableKeyword("DISABLE_LINEARIZE_DEPTH");
                }
                else
                {
                    _depthCopyMaterial.DisableKeyword("DISABLE_LINEARIZE_DEPTH");
                }
            }
        }

        private bool _disableLinearizeDepth;

        public DepthExtractTool()
        {
            _depthCopyMaterial = new Material(Shader.Find("Hidden/ExtractDepth"));
            DebugUtils.PrintEnabledKeywords(nameof(_depthCopyMaterial), _depthCopyMaterial);
        }

        public void ExtractCurrentCameraDepth(RenderTexture target)
        {
            Graphics.Blit(null, target, _depthCopyMaterial);
        }
        
        public void ExtractCurrentCameraDepth(CommandBuffer cmd, RenderTexture target)
        {
            cmd.Blit(null, target, _depthCopyMaterial);
        }
    }
}