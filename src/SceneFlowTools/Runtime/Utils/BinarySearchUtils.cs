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
                    result = mid; // Record the current value that satisfies the condition.
                    low = mid + 1; // Search for a larger value.
                }
                else
                {
                    high = mid - 1; // Search for a smaller value.
                }
            }

            return result;
        }
    }
}
