using System;

namespace Utils
{
    public class BinarySearchUtils
    {
        public static int? FindMax(int low, int high, Func<int, bool> condition)
        {
            int? result = null;
            while (low <= high)
            {
                int mid = (int)(((long)low + high) / 2);
                if (condition(mid))
                {
                    result = mid; // 记录当前满足条件的值
                    low = mid + 1; // 尝试寻找更大的值
                }
                else
                {
                    high = mid - 1; // 寻找更小的值
                }
            }

            return result;
        }
    }
}