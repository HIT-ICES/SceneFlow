using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ices.Crossport.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Ices.Crossport.Diagnostics
{
    public class MTPRecorder : MonoBehaviour
    {
        /// <summary>
        /// Maximum MTP allowed
        /// </summary>
        public const int MTP_LIMIT = 2;

        [SerializeField] GameObject flashCanvas;
        [SerializeField] bool isFlashHandler;
        [SerializeField] bool isEvaluator;
        Queue<(DateTime, Texture2D)> writeBuffer = new();
        DateTime lastBegin;
        bool isRecording = false;
        private static List<double> _mtps = new();

        private static MTPRecorder _instance;
        bool isFlashing = false;
        public static void ResetStats() { _mtps.Clear(); }

        public static Latency Export()
            => Latency.FromRaw(_mtps);

        private void Awake() { _instance = this; }


        private void FixedUpdate()
        {
            if (isEvaluator && InputSystemAgent.GetKeyDown("X"))
            {
                lastBegin = DateTime.Now;
                isRecording = true;
            }

            if (isFlashHandler && InputSystemAgent.GetKey("X") && !isFlashing)
            {
                Flash();
            }
        }

        // Update is called once per frame
        private void LateUpdate() { Capture(); }

        private void Capture()
        {
            if (isRecording) StartCoroutine(runCapture());
            return;

            IEnumerator runCapture()
            {
                yield return new WaitForEndOfFrame();
                var time = DateTime.Now;
                var screen = ScreenCapture.CaptureScreenshotAsTexture();
                var mtp = time - lastBegin;
                if (IsMotionFlash(screen))
                {
                    Debug.Log($"Captured MTP: {mtp.TotalMilliseconds:0.000} ms");
                    _mtps.Add(mtp.TotalMilliseconds);
                    isRecording = false;
                }
                else if (mtp.TotalSeconds > MTP_LIMIT)
                {
                    Debug.Log($"MTP Measurement TLE");
                    isRecording = false;
                }

                Destroy(screen);
                //writeBuffer.Enqueue((time, ScreenCapture.CaptureScreenshotAsTexture()));
            }
        }


        private static bool IsMotionFlash(Texture2D screen)
        {
            var midPixel = screen.GetPixel(screen.width >> 1, screen.height >> 1);

            return midPixel.a >= 0.9 && midPixel.r >= 0.9 && midPixel.g + midPixel.b < 0.1;
        }

        public void Flash()
        {
            StartCoroutine(runFlash());
            return;

            IEnumerator runFlash()
            {
                isFlashing = true;
                flashCanvas.SetActive(true);
                yield return null;
                flashCanvas.SetActive(false);
                isFlashing = false;
            }
        }
    }
}