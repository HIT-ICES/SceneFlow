using SceneFlowTools.Runtime;
using UnityEditor;

namespace SceneFlowTools.Editor.Menu
{
    public class MetaInfoMenu : UnityEditor.Editor
    {
        [MenuItem("Tools/Scene Division Tools/Generate Meta Info", false, 1)]
        private static void GenerateMetaInfo()
        {
            MetaInfo.GenerateToAllGameObjects();
        }
        
        [MenuItem("Tools/Scene Division Tools/Remove Meta Info", false, 1)]
        private static void RemoveMetaInfo()
        {
            MetaInfo.RemoveAll();
        }
    }
}