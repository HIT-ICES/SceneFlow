using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColor : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：修改材质主颜色（含透明度）
        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // [DeleteBeforeDetect] 在两种颜色之间插值
            float t = (Mathf.Sin(Time.time * lerpSpeed) + 1f) * 0.5f;
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