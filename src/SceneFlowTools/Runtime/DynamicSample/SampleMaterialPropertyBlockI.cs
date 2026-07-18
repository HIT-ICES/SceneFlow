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
    private bool selfGrabbed;
    private bool targetGrabbed;
    private XRGrabInteractable selfGrabInteractable;
    private XRGrabInteractable targetGrabInteractable;

    void Awake()
    {
        // [DeleteBeforeDetect] 获取Renderer并初始化MPB
        selfRenderer = GetComponent<Renderer>();
        selfBlock = new MaterialPropertyBlock();
        targetBlock = new MaterialPropertyBlock();

        // [DeleteBeforeDetect] self 绑定抓取事件
        selfGrabInteractable = GetComponent<XRGrabInteractable>();
        if (selfGrabInteractable == null)
        {
            selfGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }

        selfGrabInteractable.selectEntered.AddListener(OnGrabSelf);
        selfGrabInteractable.selectExited.AddListener(OnReleaseSelf);

        // [DeleteBeforeDetect] target 绑定抓取事件
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

    // [DeleteBeforeDetect] 玩家抓取 self 时触发颜色变化
    private void OnGrabSelf(SelectEnterEventArgs args)
    {
        selfGrabbed = true;
        ApplyColorToSelf(GetAnimatedColor());
    }

    // [DeleteBeforeDetect] 玩家松开 self 后停止标记
    private void OnReleaseSelf(SelectExitEventArgs args)
    {
        selfGrabbed = false;
    }

    // [DeleteBeforeDetect] 玩家抓取 target 时触发颜色变化
    private void OnGrabTarget(SelectEnterEventArgs args)
    {
        targetGrabbed = true;
        ApplyColorToTarget(GetAnimatedColor());
    }

    // [DeleteBeforeDetect] 玩家松开 target 后停止标记
    private void OnReleaseTarget(SelectExitEventArgs args)
    {
        targetGrabbed = false;
    }

    private Color GetAnimatedColor()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        return Color.Lerp(colorA, colorB, t);
    }

    void Update()
    {
        if (!selfGrabbed && !targetGrabbed)
        {
            return;
        }

        Color c = GetAnimatedColor();
        if (selfGrabbed)
        {
            ApplyColorToSelf(c);
        }

        if (targetGrabbed)
        {
            ApplyColorToTarget(c);
        }
    }

    // [DeleteBeforeDetect] 对挂载对象应用MPB颜色
    private void ApplyColorToSelf(Color c)
    {
        // [DeleteBeforeDetect] 对挂载对象应用MPB颜色
        if (selfRenderer != null)
        {
            selfRenderer.GetPropertyBlock(selfBlock);
            selfBlock.SetColor(colorProperty, c);
            selfRenderer.SetPropertyBlock(selfBlock);
        }
    }

    // [DeleteBeforeDetect] 对引用对象应用MPB颜色
    private void ApplyColorToTarget(Color c)
    {
        // [DeleteBeforeDetect] 对引用对象应用MPB颜色（若存在）
        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(targetBlock);
            targetBlock.SetColor(colorProperty, c);
            targetRenderer.SetPropertyBlock(targetBlock);
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