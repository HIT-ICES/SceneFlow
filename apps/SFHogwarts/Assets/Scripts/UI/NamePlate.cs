using UnityEngine;
using UnityEngine.UI;

public class NamePlate : MonoBehaviour
{
    public static Color COLOR_SELECTED = Color.green;
    public static Color COLOR_NORMAL = Color.white;
    public static Color COLOR_ENEMY = Color.red;
    public Transform dmgParent;
    public GameObject dmgPrefab;
    public Image health;
    public Text level;

    public Text Name;

    private void Start()
    {
        gameObject.name = transform.parent.name;
        //transform.SetParent(GameObject.Find ("Canvas/Plates").transform);
    }

    public void setName(string name, Color color)
    {
        Name.text = name;
        Name.color = color;
    }

    public void setLevel(int level)
    {
        this.level.text = level.ToString();
    }

    public void setDamage(int amount, bool isDPS = false)
    {
        var inst = Instantiate(dmgPrefab);
        inst.transform.SetParent(dmgParent.transform);

        if (isDPS)
            inst.GetComponent<TemporalText>().setText("-" + amount, Color.blue, TextAnchor.UpperRight, 2);
        else
            inst.GetComponent<TemporalText>().setText("-" + amount, Color.white, TextAnchor.UpperCenter, 1);

        inst.transform.localScale = inst.transform.parent.transform.localScale;
        inst.transform.localPosition = inst.transform.parent.localPosition;
    }
}