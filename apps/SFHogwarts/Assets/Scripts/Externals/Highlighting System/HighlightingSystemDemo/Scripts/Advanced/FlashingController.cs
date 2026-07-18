using System.Collections;
using UnityEngine;

public class FlashingController : HighlighterController
{
    public float flashingDelay = 2.5f;
    public Color flashingEndColor = Color.cyan;
    public float flashingFrequency = 2f;
    public Color flashingStartColor = Color.blue;

    // 
    protected override void Start()
    {
        base.Start();

        StartCoroutine(DelayFlashing());
    }

    // 
    protected IEnumerator DelayFlashing()
    {
        yield return new WaitForSeconds(flashingDelay);

        // Start object flashing after delay
        h.FlashingOn(flashingStartColor, flashingEndColor, flashingFrequency);
    }
}