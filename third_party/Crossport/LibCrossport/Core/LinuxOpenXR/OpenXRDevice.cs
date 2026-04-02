#if UNITY_EDITOR || UNITY_LINUX
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR;
using CommonUsages = UnityEngine.InputSystem.CommonUsages;
using InputDevice = UnityEngine.InputSystem.InputDevice;

namespace Ices.Crossport.Core.LinuxOpenXR
{
    /// <summary>
    ///     OpenXR Input System device
    /// </summary>
    /// <seealso cref="UnityEngine.InputSystem.InputDevice" />
    [Preserve]
    [InputControlLayout(displayName = "OpenXR Action Map")]
    public abstract class OpenXRDevice : InputDevice
    {
        /// <summary>
        ///     See [InputControl.FinishSetup](xref:UnityEngine.InputSystem.InputControl.FinishSetup)
        /// </summary>
        protected override void FinishSetup()
        {
            base.FinishSetup();

            var capabilities = description.capabilities;
            var deviceDescriptor = XRDeviceDescriptor.FromJson(capabilities);

            if (deviceDescriptor != null)
            {
#if UNITY_2019_3_OR_NEWER
                if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.Left) != 0)
                    InputSystem.SetDeviceUsage(this, CommonUsages.LeftHand);
                else if ((deviceDescriptor.characteristics & InputDeviceCharacteristics.Right) != 0)
                    InputSystem.SetDeviceUsage(this, CommonUsages.RightHand);
#else
                if (deviceDescriptor.deviceRole == InputDeviceRole.LeftHanded)
                    InputSystem.SetDeviceUsage(this, CommonUsages.LeftHand);
                else if (deviceDescriptor.deviceRole == InputDeviceRole.RightHanded)
                    InputSystem.SetDeviceUsage(this, CommonUsages.RightHand);
#endif //UNITY_2019_3_OR_NEWER
            }
        }
    }
}

#endif