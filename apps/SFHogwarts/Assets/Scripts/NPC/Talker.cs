public class Talker : NPC
{
    public override void OnClick()
    {
        Menu.Instance.showPanel("TalkPanel", false).GetComponent<TalkPanel>().showNPCText(Id);
    }
}