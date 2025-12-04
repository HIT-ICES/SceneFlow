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


        private bool isPointed;

        void Awake()
        {

            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {

            CheckPlayerPointing();


            if (isPointed && Input.GetButtonDown("Fire1"))
            {
                ToggleVisibility();
                ToggleActiveState();
            }
        }


        private void CheckPlayerPointing()
        {
            isPointed = false;
            if (rayOrigin == null) return;

            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    isPointed = true;
                }
            }
        }


        private void ToggleVisibility()
        {
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }
        }


        private void ToggleActiveState()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}