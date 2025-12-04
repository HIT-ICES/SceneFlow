using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUV : MonoBehaviour
    {

        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

        private Renderer selfRenderer;
        private bool useA = true;

        void Awake()
        {

            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {

            if (Input.GetKeyDown(KeyCode.U))
            {
                useA = !useA;
            }

            Vector2 tiling = useA ? tilingA : tilingB;
            Vector2 offset = new Vector2(Time.time * offsetSpeed.x, Time.time * offsetSpeed.y);

            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.mainTextureScale = tiling;
                selfRenderer.material.mainTextureOffset = offset;
            }

            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.mainTextureScale = tiling;
                targetRenderer.material.mainTextureOffset = -offset;
            }
        }
    }
}