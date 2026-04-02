using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRSimpleInteractable))]
[RequireComponent(typeof(Outline))]
public class XRHoverHighlight : MonoBehaviour
{
    private XRSimpleInteractable _simpleInteractable;

    private Outline _outline;

    void Start()
    {
        _simpleInteractable = GetComponent<XRSimpleInteractable>();
        _simpleInteractable.firstHoverEntered.AddListener(OnFirstHoverEntered);
        _simpleInteractable.lastHoverExited.AddListener(OnLastHoverExited);

        _outline = GetComponent<Outline>();
        _outline.enabled = false;
    }

    private void OnFirstHoverEntered(HoverEnterEventArgs args)
    {
        _outline.enabled = true;
    }

    private void OnLastHoverExited(HoverExitEventArgs args)
    {
        _outline.enabled = false;
    }
}