using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Assets.Scripts.LibCrossport.Settings;
using Ices.Crossport;
using Ices.Crossport.Core.DualHandling;
using Ices.Crossport.ObjectModel;
using Unity.RenderStreaming;
using UnityEngine;
using UnityEngine.UI;


public class BackgroundCrossportDriver : RemoteConfiguredReceiverCrossportDriver
{
    public override void Awake()
    {
        base.Awake();
        inputSender = GetComponent<InputSender>();
        inputSender.OnStartedChannel += OnStartedChannel;
    }

    private void OnStartedChannel(string connectionId)
    {
        CalculateInputRegion();
        CrossportClientUtils.ResetStats();
    }

    private void OnRectTransformDimensionsChange() { CalculateInputRegion(); }

    private void CalculateInputRegion()
    {
        if (inputSender == null || !inputSender.IsConnected)
            return;

        inputSender.CalculateInputResion
        (
            new Rect(0, 0, Screen.width, Screen.height),
            new Vector2Int(Screen.width, Screen.height)
        );
        inputSender.EnableInputPositionCorrection(true);
    }

#pragma warning disable 0649
    private InputSender inputSender;
#pragma warning restore 0649
}