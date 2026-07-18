using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{
    public Button JoinButton;
    public Text LevelLabel;
    public Text nickLabel;

    private int playerId;

    public void OnEnable()
    {
        var hasPlayer = false;

        if (Service.db.SelectCount("FROM item") < 1) DBSetup.start();

        //NetworkManager.validateGameVersion();

        // @ToDo: create a UI for selection
        foreach (var character in Service.db.Select<CharacterData>("FROM characters"))
        {
            hasPlayer = true;
            playerId = character.id;

            nickLabel.text = character.name;
            LevelLabel.text = character.level.ToString();
            JoinButton.onClick.AddListener(
                delegate { joinGame(character.id, character.name); });
            break;
        }

        if (hasPlayer)
        {
            nickLabel.transform.gameObject.SetActive(true);
            LevelLabel.transform.gameObject.SetActive(true);
            JoinButton.transform.gameObject.SetActive(true);
#if UNITY_EDITOR
            GameObject.Find("Canvas/MainPanel/LoginOptions/TestButton").SetActive(true);
#endif

            GameObject.Find("Canvas/MainPanel/LoginOptions/CreateButton").SetActive(false);
        }
        else
        {
            nickLabel.transform.gameObject.SetActive(false);
            LevelLabel.transform.gameObject.SetActive(false);
            JoinButton.transform.gameObject.SetActive(false);
            GameObject.Find("Canvas/MainPanel/LoginOptions/TestButton").SetActive(false);
        }
    }

    public void joinGame(int characterId, string name)
    {
        if (characterId < 1) return;

        var h = new Hashtable(1);
        h.Add("characterId", characterId);

        PhotonNetwork.player.SetCustomProperties(h);
        PhotonNetwork.player.NickName = name;

        NetworkManager.Instance.startConnection();
        GameObject.Find("Canvas/MainPanel/LoginOptions/JoinButton/Text").GetComponent<Text>().text =
            LanguageManager.get("CONNECTING") + "...";
    }

    public void joinTest()
    {
        Menu.defaultLevel = Menu.debugLevel;
        joinGame(playerId, "Tester");
    }
}