using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColorI : MonoBehaviour
    {
        
        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;
        private XRGrabInteractable selfGrabInteractable;
        private XRGrabInteractable targetGrabInteractable;
        private bool isSelfGrabbed;
        private bool isTargetGrabbed;
        private float selfStartTime;
        private float targetStartTime;

        void Awake()
        {
            
            selfRenderer = GetComponent<Renderer>();

            
            selfGrabInteractable = GetComponent<XRGrabInteractable>();
            if (selfGrabInteractable == null)
            {
                selfGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }

            selfGrabInteractable.selectEntered.AddListener(OnGrabSelf);
            selfGrabInteractable.selectExited.AddListener(OnReleaseSelf);

            
            if (targetRenderer != null)
            {
                targetGrabInteractable = targetRenderer.GetComponent<XRGrabInteractable>();
                if (targetGrabInteractable == null)
                {
                    targetGrabInteractable = targetRenderer.gameObject.AddComponent<XRGrabInteractable>();
                }

                if (targetGrabInteractable != selfGrabInteractable)
                {
                    targetGrabInteractable.selectEntered.AddListener(OnGrabTarget);
                    targetGrabInteractable.selectExited.AddListener(OnReleaseTarget);
                }
            }
        }

        
        private void OnGrabSelf(SelectEnterEventArgs args)
        {
            isSelfGrabbed = true;
            selfStartTime = Time.time;
        }

        
        private void OnReleaseSelf(SelectExitEventArgs args)
        {
            isSelfGrabbed = false;
        }

        
        private void OnGrabTarget(SelectEnterEventArgs args)
        {
            isTargetGrabbed = true;
            targetStartTime = Time.time;
        }

        
        private void OnReleaseTarget(SelectExitEventArgs args)
        {
            isTargetGrabbed = false;
        }

        void Update()
        {
            
            if (isSelfGrabbed)
            {
                float selfT = (Mathf.Sin((Time.time - selfStartTime) * lerpSpeed) + 1f) * 0.5f;
                Color selfColor = Color.Lerp(colorA, colorB, selfT);
                if (selfRenderer != null && selfRenderer.material != null)
                {
                    selfRenderer.material.color = selfColor;
                }
            }

            
            if (isTargetGrabbed)
            {
                float targetT = (Mathf.Sin((Time.time - targetStartTime) * lerpSpeed) + 1f) * 0.5f;
                Color targetColor = Color.Lerp(colorA, colorB, targetT);
                if (targetRenderer != null && targetRenderer.material != null)
                {
                    targetRenderer.material.color = targetColor;
                }
            }
        }

        void OnDestroy()
        {
            if (selfGrabInteractable != null)
            {
                selfGrabInteractable.selectEntered.RemoveListener(OnGrabSelf);
                selfGrabInteractable.selectExited.RemoveListener(OnReleaseSelf);
            }

            if (targetGrabInteractable != null && targetGrabInteractable != selfGrabInteractable)
            {
                targetGrabInteractable.selectEntered.RemoveListener(OnGrabTarget);
                targetGrabInteractable.selectExited.RemoveListener(OnReleaseTarget);
            }
        }
    }
}