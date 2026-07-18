using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

[RequireComponent(typeof(PhotonView))]
public class OnAwakeUsePhotonView : MonoBehaviour
{
    // tries to send an RPC as soon as this script awakes (e.g. immediately when instantiated)
    private void Awake()
    {
        if (!photonView.isMine) return;

        // Debug.Log("OnAwakeSendRPC.Awake() of " + this + " photonView: " + this.photonView + " this.photonView.instantiationData: " + this.photonView.instantiationData);
        photonView.RPC("OnAwakeRPC", PhotonTargets.All);
    }

    // tries to send an RPC as soon as this script starts (e.g. immediately when instantiated)
    private void Start()
    {
        if (!photonView.isMine) return;

        // Debug.Log("OnAwakeSendRPC.Start() of " + this + " photonView: " + this.photonView);
        photonView.RPC("OnAwakeRPC", PhotonTargets.All, (byte)1);
    }

    [PunRPC]
    public void OnAwakeRPC()
    {
        Debug.Log("RPC: 'OnAwakeRPC' PhotonView: " + photonView);
    }

    [PunRPC]
    public void OnAwakeRPC(byte myParameter)
    {
        Debug.Log("RPC: 'OnAwakeRPC' Parameter: " + myParameter + " PhotonView: " + photonView);
    }
}