using UnityEngine;

public class GameOverPanel : MonoBehaviour
{
    public GameObject respawnButton;


    public void OnRespawn()
    {
        Player.Instance.Reborn();
    }
}