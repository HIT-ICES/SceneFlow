using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.RenderStreaming;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;
#if URS_USE_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
#endif

namespace Ices.Crossport.Receiver
{
    internal enum SignalingType { WebSocket, Http, Furioos }


    public partial class ControlPanel
    {
        //[SerializeField] private Toggle toggleUseDefaultSettings;
        [SerializeField] private InputField inputFieldSignalingAddress;
        [SerializeField] private InputField inputFieldCrossportApp;
        [SerializeField] private InputField inputFieldCrossportComponent;
        [SerializeField] private Dropdown presetServerSelector;
        [SerializeField] private Dropdown appComponentSelector;
        [SerializeField] private Button refreshButton; // Re-fetch App/Components From Remote
        private List<string> _availableClients;

        private static string CodecTitle(AudioCodecInfo codec)
        {
            return $"{codec.mimeType} {codec.channelCount}: {codec.name}";
        }

        private static void RegistryText(InputField input, string defaultValue, UnityAction<string> callback)
        {
            input.text = defaultValue;
            input.onValueChanged.AddListener(callback);
        }


        private void RegisterEvents(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            action.performed += handler;
            action.canceled += handler;
            action.started += handler;
        }

        private void Start()
        {
            RegistryText(inputFieldSignalingAddress, crossportSetting.address, v => crossportSetting.address = v);
            RegistryText
                (inputFieldCrossportApp, crossportSetting.application, v => crossportSetting.application = v);
            RegistryText
                (inputFieldCrossportComponent, crossportSetting.component, v => crossportSetting.component = v);

            SetInteractableSignalingUI(true);

            presetServerSelector.options.AddRange
            (
                CrossportClientUtils.PresetServers.Select
                (
                    s => new Dropdown.OptionData(s)
                )
            );
            ReloadAppComponents();

            presetServerSelector.onValueChanged.AddListener(OnChangePresetServer);
            appComponentSelector.onValueChanged.AddListener(OnChangeAppComponent);
            refreshButton.onClick.AddListener(ReloadAppComponents);
            LoadData();
        }

        private void ReloadAppComponents()
        {
            ConsoleManager.LogWithDebug("Reload App Components...");
            StartCoroutine(CrossportClientUtils.RefreshAppList(crossportSetting, ReloadAppComponents));
        }

        private void ReloadAppComponents(CrossportClientInfo[] clients)
        {
            _availableClients = clients.Select
                                        (
                                            c => $"{c.application}/{c.component}"
                                        )
                                       .ToList();
            appComponentSelector.options.Clear();
            appComponentSelector.options.AddRange
            (
                _availableClients.Select(c => new Dropdown.OptionData(c))
            );
            OnChangeAppComponent(0);
        }

        private void LoadData()
        {
            var server = crossportSetting.port is null or 80
                             ? crossportSetting.address
                             : $"{crossportSetting.address}:{crossportSetting.port}";
            presetServerSelector.value = Array.FindIndex
                                         (
                                             CrossportClientUtils.PresetServers,
                                             s => s == server
                                         )
                                       + 1;
            var appComponentNewValue = (_availableClients?.IndexOf
                                        (
                                            $"{crossportSetting.application}/{crossportSetting.component}"
                                        )
                                     ?? -1);
            if (appComponentSelector.value != appComponentNewValue && appComponentNewValue != -1)
                appComponentSelector.value = appComponentNewValue;
            inputFieldCrossportApp.text = crossportSetting.application;
            inputFieldCrossportComponent.text = crossportSetting.component;
            inputFieldSignalingAddress.text =  crossportSetting.address;
            //inputFieldConfigFilename.text = SampleManager.Instance.ActivatedConfigName;
        }

#if URS_USE_AR_FOUNDATION
        IEnumerator CheckARAvailability(Action<bool> callback)
        {
            if ((ARSession.state == ARSessionState.None) ||
                (ARSession.state == ARSessionState.CheckingAvailability))
            {
                yield return ARSession.CheckAvailability();
            }

            callback?.Invoke(ARSession.state == ARSessionState.Ready);
        }
#endif

        private void OnChangeSignalingAddress(string value) { crossportSetting.address = value; }


        private void SetInteractableSignalingUI(bool interactable)
        {
            inputFieldSignalingAddress.interactable = interactable;
        }


        private void OnChangePresetServer(int index)
        {
            if (index == 0) return;
            var pair = CrossportClientUtils.PresetServers[index - 1].Split(":");
            crossportSetting.address = pair[0];
            crossportSetting.port = pair.Length == 1 ? null : int.Parse(pair[1]);
            ReloadAppComponents();
        }

        private void OnChangeAppComponent(int index)
        {
            if (_availableClients.Count == 0) return;
            var target = _availableClients[index].Split("/", 2);
            crossportSetting.application = target[0];
            crossportSetting.component = target[1];
            LoadData();
        }
    }
}