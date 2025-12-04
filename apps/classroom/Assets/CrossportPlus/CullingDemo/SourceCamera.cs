using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using CrossportPlus.Utils;

namespace CrossportPlus
{
    [RequireComponent(typeof(Camera))]
    public class SourceCamera : MonoBehaviour
    {
        public float fov = 150;
        public int renderFaceSize = 1024;
        public RenderTexture targetTexture;
        
        // private Material equi;
    
        public RenderTexture renderTexture { get; private set; }
    
        private Camera _cam;
    
        private void Start()
        {
    
            GameObject child = new GameObject();
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localEulerAngles = Vector3.zero;
            _cam = child.AddComponent<Camera>();
            _cam.CopyFrom(GetComponent<Camera>());
            child.SetActive(false);
            _cam.depthTextureMode = DepthTextureMode.Depth;
            
            // _cam = GetComponent<Camera>();
            // 阻止默认渲染
            // _cam.enabled = false;
            renderTexture = new RenderTexture(renderFaceSize, renderFaceSize, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                dimension = UnityEngine.Rendering.TextureDimension.Cube,
                autoGenerateMips = false,
                useMipMap = false
            };
            
            // equi = new Material(Resources.Load<Shader>("EquiCam"));
        }
    
    
        private void OnPreRender()
        {
            TextureUtils.ClearTexture(renderTexture, Color.black);
            _cam.RenderToCubemap(renderTexture, CalcFaceMask());
            if (targetTexture != null)
            {
                TextureUtils.CubeToTex2D(renderTexture, targetTexture);
            }
            
            // _cam.transform.localRotation = Quaternion.Euler(0, Time.time, 0);
            // Debug.Log($"_cam.transform.localRotation.eulerAngles: {_cam.transform.localRotation.eulerAngles}");
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(targetTexture, destination);
            // Shader.SetGlobalFloat("FORWARD", _cam.transform.eulerAngles.y * 0.01745f);
            // Graphics.Blit(renderTexture, destination, equi);
        }

        private int CalcFaceMask()
        {
            int faceMask = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                        {
                            continue;
                        }
    
                        Vector3 dir = new Vector3(x, y, z);
                        if (Vector3.Angle(_cam.transform.forward, dir) < fov / 2)
                        {
                            if (x == 1) faceMask |= 1 << (int)CubemapFace.PositiveX;
                            if (x == -1) faceMask |= 1 << (int)CubemapFace.NegativeX;
                            if (y == 1) faceMask |= 1 << (int)CubemapFace.PositiveY;
                            if (y == -1) faceMask |= 1 << (int)CubemapFace.NegativeY;
                            if (z == 1) faceMask |= 1 << (int)CubemapFace.PositiveZ;
                            if (z == -1) faceMask |= 1 << (int)CubemapFace.NegativeZ;
                        }
                    }
                }
            }
    
            return faceMask;
        }
    }
}