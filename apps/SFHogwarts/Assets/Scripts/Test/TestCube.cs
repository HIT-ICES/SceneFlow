using UnityEngine;

public class TestCube : MonoBehaviour
{
    public bool giveExp = false;
    public bool giveHealth = false;
    public bool giveMana = false;

    public bool isBad = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.transform.tag != "Player") return;

        var player = other.transform.GetComponent<Player>();
        var val = 0;

        if (isBad)
            val = -1;
        else
            val = 1;

        if (giveHealth) player.health += val;
        if (giveExp) player.exp += val;
        if (giveMana) player.mana += val;
    }
}