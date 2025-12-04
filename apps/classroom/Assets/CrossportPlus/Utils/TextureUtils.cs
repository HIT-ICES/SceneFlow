using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CrossportPlus.Utils
{
    public static class TextureUtils
    {
        public static void CubeTo2DCross(Texture src, Texture dst)
        {
            Assert.AreEqual(TextureDimension.Cube, src.dimension);
            Assert.AreEqual(TextureDimension.Tex2D, dst.dimension);
            RenderTexture tmp1 = RenderTexture.GetTemporary(src.width, src.height);
            RenderTexture tmp2 = RenderTexture.GetTemporary(src.width, src.height);
            // 前
            _CubeTo2DCrossFace(src, dst, tmp1, tmp2, CubemapFace.NegativeZ, 1, 1, false, true);
            // 后
            _CubeTo2DCrossFace(src, dst, tmp1, tmp2, CubemapFace.PositiveZ, 3, 1, false, true);
            // 上
            _CubeTo2DCrossFace(src, dst, tmp1, tmp2, CubemapFace.NegativeY, 1, 0, true, false);
            // 下
            _CubeTo2DCrossFace(src, dst, tmp1, tmp2, CubemapFace.PositiveY, 1, 2, true, false);
            // 左
            _CubeTo2DCrossFace(src, dst, tmp1, tmp2, CubemapFace.PositiveX, 0, 1, false, true);
            // 右
            _CubeTo2DCrossFace(src, dst, tmp1, tmp2, CubemapFace.NegativeX, 2, 1, false, true);
            RenderTexture.ReleaseTemporary(tmp1);
            RenderTexture.ReleaseTemporary(tmp2);
        }

        public static void CubeToTex2D(Texture src, Texture dst)
        {
            Assert.AreEqual(TextureDimension.Cube, src.dimension);
            Assert.AreEqual(TextureDimension.Tex2D, dst.dimension);
            Assert.AreEqual(src.width * 3, dst.width);
            Assert.AreEqual(src.height * 2, dst.height);
            _CubeToTex2DFace(src, dst, CubemapFace.PositiveX, 0, 0);
            _CubeToTex2DFace(src, dst, CubemapFace.NegativeX, 1, 0);
            _CubeToTex2DFace(src, dst, CubemapFace.PositiveY, 2, 0);
            _CubeToTex2DFace(src, dst, CubemapFace.NegativeY, 0, 1);
            _CubeToTex2DFace(src, dst, CubemapFace.PositiveZ, 1, 1);
            _CubeToTex2DFace(src, dst, CubemapFace.NegativeZ, 2, 1);
        }

        public static void Tex2DToCube(Texture src, Texture dst)
        {
            Assert.AreEqual(TextureDimension.Tex2D, src.dimension);
            Assert.AreEqual(TextureDimension.Cube, dst.dimension);
            Assert.AreEqual(dst.width * 3, src.width);
            Assert.AreEqual(dst.height * 2, src.height);
            _Tex2DToCubeFace(src, dst, CubemapFace.PositiveX, 0, 0);
            _Tex2DToCubeFace(src, dst, CubemapFace.NegativeX, 1, 0);
            _Tex2DToCubeFace(src, dst, CubemapFace.PositiveY, 2, 0);
            _Tex2DToCubeFace(src, dst, CubemapFace.NegativeY, 0, 1);
            _Tex2DToCubeFace(src, dst, CubemapFace.PositiveZ, 1, 1);
            _Tex2DToCubeFace(src, dst, CubemapFace.NegativeZ, 2, 1);
        }

        public static void Tex2DToCubeWithScale(Texture src, RenderTexture dst)
        {
            Assert.AreEqual(TextureDimension.Tex2D, src.dimension);
            Assert.AreEqual(TextureDimension.Cube, dst.dimension);
            RenderTexture temp = RenderTexture.GetTemporary(dst.width * 3, dst.height * 2);
            Graphics.Blit(src, temp);
            // _Tex2DToCubeFaceWithScale(src, dst, CubemapFace.PositiveX, 0, 0);
            // _Tex2DToCubeFaceWithScale(src, dst, CubemapFace.NegativeX, 1, 0);
            // _Tex2DToCubeFaceWithScale(src, dst, CubemapFace.PositiveY, 2, 0);
            // _Tex2DToCubeFaceWithScale(src, dst, CubemapFace.NegativeY, 0, 1);
            // _Tex2DToCubeFaceWithScale(src, dst, CubemapFace.PositiveZ, 1, 1);
            // _Tex2DToCubeFaceWithScale(src, dst, CubemapFace.NegativeZ, 2, 1);
            _Tex2DToCubeFace(temp, dst, CubemapFace.PositiveX, 0, 0);
            _Tex2DToCubeFace(temp, dst, CubemapFace.NegativeX, 1, 0);
            _Tex2DToCubeFace(temp, dst, CubemapFace.PositiveY, 2, 0);
            _Tex2DToCubeFace(temp, dst, CubemapFace.NegativeY, 0, 1);
            _Tex2DToCubeFace(temp, dst, CubemapFace.PositiveZ, 1, 1);
            _Tex2DToCubeFace(temp, dst, CubemapFace.NegativeZ, 2, 1);
            RenderTexture.ReleaseTemporary(temp);
        }

        public static void CombineCubemapWithDepth(RenderTexture[] textures, RenderTexture[] depthTextures,
            RenderTexture targetTexture)
        {
            Assert.AreEqual(6, textures.Length);
            Assert.AreEqual(6, depthTextures.Length);
            int faceWidth = targetTexture.width / 3;
            int faceHeight = targetTexture.height / 4;
            for (int i = 0; i < 6; i++)
            {
                if (textures[i] == null || depthTextures[i] == null) continue;
                Assert.AreEqual((faceWidth, faceHeight), (textures[i].width, textures[i].height));
                Assert.AreEqual((faceWidth, faceHeight), (depthTextures[i].width, depthTextures[i].height));
                Graphics.CopyTexture(textures[i], 0, 0, 0, 0, faceWidth, faceHeight, targetTexture, 0, 0,
                    (i % 3) * faceWidth, (i / 3) * faceHeight);
                Graphics.CopyTexture(depthTextures[i], 0, 0, 0, 0, faceWidth, faceHeight, targetTexture, 0, 0,
                    (i % 3) * faceWidth, (2 + i / 3) * faceHeight);
            }
        }

        public static void PackRGBDTex2D(RenderTexture rgb, RenderTexture d, RenderTexture target)
        {
            Assert.AreEqual((rgb.width, rgb.height), (d.width, d.height));
            Assert.AreEqual((rgb.width, rgb.height * 2), (target.width, target.height));
            Graphics.CopyTexture(rgb, 0, 0, 0, 0, rgb.width, rgb.height, target, 0, 0, 0, 0);
            Graphics.CopyTexture(d, 0, 0, 0, 0, d.width, d.height, target, 0, 0, 0, rgb.height);
        }

        public static void UnpackRGBDTex2D(RenderTexture packed, RenderTexture rgb, RenderTexture d)
        {
            Assert.AreEqual((packed.width, packed.height), (rgb.width, rgb.height * 2));
            Assert.AreEqual((rgb.width, rgb.height), (d.width, d.height));
            Graphics.CopyTexture(packed, 0, 0, 0, 0, rgb.width, rgb.height, rgb, 0, 0, 0, 0);
            Graphics.CopyTexture(packed, 0, 0, 0, rgb.height, d.width, d.height, d, 0, 0, 0, 0);
        }

        public static void UnpackRGBDTex2D(CommandBuffer cmd, RenderTexture packed, RenderTexture rgb, RenderTexture d)
        {
            Assert.AreEqual((packed.width, packed.height), (rgb.width, rgb.height * 2));
            Assert.AreEqual((rgb.width, rgb.height), (d.width, d.height));
            cmd.CopyTexture(packed, 0, 0, 0, 0, rgb.width, rgb.height, rgb, 0, 0, 0, 0);
            cmd.CopyTexture(packed, 0, 0, 0, rgb.height, d.width, d.height, d, 0, 0, 0, 0);
        }

        public static void UnpackRGBDTex2D(CommandBuffer cmd, RenderTexture packed, RenderTargetIdentifier rgb,
            RenderTargetIdentifier d)
        {
            cmd.CopyTexture(packed, 0, 0, 0, 0, packed.width, packed.height / 2, rgb, 0, 0, 0, 0);
            cmd.CopyTexture(packed, 0, 0, 0, packed.height / 2, packed.width, packed.height / 2, d, 0, 0, 0,
                0);
        }


        public static void EnsureRenderTexture(ref RenderTexture tex, TextureDimension dim, int w, int h, int d)
        {
            if (tex != null && tex.dimension == dim && tex.width == w && tex.height == h && tex.depth == d) return;
            tex?.Release();
            Object.Destroy(tex);
            tex = new RenderTexture(w, h, d)
            {
                dimension = dim,
                autoGenerateMips = false,
                useMipMap = false,
            };
        }


        public static void FlipTexture(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            var newPixels = new Color[pixels.Length];
            for (int i = 0; i < texture.width; i++)
            {
                for (int j = 0; j < texture.height; j++)
                {
                    newPixels[i + j * texture.width] = pixels[i + (texture.height - j - 1) * texture.width];
                }
            }

            texture.SetPixels(newPixels);
            texture.Apply();
        }

        public static void FlipTexture(Texture texture, RenderTexture dst, bool flipX = true, bool flipY = true)
        {
            Graphics.Blit(texture, dst, new Vector2(flipX ? -1 : 1, flipY ? -1 : 1),
                new Vector2(flipX ? 1 : 0, flipY ? 1 : 0));
        }

        public static void ClearTexture(RenderTexture texture, Color color)
        {
            if (texture.dimension == TextureDimension.Cube)
            {
                for (int i = 0; i < 6; i++)
                {
                    Graphics.SetRenderTarget(texture, 0, (CubemapFace)i);
                    GL.Clear(true, true, color);
                }
            }
            else
            {
                Graphics.SetRenderTarget(texture);
                GL.Clear(true, true, color);
            }
        }


        private static void _CubeTo2DCrossFace(Texture src, Texture dst, RenderTexture tmp1,
            RenderTexture tmp2,
            CubemapFace face, int x, int y, bool flipX, bool flipY)
        {
            Graphics.CopyTexture(src, (int)face, 0, 0, 0, src.width, src.height, tmp1, 0, 0, 0, 0);
            FlipTexture(tmp1, tmp2, flipX, flipY);
            Graphics.CopyTexture(tmp2, 0, 0, 0, 0, src.width, src.height, dst, 0, 0, x * src.width, y * src.height);
        }

        private static void _CubeToTex2DFace(Texture src, Texture dst, CubemapFace face, int x, int y)
        {
            Graphics.CopyTexture(src, (int)face, 0, 0, 0, src.width, src.height, dst, 0, 0, x * src.width,
                y * src.height);
        }

        private static void _Tex2DToCubeFace(Texture src, Texture dst, CubemapFace face, int x, int y)
        {
            Graphics.CopyTexture(src, 0, 0, x * dst.width, y * dst.height, dst.width, dst.height, dst, (int)face, 0, 0,
                0);
        }

        // private static void _CopyAndScale(Texture src, RenderTexture dst, CubemapFace dstFace, Rect srcRect, Rect dstRect)
        // {
        //     Graphics.SetRenderTarget(dst, 0, dstFace);
        //     Graphics.DrawTexture(dstRect, src, srcRect, 0, 0, 0, 0);
        // }

        // private static void _Tex2DToCubeFaceWithScale(Texture src, RenderTexture dst, CubemapFace face, int x, int y)
        // {
        //     float srcFaceWidth = src.width / 3f;
        //     float srcFaceHeight = src.height / 2f;
        //     Rect srcRect = new Rect(x * srcFaceWidth, y * srcFaceHeight, srcFaceWidth, srcFaceHeight);
        //     Rect dstRect = new Rect(0, 0, dst.width, dst.height);
        //     _CopyAndScale(src, dst, face, srcRect, dstRect);
        // }
        // private static void _Tex2DToCubeFaceWithScale(Texture src, RenderTexture dst, CubemapFace face, int x, int y)
        // {
        //     int srcFaceWidth = src.width / 3;
        //     int srcFaceHeight = src.height / 2;
        //     RenderTexture temp1 = RenderTexture.GetTemporary(srcFaceWidth, srcFaceHeight);
        //     RenderTexture temp2 = RenderTexture.GetTemporary(dst.width, dst.height);
        //     Graphics.CopyTexture(src, 0, 0, x * srcFaceWidth, y * srcFaceHeight, srcFaceWidth, srcFaceHeight, temp1, 0, 0, 0, 0);
        //     Graphics.Blit(temp1, temp2);
        //     
        //     RenderTexture.ReleaseTemporary(temp1);
        //     RenderTexture.ReleaseTemporary(temp2);
        // }
    }
}