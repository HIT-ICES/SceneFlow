using System;
using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.Reprojection
{
    public class HoleFillingTool
    {
        private readonly ComputeShader _holeFillingShader;

        public HoleFillingTool()
        {
            _holeFillingShader = Resources.Load<ComputeShader>("CrossportPlus/Shaders/HoleFilling");
        }

        public void FillHoles(RenderTexture source, RenderTexture target)
        {
            CommandBuffer cmd = new CommandBuffer();
            FillHoles(cmd, source, target);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Release();
        }

        public void FillHoles(CommandBuffer cmd, RenderTexture source, RenderTexture target)
        {
            int kernel = _holeFillingShader.FindKernel("CSMain");
            cmd.SetComputeTextureParam(_holeFillingShader, kernel, "SourceTexture", source);
            cmd.SetComputeTextureParam(_holeFillingShader, kernel, "TargetTexture", target);
            cmd.SetComputeIntParam(_holeFillingShader, "Width", source.width);
            cmd.SetComputeIntParam(_holeFillingShader, "Height", source.height);
            cmd.DispatchCompute(_holeFillingShader, kernel, source.width / 16, source.height / 16, 1);
        }
    }
}