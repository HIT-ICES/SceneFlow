using System.Collections.Generic;

public class NPCData
{
    public enum creatureRace
    {
        Monster = 1,
        Human = 2
    }

    public enum creatureSubRace
    {
        Normal = 1,
        Seller = 2,
        Quest = 3,
        Talker = 4
    }

    public enum creatureTemplate
    {
        CastleSpider = 1,
        Human = 2
    }

    public int _race;
    public int _subRace;
    private readonly List<WaypointData> _waypoints = new();
    public float attackRange = 2;
    public float attacksPerSecond = 1;
    public int damage = 50;
    public float distanceToLoseAggro = 30;
    public int expValue = 1;
    private bool firstSearch = true;
    public int health = 100;

    public int id;
    public bool isAggresive = false;
    public int level = 1;
    public string name;
    public float runSpeed = 5;

    protected string TABLE_NAME = "npc";
    public int template;

    public creatureRace race
    {
        get => (creatureRace)_race;
        set => _race = (int)value;
    }

    public creatureSubRace subRace
    {
        get => (creatureSubRace)_subRace;
        set => _subRace = (int)value;
    }

    public List<WaypointData> waypoints
    {
        get
        {
            if (firstSearch)
            {
                foreach (var data in Service.db.Select<WaypointData>(
                             "FROM " + WaypointData.TABLE_NAME + " WHERE npc ==? ORDER BY id asc ", id))
                    _waypoints.Add(data);
                firstSearch = false;
            }

            return _waypoints;
        }
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