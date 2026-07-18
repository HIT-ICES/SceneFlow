using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    // Use this for initialization
    private void Start()
    {
        PhotonNetwork.Instantiate("NPC/Spider", transform.position, Quaternion.identity, 0);
    }
}