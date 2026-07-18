using UnityEngine;
using UnityEngine.UI;

public class QuestPanel : MonoBehaviour
{
    public GameObject acceptButton;
    public GameObject completeButton;

    private Quest quest;
    public Text text;
    public Text title;

    public void setQuest(Quest questData)
    {
        quest = questData;
        title.text = quest.name;

        if (quest.isCompleted)
        {
            text.text = processText(quest.after);
            acceptButton.SetActive(false);
            completeButton.SetActive(true);
        }
        else if (QuestManager.Instance.quests.ContainsKey(quest.id))
        {
            // quest accepted, but not finished
            text.text = LanguageManager.get("QUEST_NOT_FINISHED");
        }
        else
        {
            text.text = processText(quest.pre);
            acceptButton.SetActive(true);
            completeButton.SetActive(false);
        }
    }

    private string processText(string message)
    {
        message = message.Replace("{{username}}", Player.Instance.characterData.name);

        return message;
    }

    public void OnAccept()
    {
        QuestManager.Instance.addQuest(quest);

        text.text = LanguageManager.get("QUEST_ACCEPTED");
        acceptButton.SetActive(false);

        var sound = GetComponent<AudioSource>();
        sound.clip = SoundManager.get(SoundManager.Effect.QuestAccept);
        sound.Play();
    }

    public void OnAcceptReward()
    {
        QuestManager.Instance.completeQuest(quest);

        text.text = LanguageManager.get("QUEST_ACCEPTED");
        acceptButton.SetActive(false);

        var sound = GetComponent<AudioSource>();
        sound.clip = SoundManager.get(SoundManager.Effect.QuestComplete);
        sound.Play();
    }
}