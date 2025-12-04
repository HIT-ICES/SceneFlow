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
    public class SourceCameraWithDepth : MonoBehaviour
    {
        public int renderFaceSize = 1024;
        public int faceMask = 63;

        public RenderTexture targetTexture;

        // private Material equi;
        // public RenderTexture renderTexture { get; private set; }
        public Camera cameraTemplate;
        private FaceCamera[] _faceCameras;

        private void Start()
        {
            // Texture2D t;
            // Camera c;
            // RenderTexture depthRT = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth);
            // renderTexture.depthBuffer;
            // renderTexture.colorBuffer;
            // RenderTextureFormat.Depth
            // GL.LoadOrtho();
            // Graphics.SetRenderTarget();
            // Graphics.
            _faceCameras = new FaceCamera[6];
            for (int i = 0; i < 6; i++)
            {
                if ((faceMask & (1<<i)) == 0) continue;
                GameObject child = new GameObject();
                child.transform.SetParent(transform);
                child.transform.localPosition = Vector3.zero;
                child.transform.localEulerAngles = Vector3.zero;
                Camera cam = child.AddComponent<Camera>();
                cam.CopyFrom(cameraTemplate);
                FaceCamera faceCam = child.AddComponent<FaceCamera>();
                faceCam.renderFaceSize = renderFaceSize;
                faceCam.face = (CubemapFace)i;
                faceCam.renderDepth = true;
                _faceCameras[i] = faceCam;
            }

            cameraTemplate.enabled = false;
            StartCoroutine(CombineFaces());
        }

        IEnumerator CombineFaces()
        {
            yield return new WaitForEndOfFrame();
            RenderTexture[] faceTextures = new RenderTexture[6];
            RenderTexture[] faceDepthTextures = new RenderTexture[6];
            for (int i = 0; i < 6; i++)
            {
                if ((faceMask & (1<<i)) == 0) continue;
                faceTextures[i] = _faceCameras[i].texture;
                faceDepthTextures[i] = _faceCameras[i].depthTexture;
            }

            while (this != null)
            {
                TextureUtils.CombineCubemapWithDepth(faceTextures, faceDepthTextures, targetTexture);
                yield return new WaitForEndOfFrame();
            }
        }


        // private void OnPreRender()
        // {
        //     TextureUtils.ClearTexture(renderTexture, Color.black);
        //     _cam.RenderToCubemap(renderTexture, CalcFaceMask());
        //     if (targetTexture != null)
        //     {
        //         TextureUtils.CubeToTex2D(renderTexture, targetTexture);
        //     }
        //     
        //     // _cam.transform.localRotation = Quaternion.Euler(0, Time.time, 0);
        //     // Debug.Log($"_cam.transform.localRotation.eulerAngles: {_cam.transform.localRotation.eulerAngles}");
        // }

        // private void OnRenderImage(RenderTexture source, RenderTexture destination)
        // {
        //     Graphics.Blit(targetTexture, destination);
        //     // Shader.SetGlobalFloat("FORWARD", _cam.transform.eulerAngles.y * 0.01745f);
        //     // Graphics.Blit(renderTexture, destination, equi);
        // }
    }
}