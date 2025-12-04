using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CrossportPlus.Utils
{
    public static class DebugUtils
    {
        public static string DebugString(IEnumerable<string> s)
        {
            StringBuilder result = new StringBuilder("[");
            foreach (string str in s)
            {
                result.Append(str).Append(", ");
            }

            if (result.Length > 1)
                result.Remove(result.Length - 2, 2);
            result.Append("]");
            return result.ToString();
        }

        public static void PrintEnabledKeywords(string name, ComputeShader x)
        {
            Debug.Log($"Enabled keywords for {name}: " + DebugString(x.enabledKeywords.Select(a => a.name)));
        }

        public static void PrintEnabledKeywords(string name, Material x)
        {
            Debug.Log($"Enabled keywords for {name}: " + DebugString(x.enabledKeywords.Select(a => a.name)));
        }
    }
}