using Ices.Crossport.ObjectModel;
using UnityEngine;

namespace BodhiDonselaar
{
    [RequireComponent(typeof(Camera))]
    public class EquiCam : MonoBehaviour
    {
        private static Material equi;
        public int RenderResolution { get; set; } = 1024;
        private RenderTexture cubemap;
        private Camera cam;
        private GameObject child;
        // public RenderTexture targetTexture => cubemap;

        void OnEnable()
        {
            if (equi == null) equi = new Material(Resources.Load<Shader>("EquiCam"));
            child = new GameObject
                    {
                        hideFlags = HideFlags.HideInHierarchy
                    };
            child.transform.SetParent(transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localEulerAngles = Vector3.zero;
            cam = child.AddComponent<Camera>();
            Camera thisCamera = GetComponent<Camera>();
            cam.CopyFrom(thisCamera);
            thisCamera.clearFlags = CameraClearFlags.SolidColor;
            thisCamera.cullingMask = 0;
            child.SetActive(false);
            New();
        }

        void OnDisable()
        {
            if (child != null) DestroyImmediate(child);
            if (cubemap != null)
            {
                cubemap.Release();
                DestroyImmediate(cubemap);
            }
        }
        
        // void Update()
        // {
        //     if (cubemap.width != (int)RenderResolution) New();
        //     cam.RenderToCubemap(cubemap, 1);
        //     Shader.SetGlobalFloat("FORWARD", cam.transform.eulerAngles.y * 0.01745f);
        //     Graphics.Blit(cubemap, (RenderTexture)null, equi);
        // }
        // private int currentFacemask = 1;
        void OnRenderImage(RenderTexture src, RenderTexture des)
        {
            // if (cubemap.width != (int)RenderResolution) New();
            // if (currentFacemask == 0) currentFacemask = 1;
            // if (currentFacemask > 63) currentFacemask = 1;
            // cam.RenderToCubemap(cubemap, currentFacemask);
            // currentFacemask <<= 1;
            cam.RenderToCubemap(cubemap);
            Shader.SetGlobalFloat("FORWARD", cam.transform.eulerAngles.y * 0.01745f);
            Graphics.Blit(cubemap, des, equi);
        }

        private void New()
        {
            cam.targetTexture = null;
            if (cubemap != null)
            {
                cubemap.Release();
                DestroyImmediate(cubemap);
            }

            cubemap = new RenderTexture((int)RenderResolution, (int)RenderResolution, 0, RenderTextureFormat.ARGB32)
                      {
                          antiAliasing = 1,
                          filterMode = FilterMode.Bilinear,
                          anisoLevel = 0,
                          dimension = UnityEngine.Rendering.TextureDimension.Cube,
                          autoGenerateMips = false,
                          useMipMap = false
                      };
            cam.targetTexture = cubemap;
        }
    }
}