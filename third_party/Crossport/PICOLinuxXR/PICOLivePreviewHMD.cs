using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

namespace Ices.Crossport.Core.LinuxOpenXR.PICO
{
    /// <summary>
    ///     A PICO Headset
    /// </summary>
    [Preserve]
    [InputControlLayout(displayName = "PICO Live Preview HMD")]
    public class PICOLivePreviewHMD : XRHMD
    {
        [Preserve] [InputControl] public ButtonControl userPresence { get; private set; }

        [Preserve]
        [InputControl(aliases = new[] { "devicetrackingstate" })]
        public new IntegerControl trackingState { get; private set; }

        [Preserve]
        [InputControl(aliases = new[] { "deviceistracked" })]
        public new ButtonControl isTracked { get; private set; }

        [Preserve] [InputControl] public new Vector3Control devicePosition { get; private set; }

        [Preserve] [InputControl] public new QuaternionControl deviceRotation { get; private set; }

        [Preserve] [InputControl] public Vector3Control deviceVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control deviceAngularVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control deviceAcceleration { get; private set; }

        [Preserve] [InputControl] public Vector3Control deviceAngularAcceleration { get; private set; }

        [Preserve] [InputControl] public new Vector3Control leftEyePosition { get; private set; }

        [Preserve] [InputControl] public new QuaternionControl leftEyeRotation { get; private set; }

        [Preserve] [InputControl] public Vector3Control leftEyeVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control leftEyeAngularVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control leftEyeAcceleration { get; private set; }

        [Preserve] [InputControl] public Vector3Control leftEyeAngularAcceleration { get; private set; }

        [Preserve] [InputControl] public new Vector3Control rightEyePosition { get; private set; }

        [Preserve] [InputControl] public new QuaternionControl rightEyeRotation { get; private set; }

        [Preserve] [InputControl] public Vector3Control rightEyeVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control rightEyeAngularVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control rightEyeAcceleration { get; private set; }

        [Preserve] [InputControl] public Vector3Control rightEyeAngularAcceleration { get; private set; }

        [Preserve] [InputControl] public new Vector3Control centerEyePosition { get; private set; }

        [Preserve] [InputControl] public new QuaternionControl centerEyeRotation { get; private set; }

        [Preserve] [InputControl] public Vector3Control centerEyeVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control centerEyeAngularVelocity { get; private set; }

        [Preserve] [InputControl] public Vector3Control centerEyeAcceleration { get; private set; }

        [Preserve] [InputControl] public Vector3Control centerEyeAngularAcceleration { get; private set; }


        protected override void FinishSetup()
        {
            base.FinishSetup();

            userPresence = GetChildControl<ButtonControl>("userPresence");
            trackingState = GetChildControl<IntegerControl>("trackingState");
            isTracked = GetChildControl<ButtonControl>("isTracked");
            devicePosition = GetChildControl<Vector3Control>("devicePosition");
            deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");
            deviceVelocity = GetChildControl<Vector3Control>("deviceVelocity");
            deviceAngularVelocity = GetChildControl<Vector3Control>("deviceAngularVelocity");
            deviceAcceleration = GetChildControl<Vector3Control>("deviceAcceleration");
            deviceAngularAcceleration = GetChildControl<Vector3Control>("deviceAngularAcceleration");
            leftEyePosition = GetChildControl<Vector3Control>("leftEyePosition");
            leftEyeRotation = GetChildControl<QuaternionControl>("leftEyeRotation");
            leftEyeVelocity = GetChildControl<Vector3Control>("leftEyeVelocity");
            leftEyeAngularVelocity = GetChildControl<Vector3Control>("leftEyeAngularVelocity");
            leftEyeAcceleration = GetChildControl<Vector3Control>("leftEyeAcceleration");
            leftEyeAngularAcceleration = GetChildControl<Vector3Control>("leftEyeAngularAcceleration");
            rightEyePosition = GetChildControl<Vector3Control>("rightEyePosition");
            rightEyeRotation = GetChildControl<QuaternionControl>("rightEyeRotation");
            rightEyeVelocity = GetChildControl<Vector3Control>("rightEyeVelocity");
            rightEyeAngularVelocity = GetChildControl<Vector3Control>("rightEyeAngularVelocity");
            rightEyeAcceleration = GetChildControl<Vector3Control>("rightEyeAcceleration");
            rightEyeAngularAcceleration = GetChildControl<Vector3Control>("rightEyeAngularAcceleration");
            centerEyePosition = GetChildControl<Vector3Control>("centerEyePosition");
            centerEyeRotation = GetChildControl<QuaternionControl>("centerEyeRotation");
            centerEyeVelocity = GetChildControl<Vector3Control>("centerEyeVelocity");
            centerEyeAngularVelocity = GetChildControl<Vector3Control>("centerEyeAngularVelocity");
            centerEyeAcceleration = GetChildControl<Vector3Control>("centerEyeAcceleration");
            centerEyeAngularAcceleration = GetChildControl<Vector3Control>("centerEyeAngularAcceleration");
        }
    }
}