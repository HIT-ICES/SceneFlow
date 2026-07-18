using UnityEngine;
using UnityEngine.UI;

public class FPS : MonoBehaviour
{
    private float accum;
    public Text fpst;
    private int frames;
    public bool ping = false;
    private float timeleft;

    public float updateInterval = 0.5F;

    private void Start()
    {
        timeleft = updateInterval;
    }

    private void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        if (timeleft <= 0.0)
        {
            var fps = accum / frames;
            var format = string.Format("{0:F0} FPS", fps);
            if (ping)
                fpst.text = format + "\nPing: " + PhotonNetwork.GetPing() + "ms";
            else
                fpst.text = format;

            timeleft = updateInterval;
            accum = 0.0F;
            frames = 0;
        }
    }
}