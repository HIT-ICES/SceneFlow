using SceneFlowTools.Runtime;
using UnityEditor;

namespace SceneFlowTools.Editor.Menu
{
    public class MetaInfoMenu : UnityEditor.Editor
    {
        [MenuItem("Tools/Scene Division Tools/Generate Meta Info", false, 1)]
        private static void GenerateMetaInfo()
        {
            int changed = MetaInfoManager.GenerateToAllGameObjects();
            UnityEngine.Debug.Log($"MetaInfoManager: generated or repaired {changed} MetaInfo entries.");
        }

        [MenuItem("Tools/Scene Division Tools/Validate Meta Info", false, 1)]
        private static void ValidateMetaInfo()
        {
            MetaInfoManager.RequestValidation("Menu Validate MetaInfo");
        }

        [MenuItem("Tools/Scene Division Tools/Regenerate All Meta Info IDs", false, 1)]
        private static void RegenerateAllMetaInfoIds()
        {
            int changed = MetaInfoManager.RegenerateAllIds();
            UnityEngine.Debug.Log($"MetaInfoManager: regenerated {changed} MetaInfo IDs.");
        }
        
        [MenuItem("Tools/Scene Division Tools/Remove Meta Info", false, 1)]
        private static void RemoveMetaInfo()
        {
            int changed = MetaInfoManager.RemoveAllMetaInfo();
            UnityEngine.Debug.Log($"MetaInfoManager: removed {changed} MetaInfo components.");
        }
    }
}
