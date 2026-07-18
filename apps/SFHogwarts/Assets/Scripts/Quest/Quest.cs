using System.Collections.Generic;
using UnityEngine.UI;

public class Quest
{
    private bool _isCompleted;
    public int assigner;
    public int id;
    public Dictionary<int, int> loot = new();
    public Dictionary<int, Task> tasks = new();
    public Text ui;

    public string name => LanguageManager.get("QUEST_" + id + "_TITLE");

    public string pre => LanguageManager.get("QUEST_" + id + "_PRE");

    public string after => LanguageManager.get("QUEST_" + id + "_AFTER");

    public bool isCompleted
    {
        get
        {
            if (!_isCompleted)
            {
                foreach (var task in tasks.Values)
                    if (!task.isCompleted)
                        return false;
                // UI effects (tachar la quest)
                _isCompleted = true;
            }

            return _isCompleted;
        }
    }
}