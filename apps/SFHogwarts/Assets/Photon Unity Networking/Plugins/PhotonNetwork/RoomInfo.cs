// ----------------------------------------------------------------------------
// <copyright file="RoomInfo.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   This class resembles info about available rooms, as sent by the Master
//   server's lobby. Consider all values as readonly.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using System;
using ExitGames.Client.Photon;

/// <summary>
///     A simplified room with just the info required to list and join, used for the room listing in the lobby.
///     The properties are not settable (open, MaxPlayers, etc).
/// </summary>
/// <remarks>
///     This class resembles info about available rooms, as sent by the Master server's lobby.
///     Consider all values as readonly. None are synced (only updated by events by server).
/// </remarks>
/// \ingroup publicApi
public class RoomInfo
{
    /// <summary>Backing field for property. False unless the GameProperty is set to true (else it's not sent).</summary>
    protected bool autoCleanUpField = PhotonNetwork.autoCleanUpPlayerObjects;

    /// <summary>Backing field for property.</summary>
    private readonly Hashtable customPropertiesField = new();

    /// <summary>Backing field for property.</summary>
    protected int emptyRoomTtlField;

    /// <summary>Backing field for property.</summary>
    protected string[] expectedUsersField;

    /// <summary>Backing field for master client id (actorNumber). defined by server in room props and ev leave.</summary>
    protected internal int masterClientIdField;

    /// <summary>Backing field for property.</summary>
    protected byte maxPlayersField;

    /// <summary>Backing field for property.</summary>
    protected string nameField;

    /// <summary>Backing field for property.</summary>
    protected bool openField = true;

    /// <summary>Backing field for property.</summary>
    protected int playerTtlField;

    /// <summary>Backing field for property.</summary>
    protected bool visibleField = true;

    /// <summary>
    ///     Constructs a RoomInfo to be used in room listings in lobby.
    /// </summary>
    /// <param name="roomName"></param>
    /// <param name="properties"></param>
    protected internal RoomInfo(string roomName, Hashtable properties)
    {
        InternalCacheProperties(properties);

        nameField = roomName;
    }

    /// <summary>Used internally in lobby, to mark rooms that are no longer listed.</summary>
    public bool removedFromList { get; internal set; }

    protected internal bool serverSideMasterClient { get; private set; }

    /// <summary>
    ///     Read-only "cache" of custom properties of a room. Set via Room.SetCustomProperties (not available for RoomInfo
    ///     class!).
    /// </summary>
    /// <remarks>All keys are string-typed and the values depend on the game/application.</remarks>
    /// <see cref="Room.SetCustomProperties" />
    public Hashtable CustomProperties => customPropertiesField;

    /// <summary>The name of a room. Unique identifier (per Loadbalancing group) for a room/match.</summary>
    public string Name => nameField;

    /// <summary>
    ///     Only used internally in lobby, to display number of players in room (while you're not in).
    /// </summary>
    public int PlayerCount { get; private set; }

    /// <summary>
    ///     State if the local client is already in the game or still going to join it on gameserver (in lobby always false).
    /// </summary>
    public bool IsLocalClientInside { get; set; }

    /// <summary>
    ///     Sets a limit of players to this room. This property is shown in lobby, too.
    ///     If the room is full (players count == maxplayers), joining this room will fail.
    /// </summary>
    /// <remarks>
    ///     As part of RoomInfo this can't be set.
    ///     As part of a Room (which the player joined), the setter will update the server and all clients.
    /// </remarks>
    public byte MaxPlayers => maxPlayersField;

    /// <summary>
    ///     Defines if the room can be joined.
    ///     This does not affect listing in a lobby but joining the room will fail if not open.
    ///     If not open, the room is excluded from random matchmaking.
    ///     Due to racing conditions, found matches might become closed before they are joined.
    ///     Simply re-connect to master and find another.
    ///     Use property "IsVisible" to not list the room.
    /// </summary>
    /// <remarks>
    ///     As part of RoomInfo this can't be set.
    ///     As part of a Room (which the player joined), the setter will update the server and all clients.
    /// </remarks>
    public bool IsOpen => openField;

    /// <summary>
    ///     Defines if the room is listed in its lobby.
    ///     Rooms can be created invisible, or changed to invisible.
    ///     To change if a room can be joined, use property: open.
    /// </summary>
    /// <remarks>
    ///     As part of RoomInfo this can't be set.
    ///     As part of a Room (which the player joined), the setter will update the server and all clients.
    /// </remarks>
    public bool IsVisible => visibleField;

    /// <summary>
    ///     Makes RoomInfo comparable (by name).
    /// </summary>
    public override bool Equals(object other)
    {
        var otherRoomInfo = other as RoomInfo;
        return otherRoomInfo != null && Name.Equals(otherRoomInfo.nameField);
    }

    /// <summary>
    ///     Accompanies Equals, using the name's HashCode as return.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return nameField.GetHashCode();
    }


    /// <summary>Simple printingin method.</summary>
    /// <returns>Summary of this RoomInfo instance.</returns>
    public override string ToString()
    {
        return string.Format("Room: '{0}' {1},{2} {4}/{3} players.", nameField, visibleField ? "visible" : "hidden",
            openField ? "open" : "closed", maxPlayersField, PlayerCount);
    }

    /// <summary>Simple printingin method.</summary>
    /// <returns>Summary of this RoomInfo instance.</returns>
    public string ToStringFull()
    {
        return string.Format("Room: '{0}' {1},{2} {4}/{3} players.\ncustomProps: {5}", nameField,
            visibleField ? "visible" : "hidden", openField ? "open" : "closed", maxPlayersField, PlayerCount,
            customPropertiesField.ToStringFull());
    }

    /// <summary>
    ///     Copies "well known" properties to fields (IsVisible, etc) and caches the custom properties (string-keys only)
    ///     in a local hashtable.
    /// </summary>
    /// <param name="propertiesToCache">New or updated properties to store in this RoomInfo.</param>
    protected internal void InternalCacheProperties(Hashtable propertiesToCache)
    {
        if (propertiesToCache == null || propertiesToCache.Count == 0 ||
            customPropertiesField.Equals(propertiesToCache)) return;

        // check of this game was removed from the list. in that case, we don't
        // need to read any further properties
        // list updates will remove this game from the game listing
        if (propertiesToCache.ContainsKey(GamePropertyKey.Removed))
        {
            removedFromList = (bool)propertiesToCache[GamePropertyKey.Removed];
            if (removedFromList) return;
        }

        // fetch the "well known" properties of the room, if available
        if (propertiesToCache.ContainsKey(GamePropertyKey.MaxPlayers))
            maxPlayersField = (byte)propertiesToCache[GamePropertyKey.MaxPlayers];

        if (propertiesToCache.ContainsKey(GamePropertyKey.IsOpen))
            openField = (bool)propertiesToCache[GamePropertyKey.IsOpen];

        if (propertiesToCache.ContainsKey(GamePropertyKey.IsVisible))
            visibleField = (bool)propertiesToCache[GamePropertyKey.IsVisible];

        if (propertiesToCache.ContainsKey(GamePropertyKey.PlayerCount))
            PlayerCount = (byte)propertiesToCache[GamePropertyKey.PlayerCount];

        if (propertiesToCache.ContainsKey(GamePropertyKey.CleanupCacheOnLeave))
            autoCleanUpField = (bool)propertiesToCache[GamePropertyKey.CleanupCacheOnLeave];

        if (propertiesToCache.ContainsKey(GamePropertyKey.MasterClientId))
        {
            serverSideMasterClient = true;
            var isUpdate = masterClientIdField != 0;
            masterClientIdField = (int)propertiesToCache[GamePropertyKey.MasterClientId];
            if (isUpdate) PhotonNetwork.networkingPeer.UpdateMasterClient();
        }

        //if (propertiesToCache.ContainsKey(GamePropertyKey.PropsListedInLobby))
        //{
        //    // could be cached but isn't useful
        //}

        if (propertiesToCache.ContainsKey(GamePropertyKey.ExpectedUsers))
            expectedUsersField = (string[])propertiesToCache[GamePropertyKey.ExpectedUsers];

        if (propertiesToCache.ContainsKey(GamePropertyKey.EmptyRoomTtl))
            emptyRoomTtlField = (int)propertiesToCache[GamePropertyKey.EmptyRoomTtl];

        if (propertiesToCache.ContainsKey(GamePropertyKey.PlayerTtl))
            playerTtlField = (int)propertiesToCache[GamePropertyKey.PlayerTtl];

        // merge the custom properties (from your application) to the cache (only string-typed keys will be kept)
        customPropertiesField.MergeStringKeys(propertiesToCache);
        customPropertiesField.StripKeysWithNullValues();
    }


    #region Obsoleted variable names

    [Obsolete("Please use CustomProperties (updated case for naming).")]
    public Hashtable customProperties => CustomProperties;

    [Obsolete("Please use Name (updated case for naming).")]
    public string name => Name;

    [Obsolete("Please use PlayerCount (updated case for naming).")]
    public int playerCount
    {
        get => PlayerCount;
        set => PlayerCount = value;
    }

    [Obsolete("Please use IsLocalClientInside (updated case for naming).")]
    public bool isLocalClientInside
    {
        get => IsLocalClientInside;
        set => IsLocalClientInside = value;
    }

    [Obsolete("Please use MaxPlayers (updated case for naming).")]
    public byte maxPlayers => MaxPlayers;

    [Obsolete("Please use IsOpen (updated case for naming).")]
    public bool open => IsOpen;

    [Obsolete("Please use IsVisible (updated case for naming).")]
    public bool visible => IsVisible;

    #endregion
}