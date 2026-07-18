using System.Text;
using UnityEngine;

public class SupportLogger : MonoBehaviour
{
    public bool LogTrafficStats = true;

    public void Start()
    {
        var go = GameObject.Find("PunSupportLogger");
        if (go == null)
        {
            go = new GameObject("PunSupportLogger");
            DontDestroyOnLoad(go);
            var sl = go.AddComponent<SupportLogging>();
            sl.LogTrafficStats = LogTrafficStats;
        }
    }
}

public class SupportLogging : MonoBehaviour
{
    public bool LogTrafficStats;

    public void Start()
    {
        if (LogTrafficStats) InvokeRepeating("LogStats", 10, 10);
    }


    protected void OnApplicationPause(bool pause)
    {
        Debug.Log("SupportLogger OnApplicationPause: " + pause + " connected: " + PhotonNetwork.connected);
    }

    public void OnApplicationQuit()
    {
        CancelInvoke();
    }

    public void LogStats()
    {
        if (LogTrafficStats) Debug.Log("SupportLogger " + PhotonNetwork.NetworkStatisticsToString());
    }

    private void LogBasics()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("SupportLogger Info: PUN {0}: ", PhotonNetwork.versionPUN);

        sb.AppendFormat("AppID: {0}*** GameVersion: {1} PeerId: {2} ",
            PhotonNetwork.networkingPeer.AppId.Substring(0, 8), PhotonNetwork.networkingPeer.AppVersion,
            PhotonNetwork.networkingPeer.PeerID);
        sb.AppendFormat("Server: {0}. Region: {1} ", PhotonNetwork.ServerAddress,
            PhotonNetwork.networkingPeer.CloudRegion);
        sb.AppendFormat("HostType: {0} ", PhotonNetwork.PhotonServerSettings.HostType);


        Debug.Log(sb.ToString());
    }


    public void OnConnectedToPhoton()
    {
        Debug.Log("SupportLogger OnConnectedToPhoton().");
        LogBasics();

        if (LogTrafficStats) PhotonNetwork.NetworkStatisticsEnabled = true;
    }

    public void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Debug.Log("SupportLogger OnFailedToConnectToPhoton(" + cause + ").");
        LogBasics();
    }

    public void OnJoinedLobby()
    {
        Debug.Log("SupportLogger OnJoinedLobby(" + PhotonNetwork.lobby + ").");
    }

    public void OnJoinedRoom()
    {
        Debug.Log("SupportLogger OnJoinedRoom(" + PhotonNetwork.room + "). " + PhotonNetwork.lobby + " GameServer:" +
                  PhotonNetwork.ServerAddress);
    }

    public void OnCreatedRoom()
    {
        Debug.Log("SupportLogger OnCreatedRoom(" + PhotonNetwork.room + "). " + PhotonNetwork.lobby + " GameServer:" +
                  PhotonNetwork.ServerAddress);
    }

    public void OnLeftRoom()
    {
        Debug.Log("SupportLogger OnLeftRoom().");
    }

    public void OnDisconnectedFromPhoton()
    {
        Debug.Log("SupportLogger OnDisconnectedFromPhoton().");
    }
}