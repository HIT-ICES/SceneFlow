#pragma warning disable 1587
/// \file
/// <summary>ScriptableObject defining a server setup. An instance is created as <b>PhotonServerSettings</b>.</summary>
#pragma warning restore 1587

using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

public class Region
{
    /// <summary>Unlike the CloudRegionCode, this may contain cluster information.</summary>
    public string Cluster;

    public CloudRegionCode Code;
    public string HostAndPort;
    public int Ping;

    public Region(CloudRegionCode code)
    {
        Code = code;
        Cluster = code.ToString();
    }

    public Region(CloudRegionCode code, string regionCodeString, string address)
    {
        Code = code;
        Cluster = regionCodeString;
        HostAndPort = address;
    }

    public static CloudRegionCode Parse(string codeAsString)
    {
        if (codeAsString == null) return CloudRegionCode.none;

        var slash = codeAsString.IndexOf('/');
        if (slash > 0) codeAsString = codeAsString.Substring(0, slash);
        codeAsString = codeAsString.ToLower();

        if (Enum.IsDefined(typeof(CloudRegionCode), codeAsString))
            return (CloudRegionCode)Enum.Parse(typeof(CloudRegionCode), codeAsString);

        return CloudRegionCode.none;
    }


    internal static CloudRegionFlag ParseFlag(CloudRegionCode region)
    {
        if (Enum.IsDefined(typeof(CloudRegionFlag), region.ToString()))
            return (CloudRegionFlag)Enum.Parse(typeof(CloudRegionFlag), region.ToString());

        return 0;
    }

    [Obsolete]
    internal static CloudRegionFlag ParseFlag(string codeAsString)
    {
        codeAsString = codeAsString.ToLower();

        CloudRegionFlag code = 0;
        if (Enum.IsDefined(typeof(CloudRegionFlag), codeAsString))
            code = (CloudRegionFlag)Enum.Parse(typeof(CloudRegionFlag), codeAsString);

        return code;
    }

    public override string ToString()
    {
        return string.Format("'{0}' \t{1}ms \t{2}", Cluster, Ping, HostAndPort);
    }
}


/// <summary>
///     Collection of connection-relevant settings, used internally by PhotonNetwork.ConnectUsingSettings.
/// </summary>
[Serializable]
public class ServerSettings : ScriptableObject
{
    public enum HostingOption
    {
        NotSet = 0,
        PhotonCloud = 1,
        SelfHosted = 2,
        OfflineMode = 3,
        BestRegion = 4
    }

    public string AppID = "";
    public string ChatAppID = "";

    [HideInInspector] public bool DisableAutoOpenWizard;

    public CloudRegionFlag EnabledRegions = (CloudRegionFlag)(-1);
    public bool EnableLobbyStatistics;

    public HostingOption HostType = HostingOption.NotSet;


    public bool JoinLobby;
    public DebugLevel NetworkLogging = DebugLevel.ERROR;

    public CloudRegionCode PreferredRegion;

    public ConnectionProtocol Protocol = ConnectionProtocol.Udp;
    public PhotonLogLevel PunLogging = PhotonLogLevel.ErrorsOnly;

    public List<string> RpcList = new(); // set by scripts and or via Inspector

    public bool RunInBackground = true;
    public string ServerAddress = "";
    public int ServerPort = 5055;
    public string VoiceAppID = "";
    public int VoiceServerPort = 5055; // Voice only uses UDP

    /// <summary>
    ///     Gets the best region code in preferences.
    ///     This composes the PhotonHandler, since its Internal and can not be accessed by the custom inspector
    /// </summary>
    /// <value>The best region code in preferences.</value>
    public static CloudRegionCode BestRegionCodeInPreferences => PhotonHandler.BestRegionCodeInPreferences;


    public void UseCloudBestRegion(string cloudAppid)
    {
        HostType = HostingOption.BestRegion;
        AppID = cloudAppid;
    }

    public void UseCloud(string cloudAppid)
    {
        HostType = HostingOption.PhotonCloud;
        AppID = cloudAppid;
    }

    public void UseCloud(string cloudAppid, CloudRegionCode code)
    {
        HostType = HostingOption.PhotonCloud;
        AppID = cloudAppid;
        PreferredRegion = code;
    }

    public void UseMyServer(string serverAddress, int serverPort, string application)
    {
        HostType = HostingOption.SelfHosted;
        AppID = application != null ? application : "master";

        ServerAddress = serverAddress;
        ServerPort = serverPort;
    }

    /// <summary>Checks if a string is a Guid by attempting to create one.</summary>
    /// <param name="val">The potential guid to check.</param>
    /// <returns>True if new Guid(val) did not fail.</returns>
    public static bool IsAppId(string val)
    {
        try
        {
            new Guid(val);
        }
        catch
        {
            return false;
        }

        return true;
    }

    public static void ResetBestRegionCodeInPreferences()
    {
        PhotonHandler.BestRegionCodeInPreferences = CloudRegionCode.none;
    }

    public override string ToString()
    {
        return "ServerSettings: " + HostType + " " + ServerAddress;
    }
}