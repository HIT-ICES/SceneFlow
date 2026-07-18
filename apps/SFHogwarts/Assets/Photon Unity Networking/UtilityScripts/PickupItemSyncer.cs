using System;
using System.Collections.Generic;
using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

/// <summary>Finds out which PickupItems are not spawned at the moment and send this to new players.</summary>
/// <remarks>Attach this component to a single GameObject in the scene, not to all PickupItems.</remarks>
[RequireComponent(typeof(PhotonView))]
public class PickupItemSyncer : MonoBehaviour
{
    private const float TimeDeltaToIgnore = 0.2f;
    public bool IsWaitingForPickupInit;


    public void OnPhotonPlayerConnected(PhotonPlayer newPlayer)
    {
        if (PhotonNetwork.isMasterClient) SendPickedUpItems(newPlayer);
    }

    public void OnJoinedRoom()
    {
        Debug.Log("Joined Room. isMasterClient: " + PhotonNetwork.isMasterClient + " id: " + PhotonNetwork.player.ID);
        // this client joined the room. let's see if there are players and if someone has to inform us about pickups
        IsWaitingForPickupInit = !PhotonNetwork.isMasterClient;

        if (PhotonNetwork.playerList.Length >= 2) Invoke("AskForPickupItemSpawnTimes", 2.0f);
    }


    public void AskForPickupItemSpawnTimes()
    {
        if (IsWaitingForPickupInit)
        {
            if (PhotonNetwork.playerList.Length < 2)
            {
                Debug.Log("Cant ask anyone else for PickupItem spawn times.");
                IsWaitingForPickupInit = false;
                return;
            }


            // find a another player (than the master, who likely is gone) to ask for the PickupItem spawn times
            var nextPlayer = PhotonNetwork.masterClient.GetNext();
            if (nextPlayer == null || nextPlayer.Equals(PhotonNetwork.player))
                nextPlayer = PhotonNetwork.player.GetNext();
            //Debug.Log("This player is the Master's next. Asking this client's 'next' player: " + ((nextPlayer != null) ? nextPlayer.ToStringFull() : ""));
            if (nextPlayer != null && !nextPlayer.Equals(PhotonNetwork.player))
            {
                photonView.RPC("RequestForPickupItems", nextPlayer);

                // you could restart this invoke and try to find another player after 4 seconds. but after a while it doesnt make a difference anymore
                //this.Invoke("AskForPickupItemSpawnTimes", 2.0f);
            }
            else
            {
                Debug.Log("No player left to ask");
                IsWaitingForPickupInit = false;
            }
        }
    }

    [PunRPC]
    [Obsolete("Use RequestForPickupItems(PhotonMessageInfo msgInfo) with corrected typing instead.")]
    public void RequestForPickupTimes(PhotonMessageInfo msgInfo)
    {
        RequestForPickupItems(msgInfo);
    }

    [PunRPC]
    public void RequestForPickupItems(PhotonMessageInfo msgInfo)
    {
        if (msgInfo.sender == null)
        {
            Debug.LogError("Unknown player asked for PickupItems");
            return;
        }

        SendPickedUpItems(msgInfo.sender);
    }

    /// <summary>Summarizes all PickupItem ids and spawn times for new players. Calls RPC "PickupItemInit".</summary>
    /// <param name="targetPlayer">The player to send the pickup times to. It's a targetted RPC.</param>
    private void SendPickedUpItems(PhotonPlayer targetPlayer)
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("Cant send PickupItem spawn times to unknown targetPlayer.");
            return;
        }

        var now = PhotonNetwork.time;
        var soon = now + TimeDeltaToIgnore;


        var items = new PickupItem[PickupItem.DisabledPickupItems.Count];
        PickupItem.DisabledPickupItems.CopyTo(items);

        var valuesToSend = new List<float>(items.Length * 2);
        for (var i = 0; i < items.Length; i++)
        {
            var pi = items[i];
            if (pi.SecondsBeforeRespawn <= 0)
            {
                valuesToSend.Add(pi.ViewID);
                valuesToSend.Add(0.0f);
            }
            else
            {
                var timeUntilRespawn = pi.TimeOfRespawn - PhotonNetwork.time;
                if (pi.TimeOfRespawn > soon)
                {
                    // the respawn of this item is not "immediately", so we include it in the message "these items are not active" for the new player
                    Debug.Log(pi.ViewID + " respawn: " + pi.TimeOfRespawn + " timeUntilRespawn: " + timeUntilRespawn +
                              " (now: " + PhotonNetwork.time + ")");
                    valuesToSend.Add(pi.ViewID);
                    valuesToSend.Add((float)timeUntilRespawn);
                }
            }
        }

        Debug.Log("Sent count: " + valuesToSend.Count + " now: " + now);
        photonView.RPC("PickupItemInit", targetPlayer, PhotonNetwork.time, valuesToSend.ToArray());
    }


    [PunRPC]
    public void PickupItemInit(double timeBase, float[] inactivePickupsAndTimes)
    {
        IsWaitingForPickupInit = false;

        // if there are no inactive pickups, the sender will send a list of 0 items. this is not a problem...
        for (var i = 0; i < inactivePickupsAndTimes.Length / 2; i++)
        {
            var arrayIndex = i * 2;
            var viewIdOfPickup = (int)inactivePickupsAndTimes[arrayIndex];
            var timeUntilRespawnBasedOnTimeBase = inactivePickupsAndTimes[arrayIndex + 1];


            var view = PhotonView.Find(viewIdOfPickup);
            var pi = view.GetComponent<PickupItem>();

            if (timeUntilRespawnBasedOnTimeBase <= 0)
            {
                pi.PickedUp(0.0f);
            }
            else
            {
                var timeOfRespawn = timeUntilRespawnBasedOnTimeBase + timeBase;

                Debug.Log(view.viewID + " respawn: " + timeOfRespawn + " timeUntilRespawnBasedOnTimeBase:" +
                          timeUntilRespawnBasedOnTimeBase + " SecondsBeforeRespawn: " + pi.SecondsBeforeRespawn);
                var timeBeforeRespawn = timeOfRespawn - PhotonNetwork.time;
                if (timeUntilRespawnBasedOnTimeBase <= 0) timeBeforeRespawn = 0.0f;

                pi.PickedUp((float)timeBeforeRespawn);
            }
        }
    }
}