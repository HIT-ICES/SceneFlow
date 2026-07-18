using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class ManualPhotonViewAllocator : MonoBehaviour
{
    public GameObject Prefab;

    public void AllocateManualPhotonView()
    {
        var pv = gameObject.GetPhotonView();
        if (pv == null)
        {
            Debug.LogError("Can't do manual instantiation without PhotonView component.");
            return;
        }

        var viewID = PhotonNetwork.AllocateViewID();
        pv.RPC("InstantiateRpc", PhotonTargets.AllBuffered, viewID);
    }

    [PunRPC]
    public void InstantiateRpc(int viewID)
    {
        var go = Instantiate(Prefab, InputToEvent.inputHitPos + new Vector3(0, 5f, 0), Quaternion.identity);
        go.GetPhotonView().viewID = viewID;

        var ocd = go.GetComponent<OnClickDestroy>();
        ocd.DestroyByRpc = true;
    }
}