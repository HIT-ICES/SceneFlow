public class CharacterItem
{
    public enum equipmentPosition
    {
        head = 1,
        handLeft = 2,
        handRight = 3,
        hands = 4
    }

    public int _position;
    public int attrition = 0;
    public int character;

    public int item;
    public int quantity;
    public int slot = 0;

    protected string TABLE_NAME = "inventory";

    public equipmentPosition position
    {
        get => (equipmentPosition)_position;
        set => _position = (int)value;
    }

    public void save()
    {
        if (quantity < 1)
            delete();
        else
            Service.db.Update(TABLE_NAME, this);
    }

    public bool create()
    {
        if (character == 0) character = (int)PhotonNetwork.player.CustomProperties["characterId"];

        return Service.db.Insert(TABLE_NAME, this);
    }

    public void delete()
    {
        Service.db.Delete(TABLE_NAME, item);
    }
}