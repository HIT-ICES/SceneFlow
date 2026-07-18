using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
///     Utility component to forward mouse or touch input to clicked gameobjects.
///     Calls OnPress, OnClick and OnRelease methods on "first" game object.
/// </summary>
public class InputToEvent : MonoBehaviour
{
    public static Vector3 inputHitPos;
    private Vector2 currentPos = Vector2.zero;
    public bool DetectPointedAtGameObject;
    public bool Dragging;
    private GameObject lastGo;

    private Camera m_Camera;

    private Vector2 pressedPosition = Vector2.zero;
    public static GameObject goPointedAt { get; private set; }

    public Vector2 DragVector => Dragging ? currentPos - pressedPosition : Vector2.zero;

    private void Start()
    {
        m_Camera = GetComponent<Camera>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (DetectPointedAtGameObject) goPointedAt = RaycastObject(Input.mousePosition);

        if (Input.touchCount > 0)
        {
            var touch = Touch.activeTouches[0];
            currentPos = touch.screenPosition;

            if (touch.phase == TouchPhase.Began)
                Press(touch.screenPosition);
            else if (touch.phase == TouchPhase.Ended) Release(touch.screenPosition);

            return;
        }

        currentPos = Input.mousePosition;
        if (InputSystemAgent.GetKeyDown("LMaus")) Press(Input.mousePosition);
        if (InputSystemAgent.GetKeyUp("LMaus")) Release(Input.mousePosition);

        if (InputSystemAgent.GetKeyDown("RMaus"))
        {
            pressedPosition = Input.mousePosition;
            lastGo = RaycastObject(pressedPosition);
            if (lastGo != null) lastGo.SendMessage("OnPressRight", SendMessageOptions.DontRequireReceiver);
        }
    }


    private void Press(Vector2 screenPos)
    {
        pressedPosition = screenPos;
        Dragging = true;

        lastGo = RaycastObject(screenPos);
        if (lastGo != null) lastGo.SendMessage("OnPress", SendMessageOptions.DontRequireReceiver);
    }

    private void Release(Vector2 screenPos)
    {
        if (lastGo != null)
        {
            var currentGo = RaycastObject(screenPos);
            if (currentGo == lastGo) lastGo.SendMessage("OnClick", SendMessageOptions.DontRequireReceiver);

            lastGo.SendMessage("OnRelease", SendMessageOptions.DontRequireReceiver);
            lastGo = null;
        }

        pressedPosition = Vector2.zero;
        Dragging = false;
    }

    private GameObject RaycastObject(Vector2 screenPos)
    {
        RaycastHit info;
        if (Physics.Raycast(m_Camera.ScreenPointToRay(screenPos), out info, 200))
        {
            inputHitPos = info.point;
            return info.collider.gameObject;
        }

        return null;
    }
}