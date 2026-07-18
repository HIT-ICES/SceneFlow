using UnityEngine;
using UnityEngine.UI;

/*
    Provided By Vancete, available in the Unity Asset Store
*/

public class Chat : MonoBehaviour
{
    public static Chat Instance;
    public static int MAX_CHARACTERS = 5000;
    public Image background;

    public Text chatbox;
    private bool hasMouseOver;
    public Text input;
    public InputField input2;
    public bool isWritting;
    public Scrollbar scroll;

    private void Start()
    {
        Instance = this;
    }

    // used in Unity UI (when chat input gets clicked)
    public void setWritting()
    {
        isWritting = true;
        background.enabled = true;
    }

    public void endWritting()
    {
        isWritting = false;
        background.enabled = false;
    }

    public void onPointerEnter()
    {
        background.enabled = true;
        hasMouseOver = true;
    }

    public void onPointerExit()
    {
        background.enabled = false;
        hasMouseOver = false;
    }

    public void sendMessage()
    {
        if (input.text == "")
        {
            endWritting();
            return;
        }

        if (input.text.Length < 4)
        {
            GetComponent<PhotonView>().RPC("Msg", PhotonTargets.All,
                "[" + PhotonNetwork.player.NickName + "] " + input.text);
        }
        else
        {
            // We can use this to send special commands, like GM messages, Global Announcements, etc
            if (input.text.Substring(0, 4) == "!gm ")
                GetComponent<PhotonView>().RPC("Msg", PhotonTargets.All,
                    "<color=\"#00c0ff\">[GM]</color> " + input.text.Replace("!gm ", ""));
            else if (input.text.Substring(0, 4) == "!ga ")
                GetComponent<PhotonView>().RPC("Msg", PhotonTargets.All,
                    "<color=\"#fe8f00\">[Global Announcement]</color> " + input.text.Replace("!ga ", ""));
            else
                GetComponent<PhotonView>().RPC("Msg", PhotonTargets.All,
                    "[" + PhotonNetwork.player.NickName + "] " + input.text);
        }

        endWritting();
    }

    // Needs to be RPC to work online
    [PunRPC]
    public void Msg(string msg)
    {
        AddMsg(msg);

        // if the mouse is not over the chat, scroll down automatically
        if (!hasMouseOver) scroll.value = 0;
    }

    //Just add the msg to the chatbox
    private void AddMsg(string msg)
    {
        chatbox.text = chatbox.text + "\n" + msg;
        input.text = "";
        input2.text = "";

        if (chatbox.text.Length > MAX_CHARACTERS) chatbox.text = chatbox.text.Substring(MAX_CHARACTERS - 100);
    }

    public void LocalMsg(string msg)
    {
        AddMsg(msg);
    }
}