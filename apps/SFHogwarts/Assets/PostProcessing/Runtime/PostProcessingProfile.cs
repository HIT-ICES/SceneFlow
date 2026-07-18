using System;

namespace UnityEngine.PostProcessing
{
    public class PostProcessingProfile : ScriptableObject
    {
#pragma warning disable 0169 // "field x is never used"

        public BuiltinDebugViewsModel debugViews = new();
        public AntialiasingModel antialiasing = new();
        public AmbientOcclusionModel ambientOcclusion = new();
        public ScreenSpaceReflectionModel screenSpaceReflection = new();
        public DepthOfFieldModel depthOfField = new();
        public MotionBlurModel motionBlur = new();
        public EyeAdaptationModel eyeAdaptation = new();
        public BloomModel bloom = new();
        public ColorGradingModel colorGrading = new();
        public UserLutModel userLut = new();
        public ChromaticAberrationModel chromaticAberration = new();
        public GrainModel grain = new();
        public VignetteModel vignette = new();

#if UNITY_EDITOR
        // Monitor settings
        [Serializable]
        public class MonitorSettings
        {
            // Histogram
            public enum HistogramMode
            {
                Red = 0,
                Green = 1,
                Blue = 2,
                Luminance = 3,
                RGBMerged,
                RGBSplit
            }

            // Global
            public int currentMonitorID = 0;

            public HistogramMode histogramMode = HistogramMode.Luminance;

            // Callback used in the editor to grab the rendered frame and sent it to monitors
            public Action<RenderTexture> onFrameEndEditorOnly;

            // Parade
            public float paradeExposure = 0.12f;
            public bool refreshOnPlay = false;

            // Vectorscope
            public float vectorscopeExposure = 0.12f;
            public bool vectorscopeShowBackground = true;
            public bool waveformB = true;

            // Waveform
            public float waveformExposure = 0.12f;
            public bool waveformG = true;
            public bool waveformR = true;
            public bool waveformY = false;
        }

        public MonitorSettings monitors = new();
#endif
    }
}