using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private const float updateTime = 1f;

    private string fps = "";

    private float frames;
    private float time;

    // 
    private void Update()
    {
        time += Time.deltaTime;
        if (time >= updateTime)
        {
            fps = "FPS: " + (frames / time).ToString("f2");
            time = 0f;
            frames = 0f;
        }

        frames++;
    }

    // 
    private void OnGUI()
    {
        GUI.Label(new Rect(Screen.width - 100, Screen.height - 45, 100, 20), fps);
    }
}