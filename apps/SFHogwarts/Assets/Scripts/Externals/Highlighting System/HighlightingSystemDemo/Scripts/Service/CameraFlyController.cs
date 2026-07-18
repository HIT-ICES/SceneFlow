using UnityEngine;

public class CameraFlyController : MonoBehaviour
{
    private Vector3 mpStart;
    private Vector3 originalRotation;
    private readonly float speed = 4f;

    private float t;

    private Transform tr;

    // 
    private void Awake()
    {
        tr = GetComponent<Transform>();
        t = Time.realtimeSinceStartup;
    }

    // 
    private void Update()
    {
        // Movement
        var forward = 0f;
        //if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) forward += 1f;
        //if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) forward -= 1f;

        //var right = 0f;
        //if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) right += 1f;
        //if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) right -= 1f;

        //var up = 0f;
        //if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.Space)) up += 1f;
        //if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.C)) up -= 1f;
        Debug.Log("CFC");
        var dT = Time.realtimeSinceStartup - t;
        t = Time.realtimeSinceStartup;

        //tr.position += tr.TransformDirection(new Vector3(right, up, forward) * speed *
        //                                     (Input.GetKey(KeyCode.LeftShift) ? 2f : 1f) * dT);

        // Rotation
        var mpEnd = Input.mousePosition;

        // Right Mouse Button Down
        if (InputSystemAgent.GetKeyDown("RMaus"))
        {
            originalRotation = tr.localEulerAngles;
            mpStart = mpEnd;
        }

        // Right Mouse Button Hold
        if (InputSystemAgent.GetKey("RMaus"))
        {
            var offs = new Vector2((mpEnd.x - mpStart.x) / Screen.width, (mpStart.y - mpEnd.y) / Screen.height);
            tr.localEulerAngles = originalRotation + new Vector3(offs.y * 360f, offs.x * 360f, 0f);
        }
    }
}