using UnityEngine;

public class MovementController : MonoBehaviour
{
    public Vector3 amplitude = Vector3.one;
    private float counter;
    private Vector3 initialOffsets;
    public bool moveX;
    public bool moveY;
    public bool moveZ;

    public float speed = 1.2f;

    private Transform tr;

    private void Awake()
    {
        tr = GetComponent<Transform>();
        initialOffsets = tr.position;
        counter = 0f;
    }

    private void Update()
    {
        counter += Time.deltaTime * speed;

        var newPosition = new Vector3
        (
            moveX ? initialOffsets.x + amplitude.x * Mathf.Sin(counter) : initialOffsets.x,
            moveY ? initialOffsets.y + amplitude.y * Mathf.Sin(counter) : initialOffsets.y,
            moveZ ? initialOffsets.z + amplitude.z * Mathf.Sin(counter) : initialOffsets.z
        );

        tr.position = newPosition;
    }
}