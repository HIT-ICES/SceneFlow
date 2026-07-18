using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SellPanel : MonoBehaviour
{
    private const int MAX_ITEMS = 3;
    public static Item _selectedItem;

    public static SellPanel _instance;

    private readonly List<Item> itemList = new();
    public RectTransform scrollPanel;

    public AudioSource sound;

    public static Item selectedItem
    {
        set
        {
            // check if player can buy it
            if (value == null || value.price > Player.Instance.money)
            {
                Instance.transform.Find("BuyButton").GetComponent<Button>().interactable = false;
                return;
            }

            _selectedItem = value;
            Instance.transform.Find("BuyButton").GetComponent<Button>().interactable = true;
        }
        get => _selectedItem;
    }

    public static SellPanel Instance => _instance;

    private void destroyOldIcons()
    {
        var children = new List<GameObject>();
        foreach (Transform child in scrollPanel.transform)
            if (child.tag == "TemporalPanel")
                children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));
    }

    private void OnEnable()
    {
        _instance = this;
        itemList.Clear();

        // @ToDo: select items depending on NPC level and area
        foreach (var itm in Service.db.Select<Item>("FROM item limit 0," + MAX_ITEMS)) itemList.Add(itm);

        var scroll = scrollPanel.GetComponent<ScrollableList>();
        scroll.itemCount = MAX_ITEMS;
        scroll.load(delegate(GameObject newItem, int num)
        {
            var tItem = itemList[num];
            var price = Util.formatMoney(tItem.price);

            newItem.SetActive(true);
            newItem.gameObject.transform.Find("Button/NameLabel").GetComponent<Text>().text = tItem.name;
            newItem.gameObject.transform.Find("Button/GalleonLabel").GetComponent<Text>().text = price.x.ToString();
            newItem.gameObject.transform.Find("Button/SickleLabel").GetComponent<Text>().text = price.y.ToString();
            newItem.gameObject.transform.Find("Button/KnutLabel").GetComponent<Text>().text = price.z.ToString();
            newItem.gameObject.transform.Find("Icon").GetComponent<RawImage>().texture = tItem.icon;

            newItem.gameObject.transform.Find("Button").GetComponent<Button>().onClick.AddListener(
                delegate { selectedItem = tItem; });

            return newItem;
        });

        updateMoney();
    }

    private void OnDisable()
    {
        destroyOldIcons(); // Removes Items in Seller Menu
        selectedItem = null;
    }

    public void updateMoney()
    {
        var money = Util.formatMoney(Player.Instance.money);
        transform.Find("GalleonLabel").GetComponent<Text>().text = money.x.ToString();
        transform.Find("SickleLabel").GetComponent<Text>().text = money.y.ToString();
        transform.Find("KnutLabel").GetComponent<Text>().text = money.z.ToString();
    }

    public void buyButton()
    {
        if (selectedItem == null || selectedItem.price > Player.Instance.money) return;

        sound.clip = SoundManager.get(SoundManager.Effect.Buy);
        sound.Play();

        Player.Instance.money -= selectedItem.price;
        Player.Instance.addItem(selectedItem.id);
        updateMoney();
    }
}