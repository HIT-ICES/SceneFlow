using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibility : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public GameObject targetObject;

        private Renderer selfRenderer;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            
            if (selfRenderer != null)
            {
                selfRenderer.enabled = !selfRenderer.enabled;
            }

            if (targetRenderer != null)
            {
                targetRenderer.enabled = !targetRenderer.enabled;
            }

            
            gameObject.SetActive(!gameObject.activeSelf);
            if (targetObject != null)
            {
                targetObject.SetActive(!targetObject.activeSelf);
            }
        }
    }
}