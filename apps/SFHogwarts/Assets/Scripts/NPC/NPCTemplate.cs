public class NPCTemplate
{
    public int _creatureRace;
    public int _creatureSubRace;
    public int healthBase = 100; // health at level 1

    public int id;
    public bool isAgressive = false;
    public string name;

    protected string TABLE_NAME = "npc_template";

    public NPCData.creatureRace creatureRace
    {
        get => (NPCData.creatureRace)_creatureRace;
        set => _creatureRace = (int)value;
    }

    public NPCData.creatureSubRace creatureSubRace
    {
        get => (NPCData.creatureSubRace)_creatureSubRace;
        set => _creatureSubRace = (int)value;
    }

    public static NPCTemplate get(int id)
    {
        return Service.db.SelectKey<NPCTemplate>("npc_template", id);
    }

    public static NPCData fillById(NPCData.creatureTemplate id, int level)
    {
        var _id = (int)id;
        var npc = new NPCData();
        var template = get(_id);

        npc.name = template.name;
        npc.template = _id;
        npc.race = template.creatureRace;
        npc.subRace = template.creatureSubRace;
        npc.level = level;
        npc.health = template.healthBase * level;
        npc.isAggresive = template.isAgressive;

        return npc;
    }

    public void save()
    {
        Service.db.Update(TABLE_NAME, this);
    }

    public bool create()
    {
        return Service.db.Insert(TABLE_NAME, this);
    }
}