using System.Collections.Generic;
using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

[RequireComponent(typeof(PhotonView))]
public class InRoomChat : MonoBehaviour
{
    public static readonly string ChatRPC = "Chat";
    public bool AlignBottom = false;
    public Rect GuiRect = new(0, 0, 250, 300);
    private string inputLine = "";
    public bool IsVisible = true;
    public List<string> messages = new();
    private Vector2 scrollPos = Vector2.zero;

    public void Start()
    {
        if (AlignBottom) GuiRect.y = Screen.height - GuiRect.height;
    }

    public void OnGUI()
    {
        if (!IsVisible || !PhotonNetwork.inRoom) return;

        if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.KeypadEnter || Event.current.keyCode == KeyCode.Return))
        {
            if (!string.IsNullOrEmpty(inputLine))
            {
                photonView.RPC("Chat", PhotonTargets.All, inputLine);
                inputLine = "";
                GUI.FocusControl("");
                return; // printing the now modified list would result in an error. to avoid this, we just skip this single frame
            }

            GUI.FocusControl("ChatInput");
        }

        GUI.SetNextControlName("");
        GUILayout.BeginArea(GuiRect);

        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.FlexibleSpace();
        for (var i = messages.Count - 1; i >= 0; i--) GUILayout.Label(messages[i]);
        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("ChatInput");
        inputLine = GUILayout.TextField(inputLine);
        if (GUILayout.Button("Send", GUILayout.ExpandWidth(false)))
        {
            photonView.RPC("Chat", PhotonTargets.All, inputLine);
            inputLine = "";
            GUI.FocusControl("");
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    [PunRPC]
    public void Chat(string newLine, PhotonMessageInfo mi)
    {
        var senderName = "anonymous";

        if (mi.sender != null)
        {
            if (!string.IsNullOrEmpty(mi.sender.NickName))
                senderName = mi.sender.NickName;
            else
                senderName = "player " + mi.sender.ID;
        }

        messages.Add(senderName + ": " + newLine);
    }

    public void AddLine(string newLine)
    {
        messages.Add(newLine);
    }
}