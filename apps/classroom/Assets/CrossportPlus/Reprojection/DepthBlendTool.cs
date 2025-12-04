using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace CrossportPlus.Reprojection
{
    public class DepthBlendTool
    {
        private readonly ComputeShader _depthBlendShader =
            Resources.Load<ComputeShader>("CrossportPlus/Shaders/DepthBlend");

        public void BlendRGBD(CommandBuffer cmd, List<Texture> rgbds, RenderTexture target)
        {
            Assert.IsTrue(target.enableRandomWrite);
            List<int> tmps = new List<int>();
            List<RenderTargetIdentifier> textures = new List<RenderTargetIdentifier>();
            RenderTextureDescriptor descriptor = target.descriptor;
            descriptor.enableRandomWrite = false;
            for (int i = 0; i < rgbds.Count; i++)
            {
                var t = rgbds[i];
                if ((t.width, t.height) != (target.width, target.height))
                {
                    int tmp = Shader.PropertyToID($"DepthBlendTool_BlendRGBD_TmpRGBD_{i}");
                    cmd.GetTemporaryRT(tmp, descriptor);
                    tmps.Add(tmp);
                    textures.Add(tmp);
                    cmd.Blit(t, tmp);
                }
                else
                {
                    textures.Add(t);
                }
            }
            _BlendRGBD_WithoutScale(cmd, textures, target);
            foreach (var tmp in tmps)
            {
                cmd.ReleaseTemporaryRT(tmp);
            }
        }

        public void _BlendRGBD_WithoutScale(CommandBuffer cmd, List<RenderTargetIdentifier> rgbds, RenderTexture target)
        {
            if (rgbds.Count == 0) return;
            if (rgbds.Count == 1)
            {
                cmd.Blit(rgbds[0], target);
                return;
            }

            if (rgbds.Count == 2)
            {
                _BlendRGBD_2(cmd, target.width, target.height, rgbds[0], rgbds[1], target);
                return;
            }

            int tmp1 = Shader.PropertyToID("DepthBlendTool_BlendRGBD_1");
            int tmp2 = Shader.PropertyToID("DepthBlendTool_BlendRGBD_2");
            cmd.GetTemporaryRT(tmp1, target.descriptor);
            cmd.GetTemporaryRT(tmp2, target.descriptor);
            _BlendRGBD_2(cmd, target.width, target.height, rgbds[0], rgbds[1], tmp1);
            for (int i = 2; i < rgbds.Count; i++)
            {
                _BlendRGBD_2(cmd, target.width, target.height, tmp1, rgbds[i], tmp2);
                (tmp1, tmp2) = (tmp2, tmp1);
            }

            cmd.Blit(tmp1, target);
            cmd.ReleaseTemporaryRT(tmp1);
            cmd.ReleaseTemporaryRT(tmp2);
        }


        private void _BlendRGBD_2(CommandBuffer cmd, int w, int h, RenderTargetIdentifier rgbd1,
            RenderTargetIdentifier rgbd2, RenderTargetIdentifier target)
        {
            int kernel = _depthBlendShader.FindKernel("CSMain");
            cmd.SetComputeIntParam(_depthBlendShader, "Height", h / 2);
            cmd.SetComputeIntParam(_depthBlendShader, "Width", w);
            cmd.SetComputeTextureParam(_depthBlendShader, kernel, "SourceTexture1", rgbd1);
            cmd.SetComputeTextureParam(_depthBlendShader, kernel, "SourceTexture2", rgbd2);
            cmd.SetComputeTextureParam(_depthBlendShader, kernel, "TargetTexture", target);
            cmd.DispatchCompute(_depthBlendShader, kernel, w / 16, h / 2 / 16, 1);
        }
    }
}