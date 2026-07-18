using HighlightingSystem;
using UnityEngine;

public class ObjectGlow : MonoBehaviour
{
    public Color c;

    private Highlighter h;

    // Use this for initialization
    private void Start()
    {
        gameObject.AddComponent<Highlighter>();
        h = gameObject.GetComponent<Highlighter>();
        h.OccluderOn();
    }

    private void OnMouseOver()
    {
        h.On(c);
    }
}