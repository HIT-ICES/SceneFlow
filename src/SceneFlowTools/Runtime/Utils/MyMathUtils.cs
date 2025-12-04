using System.Collections.Generic;
using UnityEngine;

namespace SceneFlowTools.Runtime
{
    public static class MyMathUtils
    {
        public static readonly Vector3Int[] Directions6 =
        {
            new(1, 0, 0), // Right
            new(-1, 0, 0), // Left
            new(0, 1, 0), // Up
            new(0, -1, 0), // Down
            new(0, 0, 1), // Forward
            new(0, 0, -1) // Backward
        };

        public static int GetIndex(Vector3Int size, Vector3Int pos)
        {
            return pos.x + pos.y * size.x + pos.z * size.x * size.y;
        }

        public static int Cube(int v)
        {
            return v * v * v;
        }

        // "1 GiB" -> 1073741824
        public static long ParseSizeInBytes(string s)
        {
            s = s.Trim();
            string numberPart = s.Substring(0, s.LastIndexOfAny("0123456789".ToCharArray()) + 1).Trim();
            string unitPart = s.Substring(numberPart.Length).Trim();
            long number = long.Parse(numberPart);
            return unitPart switch
            {
                "B" => number,
                "KiB" => number * 1024L,
                "MiB" => number * 1024L * 1024L,
                "GiB" => number * 1024L * 1024L * 1024L,
                "TiB" => number * 1024L * 1024L * 1024L * 1024L,
                _ => throw new System.Exception("Unknown size unit: " + unitPart)
            };
        }
        
        public static List<int> GenerateRange(int start, int end)
        {
            List<int> result = new List<int>();
            for (int i = start; i < end; i++)
            {
                result.Add(i);
            }
            return result;
        }

        // public static int AlignFloor(int value, int alignment)
        // {
        //     return value / alignment * alignment;
        // }
        //
        // public static int AlignCeil(int value, int alignment)
        // {
        //     return (value + alignment - 1) / alignment * alignment;
        // }
        //
        // public static Vector3Int AlignFloor(Vector3Int value, int alignment)
        // {
        //     return new Vector3Int(
        //         AlignFloor(value.x, alignment),
        //         AlignFloor(value.y, alignment),
        //         AlignFloor(value.z, alignment)
        //     );
        // }
        //
        // public static Vector3Int AlignCeil(Vector3Int value, int alignment)
        // {
        //     return new Vector3Int(
        //         AlignCeil(value.x, alignment),
        //         AlignCeil(value.y, alignment),
        //         AlignCeil(value.z, alignment)
        //     );
        // }
    }
}