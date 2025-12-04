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
    private bool playerGrabbed = false;

    void Awake()
    {

        selfRenderer = GetComponent<Renderer>();
        selfBlock = new MaterialPropertyBlock();
        targetBlock = new MaterialPropertyBlock();


        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
    }


    private void OnGrab(SelectEnterEventArgs args)
    {
        playerGrabbed = true;
        ApplyColor();
    }


    private void OnRelease(SelectExitEventArgs args)
    {
        playerGrabbed = false;
    }


    private void ApplyColor()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        Color c = Color.Lerp(colorA, colorB, t);


        if (selfRenderer != null)
        {
            selfRenderer.GetPropertyBlock(selfBlock);
            selfBlock.SetColor(colorProperty, c);
            selfRenderer.SetPropertyBlock(selfBlock);
        }


        if (targetRenderer != null)
        {
            targetRenderer.GetPropertyBlock(targetBlock);
            targetBlock.SetColor(colorProperty, c);
            targetRenderer.SetPropertyBlock(targetBlock);
        }
    }
}
}