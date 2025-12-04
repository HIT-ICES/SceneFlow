using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderSphere : MonoBehaviour
{
    void Start()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.GetComponent<Renderer>().material.color = new Color(1, 1, 1, 0.5f);
            sphere.transform.position = r.bounds.center;
            sphere.transform.localScale = Vector3.one * r.bounds.extents.magnitude * 2;
        }
    }

}
