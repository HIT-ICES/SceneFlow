using System.Linq;
using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
///     A Replacement for main panel
/// </summary>
public class MainMenuReplacement : MonoBehaviour
{
    private bool started;

    private void joinGame(int characterId, string name)
    {
        if (characterId < 1 || started) return;

        var h = new Hashtable(1) { { "characterId", characterId } };

        PhotonNetwork.player.SetCustomProperties(h);
        PhotonNetwork.player.NickName = name;
        gameObject.GetComponent<NetworkManager>().startConnection();
        started = true;
        //GameObject.Find("Canvas/MainPanel/LoginOptions/JoinButton/Text").GetComponent<Text>().text = LanguageManager.get("CONNECTING") + "...";
    }

    private void OnEnable()
    {
    }

    // Start is called before the first frame update
    private void Start()
    {
        if (Service.db.SelectCount("FROM item") < 1) DBSetup.start();

        // @ToDo: create a UI for selection
        var character = Service.db.Select<CharacterData>("FROM characters").FirstOrDefault();
        if (character is null)
        {
            const string nick = "Harry Potter";
            const int initialHealth = 270;
            const int initialMana = 130;

            if (!new CharacterData
                {
                    name = nick,
                    model = "male_01",
                    position = "",
                    level = 1,
                    health = initialHealth,
                    maxHealth = initialHealth,
                    mana = initialMana,
                    maxMana = initialMana,
                    money = 234670,
                    id = Service.db.Id(1)
                }.create())
            {
                Debug.Log("Failed to create character.");
                return;
            }
        }


        joinGame(1, character.name);
    }

    // Update is called once per frame
    private void Update()
    {
    }
}