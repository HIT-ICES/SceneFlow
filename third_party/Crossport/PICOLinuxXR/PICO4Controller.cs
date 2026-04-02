#if UNITY_EDITOR || UNITY_LINUX


using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR.OpenXR.Input;
#if USE_INPUT_SYSTEM_POSE_CONTROL
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;

#else
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#endif

namespace Ices.Crossport.Core.LinuxOpenXR.PICO
{
    /// <summary>
    ///     An Input System device based on the hand interaction profile in the PICO Touch Controller</a>.
    /// </summary>
    [Preserve]
    [InputControlLayout
    (
        displayName = "PICO4 Touch Controller (OpenXR)",
        commonUsages = new[] { "LeftHand", "RightHand" }
    )]
    public class PICO4TouchController : XRControllerWithRumble
    {
        /// <summary>
        ///     A [Vector2Control](xref:UnityEngine.InputSystem.Controls.Vector2Control) that represents the
        ///     <see cref="PICO4TouchControllerProfile.thumbstick" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "Primary2DAxis", "Joystick" }, usage = "Primary2DAxis")]
        public Vector2Control thumbstick { get; private set; }

        /// <summary>
        ///     A [AxisControl](xref:UnityEngine.InputSystem.Controls.AxisControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.squeezeValue" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "GripAxis", "squeeze" }, usage = "Grip")]
        public AxisControl grip { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.squeezeClick" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "GripButton", "squeezeClicked" }, usage = "GripButton")]
        public ButtonControl gripPressed { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.menu" /> OpenXR bindings.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "Primary", "menuButton" }, usage = "Menu")]
        public ButtonControl menu { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.system" /> OpenXR bindings.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "systemButton" }, usage = "system")]
        public ButtonControl system { get; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.buttonA" /> <see cref="PICO4TouchControllerProfile.buttonX" /> OpenXR
        ///     bindings, depending on handedness.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "A", "X", "buttonA", "buttonX" }, usage = "PrimaryButton")]
        public ButtonControl primaryButton { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.buttonATouch" /> <see cref="PICO4TouchControllerProfile.buttonYTouch" />
        ///     OpenXR bindings, depending on handedness.
        /// </summary>
        [Preserve]
        [InputControl
        (
            aliases = new[] { "ATouched", "XTouched", "ATouch", "XTouch", "buttonATouched", "buttonXTouched" },
            usage = "PrimaryTouch"
        )]
        public ButtonControl primaryTouched { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.buttonB" /> <see cref="PICO4TouchControllerProfile.buttonY" /> OpenXR
        ///     bindings, depending on handedness.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "B", "Y", "buttonB", "buttonY" }, usage = "SecondaryButton")]
        public ButtonControl secondaryButton { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.buttonBTouch" /> <see cref="PICO4TouchControllerProfile.buttonYTouch" />
        ///     OpenXR bindings, depending on handedness.
        /// </summary>
        [Preserve]
        [InputControl
        (
            aliases = new[] { "BTouched", "YTouched", "BTouch", "YTouch", "buttonBTouched", "buttonYTouched" },
            usage = "SecondaryTouch"
        )]
        public ButtonControl secondaryTouched { get; private set; }

        /// <summary>
        ///     A [AxisControl](xref:UnityEngine.InputSystem.Controls.AxisControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.trigger" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(usage = "Trigger")]
        public AxisControl trigger { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.triggerClick" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "indexButton", "indexTouched", "triggerbutton" }, usage = "TriggerButton")]
        public ButtonControl triggerPressed { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.triggerTouch" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(aliases = new[] { "indexTouch", "indexNearTouched" }, usage = "TriggerTouch")]
        public ButtonControl triggerTouched { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.thumbstickClick" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl
        (
            aliases = new[] { "JoystickOrPadPressed", "thumbstickClick", "joystickClicked" },
            usage = "Primary2DAxisClick"
        )]
        public ButtonControl thumbstickClicked { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="PICO4TouchControllerProfile.thumbstickTouch" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl
        (
            aliases = new[] { "JoystickOrPadTouched", "thumbstickTouch", "joystickTouched" },
            usage = "Primary2DAxisTouch"
        )]
        public ButtonControl thumbstickTouched { get; private set; }

        /// <summary>
        ///     A <see cref="PoseControl" /> that represents the <see cref="PICO4TouchControllerProfile.grip" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(offset = 0, aliases = new[] { "device", "gripPose" }, usage = "Device")]
        public PoseControl devicePose { get; private set; }

        /// <summary>
        ///     A <see cref="PoseControl" /> that represents the <see cref="PICO4TouchControllerProfile.aim" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(offset = 0, alias = "aimPose", usage = "Pointer")]
        public PoseControl pointer { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) required for backwards compatibility with
        ///     the XRSDK layouts. This represents the overall tracking state of the device. This value is equivalent to mapping
        ///     devicePose/isTracked.
        /// </summary>
        [Preserve]
        [InputControl(offset = 28, usage = "IsTracked")]
        public new ButtonControl isTracked { get; private set; }

        /// <summary>
        ///     A [IntegerControl](xref:UnityEngine.InputSystem.Controls.IntegerControl) required for backwards compatibility with
        ///     the XRSDK layouts. This represents the bit flag set to indicate what data is valid. This value is equivalent to
        ///     mapping devicePose/trackingState.
        /// </summary>
        [Preserve]
        [InputControl(offset = 32, usage = "TrackingState")]
        public new IntegerControl trackingState { get; private set; }

        /// <summary>
        ///     A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for backwards compatibility with
        ///     the XRSDK layouts. This is the device position. For the PICO Touch device, this is both the grip and the pointer
        ///     position. This value is equivalent to mapping devicePose/position.
        /// </summary>
        [Preserve]
        [InputControl(offset = 36, noisy = true, alias = "gripPosition")]
        public new Vector3Control devicePosition { get; private set; }

        /// <summary>
        ///     A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility
        ///     with the XRSDK layouts. This is the device orientation. For the PICO Touch device, this is both the grip and the
        ///     pointer rotation. This value is equivalent to mapping devicePose/rotation.
        /// </summary>
        [Preserve]
        [InputControl(offset = 48, noisy = true, alias = "gripOrientation")]
        public new QuaternionControl deviceRotation { get; private set; }

        /// <summary>
        ///     A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for back compatibility with the
        ///     XRSDK layouts. This is the pointer position. This value is equivalent to mapping pointerPose/position.
        /// </summary>
        [Preserve]
        [InputControl(offset = 96)]
        public Vector3Control pointerPosition { get; private set; }

        /// <summary>
        ///     A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility
        ///     with the XRSDK layouts. This is the pointer rotation. This value is equivalent to mapping pointerPose/rotation.
        /// </summary>
        [Preserve]
        [InputControl(offset = 108, alias = "pointerOrientation")]
        public QuaternionControl pointerRotation { get; private set; }

        /// <summary>
        ///     A <see cref="HapticControl" /> that represents the <see cref="PICO4TouchControllerProfile.haptic" /> binding.
        /// </summary>
        [Preserve]
        [InputControl(usage = "Haptic")]
        public HapticControl haptic { get; private set; }

        /// <summary>
        ///     Internal call used to assign controls to the the correct element.
        /// </summary>
        protected override void FinishSetup()
        {
            base.FinishSetup();
            thumbstick = GetChildControl<Vector2Control>("thumbstick");
            trigger = GetChildControl<AxisControl>("trigger");
            triggerPressed = GetChildControl<ButtonControl>("triggerPressed");
            triggerTouched = GetChildControl<ButtonControl>("triggerTouched");
            grip = GetChildControl<AxisControl>("grip");
            gripPressed = GetChildControl<ButtonControl>("gripPressed");
            menu = GetChildControl<ButtonControl>("menu");
            primaryButton = GetChildControl<ButtonControl>("primaryButton");
            primaryTouched = GetChildControl<ButtonControl>("primaryTouched");
            secondaryButton = GetChildControl<ButtonControl>("secondaryButton");
            secondaryTouched = GetChildControl<ButtonControl>("secondaryTouched");
            thumbstickClicked = GetChildControl<ButtonControl>("thumbstickClicked");
            thumbstickTouched = GetChildControl<ButtonControl>("thumbstickTouched");

            devicePose = GetChildControl<PoseControl>("devicePose");
            pointer = GetChildControl<PoseControl>("pointer");

            isTracked = GetChildControl<ButtonControl>("isTracked");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            pointerPosition = GetChildControl<Vector3Control>("pointerPosition");
            pointerRotation = GetChildControl<QuaternionControl>("pointerRotation");

            haptic = GetChildControl<HapticControl>("haptic");
        }
    }
}
#endif