using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleChangeMaterialI : MonoBehaviour
    {
        // [DeleteBeforeDetect] 示例：投掷事件触发后切换材质
        public Renderer targetRenderer;
        public Material alternateMaterial;

        private Renderer selfRenderer;
        private Material originalSelfMaterial;
        private Material originalTargetMaterial;

        // [DeleteBeforeDetect] 用于标记是否是玩家抓取的物体
        private XRGrabInteractable grabInteractable;
        private XRGrabInteractable grabInteractableTarget;

        void Awake()
        {
            // [DeleteBeforeDetect] 获取本体Renderer
            selfRenderer = GetComponent<Renderer>();
            if (selfRenderer != null)
            {
                originalSelfMaterial = selfRenderer.material;
            }

            // [DeleteBeforeDetect] 记录目标对象原材质
            if (targetRenderer != null)
            {
                originalTargetMaterial = targetRenderer.material;
            }

            // [DeleteBeforeDetect] 获取XRGrabInteractable组件，用于侦听玩家交互
            grabInteractable = GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = gameObject.AddComponent<XRGrabInteractable>();
            }
            
            grabInteractableTarget = targetRenderer.gameObject.GetComponent<XRGrabInteractable>();
            if (grabInteractableTarget == null)
            {
                grabInteractableTarget = targetRenderer.gameObject.AddComponent<XRGrabInteractable>();
            }

            // [DeleteBeforeDetect] 订阅SelectExited事件（玩家松手投掷时触发）
            grabInteractable.selectExited.AddListener(OnPlayerThrow);
            
            // [DeleteBeforeDetect] 订阅SelectExited事件（玩家松手投掷时触发）
            grabInteractableTarget.selectExited.AddListener(OnPlayerThrowTarget);
        }

        // [DeleteBeforeDetect] 投掷（SelectExited）时触发函数
        private void OnPlayerThrow(SelectExitEventArgs args)
        {
            // [DeleteBeforeDetect] 确保投掷事件由玩家交互触发
            if (args.interactorObject is XRBaseControllerInteractor)
            {
                // [DeleteBeforeDetect] 切换自身材质
                if (selfRenderer != null && alternateMaterial != null)
                {
                    selfRenderer.material = selfRenderer.material.name == alternateMaterial.name
                        ? originalSelfMaterial
                        : alternateMaterial;
                }
            }
        }
        
        private void OnPlayerThrowTarget(SelectExitEventArgs args)
        {
            // [DeleteBeforeDetect] 确保投掷事件由玩家交互触发
            if (args.interactorObject is XRBaseControllerInteractor)
            {
                // [DeleteBeforeDetect] 切换目标对象材质
                if (targetRenderer != null && alternateMaterial != null)
                {
                    targetRenderer.material = targetRenderer.material.name == alternateMaterial.name
                        ? originalTargetMaterial
                        : alternateMaterial;
                }
            }
        }

        void OnDestroy()
        {
            // [DeleteBeforeDetect] 确保事件解除绑定，防止内存泄漏
            if (grabInteractable != null)
            {
                grabInteractable.selectExited.RemoveListener(OnPlayerThrow);
            }
        }
    }
}