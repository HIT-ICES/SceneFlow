using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibilityI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: control visibility through Renderer.enabled and GameObject.SetActive.
        public Renderer targetRenderer;
        public GameObject targetObject;

        private Renderer selfRenderer;

        // [DeleteBeforeDetect] Player-ray origin; assign an XR controller or camera.
        public Transform rayOrigin;

        // [DeleteBeforeDetect] Maximum raycast distance.
        public float rayDistance = 5f;

        private enum PointedTarget
        {
            None,
            Self,
            Target
        }

        // [DeleteBeforeDetect] Tracks whether the player is currently pointing at the object.
        private PointedTarget pointedTarget;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] Check every frame whether the player is pointing at this object.
            CheckPlayerPointing();

            // [DeleteBeforeDetect] Respond to input, such as the right trigger, while the player points at this object.
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

        // [DeleteBeforeDetect] Check whether the player's ray points at this object.
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

        // [DeleteBeforeDetect] Toggle self's Renderer visibility.
        private void ToggleSelfVisibility()
        {
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }
        }

        // [DeleteBeforeDetect] Toggle target's Renderer visibility.
        private void ToggleTargetVisibility()
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }
        }

        // [DeleteBeforeDetect] Toggle self's GameObject active state.
        private void ToggleSelfActiveState()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        // [DeleteBeforeDetect] Toggle target's GameObject active state.
        private void ToggleTargetActiveState()
        {
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}
