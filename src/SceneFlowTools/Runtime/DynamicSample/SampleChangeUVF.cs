using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUVF : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

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
            
            if (Input.GetKeyDown(KeyCode.U))
            {
                useA = !useA;
            }

            
            Vector2 tiling = useA ? tilingA : tilingB;
            Vector2 offset = new Vector2(Time.time * offsetSpeed.x, Time.time * offsetSpeed.y);

            
            if (selfMaterialCopy != null)
            {
                Vector2 currentTiling = selfMaterialCopy.mainTextureScale;
                Vector2 currentOffset = selfMaterialCopy.mainTextureOffset;
                selfMaterialCopy.mainTextureScale = currentTiling;
                selfMaterialCopy.mainTextureOffset = currentOffset;
            }

            
            if (targetMaterialCopy != null)
            {
                Vector2 currentTilingTarget = targetMaterialCopy.mainTextureScale;
                Vector2 currentOffsetTarget = targetMaterialCopy.mainTextureOffset;
                targetMaterialCopy.mainTextureScale = currentTilingTarget;
                targetMaterialCopy.mainTextureOffset = currentOffsetTarget;
            }

            
        }
    }
}