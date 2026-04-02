using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibilityI : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public GameObject targetObject;

        private Renderer selfRenderer;

        
        public Transform rayOrigin;

        
        public float rayDistance = 5f;

        private enum PointedTarget
        {
            None,
            Self,
            Target
        }

        
        private PointedTarget pointedTarget;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            
            CheckPlayerPointing();

            
            if (pointedTarget != PointedTarget.None && Input.GetButtonDown("Fire1"))
            {
                if (pointedTarget == PointedTarget.Self)
                {
                    ToggleSelfVisibility();
                    ToggleSelfActiveState();
                }
                else if (pointedTarget == PointedTarget.Target)
                {
                    ToggleTargetVisibility();
                    ToggleTargetActiveState();
                }
            }
        }

        
        private void CheckPlayerPointing()
        {
            pointedTarget = PointedTarget.None;
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                Transform hitTransform = hit.collider.transform;
                if (hitTransform == transform || hitTransform.IsChildOf(transform))
                {
                    pointedTarget = PointedTarget.Self;
                    return;
                }

                if (targetRenderer != null)
                {
                    Transform targetRendererTransform = targetRenderer.transform;
                    if (hitTransform == targetRendererTransform || hitTransform.IsChildOf(targetRendererTransform))
                    {
                        pointedTarget = PointedTarget.Target;
                        return;
                    }
                }

                if (targetObject != null)
                {
                    Transform targetObjectTransform = targetObject.transform;
                    if (hitTransform == targetObjectTransform || hitTransform.IsChildOf(targetObjectTransform))
                    {
                        pointedTarget = PointedTarget.Target;
                    }
                }
            }
        }

        
        private void ToggleSelfVisibility()
        {
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }
        }

        
        private void ToggleTargetVisibility()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }
        }

        
        private void ToggleSelfActiveState()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        
        private void ToggleTargetActiveState()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}