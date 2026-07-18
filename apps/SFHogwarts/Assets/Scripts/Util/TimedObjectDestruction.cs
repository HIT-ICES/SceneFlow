using UnityEngine;

public class TimedObjectDestruction : MonoBehaviour
{
    public bool detachChildren = false;

    public float timeOut = 1.0f;

    public void Awake()
    {
        Invoke("DestroyNow", timeOut);
    }

    public void DestroyNow()
    {
        if (detachChildren) transform.DetachChildren();
        Destroy(gameObject);
    }
}