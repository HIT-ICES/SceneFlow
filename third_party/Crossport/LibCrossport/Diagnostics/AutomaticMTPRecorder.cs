using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ices.Crossport.Diagnostics;
using UnityEngine;

namespace Ices.Crossport.Diagnostics
{
    public class AutomaticMTPRecorder : MonoBehaviour
    {
        /// <summary>
        /// Maximum MTP allowed
        /// </summary>
        public const int MTP_LIMIT = 2;

        [Header("Configuration")]
        [SerializeField] GameObject flashCanvas;
        [SerializeField] bool isFlashHandler;
        [SerializeField] bool isEvaluator;

        [Header("Auto Recording Settings")]
        [Tooltip("Enable automatic MTP recording")]
        [SerializeField] bool enableAutoRecording = false;

        [Tooltip("Interval between automatic recordings (in seconds)")]
        [SerializeField] float autoRecordInterval = 0.5f;

        DateTime lastBegin;
        bool isRecording = false;
        private static List<double> _mtps = new();

        bool isFlashing = false;
        float timeSinceLastAutoRecord = 0f;
        Coroutine captureCoroutine = null;

        public static void ResetStats() { _mtps.Clear(); }

        public static Latency Export()
            => Latency.FromRaw(_mtps);

        /// <summary>
        /// Enable or disable automatic recording at runtime
        /// </summary>
        public void SetAutoRecording(bool enabled)
        {
            enableAutoRecording = enabled;
            if (enabled)
            {
                timeSinceLastAutoRecord = autoRecordInterval; // Trigger immediately on enable
                Debug.Log("Automatic MTP recording enabled");
            }
            else
            {
                Debug.Log("Automatic MTP recording disabled");
            }
        }

        /// <summary>
        /// Get current auto recording state
        /// </summary>
        public bool IsAutoRecordingEnabled() => enableAutoRecording;

        private void OnDisable()
        {
            // Stop any running coroutine when disabled
            if (captureCoroutine != null)
            {
                StopCoroutine(captureCoroutine);
                captureCoroutine = null;
            }
        }


        private void FixedUpdate()
        {
            // Auto recording logic
            if (enableAutoRecording && !isRecording)
            {
                timeSinceLastAutoRecord += Time.fixedDeltaTime;

                if (timeSinceLastAutoRecord >= autoRecordInterval)
                {
                    timeSinceLastAutoRecord = 0f;
                    TriggerMeasurement();

                    // Auto flash trigger - synchronized with measurement
                    if (isFlashHandler && !isFlashing)
                    {
                        Flash();
                    }
                }
            }
        }

        /// <summary>
        /// Trigger a single MTP measurement
        /// </summary>
        private void TriggerMeasurement()
        {
            if (!isRecording)
            {
                lastBegin = DateTime.Now;
                isRecording = true;
                Debug.Log("MTP measurement triggered");
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
                FrameRateRecorder.IgnoreNextLocalFrame();
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
                else
                {
                    Debug.Log($"MTP Measurement No Flash Detected");
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