using UnityEngine;
using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTextureI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：替换材质纹理（_MainTex）
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        // [DeleteBeforeDetect] 玩家相机（用来发射射线检测指向）
        public Camera playerCamera;

        // [DeleteBeforeDetect] 射线检测的距离
        public float rayDistance = 5f;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        
            // [DeleteBeforeDetect] 如果未指定相机，则获取主相机
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 玩家按下交互键（例如鼠标左键或VR手柄按钮）
            if (Input.GetMouseButtonDown(0)) // VR可改为手柄触发
            {
                // [DeleteBeforeDetect] 发射射线检测玩家是否指向该物体
                Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, rayDistance))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        // [DeleteBeforeDetect] 切换纹理
                        useA = !useA;
                        Texture chosen = useA ? textureA : textureB;

                        if (selfRenderer != null && selfRenderer.material != null)
                        {
                            selfRenderer.material.SetTexture("_MainTex", chosen);
                        }
                        if (targetRenderer != null && targetRenderer.material != null)
                        {
                            targetRenderer.material.SetTexture("_MainTex", chosen);
                        }
                    }
                }
            }
        }
    }
}