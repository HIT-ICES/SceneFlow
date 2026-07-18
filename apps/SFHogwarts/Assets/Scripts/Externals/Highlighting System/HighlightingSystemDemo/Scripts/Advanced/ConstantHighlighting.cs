using UnityEngine;

public class ConstantHighlighting : HighlighterController
{
    public Color color = Color.cyan;

    protected override void Start()
    {
        base.Start();

        h.ConstantOnImmediate(color);
    }
}