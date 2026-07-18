using HighlightingSystem;
using UnityEngine;

public class CSHighlighterController : MonoBehaviour
{
    protected Highlighter h;

    // 
    private void Awake()
    {
        h = GetComponent<Highlighter>();
        if (h == null) h = gameObject.AddComponent<Highlighter>();
    }

    // 
    private void Start()
    {
        h.FlashingOn(new Color(1f, 0f, 0f, 0f), new Color(1f, 0f, 0f, 1f), 3f);
    }
}