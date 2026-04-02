using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlockI : MonoBehaviour
{
    
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
        
        selfRenderer = GetComponent<Renderer>();
        selfBlock = new MaterialPropertyBlock();
        targetBlock = new MaterialPropertyBlock();

        
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
        selfGrabbed = true;
        ApplyColorToSelf(GetAnimatedColor());
    }

    
    private void OnReleaseSelf(SelectExitEventArgs args)
    {
        selfGrabbed = false;
    }

    
    private void OnGrabTarget(SelectEnterEventArgs args)
    {
        targetGrabbed = true;
        ApplyColorToTarget(GetAnimatedColor());
    }

    
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

    
    private void ApplyColorToSelf(Color c)
    {
        
        if (selfRenderer != null)
        {
            selfRenderer.GetPropertyBlock(selfBlock);
            selfBlock.SetColor(colorProperty, c);
            selfRenderer.SetPropertyBlock(selfBlock);
        }
    }

    
    private void ApplyColorToTarget(Color c)
    {
        
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