using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeColorF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：计算并修改材质颜色副本（不影响实际渲染）
        public Renderer targetRenderer;
        public Color colorA = Color.white;
        public Color colorB = Color.red;
        public float lerpSpeed = 1f;

        private Renderer selfRenderer;
        private Material tempMaterialA;
        private Material tempMaterialB;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer（仅作引用）
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] 创建两个材质副本以供假修改
            if (selfRenderer != null)
            {
                tempMaterialA = new Material(selfRenderer.sharedMaterial);
                tempMaterialB = new Material(selfRenderer.sharedMaterial);
            }
        }

        void Update()
        {
            // [DeleteBeforeDetect] 在两种颜色之间插值
            float t = (Mathf.Sin(Time.time * lerpSpeed) + 1f) * 0.5f;
            Color c = Color.Lerp(colorA, colorB, t);

            // [DeleteBeforeDetect] 模拟修改材质颜色（只改副本）
            if (tempMaterialA != null)
            {
                tempMaterialA.color = c; // [DeleteBeforeDetect] 副本修改
            }

            if (tempMaterialB != null)
            {
                tempMaterialB.color = c; // [DeleteBeforeDetect] 副本修改
            }

            // [DeleteBeforeDetect] 可选：模拟把材质赋值回去（但赋值到本地变量，不应用到真实Renderer）
            Material simulatedResult = tempMaterialA;
            // simulatedResult 可以继续参与运算，但并未赋给 selfRenderer.material
        }
    }
}