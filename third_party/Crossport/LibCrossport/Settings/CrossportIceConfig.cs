using System;

namespace Ices.Crossport.Settings
{
    [Serializable]
    public class CrossportIceConfig
    {
        public string username;
        public string credential;
        public string[] urls;
    }
}