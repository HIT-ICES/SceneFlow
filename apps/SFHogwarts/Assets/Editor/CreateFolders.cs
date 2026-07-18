using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateFolders : Editor
{
    [MenuItem("Spell Maker/Create Folders")]
    private static void Init()
    {
        if (!Directory.Exists(Application.dataPath + "/Resources/Spells"))
            Directory.CreateDirectory(Application.dataPath + "/Resources/Spells");
    }
}