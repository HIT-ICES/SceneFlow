using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.Characters.ThirdPerson;
using MonoBehaviour = Photon.MonoBehaviour;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    public Texture mmarow;

    private void Start()
    {

        Instance = this;
        SceneManager.sceneLoaded += (s, mode) =>
        {
            if (s.name == Menu.defaultLevel) spawnPlayer();
        };
    }

    public static void validateGameVersion()
    {
        // http://answers.unity3d.com/questions/792342/how-to-validate-ssl-certificates-when-using-httpwe.html
        ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
        var latestVersion = new WebClient()
            .DownloadString("https://raw.githubusercontent.com/OpenHogwarts/hogwarts/master/latest_build.txt").Trim();

        if (Menu.GAME_VERSION != latestVersion)
        {
            Application.Quit();
            throw new Exception("Please download the latest build " + Menu.GAME_VERSION + " <-> " + latestVersion);
        }
    }

    public void startConnection()
    {
        if (PhotonNetwork.offlineMode) return; // Execute once.
        //PhotonNetwork.ConnectUsingSettings(Menu.GAME_VERSION);
        PhotonNetwork.offlineMode = true;
        //PhotonNetwork.JoinLobby();
        OnJoinedLobby();
    }

    public void spawnPlayer()
    {
        // var firstJoin = GameObject.Find("SpawnPoints/FirstJoin");//"FirstJoin"
        // var position = firstJoin.transform.position;//(633.51, 161.38, 415.70)

        var player = PhotonNetwork.Instantiate(
            "Characters/Player",
            new Vector3(633.51f, 161.38f, 415.70f),
            Quaternion.identity, 0);

        // get character data
        var character = Service.db.Select<CharacterData>("FROM characters").FirstOrDefault();
        var playerComponent = player.GetComponent<Player>();
        playerComponent.characterData = character;

        player.GetComponent<ThirdPersonUserControl>().enabled = true;
        player.GetComponent<ThirdPersonCharacter>().enabled = true;
        //player.GetComponent<Motor> ().enabled = true;
        player.GetComponent<PlayerHotkeys>().enabled = true;
        player.GetComponent<PlayerCombat>().enabled = true;
        player.transform.Find("Main Camera").gameObject.SetActive(true);
        player.transform.Find("NamePlate").gameObject.SetActive(false);

        // Set minimap target
        GameObject.Find("MiniMapCamera").GetComponent<MiniMap>().target = player.transform;
        GameObject.Find("MiniMapElementsCamera").GetComponent<MiniMap>().target = player.transform;
        var menu = FindObjectOfType<ConfigMenu>();
        var configObj = GameObject.Find("Canvas/TopMenu/Config");
        var configMenu = configObj?.GetComponent<ConfigMenu>();
        if (configMenu is not null) configMenu.player = player;

        player.transform.Find("Indicator").GetComponent<Renderer>().material.mainTexture = mmarow;
    }
    /*
    void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        chat.sendMessage(player.name + " left the game");
    }

    void OnPhotonPlayerConnect(PhotonPlayer player)
    {
        chat.sendMessage(player.name + " joined the game");
    }*/

    private void OnJoinedLobby()
    {
        PhotonNetwork.LoadLevel(Menu.defaultLevel);
        PhotonNetwork.JoinRandomRoom();
        //Menu.Instance.showPanel("LoadingPanel");
    }


    private void OnJoinedRoom()
    {
    }

    private void OnCreatedRoom()
    {
        //OnJoinedRoom ();
    }

    private void OnPhotonRandomJoinFailed()
    {
        PhotonNetwork.CreateRoom(null);
    }


    public void OnPhotonCreateRoomFailed()
    {
        Debug.Log(
            "OnPhotonCreateRoomFailed got called. This can happen if the room exists (even if not visible). Try another room name.");
    }

    public void OnPhotonJoinRoomFailed()
    {
        Debug.Log("OnPhotonJoinRoomFailed got called. This can happen if the room is not existing or full or closed.");
    }

    public void OnDisconnectedFromPhoton()
    {
        Debug.Log("Disconnected from Photon.");
    }

    public void OnFailedToConnectToPhoton(object parameters)
    {
        Debug.Log("OnFailedToConnectToPhoton. StatusCode: " + parameters + " ServerAddress: " +
                  PhotonNetwork.networkingPeer.ServerAddress);
    }

    public static bool MyRemoteCertificateValidationCallback(object sender, X509Certificate certificate,
        X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        var isOk = true;
        // If there are errors in the certificate chain, look at each error to determine the cause.
        if (sslPolicyErrors != SslPolicyErrors.None)
            for (var i = 0; i < chain.ChainStatus.Length; i++)
                if (chain.ChainStatus[i].Status != X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    var chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid) isOk = false;
                }

        return isOk;
    }
}