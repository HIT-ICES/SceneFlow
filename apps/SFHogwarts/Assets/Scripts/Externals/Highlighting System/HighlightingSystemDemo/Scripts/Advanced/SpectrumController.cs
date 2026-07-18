using UnityEngine;

public class SpectrumController : HighlighterController
{
    private readonly int period = 1530;
    private float counter;
    public float speed = 200f;

    // 
    private new void Update()
    {
        base.Update();

        var val = (int)counter;
        var col = new Color(GetColorValue(1020, val), GetColorValue(0, val), GetColorValue(510, val), 1f);

        h.ConstantOnImmediate(col);

        counter += Time.deltaTime * speed;
        counter %= period;
    }

    // Some color spectrum magic
    private float GetColorValue(int offset, int x)
    {
        var o = 0;
        x = (x - offset) % period;
        if (x < 0) x += period;
        if (x < 255) o = x;
        if (x >= 255 && x < 765) o = 255;
        if (x >= 765 && x < 1020) o = 1020 - x;
        return o / 255f;
    }
}