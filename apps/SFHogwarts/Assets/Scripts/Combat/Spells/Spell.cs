using UnityEngine;

public class Spell : ScriptableObject
{
    public enum BuffType
    {
        Heal,
        MagicalDefense,
        PhysicalDefense
    }

    public enum SpellCategory
    {
        Fire,
        Frost
    }

    public enum SpellDirection
    {
        Directional,
        Follow,
        Point
    }

    public enum SpellFlag
    {
        Slow,
        DamagePerSecond,
        None
    }

    public enum SpellPosition
    {
        MyTransform,
        TargetTransform
    }

    public enum SpellType
    {
        Single,
        Buff,
        Aoe
    }

    public BuffType buffType = BuffType.Heal;
    public int dotDamage = 0;
    public GameObject dotEffect = null;
    public int dotSeconds = 0;
    public int dotTick = 0;
    public int maxBuffAmount = 0;
    public int minBuffAmount = 0;
    public int minLevel = 0;
    public int projectileSpeed = 0;
    public int slowDuration = 0;
    public int spellCastTime = 0;
    public SpellCategory spellCategory = SpellCategory.Fire;
    public GameObject spellCollisionParticle = null;
    public SpellDirection spellDirection = SpellDirection.Directional;
    public SpellFlag spellFlag = SpellFlag.None;
    public Texture2D spellIcon = null;
    public string spellInfo = "";

    public int spellManaCost = 0;
    public int spellMaxDamage = 0;
    public int spellMinDamage = 0;


    public string spellName = "";
    public SpellPosition spellPosition = SpellPosition.MyTransform;

    public GameObject spellPrefab = null;

    public SpellType spellType = SpellType.Single;
}