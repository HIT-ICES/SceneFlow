using UnityEngine;

public class Slot : MonoBehaviour
{
    public enum slotType
    {
        inventory = 1,
        equipment = 2
    }

    public bool available = true;
    public int num;
    public CharacterItem.equipmentPosition subType;
    public slotType type;
}