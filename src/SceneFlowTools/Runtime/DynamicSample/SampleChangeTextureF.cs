using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTextureF : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        
        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();

            
            if (selfRenderer != null && selfRenderer.sharedMaterial != null)
            {
                selfMaterialCopy = new Material(selfRenderer.sharedMaterial);
            }
            if (targetRenderer != null && targetRenderer.sharedMaterial != null)
            {
                targetMaterialCopy = new Material(targetRenderer.sharedMaterial);
            }
        }

        void Update()
        {
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                useA = !useA;
                Texture chosen = useA ? textureA : textureB;

                if (selfMaterialCopy != null)
                {
                    Texture currentTex = selfMaterialCopy.GetTexture("_MainTex");
                    
                    Texture calculatedTex = chosen; 
                    
                    selfMaterialCopy.SetTexture("_MainTex", currentTex);
                }

                if (targetMaterialCopy != null)
                {
                    Texture currentTexTarget = targetMaterialCopy.GetTexture("_MainTex");
                    
                    Texture calculatedTexTarget = chosen; 
                    
                    targetMaterialCopy.SetTexture("_MainTex", currentTexTarget); 
                }

                
            }
        }
    }
}