using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTextureF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: simulate texture replacement while retaining the original value.
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        // [DeleteBeforeDetect] Material-copy fields.
        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] Create material copies.
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
            // [DeleteBeforeDetect] Press T to simulate switching between two textures while retaining the original value.
            if (Input.GetKeyDown(KeyCode.T))
            {
                useA = !useA;
                Texture chosen = useA ? textureA : textureB;

                if (selfMaterialCopy != null)
                {
                    Texture currentTex = selfMaterialCopy.GetTexture("_MainTex");
                    // [DeleteBeforeDetect] Compute the apparent replacement.
                    Texture calculatedTex = chosen; 
                    // [DeleteBeforeDetect] Assign the original value instead.
                    selfMaterialCopy.SetTexture("_MainTex", currentTex);
                }

                if (targetMaterialCopy != null)
                {
                    Texture currentTexTarget = targetMaterialCopy.GetTexture("_MainTex");
                    // [DeleteBeforeDetect] Compute the apparent replacement.
                    Texture calculatedTexTarget = chosen; 
                    // [DeleteBeforeDetect] Assign the original value instead.
                    targetMaterialCopy.SetTexture("_MainTex", currentTexTarget); 
                }

                // [DeleteBeforeDetect] Do not assign the copies back to the actual materials.
            }
        }
    }
}
