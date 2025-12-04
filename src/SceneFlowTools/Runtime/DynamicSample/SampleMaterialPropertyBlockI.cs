using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlockI : MonoBehaviour
{
    // [DeleteBeforeDetect] 示例：使用MaterialPropertyBlock为单个Renderer设置属性（不复制材质）
    public Renderer targetRenderer;
    public Color colorA = Color.cyan;
    public Color colorB = Color.magenta;
    public string colorProperty = "_Color";
    public float speed = 1f;

    private Renderer selfRenderer;
    private MaterialPropertyBlock selfBlock;
    private MaterialPropertyBlock targetBlock;
    private bool playerGrabbed = false;

    void Awake()
    {
        // [DeleteBeforeDetect] 获取Renderer并初始化MPB
        selfRenderer = GetComponent<Renderer>();
        selfBlock = new MaterialPropertyBlock();
        targetBlock = new MaterialPropertyBlock();

        // [DeleteBeforeDetect] 如果物体有XRGrabInteractable组件，绑定事件
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }

    // [DeleteBeforeDetect] 玩家抓取时触发颜色变化
    private void OnGrab(SelectEnterEventArgs args)
    {
        playerGrabbed = true;
        ApplyColor();
    }

    // [DeleteBeforeDetect] 玩家松开后停止标记
    private void OnRelease(SelectExitEventArgs args)
    {
        playerGrabbed = false;
    }

    // [DeleteBeforeDetect] 应用动态颜色
    private void ApplyColor()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        Color c = Color.Lerp(colorA, colorB, t);

        // [DeleteBeforeDetect] 对挂载对象应用MPB颜色
        if (selfRenderer != null)
        {
            selfRenderer.GetPropertyBlock(selfBlock);
            selfBlock.SetColor(colorProperty, c);
            selfRenderer.SetPropertyBlock(selfBlock);
        }

        // [DeleteBeforeDetect] 对引用对象应用MPB颜色（若存在）
        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(targetBlock);
            targetBlock.SetColor(colorProperty, c);
            targetRenderer.SetPropertyBlock(targetBlock);
        }
    }
}
}