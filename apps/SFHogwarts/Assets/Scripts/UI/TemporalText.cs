using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/*
Used to display temporal messages like, hits damage, won xp, etc..
*/

public class TemporalText : MonoBehaviour
{
    public Animator anim;

    public Text textObj;

    // Use this for initialization
    private void Start()
    {
        StartCoroutine("Destroy");
    }

    public void setText(string t, Color c, TextAnchor a, int type)
    {
        gameObject.GetComponent<Text>().text = t;
        textObj.text = t;
        textObj.color = c;
        gameObject.GetComponent<Text>().alignment = a;
        textObj.alignment = a;
        anim.SetInteger("Type", type);
    }

    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(4f);
        Destroy(gameObject);
    }
}