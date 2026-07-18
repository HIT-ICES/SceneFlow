using UnityEngine;
using MonoBehaviour = Photon.MonoBehaviour;

/// <summary>
///     Can be attached to a GameObject to show info about the owner of the PhotonView.
/// </summary>
/// <remarks>
///     This is a Photon.Monobehaviour, which adds the property photonView (that's all).
/// </remarks>
[RequireComponent(typeof(PhotonView))]
public class ShowInfoOfPlayer : MonoBehaviour
{
    public float CharacterSize = 0;
    public bool DisableOnOwnObjects;

    public Font font;
    private GameObject textGo;
    private TextMesh tm;

    private void Start()
    {
        if (font == null)
        {
#if UNITY_3_5
            font = (Font)FindObjectsOfTypeIncludingAssets(typeof(Font))[0];
#else
            font = (Font)Resources.FindObjectsOfTypeAll(typeof(Font))[0];
#endif
            Debug.LogWarning("No font defined. Found font: " + font);
        }

        if (tm == null)
        {
            textGo = new GameObject("3d text");
            //textGo.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
            textGo.transform.parent = gameObject.transform;
            textGo.transform.localPosition = Vector3.zero;

            var mr = textGo.AddComponent<MeshRenderer>();
            mr.material = font.material;
            tm = textGo.AddComponent<TextMesh>();
            tm.font = font;
            tm.anchor = TextAnchor.MiddleCenter;
            if (CharacterSize > 0) tm.characterSize = CharacterSize;
        }
    }

    private void Update()
    {
        var showInfo = !DisableOnOwnObjects || photonView.isMine;
        if (textGo != null) textGo.SetActive(showInfo);
        if (!showInfo) return;


        var owner = photonView.owner;
        if (owner != null)
            tm.text = string.IsNullOrEmpty(owner.NickName) ? "player" + owner.ID : owner.NickName;
        else if (photonView.isSceneView)
            tm.text = "scn";
        else
            tm.text = "n/a";
    }
}