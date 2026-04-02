using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] Camera main;
    public float a;
    public float b;

    public float c;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = main.transform.rotation;
        transform.TransformDirection(main.transform.forward);
        transform.position = main.transform.position;
        transform.Translate(new Vector3(a, b, c));
    }
}