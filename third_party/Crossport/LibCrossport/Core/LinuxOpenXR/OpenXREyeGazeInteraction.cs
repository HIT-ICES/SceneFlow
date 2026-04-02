#if UNITY_EDITOR || UNITY_LINUX

#if UNITY_EDITOR
#endif
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;
#if USE_INPUT_SYSTEM_POSE_CONTROL
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;

#else
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#endif

namespace Ices.Crossport.Core.LinuxOpenXR
{
    /// <summary>
    ///     An Input System device based off the
    ///     <see href="https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#_eye_gaze_input">
    ///         Eye Gaze Interaction
    ///         Profile
    ///     </see>
    ///     . Enabled through <see cref="EyeGazeInteraction" />.
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "Eye Gaze (OpenXR)", isGenericTypeOfDevice = true)]
    public class EyeGazeDevice : OpenXRDevice
    {
        /// <summary>
        ///     A <see cref="PoseControl" /> representing the <see cref="EyeGazeInteraction.pose" /> OpenXR binding.
        /// </summary>
        [Preserve]
        [InputControl(offset = 0, usages = new[] { "Device", "gaze" })]
        public PoseControl pose { get; private set; }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            base.FinishSetup();
            pose = GetChildControl<PoseControl>("pose");
        }
    }
}
#endif