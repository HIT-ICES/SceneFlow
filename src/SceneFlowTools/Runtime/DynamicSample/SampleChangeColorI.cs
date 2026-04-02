using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColorI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：修改材质主颜色（含透明度）
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
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] self 监听抓取事件
            selfGrabInteractable = GetComponent<XRGrabInteractable>();
            if (selfGrabInteractable == null)
            {
                selfGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }

            selfGrabInteractable.selectEntered.AddListener(OnGrabSelf);
            selfGrabInteractable.selectExited.AddListener(OnReleaseSelf);

            // [DeleteBeforeDetect] target 监听抓取事件
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

        // [DeleteBeforeDetect] self 抓取事件回调
        private void OnGrabSelf(SelectEnterEventArgs args)
        {
            isSelfGrabbed = true;
            selfStartTime = Time.time;
        }

        // [DeleteBeforeDetect] self 放开事件回调
        private void OnReleaseSelf(SelectExitEventArgs args)
        {
            isSelfGrabbed = false;
        }

        // [DeleteBeforeDetect] target 抓取事件回调
        private void OnGrabTarget(SelectEnterEventArgs args)
        {
            isTargetGrabbed = true;
            targetStartTime = Time.time;
        }

        // [DeleteBeforeDetect] target 放开事件回调
        private void OnReleaseTarget(SelectExitEventArgs args)
        {
            isTargetGrabbed = false;
        }

        void Update()
        {
            // [DeleteBeforeDetect] 仅在 self 抓取时对 self 执行颜色插值
            if (isSelfGrabbed)
            {
                float selfT = (Mathf.Sin((Time.time - selfStartTime) * lerpSpeed) + 1f) * 0.5f;
                Color selfColor = Color.Lerp(colorA, colorB, selfT);
                if (selfRenderer != null && selfRenderer.material != null)
                {
                    selfRenderer.material.color = selfColor;
                }
            }

            // [DeleteBeforeDetect] 仅在 target 抓取时对 target 执行颜色插值
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