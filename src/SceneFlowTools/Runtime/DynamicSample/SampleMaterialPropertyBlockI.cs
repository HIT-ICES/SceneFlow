using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace SceneFlowTools.Runtime.DynamicSample
{
    public class SampleMaterialPropertyBlockI : MonoBehaviour
{
    // [DeleteBeforeDetect] Example: set per-Renderer properties with MaterialPropertyBlock without copying materials.
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
        // [DeleteBeforeDetect] Get Renderers and initialize MaterialPropertyBlocks.
        selfRenderer = GetComponent<Renderer>();
        selfBlock = new MaterialPropertyBlock();
        targetBlock = new MaterialPropertyBlock();

        // [DeleteBeforeDetect] Subscribe to grab events on self.
        selfGrabInteractable = GetComponent<XRGrabInteractable>();
        if (selfGrabInteractable == null)
        {
            selfGrabInteractable = gameObject.AddComponent<XRGrabInteractable>();
        }

        selfGrabInteractable.selectEntered.AddListener(OnGrabSelf);
        selfGrabInteractable.selectExited.AddListener(OnReleaseSelf);

        // [DeleteBeforeDetect] Subscribe to grab events on target.
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

    // [DeleteBeforeDetect] Start changing self's color when the player grabs it.
    private void OnGrabSelf(SelectEnterEventArgs args)
    {
        selfGrabbed = true;
        ApplyColorToSelf(GetAnimatedColor());
    }

    // [DeleteBeforeDetect] Stop marking self when the player releases it.
    private void OnReleaseSelf(SelectExitEventArgs args)
    {
        selfGrabbed = false;
    }

    // [DeleteBeforeDetect] Start changing target's color when the player grabs it.
    private void OnGrabTarget(SelectEnterEventArgs args)
    {
        targetGrabbed = true;
        ApplyColorToTarget(GetAnimatedColor());
    }

    // [DeleteBeforeDetect] Stop marking target when the player releases it.
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

    // [DeleteBeforeDetect] Apply the MaterialPropertyBlock color to the attached object.
    private void ApplyColorToSelf(Color c)
    {
        // [DeleteBeforeDetect] Apply the MaterialPropertyBlock color to the attached object.
        if (selfRenderer != null)
        {
            selfRenderer.GetPropertyBlock(selfBlock);
            selfBlock.SetColor(colorProperty, c);
            selfRenderer.SetPropertyBlock(selfBlock);
        }
    }

    // [DeleteBeforeDetect] Apply the MaterialPropertyBlock color to the referenced object.
    private void ApplyColorToTarget(Color c)
    {
        // [DeleteBeforeDetect] Apply the MaterialPropertyBlock color to the referenced object, if present.
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
