using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.Characters.ThirdPerson;
using MonoBehaviour = Photon.MonoBehaviour;

public class Player : MonoBehaviour
{
    private const int XP_BASE = 400;
    private const int HEALTH_REGEN_BASE = 5;
    private const int MANA_REGEN_BASE = 2;

    public static Player _instance;
    private NPC _target;

    public Animator anim;

    private bool broom;

    private Vector3 correctPlayerPos = Vector3.zero;
    private Quaternion correctPlayerRot = Quaternion.identity;
    private bool gotFirstUpdate;

    public Image healthBar;
    public Image healthBar2;
    private bool healthRegenStarted;
    public bool isFlying = false;
    public float lastHitTime;
    private bool manaRegenStarted;

    public new string name;

    public NamePlate namePlate;
    public Text nick;

    public GameObject trailRenderer;

    public int health
    {
        get => characterData.health;
        set
        {
            var hasResurrected = false;
            //prevent negative health values
            if (value < 0) value = 0;

            // has resurrected
            if (health == 0 && value > 0) hasResurrected = true;

            // We dont check if health > maxHealth here, because player may have some temporal buff
            characterData.health = value;

            if (photonView.isMine)
            {
                characterData.save();
                PlayerPanel.Instance.updateBar(PlayerPanel.BarType.Health, characterData.health,
                    characterData.maxHealth);
                startHealthRegeneration();

                if (hasResurrected) startManaRegeneration();
            }
        }
    }

    public int maxHealth => characterData.maxHealth;

    public int maxMana => characterData.maxMana;

    public int exp
    {
        get => characterData.exp;
        set
        {
            //prevent negative values
            if (value < 0) value = 0;

            characterData.exp = value;

            if (photonView.isMine)
            {
                var nextLevelExp = XP_BASE * level;

                if (exp >= nextLevelExp)
                {
                    characterData.exp = exp - nextLevelExp;
                    level += 1;
                }

                characterData.save();
                PlayerPanel.Instance.showWonXP(value);
                PlayerPanel.Instance.updateBar(PlayerPanel.BarType.Exp, characterData.exp, XP_BASE * level);
            }
        }
    }

    public int mana
    {
        get => characterData.mana;
        set
        {
            //prevent negative values
            if (value < 0) value = 0;

            // We dont check if mana > maxMana here, because player may have some temporal buff
            characterData.mana = value;

            if (photonView.isMine)
            {
                characterData.save();
                PlayerPanel.Instance.updateBar(PlayerPanel.BarType.Mana, characterData.mana, characterData.maxMana);
                startManaRegeneration();
            }
        }
    }

    public int money
    {
        get => characterData.money;
        set
        {
            //prevent negative values
            if (value < 0) value = 0;

            characterData.money = value;

            if (photonView.isMine)
            {
                characterData.save();
                try
                {
                    Inventory.Instance.updateMoney();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public int level
    {
        get => characterData.level;
        set
        {
            //prevent negative values
            if (value < 0) value = 0;

            characterData.level = value;

            if (photonView.isMine)
            {
                characterData.save();
                SkillsUI.Instance.displayUnlockedSkills();
            }
        }
    }

    public bool isMine => photonView.isMine;

    public bool isFriendly => true; // this will change when we have multiple classes

    public int id => photonView.ownerId;

    public CharacterData characterData { get; set; }

    public NPC target
    {
        get => _target;
        set
        {
            _target = value;

            if (value == null)
                PlayerPanel.Instance.hideTargetPanel();
            else
                PlayerPanel.Instance.showTargetPanel(value.transform);

            SkillsUI.Instance.updateStatus();
        }
    }

    public bool isInCombat
    {
        get
        {
            if (Time.time > lastHitTime + 10) return false;
            return true;
        }
        set
        {
            if (value) lastHitTime = Time.time;
        }
    }

    public bool isDead
    {
        get
        {
            if (health < 1) return true;
            return false;
        }
    }

    public static Player Instance => _instance;


    private void Start()
    {
        if (photonView.isMine)
        {
            _instance = this;
            SkillsUI.Instance.displayUnlockedSkills();
            startHealthRegeneration();
            startManaRegeneration();
            Destroy(trailRenderer);
        }
        else
        {
            Chat.Instance.LocalMsg("<color=\"#e8bf00\">[Sistema]</color> " + photonView.owner.NickName + " entró");
            Destroy(GetComponent<ThirdPersonUserControl>());
            Destroy(GetComponent<ThirdPersonCharacter>());
            Destroy(GetComponent<Rigidbody>());
            Destroy(GetComponent<NPCActivator>());
            Destroy(gameObject.transform.Find("NPCActivator").gameObject);
        }
    }

    private void Update()
    {
        if (!photonView.isMine)
        {
            transform.position = Vector3.Lerp(transform.position, correctPlayerPos, Time.deltaTime * 5);
            transform.rotation = Quaternion.Lerp(transform.rotation, correctPlayerRot, Time.deltaTime * 5);
        }
        else
        {
            if (!gotFirstUpdate)
            {
                photonView.RPC("setNick", PhotonTargets.OthersBuffered, PhotonNetwork.player.NickName);

                healthBar = GameObject.Find("Canvas/PlayerPanel/HP Orb Bg/HP").GetComponent<Image>();

                PlayerPanel.Instance.updateBar(PlayerPanel.BarType.Health, characterData.health,
                    characterData.maxHealth);
                PlayerPanel.Instance.updateBar(PlayerPanel.BarType.Exp, characterData.exp, XP_BASE * level);
                PlayerPanel.Instance.updateBar(PlayerPanel.BarType.Mana, characterData.mana, characterData.maxMana);

                gotFirstUpdate = true;
            }

            if (healthBar.fillAmount != health)
            {
                healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount, health / 270f, 4f * Time.deltaTime);
                namePlate.health.fillAmount =
                    Mathf.Lerp(namePlate.health.fillAmount, health / 270f, 4f * Time.deltaTime);
            }

            //looks like player is falling
            if (transform.position.y < -100) Respawn();
        }
    }

    private void LateUpdate()
    {
        if (gotFirstUpdate && isDead && anim.GetBool("Dead") == false)
        {
            anim.SetBool("Dead", true);
            freeze();

            Menu.Instance.togglePanel("GameOverPanel");
            Menu.Instance.GetComponent<Animator>().SetTrigger("GameOver");
        }
    }

    public void Reborn()
    {
        Menu.Instance.GetComponent<Animator>().SetBool("GameOver", false);
        Menu.Instance.togglePanel("GameOverPanel");

        health = maxHealth / 2;
        anim.SetBool("Dead", false);

        transform.position =
            new Vector3(633.51f, 161.38f, 415.70f); //GameObject.Find("SpawnPoints/FirstJoin").transform.position;
        unfreeze();
    }

    public void Respawn()
    {
        transform.position =
            new Vector3(633.51f, 161.38f, 415.70f); //GameObject.Find("SpawnPoints/FirstJoin").transform.position;
    }

    private void startHealthRegeneration()
    {
        if (healthRegenStarted) return;
        StartCoroutine("regenerateHealth");
    }

    private void startManaRegeneration()
    {
        if (manaRegenStarted) return;
        StartCoroutine("regenerateMana");
    }


    private IEnumerator regenerateHealth()
    {
        if (isDead) yield break;
        healthRegenStarted = true;

        while (health != maxHealth)
        {
            if (!isInCombat && !target) health += 1;
            if (health > maxHealth) health = maxHealth;
            yield return new WaitForSeconds(1f / (HEALTH_REGEN_BASE + level / 15));
        }

        healthRegenStarted = false;
    }

    private IEnumerator regenerateMana()
    {
        if (isDead) yield break;
        manaRegenStarted = true;

        while (mana != maxMana)
        {
            if (!isInCombat && !target) mana += 1;
            if (mana > maxMana) mana = maxMana;
            yield return new WaitForSeconds(1f / (MANA_REGEN_BASE + level / 15));
        }

        manaRegenStarted = false;
    }

    private void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            // We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(anim.GetFloat("Forward"));
            stream.SendNext(anim.GetFloat("Turn"));
            stream.SendNext(anim.GetFloat("Jump"));
            stream.SendNext(anim.GetBool("OnGround"));
            stream.SendNext(anim.GetBool("InvokeSpell"));
            stream.SendNext(anim.GetInteger("SpellType"));
            stream.SendNext(anim.GetBool("Broomstick"));
        }
        else
        {
            // Network player, receive data
            correctPlayerPos = (Vector3)stream.ReceiveNext();
            correctPlayerRot = (Quaternion)stream.ReceiveNext();

            if (anim)
            {
                anim.SetFloat("Forward", (float)stream.ReceiveNext());
                anim.SetFloat("Turn", (float)stream.ReceiveNext());
                anim.SetFloat("Jump", (float)stream.ReceiveNext());
                anim.SetBool("OnGround", (bool)stream.ReceiveNext());
                anim.SetBool("InvokeSpell", (bool)stream.ReceiveNext());
                anim.SetInteger("SpellType", (int)stream.ReceiveNext());
                broom = (bool)stream.ReceiveNext();
                anim.SetBool("Broomstick", broom);
            }

            if (broom)
                gameObject.GetComponent<PlayerHotkeys>().broom.SetActive(true);
            else
                gameObject.GetComponent<PlayerHotkeys>().broom.SetActive(false);


            if (gotFirstUpdate == false)
            {
                transform.position = correctPlayerPos;
                transform.rotation = correctPlayerRot;
                gotFirstUpdate = true;
            }
        }
    }

    [PunRPC]
    private void setNick(string nickName)
    {
        //name = nickName;
        namePlate.setName(nickName, NamePlate.COLOR_NORMAL);
        namePlate.gameObject.SetActive(true);
    }

    private static Vector3 getVector3(string rString)
    {
        var temp = rString.Substring(1, rString.Length - 2).Split(',');
        var x = float.Parse(temp[0]);
        var y = float.Parse(temp[1]);
        var z = float.Parse(temp[2]);
        var rValue = new Vector3(x, y, z);
        return rValue;
    }

    /**
     * Adds an item to current user
     * 
     * @param int id item id
     * @param int amount item quantity
     * 
     * @return bool
     */
    public bool addItem(int id, int amount = 1)
    {
        bool success;
        var characterItem = Service.getOne<CharacterItem>("FROM inventory WHERE item == ?", id);

        if (characterItem != null)
        {
            characterItem.quantity += amount;
            characterItem.save();
            success = true;
        }
        else
        {
            var item = new CharacterItem
            {
                item = id,
                quantity = amount
            };
            success = item.create();
        }

        // reload inventory
        if (success)
            try
            {
                QuestManager.Instance.sendAction(id, Task.ActorType.Item, Task.ActionType.GetItem, amount);
                Inventory.Instance.reload();
            }
            catch (Exception)
            {
            }

        return success;
    }

    public void freeze()
    {
        transform.GetComponent<ThirdPersonUserControl>().enabled = false;
        anim.SetBool("Jumping", false);
        anim.SetFloat("Speed", 0);

        var rigidbody = GetComponent<Rigidbody>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    public void unfreeze()
    {
        transform.GetComponent<ThirdPersonUserControl>().enabled = true;
    }

    [PunRPC]
    public void getDamage(int amount, int attacker)
    {
        isInCombat = true;
        health -= amount;
        anim.Play("GettingHit");

        if (target == null) PhotonView.Find(attacker).gameObject.GetComponent<NPC>().setSelected();
    }

    // Has killed someone/NPC or made an assistance
    [PunRPC]
    public void addKill(int id, Task.ActorType type, int level, int damage, int totalHealth, int expValue,
        int templateId = 0)
    {
        var wonExp = 0;
        var levelDiff = level - level;

        // do not give exp if player has > 3 levels than the killed npc
        if (levelDiff > -2)
        {
            if (damage >= totalHealth)
                wonExp = expValue;
            else
                wonExp = expValue / 2;
            exp += wonExp + levelDiff * 10;
        }

        QuestManager.Instance.sendAction(id, type, Task.ActionType.Kill, 1, templateId);
    }
}