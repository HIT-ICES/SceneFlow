#nullable enable
using System;

namespace Ices.Crossport.Settings
{
    [Serializable]
    public class AppConfig
    {
        public int resolutionX;
        public int resolutionY;
        public int frameRate;
        public int qualityLevel;
        public string? profile;
    }
}