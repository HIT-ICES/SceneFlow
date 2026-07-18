using UnityEngine;

public class UIScreenPos : MonoBehaviour
{
    public GameObject follow;

    private void LateUpdate()
    {
        if (follow == null || Camera.main == null) return;

        var screenPos = Camera.main.WorldToScreenPoint(follow.transform.position);

        if (Vector3.Distance(Camera.main.transform.position, follow.transform.position) > 40)
        {
            GetComponent<CanvasGroup>().alpha = 0;
        }
        else
        {
            if (screenPos.z > 0)
            {
                GetComponent<CanvasGroup>().alpha = 1;
                gameObject.GetComponent<RectTransform>().position = screenPos;
            }
        }

        //Debug.Log (screenPos);
    }
}