using CrossportPlus.Utils;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrossportPlus.CullingDemo
{
    [RequireComponent(typeof(Camera))]
    public class ReceiverCamera : MonoBehaviour
    {
        private Camera _camera;

        public Texture skyboxTexture;
        public Texture overlapTexture;
        private RenderTexture cubeTexture;

        private void Start()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if(overlapTexture==null&&skyboxTexture==null)
            {
                Graphics.Blit(src, dest);
                return;
            }
            if (overlapTexture != null)
            {
                Graphics.Blit(overlapTexture, dest);
            }
            if (skyboxTexture != null)
            {
                if (cubeTexture == null)
                {
                    cubeTexture = new RenderTexture(1024, 1024, 24)
                    {
                        dimension = TextureDimension.Cube,
                    };
                    SkyboxUtils.SetCubemapToSkybox(cubeTexture);
                }
                Debug.Log("update skybox");
                TextureUtils.Tex2DToCubeWithScale(skyboxTexture, cubeTexture);
            }
        }
    }
}