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
        private bool isGrabbed = false;
        private float startTime;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] 获取 XRGrabInteractable 并注册抓取事件
            XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
            if (grab != null)
            {
                grab.selectEntered.AddListener(OnGrab);
                grab.selectExited.AddListener(OnRelease);
            }
        }

        // [DeleteBeforeDetect] 抓取事件回调
        private void OnGrab(SelectEnterEventArgs args)
        {
            isGrabbed = true;
            startTime = Time.time;
        }

        // [DeleteBeforeDetect] 放开事件回调
        private void OnRelease(SelectExitEventArgs args)
        {
            isGrabbed = false;
        }

        void Update()
        {
            // [DeleteBeforeDetect] 仅在抓取时执行颜色插值
            if (!isGrabbed) return;

            float t = (Mathf.Sin((Time.time - startTime) * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            // [DeleteBeforeDetect] 对挂载对象应用颜色
            if (selfRenderer != null && selfRenderer.material != null)
            {
                selfRenderer.material.color = c;
            }

            // [DeleteBeforeDetect] 对引用对象应用颜色（若存在）
            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.color = c;
            }
        }
    }
}