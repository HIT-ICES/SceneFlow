using UnityEngine;
using UnityEngine.UI;

public class CastingPanel : MonoBehaviour
{
    private static CastingPanel _instance;
    public Image bar;
    private float curTime = 10f;
    public bool isCasting;
    public new Text name;
    private float skillTime = 10f;
    public static CastingPanel Instance => _instance ??= FindObjectOfType<CastingPanel>();

    // Use this for initialization
    private void Start()
    {
        //Instance = this;
    }

    // Update is called once per frame
    private void Update()
    {
        curTime -= Time.deltaTime;
        bar.fillAmount = curTime / skillTime;
        if (curTime / skillTime <= 0)
        {
            gameObject.SetActive(false);
            isCasting = false;
        }
        else
        {
            isCasting = true;
        }
    }

    public void Cast(string n, float t)
    {
        gameObject.SetActive(true);
        name.text = n;
        skillTime = t;
        curTime = t;
    }
}