using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

[RequireComponent(typeof(PhotonView))]
public class HighlightOwnedGameObj : MonoBehaviour
{
    private Transform markerTransform;
    public float Offset = 0.5f;
    public GameObject PointerPrefab;


    // Update is called once per frame
    private void Update()
    {
        if (photonView.isMine)
        {
            if (markerTransform == null)
            {
                var markerObject = Instantiate(PointerPrefab);
                markerObject.transform.parent = gameObject.transform;
                markerTransform = markerObject.transform;
            }

            var parentPos = gameObject.transform.position;
            markerTransform.position = new Vector3(parentPos.x, parentPos.y + Offset, parentPos.z);
            markerTransform.rotation = Quaternion.identity;
        }
        else if (markerTransform != null)
        {
            Destroy(markerTransform.gameObject);
            markerTransform = null;
        }
    }
}