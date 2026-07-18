using ExitGames.Client.Photon;
using UnityEngine;

public class PunPlayerScores : MonoBehaviour
{
    public const string PlayerScoreProp = "score";
}

public static class ScoreExtensions
{
    public static void SetScore(this PhotonPlayer player, int newScore)
    {
        var score = new Hashtable(); // using PUN's implementation of Hashtable
        score[PunPlayerScores.PlayerScoreProp] = newScore;

        player.SetCustomProperties(score); // this locally sets the score and will sync it in-game asap.
    }

    public static void AddScore(this PhotonPlayer player, int scoreToAddToCurrent)
    {
        var current = player.GetScore();
        current = current + scoreToAddToCurrent;

        var score = new Hashtable(); // using PUN's implementation of Hashtable
        score[PunPlayerScores.PlayerScoreProp] = current;

        player.SetCustomProperties(score); // this locally sets the score and will sync it in-game asap.
    }

    public static int GetScore(this PhotonPlayer player)
    {
        object score;
        if (player.CustomProperties.TryGetValue(PunPlayerScores.PlayerScoreProp, out score)) return (int)score;

        return 0;
    }
}