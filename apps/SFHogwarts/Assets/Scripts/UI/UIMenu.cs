using UnityEngine;

/**
    Basic menu to toggle ingame UI panels
 */
public class UIMenu : MonoBehaviour
{
    public static UIMenu _instance;

    public GameObject BagPanel;
    public GameObject CharacterPanel;
    public GameObject SellerPanel;

    public static UIMenu Instance => _instance;

    public void Start()
    {
        _instance = this;
    }

    public void togglePanel(string name)
    {
        var panel = (GameObject)GetType().GetField(name).GetValue(this);
        panel.SetActive(!panel.GetActive());
    }

    public void showPanel(string name)
    {
        var panel = (GameObject)GetType().GetField(name).GetValue(this);
        panel.SetActive(true);
    }

    public void hideAllPanels()
    {
        BagPanel.SetActive(false);
        SellerPanel.SetActive(false);
        CharacterPanel.SetActive(false);
    }
}