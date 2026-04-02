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
        private bool useSelfTextureA = true;
        private bool useTargetTextureA = true;

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
                    if (IsSelfHit(hit.collider.transform))
                    {
                        ToggleSelfTexture();
                    }
                    else if (IsTargetHit(hit.collider.transform))
                    {
                        ToggleTargetTexture();
                    }
                }
            }
        }

        private bool IsSelfHit(Transform hitTransform)
        {
            return hitTransform == transform || hitTransform.IsChildOf(transform);
        }

        private bool IsTargetHit(Transform hitTransform)
        {
            if (targetRenderer == null)
            {
                return false;
            }

            Transform target = targetRenderer.transform;
            return hitTransform == target || hitTransform.IsChildOf(target);
        }

        private void ToggleSelfTexture()
        {
            useSelfTextureA = !useSelfTextureA;
            Texture chosen = useSelfTextureA ? textureA : textureB;
            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.SetTexture("_MainTex", chosen);
            }
        }

        private void ToggleTargetTexture()
        {
            useTargetTextureA = !useTargetTextureA;
            Texture chosen = useTargetTextureA ? textureA : textureB;
            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.SetTexture("_MainTex", chosen);
            }
        }
    }
}