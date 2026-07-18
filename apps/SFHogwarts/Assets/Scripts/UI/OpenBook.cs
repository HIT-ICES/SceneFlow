using UnityEngine;

public class OpenBook : MonoBehaviour
{
    public int book;

    public bool isBook = true;

    private void OnMouseDown()
    {
        if (isBook)
        {
            GameObject.Find("Canvas/Books/Panel").SetActive(true);
            GameObject.Find("Canvas/Books/Base").SetActive(true);
            GameObject.Find("Canvas/Books/Close").SetActive(true);
            GameObject.Find("Canvas/Books/Book" + book).SetActive(true);
        }
    }

    public void Close()
    {
        for (var i = 0; i < GameObject.Find("Canvas/Books").transform.childCount; i++)
        {
            var child = GameObject.Find("Canvas/Books").transform.GetChild(i).gameObject;
            if (child != null)
                child.SetActive(false);
        }
    }
}