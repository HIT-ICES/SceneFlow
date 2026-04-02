#if UNITY_EDITOR || UNITY_LINUX

#if UNITY_EDITOR
#endif
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
#if USE_INPUT_SYSTEM_POSE_CONTROL

#else
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#endif

namespace Ices.Crossport.Core.LinuxOpenXR
{
    /// <summary>
    ///     A  dpad-like interaction feature that allows the application to bind one or more digital actions to a trackpad or
    ///     thumbstick as though it were a dpad.
    ///     <a href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XrInteractionProfileDpadBindingEXT"></a>
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "D-Pad Binding (OpenXR)", commonUsages = new[] { "LeftHand", "RightHand" })]
    public class DPad : XRController
    {
        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.thumbstickDpadUp" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl thumbstickDpadUp { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.thumbstickDpadDown" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl thumbstickDpadDown { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.thumbstickDpadLeft" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl thumbstickDpadLeft { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.thumbstickDpadRight" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl thumbstickDpadRight { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.trackpadDpadUp" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl trackpadDpadUp { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.trackpadDpadDown" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl trackpadDpadDown { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteractionP.trackpadDpadLeft" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl trackpadDpadLeft { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.trackpadDpadRight" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl trackpadDpadRight { get; private set; }

        /// <summary>
        ///     A [ButtonControl](xref:UnityEngine.InputSystem.Controls.ButtonControl) that represents the
        ///     <see cref="DPadInteraction.trackpadDpadCenter" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl]
        public ButtonControl trackpadDpadCenter { get; private set; }

        /// <summary>
        ///     Internal call used to assign controls to the the correct element.
        /// </summary>
        protected override void FinishSetup()
        {
            base.FinishSetup();
            thumbstickDpadUp = GetChildControl<ButtonControl>("thumbstickDpadUp");
            thumbstickDpadDown = GetChildControl<ButtonControl>("thumbstickDpadDown");
            thumbstickDpadLeft = GetChildControl<ButtonControl>("thumbstickDpadLeft");
            thumbstickDpadRight = GetChildControl<ButtonControl>("thumbstickDpadRight");
            trackpadDpadUp = GetChildControl<ButtonControl>("trackpadDpadUp");
            trackpadDpadDown = GetChildControl<ButtonControl>("trackpadDpadDown");
            trackpadDpadLeft = GetChildControl<ButtonControl>("trackpadDpadLeft");
            trackpadDpadRight = GetChildControl<ButtonControl>("trackpadDpadRight");
            trackpadDpadCenter = GetChildControl<ButtonControl>("trackpadDpadCenter");
        }
    }
}
#endif