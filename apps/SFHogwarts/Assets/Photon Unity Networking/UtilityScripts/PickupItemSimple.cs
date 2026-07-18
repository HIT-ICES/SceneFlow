using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

/// <summary>
///     Makes a scene object pickup-able. Needs a PhotonView which belongs to the scene.
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PickupItemSimple : MonoBehaviour
{
    public bool PickupOnCollide;
    public float SecondsBeforeRespawn = 2;
    public bool SentPickup;

    public void OnTriggerEnter(Collider other)
    {
        // we only call Pickup() if "our" character collides with this PickupItem.
        // note: if you "position" remote characters by setting their translation, triggers won't be hit.

        var otherpv = other.GetComponent<PhotonView>();
        if (PickupOnCollide && otherpv != null && otherpv.isMine)
            //Debug.Log("OnTriggerEnter() calls Pickup().");
            Pickup();
    }

    public void Pickup()
    {
        if (SentPickup)
            // skip sending more pickups until the original pickup-RPC got back to this client
            return;

        SentPickup = true;
        photonView.RPC("PunPickupSimple", PhotonTargets.AllViaServer);
    }

    [PunRPC]
    public void PunPickupSimple(PhotonMessageInfo msgInfo)
    {
        // one of the messages might be ours
        // note: you could check "active" first, if you're not interested in your own, failed pickup-attempts.
        if (SentPickup && msgInfo.sender.IsLocal)
        {
            if (gameObject.GetActive())
            {
                // picked up! yay.
            }
            // pickup failed. too late (compared to others)
        }

        SentPickup = false;

        if (!gameObject.GetActive())
        {
            Debug.Log("Ignored PU RPC, cause item is inactive. " + gameObject);
            return;
        }


        // how long it is until this item respanws, depends on the pickup time and the respawn time
        var timeSinceRpcCall = PhotonNetwork.time - msgInfo.timestamp;
        var timeUntilRespawn = SecondsBeforeRespawn - (float)timeSinceRpcCall;
        //Debug.Log("msg timestamp: " + msgInfo.timestamp + " time until respawn: " + timeUntilRespawn);

        if (timeUntilRespawn > 0)
        {
            // this script simply disables the GO for a while until it respawns.
            gameObject.SetActive(false);
            Invoke("RespawnAfter", timeUntilRespawn);
        }
    }

    public void RespawnAfter()
    {
        if (gameObject != null) gameObject.SetActive(true);
    }
}