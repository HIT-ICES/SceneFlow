using System;
using System.Collections.Generic;
using Ices.Crossport.Core.LinuxOpenXR;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.OpenXR.Input;

namespace Ices.Crossport.Core.LinuxOpenXR
{
    public abstract class LinuxOpenXRLayoutLoader : MonoBehaviour
    {
        protected void RegisterLayout<T>
        (
            string layoutName = null,
            string productName = null,
            string manufacturer = null
        )
        {
            RegisterLayout(typeof(T), layoutName, productName, manufacturer);
        }

        protected void RegisterLayout
        (
            Type layout,
            string layoutName = null,
            string productName = null,
            string manufacturer = null
        )
        {
            var anyVersionMatcher = new InputDeviceMatcher().WithInterface(XRUtilities.InterfaceMatchAnyVersion);
            if (!string.IsNullOrEmpty(productName)) anyVersionMatcher = anyVersionMatcher.WithProduct(productName);
            if (!string.IsNullOrEmpty(manufacturer))
                anyVersionMatcher = anyVersionMatcher.WithManufacturer(manufacturer);
            InputSystem.RegisterLayout(layout, layoutName, anyVersionMatcher);
        }

        protected abstract IEnumerable<(Type layout, string name, string productName, string manufacturer)>
            GetThirdPartyLayouts();

#if UNITY_EDITOR || UNITY_LINUX
        private void Awake()
        {
            RegisterLayout<HapticControl>("Haptic");
#if USE_INPUT_SYSTEM_POSE_CONTROL
#if UNITY_FORCE_INPUTSYSTEM_XR_OFF
            RegisterLayout<UnityEngine.InputSystem.XR.PoseControl>("Pose");
#endif //UNITY_FORCE_INPUTSYSTEM_XR_OFF
#else
            RegisterLayout<UnityEngine.InputSystem.XR.PoseControl>("Pose");
#endif //USE_INPUT_SYSTEM_POSE_CONTROL
            RegisterLayout<OpenXRDevice>();
            RegisterLayout(typeof(OpenXRHmd), productName: "Head Tracking - OpenXR", manufacturer: "OpenXR");
            // OpenXR
            RegisterLayout(typeof(DPad), productName: "DPad Interaction OpenXR");
            RegisterLayout(typeof(EyeGazeDevice), "EyeGaze", "Eye Tracking OpenXR");

            // Third Party
            foreach (var (layout, layoutName, productName, manufacturer) in GetThirdPartyLayouts())
            {
                RegisterLayout(layout, layoutName, productName, manufacturer);
                Debug.Log($"Third party layout `{layoutName ?? productName ?? $"By {manufacturer}"}` registered.");
            }

            Debug.Log("All Layouts registered.");
        }
#endif
    }
}