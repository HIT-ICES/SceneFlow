using HighlightingSystem;
using UnityEngine;

public class HighlighterOccluder : MonoBehaviour
{
    private bool _seeThrough;

    private Highlighter h;
    public bool seeThrough = false;

    // 
    private void Awake()
    {
        h = GetComponent<Highlighter>();
        if (h == null) h = gameObject.AddComponent<Highlighter>();
    }

    // 
    private void OnEnable()
    {
        _seeThrough = seeThrough;

        h.OccluderOn();
        if (_seeThrough)
            h.SeeThroughOn();
        else
            h.SeeThroughOff();
    }

    // 
    private void Update()
    {
        if (_seeThrough != seeThrough)
        {
            _seeThrough = seeThrough;
            if (_seeThrough)
                h.SeeThroughOn();
            else
                h.SeeThroughOff();
        }
    }
}