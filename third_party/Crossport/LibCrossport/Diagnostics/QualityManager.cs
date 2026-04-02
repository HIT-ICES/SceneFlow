using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ices.Crossport.Diagnostics
{
    public class QualityManager
    {
        public static void SetQualityLevel(int level) => QualitySettings.SetQualityLevel(level, true);
        public static void SetTargetFrameRate(int frameRate)
        {
            Application.targetFrameRate = frameRate;
        }

        public static void SetResolution(int x, int y)
        {
            Screen.SetResolution(x, y, false);
        }
    }
}