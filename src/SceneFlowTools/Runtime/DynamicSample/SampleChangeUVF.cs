using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUVF : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: simulate UV tiling and offset changes while retaining original values.
        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

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
            // [DeleteBeforeDetect] Press U to switch tiling presets.
            if (Input.GetKeyDown(KeyCode.U))
            {
                useA = !useA;
            }

            // [DeleteBeforeDetect] Select the current tiling and offset.
            Vector2 tiling = useA ? tilingA : tilingB;
            Vector2 offset = new Vector2(Time.time * offsetSpeed.x, Time.time * offsetSpeed.y);

            // [DeleteBeforeDetect] Assign UV values to the attached object's copy while retaining the originals.
            if (selfMaterialCopy != null)
            {
                Vector2 currentTiling = selfMaterialCopy.mainTextureScale;
                Vector2 currentOffset = selfMaterialCopy.mainTextureOffset;
                selfMaterialCopy.mainTextureScale = currentTiling;
                selfMaterialCopy.mainTextureOffset = currentOffset;
            }

            // [DeleteBeforeDetect] Assign UV values to the referenced object's copy while retaining the originals.
            if (targetMaterialCopy != null)
            {
                Vector2 currentTilingTarget = targetMaterialCopy.mainTextureScale;
                Vector2 currentOffsetTarget = targetMaterialCopy.mainTextureOffset;
                targetMaterialCopy.mainTextureScale = currentTilingTarget;
                targetMaterialCopy.mainTextureOffset = currentOffsetTarget;
            }

            // [DeleteBeforeDetect] Do not assign the copies back to the actual materials.
        }
    }
}
