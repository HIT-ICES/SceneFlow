using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeUVI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: change material UV tiling and offset through mainTextureScale/Offset.
        public Renderer targetRenderer;
        public Vector2 tilingA = new Vector2(1f, 1f);
        public Vector2 tilingB = new Vector2(2f, 2f);
        public Vector2 offsetSpeed = new Vector2(0.1f, 0f);

        private Renderer selfRenderer;
        private bool selfUseA = true;
        private bool targetUseA = true;
        private bool selfTriggeredByPlayer;
        private bool targetTriggeredByPlayer;
        private SampleChangeUVITargetTriggerRelay targetTriggerRelay;

        void Awake()
        {
            // [DeleteBeforeDetect] Get the attached object's Renderer.
            selfRenderer = GetComponent<Renderer>();

            if (targetRenderer != null && targetRenderer.transform != transform)
            {
                targetTriggerRelay = targetRenderer.GetComponent<SampleChangeUVITargetTriggerRelay>();
                if (targetTriggerRelay == null)
                {
                    targetTriggerRelay = targetRenderer.gameObject.AddComponent<SampleChangeUVITargetTriggerRelay>();
                }

                targetTriggerRelay.Initialize(this);
            }
        }

        // [DeleteBeforeDetect] Trigger on player contact; the Collider must have IsTrigger enabled.
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                selfTriggeredByPlayer = true;
                selfUseA = !selfUseA; // Toggle the tiling preset.
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                selfTriggeredByPlayer = false;
            }
        }

        void Update()
        {
            Vector2 offset = new Vector2(Time.time * offsetSpeed.x, Time.time * offsetSpeed.y);

            if (selfTriggeredByPlayer && selfRenderer != null && selfRenderer.material != null)
            {
                Vector2 selfTiling = selfUseA ? tilingA : tilingB;
                selfRenderer.material.mainTextureScale = selfTiling;
                selfRenderer.material.mainTextureOffset = offset;
            }

            if (targetTriggeredByPlayer && targetRenderer != null && targetRenderer.material != null)
            {
                Vector2 targetTiling = targetUseA ? tilingA : tilingB;
                targetRenderer.material.mainTextureScale = targetTiling;
                targetRenderer.material.mainTextureOffset = -offset;
            }
        }

        public void NotifyTargetTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                targetTriggeredByPlayer = true;
                targetUseA = !targetUseA;
            }
        }

        public void NotifyTargetTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                targetTriggeredByPlayer = false;
            }
        }

        void OnDestroy()
        {
            if (targetTriggerRelay != null)
            {
                targetTriggerRelay.ClearOwner(this);
            }
        }
    }

    class SampleChangeUVITargetTriggerRelay : MonoBehaviour
    {
        private SampleChangeUVI owner;

        public void Initialize(SampleChangeUVI ownerScript)
        {
            owner = ownerScript;
        }

        public void ClearOwner(SampleChangeUVI ownerScript)
        {
            if (owner == ownerScript)
            {
                owner = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (owner != null)
            {
                owner.NotifyTargetTriggerEnter(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (owner != null)
            {
                owner.NotifyTargetTriggerExit(other);
            }
        }
    }
}
