using UnityEngine;
using UnityEngine.UI;

public class TargetPanel : MonoBehaviour
{
    public Image health;

    private bool isNPC;

    public new Text name;
    private NPC npc;
    private Player player;
    public Transform target;

    public void init(Transform newTarget)
    {
        target = newTarget;

        if (target.GetComponent<NPC>() != null)
        {
            isNPC = true;
            npc = target.GetComponent<NPC>();
        }
        else
        {
            isNPC = false;
            player = target.GetComponent<Player>();
        }

        if (isNPC)
        {
            name.text = npc.name;

            if (npc.isFriendly)
                name.color = NamePlate.COLOR_SELECTED;
            else
                name.color = NamePlate.COLOR_ENEMY;
        }
        else
        {
            name.text = player.name;

            if (player.isFriendly)
                name.color = NamePlate.COLOR_SELECTED;
            else
                name.color = NamePlate.COLOR_ENEMY;
        }

        InvokeRepeating("updateHealth", 1f, 1f);
    }

    public void updateHealth()
    {
        int currentHealth;
        int maxHealth;

        if (isNPC)
        {
            currentHealth = npc.health;
            maxHealth = npc.maxHealth;
        }
        else
        {
            currentHealth = player.health;
            maxHealth = player.maxHealth;
        }

        health.fillAmount = currentHealth / (float)maxHealth;
    }

    private void OnDisable()
    {
        CancelInvoke("updateHealth");
    }
}