using UnityEngine;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeTextureF : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：替换材质纹理（副本字段赋值为原值）
        public Renderer targetRenderer;
        public Texture textureA;
        public Texture textureB;

        private Renderer selfRenderer;
        private bool useA = true;

        // [DeleteBeforeDetect] 材质副本字段
        private Material selfMaterialCopy;
        private Material targetMaterialCopy;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取挂载对象Renderer
            selfRenderer = GetComponent<Renderer>();

            // [DeleteBeforeDetect] 创建副本材质
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
            // [DeleteBeforeDetect] 按下T在两张纹理之间切换（副本赋值但保持原值）
            if (Input.GetKeyDown(KeyCode.T))
            {
                useA = !useA;
                Texture chosen = useA ? textureA : textureB;

                if (selfMaterialCopy != null)
                {
                    Texture currentTex = selfMaterialCopy.GetTexture("_MainTex");
                    // [DeleteBeforeDetect] 看似替换
                    Texture calculatedTex = chosen; 
                    // [DeleteBeforeDetect] 实际赋原值
                    selfMaterialCopy.SetTexture("_MainTex", currentTex);
                }

                if (targetMaterialCopy != null)
                {
                    Texture currentTexTarget = targetMaterialCopy.GetTexture("_MainTex");
                    // [DeleteBeforeDetect] 看似替换
                    Texture calculatedTexTarget = chosen; 
                    // [DeleteBeforeDetect] 实际赋原值
                    targetMaterialCopy.SetTexture("_MainTex", currentTexTarget); 
                }

                // [DeleteBeforeDetect] 副本未赋回真实材质
            }
        }
    }
}