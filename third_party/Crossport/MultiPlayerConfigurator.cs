using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ices.Crossport.ObjectModel;
using UnityEngine;
using UnityEngine.UI;

namespace CrossportPlus
{
    public class MultiPlayerConfigurator : MonoBehaviour
    {
        private string MultiPlayerConfigPath => "multiplayer.json";
        private const string GracefulStopSignalFileName = "sceneflow_graceful_stop.signal";
        private const float GracefulStopPollIntervalSeconds = 0.5f;
        private const float GracefulStopQuitDelaySeconds = 5f;

        public List<GameObject> player;
        public GameObject sender;
        public GameObject receiver;

        [Header("ADB Graceful Stop")]
        [SerializeField] private bool enableAdbGracefulStop = true;

        private MultiPlayerConfig _config;
        private bool _gracefulStopRequested;

        private List<GameObject> _additionalSenders = new();
        private List<GameObject> _additionalReceivers = new();

        // Experiment automation hook: adb creates a signal file under the app files
        // directory, then this component invokes the existing Stop button so Crossport
        // stats are submitted before the Android app exits.
        public void Start()
        {
            if (enableAdbGracefulStop)
                StartCoroutine(WatchGracefulStopSignal());

            var args = Environment.GetCommandLineArgs().ToList();
            var fakeClientArgIndex = args.IndexOf("-fake-client");
            if (fakeClientArgIndex != -1 && fakeClientArgIndex + 1 < args.Count &&
                int.TryParse(args[fakeClientArgIndex + 1], out var cameraCount))
            {
                for (int i = 1; i < cameraCount; i++)
                {
                    var x = Instantiate(receiver);
                    _additionalReceivers.Add(x);
                    x.name = $"{receiver.name}-MultiPlayer-{i}";
                }

                return;
            }

            string json = null;
            if (File.Exists(MultiPlayerConfigPath))
            {
                json = File.ReadAllText(MultiPlayerConfigPath);
            }
            else if (File.Exists(Path.Combine(Application.persistentDataPath, MultiPlayerConfigPath)))
            {
                json = File.ReadAllText(Path.Combine(Application.persistentDataPath, MultiPlayerConfigPath));
            }

            if (json == null)
            {
                Debug.LogError("Multiplayer config file not found");
                return;
            }

            _config = JsonUtility.FromJson<MultiPlayerConfig>(json);


            if (_config.mainCameraConfig != null)
            {
                var c = _config.mainCameraConfig;
                if (c.position.HasValue)
                    sender.transform.position = c.position.Value;
                if (c.rotation.HasValue)
                    sender.transform.rotation = Quaternion.Euler(c.rotation.Value);
            }

            for (var i = 0; i < _config.additionalCameraCount; i++)
            {
                var c = i < _config.additionalCameraConfigs.Count ? _config.additionalCameraConfigs[i] : null;
                var newRemoting = Instantiate(sender);
                _additionalSenders.Add(newRemoting);
                if (c != null && c.position.HasValue)
                    newRemoting.transform.position = c.position.Value;
                if (c != null && c.rotation.HasValue)
                    newRemoting.transform.rotation = Quaternion.Euler(c.rotation.Value);
                newRemoting.name = $"{sender.name}-MultiPlayer-{i}";
            }
        }

        private IEnumerator WatchGracefulStopSignal()
        {
            while (!_gracefulStopRequested)
            {
                var signalPath = FindGracefulStopSignalPath();
                if (!string.IsNullOrEmpty(signalPath))
                {
                    _gracefulStopRequested = true;
                    DeleteGracefulStopSignal(signalPath);
                    yield return GracefulStopAndQuit();
                    yield break;
                }

                yield return new WaitForSeconds(GracefulStopPollIntervalSeconds);
            }
        }

        private string FindGracefulStopSignalPath()
        {
            foreach (var path in GracefulStopSignalPaths())
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        private IEnumerable<string> GracefulStopSignalPaths()
        {
            yield return Path.Combine(Application.persistentDataPath, GracefulStopSignalFileName);
            yield return GracefulStopSignalFileName;
        }

        private void DeleteGracefulStopSignal(string signalPath)
        {
            try
            {
                File.Delete(signalPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to delete graceful stop signal file: {signalPath}. {e}");
            }
        }

        private IEnumerator GracefulStopAndQuit()
        {
            Debug.Log("ADB graceful stop signal received. Invoking StopButton before quit.");

            var stopButton = FindStopButton();
            if (stopButton == null)
            {
                Debug.LogWarning("StopButton not found. Quitting without submitting Crossport stats.");
            }
            else
            {
                stopButton.onClick.Invoke();
            }

            yield return new WaitForSeconds(GracefulStopQuitDelaySeconds);
            Debug.Log("Graceful stop wait finished. Quitting application.");
            Application.Quit();
        }

        private Button FindStopButton()
        {
            var buttons = Resources.FindObjectsOfTypeAll<Button>()
                .Where(button => button != null &&
                                 button.name == "StopButton" &&
                                 button.gameObject.scene.IsValid())
                .ToList();

            return buttons.FirstOrDefault(button => button.gameObject.activeInHierarchy)
                   ?? buttons.FirstOrDefault();
        }

        public IEnumerator ForceScreenSize(int width, int height)
        {
            while (true)
            {
                if (Screen.currentResolution.width != width || Screen.currentResolution.height != height)
                {
                    Screen.SetResolution(width, height, false);
                    Debug.Log($"Forced screen size to {width}x{height}");
                    yield return null;
                }

                yield return null;
            }
        }

        public void StartPlayer()
        {
            if (Environment.GetCommandLineArgs().Contains("-fake-client"))
                return;
            if (player == null) return;
            foreach (var p in player)
            {
                Debug.Log($"Activate player {p.name}");
                p.SetActive(true);
            }
        }

        public void StartSenders()
        {
            sender.SetActive(true);
            foreach (var additionalSender in _additionalSenders)
                additionalSender.SetActive(true);
        }

        public void StartReceivers()
        {
            receiver.SetActive(true);
            foreach (var additionalReceiver in _additionalReceivers)
                additionalReceiver.SetActive(true);
        }
    }

    [Serializable]
    public class MultiPlayerConfig
    {
        public CameraConfig mainCameraConfig;
        public int additionalCameraCount;
        public List<CameraConfig> additionalCameraConfigs;
    }

    [Serializable]
    public class CameraConfig
    {
        public Vector3? position;
        public Vector3? rotation;
    }
}
