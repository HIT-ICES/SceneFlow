using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class Intro : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<VideoPlayer>().loopPointReached += EndReached;
    }

    private void Update()
    {
        if (Input.anyKeyDown) SceneManager.LoadScene("MainMenu");
    }

    private void EndReached(VideoPlayer vp)
    {
        SceneManager.LoadScene("MainMenu");
    }
}