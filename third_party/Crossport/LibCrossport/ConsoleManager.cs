using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour
{
    private static ConsoleManager _instance;
    [SerializeField] private Text console;
    private string consoleText;

    private static void Log(string msg)
    {
        if (_instance == null) return;
        if (_instance.console.text.Length > 2000) _instance.console.text = msg;
        else
            _instance.consoleText = _instance.console.text + "\n" + msg;
    }

    public static void LogWithDebug(string msg)
    {
        Debug.Log(msg);
        Log($"[Debug][{DateTime.Now:HH:mm:ss:fff}]{msg}");
    }

    public static void LogWithDebugWarning(string msg)
    {
        Debug.Log(msg);
        Log($"[Warning][{DateTime.Now:HH:mm:ss:fff}]{msg}");
    }

    public static void LogWithDebugError(string msg)
    {
        Debug.Log(msg);
        Log($"[Error][{DateTime.Now:HH:mm:ss:fff}]{msg}");
    }

    //private Text console;
    private void Awake()
    {
        _instance = this;
        //console = Instantiate(baseText, displayParent);
        console.text = "";
        console.gameObject.SetActive(true);
    }

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update() { console.text = consoleText; }
}