using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ices.Crossport;
using Ices.Crossport.Diagnostics;
using Ices.Crossport.Settings;
using JetBrains.Annotations;
using Unity.RenderStreaming;
using Unity.RenderStreaming.Signaling;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Ices.Crossport.Receiver
{
    public partial class ControlPanel : MonoBehaviour
    {
        [SerializeField] List<string> options = new List<string>();
        private bool runningLock = false;
        private bool isRunning = false;

        protected virtual void Awake()
        {
            startButton.onClick.AddListener(OnStart);
            stopButton.onClick.AddListener(OnStop);
            var control = new CrossportUIControl();

            control.UIControl.Enable();
            RegisterEvents
            (
                control.UIControl.ToggleUI,
                c =>
                {
                    if ((c.control as ButtonControl).wasReleasedThisFrame)
                    {
                        gameObject.SetActive(!gameObject.activeSelf);
                    }
                }
            );
            RegisterEvents
            (
                control.UIControl.ToggleRunning,
                c =>
                {
                    if ((c.control as ButtonControl).wasReleasedThisFrame)
                    {
                        if (isRunning) OnStop();
                        else OnStart();
                    }
                }
            );

            //settings = SampleManager.Instance.Settings;
        }

        protected void SetRunning(bool running)
        {
            isRunning = running;
            runningLock = false;

            stopButton.gameObject.SetActive(running);
            startButton.gameObject.SetActive(!running);

            ;
        }

        protected bool AcquireRunningLock()
        {
            return true;
            if (runningLock)
            {
                return false;
            }

            runningLock = true;
            return true;
        }


        protected virtual void OnStart()
        {
            if (!AcquireRunningLock()) return;
            ConsoleManager.LogWithDebug($"Connecting to server: {crossportSetting.GetFetchAppUrl()}");


            StartCoroutine(StartAsync());
        }

        protected IEnumerator StartAsync()
        {
            yield return CrossportClientUtils.FetchAndApplyConfig(crossportSetting, true);
            automaticMTPRecorder?.SetAutoRecording(true);

            SetRunning(true);
        }

        protected virtual void OnStop()
        {
            automaticMTPRecorder?.SetAutoRecording(false);
            StartCoroutine(StopAsync());
        }

        protected IEnumerator StopAsync()
        {
            yield return CrossportClientUtils.SubmitStats(crossportSetting);
            SetRunning(false);
        }
#pragma warning disable 0649
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField, CanBeNull] private AutomaticMTPRecorder automaticMTPRecorder;
        [Tooltip("Crossport Receiver Setting")]
        protected CrossportClientSetting crossportSetting = new(); // => SampleManager.Instance.CrossportSettings;
#pragma warning restore 0649
    }
}