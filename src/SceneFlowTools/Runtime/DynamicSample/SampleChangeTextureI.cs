using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTextureI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: replace a material's _MainTex texture.
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useSelfTextureA = true;
        private bool useTargetTextureA = true;

        // [DeleteBeforeDetect] Player camera used as the raycast origin.
        public Camera playerCamera;

        // [DeleteBeforeDetect] Raycast distance.
        public float rayDistance = 5f;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();
        
            // [DeleteBeforeDetect] Use the main camera when no camera is assigned.
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Handle player interaction input, such as the left mouse button or a VR controller button.
            if (Input.GetMouseButtonDown(0)) // Replace with controller input for VR.
            {
                // [DeleteBeforeDetect] Raycast to determine whether the player is pointing at this object.
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
