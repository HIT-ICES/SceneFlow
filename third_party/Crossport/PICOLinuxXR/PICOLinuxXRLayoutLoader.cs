using System;
using System.Collections.Generic;
using Ices.Crossport.Core.LinuxOpenXR;

namespace Ices.Crossport.Core.LinuxOpenXR.PICO
{
    public class PICOLinuxXRLayoutLoader : LinuxOpenXRLayoutLoader
    {
        protected override IEnumerable<(Type layout, string name, string productName, string manufacturer)>
            GetThirdPartyLayouts()
        {
#if UNITY_EDITOR || UNITY_LINUX
            yield return (typeof(PICONeo3Controller), null, "PICO Neo3 Touch Controller OpenXR", null);
            yield return (typeof(PICO4TouchController), null, "PICO4 Touch Controller OpenXR", null);
            yield return (typeof(PICOLivePreviewHMD), null, "^(PICO Live Preview HMD)", null);
            yield return (typeof(PICOLivePreviewController), null, "^(PICO Live Preview Controller)", null);
#endif
            yield break;
        }
    }
}