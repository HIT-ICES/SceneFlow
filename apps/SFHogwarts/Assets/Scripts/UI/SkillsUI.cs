using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillsUI : MonoBehaviour
{
    private static SkillsUI _instance;

    public List<Button> Skills;

    public static SkillsUI Instance => _instance ??= FindObjectOfType<SkillsUI>();

    // Use this for initialization
    private void Start()
    {
        //Instance = this;
    }

    public void displayUnlockedSkills()
    {
        var playerLevel = Player.Instance.level;
        playerLevel = 2;

        if (playerLevel >= 2) Skills[2].transform.parent.gameObject.SetActive(true);
        if (playerLevel >= 4) Skills[3].transform.parent.gameObject.SetActive(true);
        if (playerLevel >= 6) Skills[4].transform.parent.gameObject.SetActive(true);
    }

    public void fillSlots()
    {
        var i = 0;
        var total = PlayerCombat.Instance.spellList.Count;
        SkillTooltip tooltip;
        Spell spell;

        foreach (var button in Skills)
        {
            tooltip = button.transform.GetComponent<SkillTooltip>();

            if (i < total)
            {
                spell = PlayerCombat.Instance.spellList[i];

                //tooltip.id = spell.id;
                tooltip.name = spell.spellName;
                tooltip.description = spell.spellInfo;
            }

            i++;
        }
    }

    public void execSkill(int num)
    {
        num--;
        PlayerCombat.Instance.spellCast(num);
    }

    public void disableSkill(int num)
    {
        Skills[num].interactable = false;
    }

    public void updateStatus()
    {
        var enabled = false;

        if (Player.Instance.target) enabled = true;

        foreach (var button in Skills) button.interactable = enabled;
    }

    public void enableSkill(int num)
    {
        Skills[num].interactable = true;
    }

    public void toggleBroomStick()
    {
        PlayerHotkeys.Instance.toggleBroomStick();
    }

    public void toggleLight()
    {
        PlayerHotkeys.Instance.toggleLight();
    }
}