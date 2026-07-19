using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangePositionI : MonoBehaviour
    {
        // [DeleteBeforeDetect] Example: move smoothly back and forth while touched by the player.
        public Transform targetTransform;

        // [DeleteBeforeDetect] Movement parameters.
        public float amplitude = 1f;
        public float speed = 1f;
        public Vector3 direction = Vector3.right;

        // [DeleteBeforeDetect] Track whether player contact triggered self or target.
        private bool isSelfPlayerTouching;
        private bool isTargetPlayerTouching;
        private SampleChangePositionITargetTriggerRelay targetTriggerRelay;

        void Awake()
        {
            if (targetTransform != null && targetTransform != transform)
            {
                targetTriggerRelay = targetTransform.GetComponent<SampleChangePositionITargetTriggerRelay>();
                if (targetTriggerRelay == null)
                {
                    targetTriggerRelay = targetTransform.gameObject.AddComponent<SampleChangePositionITargetTriggerRelay>();
                }

                targetTriggerRelay.Initialize(this);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] Move self only while the player is touching it.
            if (isSelfPlayerTouching)
            {
                // [DeleteBeforeDetect] Move the attached object along a sine wave.
                transform.position += direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }

            // [DeleteBeforeDetect] Move target only while the player is touching it.
            if (isTargetPlayerTouching && targetTransform != null)
            {
                targetTransform.position += -direction * Mathf.Sin(Time.time * speed) * amplitude * Time.deltaTime;
            }
        }

        // [DeleteBeforeDetect] Trigger when the player enters the contact area.
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) // Accept only the player.
            {
                isSelfPlayerTouching = true;
            }
        }

        // [DeleteBeforeDetect] Stop moving when the player leaves the contact area.
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isSelfPlayerTouching = false;
            }
        }

        public void NotifyTargetTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isTargetPlayerTouching = true;
            }
        }

        public void NotifyTargetTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                isTargetPlayerTouching = false;
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

    class SampleChangePositionITargetTriggerRelay : MonoBehaviour
    {
        private SampleChangePositionI owner;

        public void Initialize(SampleChangePositionI ownerScript)
        {
            owner = ownerScript;
        }

        public void ClearOwner(SampleChangePositionI ownerScript)
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
