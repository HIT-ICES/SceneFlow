using System.Collections.Generic;
using UnityEngine;

public class CharacterPanel : MonoBehaviour
{
    public GameObject itemSlotPrefab;

    // Use this for initialization
    private void OnEnable()
    {
        reload();
    }

    public void reload()
    {
        GameObject itemSlot;
        var itm = new Item();

        destroyOldIcons();

        foreach (var characterItem in Service.db.Select<CharacterItem>(
                     "FROM inventory WHERE _position != ? & character == ?", 0,
                     PhotonNetwork.player.CustomProperties["characterId"]))
        {
            var slot = transform.Find("Slot" + characterItem._position).GetComponent<Slot>();
            slot.available = false;
            itemSlot = Instantiate(itemSlotPrefab);
            itemSlot.tag = "TemporalPanel";
            itemSlot.GetComponent<ItemSlot>().item = itm.get(characterItem);
            itemSlot.GetComponent<ItemSlot>().currentSlot = slot;

            itemSlot.transform.SetParent(gameObject.transform, false);
            itemSlot.GetComponent<RectTransform>().localPosition = slot.transform.localPosition;
        }
    }


    private void destroyOldIcons()
    {
        var children = new List<GameObject>();
        foreach (Transform child in transform)
            if (child.tag == "TemporalPanel")
                children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));
    }
}