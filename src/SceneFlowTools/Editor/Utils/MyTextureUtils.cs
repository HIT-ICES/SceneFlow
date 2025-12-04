using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SceneFlowTools.Editor.Utils
{
    public class MyTextureUtils
    {
        public static long GetStorageMemorySizeLong(Texture tex)
        {
            long fileSize = 0;

            Type textureUtilType = typeof(TextureImporter).Assembly.GetType("UnityEditor.TextureUtil");
            MethodInfo getStorageMemorySizeLongMethod = textureUtilType.GetMethod("GetStorageMemorySizeLong",
                BindingFlags.Static | BindingFlags.Public);
            fileSize = (long)getStorageMemorySizeLongMethod.Invoke(null, new object[] { tex });

            return fileSize;
        }
    }
}