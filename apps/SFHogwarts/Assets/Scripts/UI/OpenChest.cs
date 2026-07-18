using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider))]
public class OpenChest : MonoBehaviour
{
    private Animator anim;


    private readonly bool isLocked = false;

    private bool isOpen;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnMouseDown()
    {
        if (isLocked)
        {
            // TODO later. Magic spell to open it
        }
        else if (isOpen == false)
        {
            anim.SetTrigger("open");
            isOpen = true;
        }
        else if (isOpen)
        {
            anim.SetTrigger("close");
            isOpen = false;
        }
    }
}