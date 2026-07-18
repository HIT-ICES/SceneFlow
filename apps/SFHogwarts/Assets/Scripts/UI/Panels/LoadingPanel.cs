using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingPanel : MonoBehaviour
{
    public RectTransform loadBar;
    public GameObject self;
    public Text text;

    // Use this for initialization
    private void Start()
    {
        //PhotonNetwork.isMessageQueueRunning = false;
        //AsyncOperation async = Application.LoadLevelAsync(Menu.defaultLevel);
        //StartCoroutine (loadLevel(async));
    }

    private IEnumerator loadLevel(AsyncOperation async)
    {
        int progress;
        text.text = "Cargando 0%";


        while (!async.isDone)
        {
            progress = (int)async.progress * 100;

            text.text = "Cargando " + progress + "%";

            //loadBar.localScale = new Vector2(load.progress, loadBar.localScale.y);
            yield return null;
        }

        if (async.isDone) PhotonNetwork.isMessageQueueRunning = true;
    }
}