using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    [PostProcessingModelEditor(typeof(EyeAdaptationModel))]
    public class EyeAdaptationModelEditor : PostProcessingModelEditor
    {
        private SerializedProperty m_AdaptationType;
        private SerializedProperty m_ExposureCompensation;
        private SerializedProperty m_HighPercent;
        private SerializedProperty m_LogMax;
        private SerializedProperty m_LogMin;
        private SerializedProperty m_LowPercent;
        private SerializedProperty m_MaxLuminance;
        private SerializedProperty m_MinLuminance;
        private SerializedProperty m_SpeedDown;
        private SerializedProperty m_SpeedUp;

        public override void OnEnable()
        {
            m_LowPercent = FindSetting((EyeAdaptationModel.Settings x) => x.lowPercent);
            m_HighPercent = FindSetting((EyeAdaptationModel.Settings x) => x.highPercent);
            m_MinLuminance = FindSetting((EyeAdaptationModel.Settings x) => x.minLuminance);
            m_MaxLuminance = FindSetting((EyeAdaptationModel.Settings x) => x.maxLuminance);
            m_ExposureCompensation = FindSetting((EyeAdaptationModel.Settings x) => x.exposureCompensation);
            m_AdaptationType = FindSetting((EyeAdaptationModel.Settings x) => x.adaptationType);
            m_SpeedUp = FindSetting((EyeAdaptationModel.Settings x) => x.speedUp);
            m_SpeedDown = FindSetting((EyeAdaptationModel.Settings x) => x.speedDown);
            m_LogMin = FindSetting((EyeAdaptationModel.Settings x) => x.logMin);
            m_LogMax = FindSetting((EyeAdaptationModel.Settings x) => x.logMax);
        }

        public override void OnInspectorGUI()
        {
            if (!GraphicsUtils.supportsDX11)
            {
                EditorGUILayout.HelpBox(
                    "This effect requires support for compute shaders. Enabling it won't do anything on unsupported platforms.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(m_LogMin, EditorGUIHelper.GetContent("Histogram Log Min"));
            EditorGUILayout.PropertyField(m_LogMax, EditorGUIHelper.GetContent("Histogram Log Max"));
            EditorGUILayout.Space();

            var low = m_LowPercent.floatValue;
            var high = m_HighPercent.floatValue;

            EditorGUILayout.MinMaxSlider(
                EditorGUIHelper.GetContent(
                    "Filter|These values are the lower and upper percentages of the histogram that will be used to find a stable average luminance. Values outside of this range will be discarded and won't contribute to the average luminance."),
                ref low, ref high, 1f, 99f);

            m_LowPercent.floatValue = low;
            m_HighPercent.floatValue = high;

            EditorGUILayout.PropertyField(m_MinLuminance);
            EditorGUILayout.PropertyField(m_MaxLuminance);
            EditorGUILayout.PropertyField(m_ExposureCompensation);

            EditorGUILayout.PropertyField(m_AdaptationType);

            if (m_AdaptationType.intValue == (int)EyeAdaptationModel.EyeAdaptationType.Progressive)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SpeedUp);
                EditorGUILayout.PropertyField(m_SpeedDown);
                EditorGUI.indentLevel--;
            }
        }
    }
}