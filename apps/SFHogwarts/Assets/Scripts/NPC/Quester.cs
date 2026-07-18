public class Quester : NPC
{
    public override void OnClick()
    {
        var quests = QuestManager.Instance.getByNPC(Id);

        var panel = Menu.Instance.showPanel("QuestPanel", false);
        panel.GetComponent<QuestPanel>().setQuest(QuestManager.Instance.allQuests[quests[0]]);
    }
}