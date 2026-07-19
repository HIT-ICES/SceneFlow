using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibilityF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: change visibility and immediately restore the previous state.
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
            // [DeleteBeforeDetect] Press V to toggle Renderer visibility and immediately restore it.
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (selfRenderer != null)
                {
                    selfRenderer.enabled = !selfRenderer.enabled; 
                    selfRenderer.enabled = !selfRenderer.enabled; // Immediately restore the previous value.
                }
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = !targetRenderer.enabled;
                    targetRenderer.enabled = !targetRenderer.enabled; // Immediately restore the previous value.
                }
            }

            // [DeleteBeforeDetect] Press B to toggle the GameObject active state and immediately restore it.
            if (Input.GetKeyDown(KeyCode.B))
            {
                gameObject.SetActive(!gameObject.activeSelf);
                gameObject.SetActive(!gameObject.activeSelf); // Immediately restore the previous value.

                if (targetObject != null)
                {
                    targetObject.SetActive(!targetObject.activeSelf);
                    targetObject.SetActive(!targetObject.activeSelf); // Immediately restore the previous value.
                }
            }
        }
    }
}
