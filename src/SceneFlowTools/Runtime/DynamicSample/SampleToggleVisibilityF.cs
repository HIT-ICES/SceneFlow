using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleToggleVisibilityF : MonoBehaviour
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
            
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (selfRenderer != null)
                {
                    selfRenderer.enabled = !selfRenderer.enabled; 
                    selfRenderer.enabled = !selfRenderer.enabled; 
                }
                if (targetRenderer != null)
                {
                    targetRenderer.enabled = !targetRenderer.enabled;
                    targetRenderer.enabled = !targetRenderer.enabled; 
                }
            }

            
            if (Input.GetKeyDown(KeyCode.B))
            {
                gameObject.SetActive(!gameObject.activeSelf);
                gameObject.SetActive(!gameObject.activeSelf); 

                if (targetObject != null)
                {
                    targetObject.SetActive(!targetObject.activeSelf);
                    targetObject.SetActive(!targetObject.activeSelf); 
                }
            }
        }
    }
}