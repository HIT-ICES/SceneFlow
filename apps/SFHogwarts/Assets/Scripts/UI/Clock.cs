using System;
using UnityEngine;
using UnityEngine.UI;

public class Clock : MonoBehaviour
{
    private void Update()
    {
        gameObject.GetComponent<Text>().text = DateTime.Now.ToString("HH:mm");
    }
}