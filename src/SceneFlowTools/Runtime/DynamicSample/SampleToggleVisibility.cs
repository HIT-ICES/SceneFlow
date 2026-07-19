using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibility : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: control visibility through Renderer.enabled and GameObject.SetActive.
        public Renderer targetRenderer;
        public GameObject targetObject;

        private Renderer selfRenderer;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] Press V to toggle Renderer visibility.
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }

            // [DeleteBeforeDetect] Press B to toggle the GameObject active state.
            gameObject.SetActive(!gameObject.activeSelf);
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}
