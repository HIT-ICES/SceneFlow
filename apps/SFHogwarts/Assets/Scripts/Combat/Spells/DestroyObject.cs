using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

public class DestroyObject : MonoBehaviour
{
    public bool detachChildren = false;
    public bool isParticle = false;

    public float timeOut = 10.0f;

    public void Awake()
    {
        if (!photonView.isMine) return;

        if (isParticle)
        {
            var ps = GetComponentInChildren<ParticleSystem>();
            timeOut = ps.main.duration;
        }

        Invoke("DestroyNow", timeOut);
    }

    public void DestroyNow()
    {
        if (detachChildren) transform.DetachChildren();

        PhotonNetwork.Destroy(gameObject);
    }
}