public class CharacterData
{
    public int exp;

    public int health;
    public int house;

    public int id;
    public int level;
    public int mana;
    public int maxHealth;
    public int maxMana;
    public string model;

    public int money;
    public string name;
    public string position;

    protected string TABLE_NAME = "characters";

    public void save()
    {
        Service.db.Update(TABLE_NAME, this);
    }

    public bool create()
    {
        return Service.db.Insert(TABLE_NAME, this);
    }
}