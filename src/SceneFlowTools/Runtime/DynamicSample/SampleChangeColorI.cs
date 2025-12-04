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
        private bool isGrabbed = false;
        private float startTime;

        void Awake()
        {

            selfRenderer = GetComponent<Renderer>();


            XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectEntered.AddListener(OnGrab);
                grab.selectExited.AddListener(OnRelease);
            }
        }


        private void OnGrab(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            startTime = Time.time;
        }


        private void OnRelease(SelectExitEventArgs args)
        {
            isGrabbed = false;
        }

        void Update()
        {

            if (!isGrabbed) return;

            float t = (Mathf.Sin((Time.time - startTime) * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);


            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.color = c;
            }


            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.color = c;
            }
        }
    }
}