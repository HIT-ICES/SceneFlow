using System.Collections;
using UnityEngine;

public class QuestItem : MonoBehaviour
{
    [Tooltip("Item id")] public int id;

    private bool isHidden;
    public int quantity = 1;

    [Tooltip("Seconds, Set to 0 to not hide this object after a player click")]
    public int respawnAfter = 30;

    public void Start()
    {
        if (id < 1 || quantity < 1)
            Debug.LogError("There is a QuestItem in this scene without a proper id/quantity set");
    }

    public void OnMouseDown()
    {
        if (isHidden) return;

        Player.Instance.addItem(id, quantity);

        if (respawnAfter > 0)
        {
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            isHidden = true;
            StartCoroutine(respawn());
        }
    }

    private IEnumerator respawn()
    {
        yield return new WaitForSeconds(respawnAfter);

        gameObject.GetComponent<MeshRenderer>().enabled = true;
        isHidden = false;
    }
}