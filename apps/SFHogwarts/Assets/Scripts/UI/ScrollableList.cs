using System;
using UnityEngine;

public class ScrollableList : MonoBehaviour
{
    public delegate GameObject CallBack(GameObject newItem, int num);

    public int itemCount = 10,
        columnCount = 1;

    public GameObject itemPrefab;

    public void load(CallBack callback)
    {
        var j = 0;
        for (var i = 0; i < itemCount; i++)
        {
            //this is used instead of a double for loop because itemCount may not fit perfectly into the rows/columns
            if (i % columnCount == 0) j++;

            try
            {
                var newItem = createNewItem(i, j, callback);

                //move and size the new item
                var rectTransform = newItem.GetComponent<RectTransform>();
                rectTransform.SetParent(gameObject.GetComponent<RectTransform>(), false);
            }
            catch (Exception)
            {
                //looks like there are no more results to show
                break;
            }
        }
    }

    //create a new item, name it, and set the parent
    private GameObject createNewItem(int num, int j, CallBack callback)
    {
        var newItem = Instantiate(itemPrefab);

        newItem.tag = "TemporalPanel";
        newItem.name = gameObject.name + " item at (" + num + "," + j + ")";
        newItem.transform.SetParent(gameObject.transform);

        return callback(newItem, num);
    }
}