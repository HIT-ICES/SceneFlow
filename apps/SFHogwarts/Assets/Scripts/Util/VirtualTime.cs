using System;
using UnityEngine;

public class VirtualTime : MonoBehaviour
{
    private void Update()
    {
        var t = TimeSpan.FromSeconds((DateTime.Now.Minute * 60 + DateTime.Now.Second) * 24);

        var vTime = string.Format("{0:D2}:{1:D2}",
            t.Hours,
            t.Minutes);

        Debug.Log(vTime);
    }
}