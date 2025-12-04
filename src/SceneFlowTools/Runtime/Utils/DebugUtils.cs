using System.Collections.Generic;
using System.Text;

namespace SceneFlowTools.Runtime
{
    public static class DebugUtils
    {
        public static string Format<T1, T2>(Dictionary<T1, T2> v)
        {
            if (v == null)
                return "null";
            var result = new StringBuilder();
            result.Append("{");
            foreach (var pair in v)
            {
                result.AppendLine($"{pair.Key}: {pair.Value}, ");
            }
            if (result.Length > 1)
            {
                result.Length -= 2; // 去掉最后的逗号和空格
            }
            result.Append("}");
            return result.ToString();
        }
    }
}