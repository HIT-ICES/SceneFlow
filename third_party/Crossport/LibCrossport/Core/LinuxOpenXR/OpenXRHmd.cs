#if UNITY_EDITOR || UNITY_LINUX
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;

namespace Ices.Crossport.Core.LinuxOpenXR
{
    [Preserve]
    [InputControlLayout(displayName = "OpenXR HMD")]
    internal class OpenXRHmd : XRHMD
    {
        [Preserve] [InputControl] private ButtonControl userPresence { get; set; }

        /// <inheritdoc />
        protected override void FinishSetup()
        {
            base.FinishSetup();
            userPresence = GetChildControl<ButtonControl>("UserPresence");
        }
    }
}
#endif