using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string cooldown;
    public string description;
    public string skillName;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }

    public void Show()
    {
        Menu.Instance.showSkillTooltip(
            "<size=20>" + LanguageManager.get(skillName) + "</size>\n\n<size=14>" + LanguageManager.get(description) +
            "</size>", cooldown);
    }

    public void Hide()
    {
        Menu.Instance.SkillTooltip.SetActive(false);
    }
}