using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KitchenECSExplorer.Utils
{
    internal static class StringUtils
    {
        public static int LevenshteinDistance(string s1, string s2)
        {
            int length1 = s1.Length;
            int length2 = s2.Length;
            int[,] d = new int[length1 + 1, length2 + 1];

            // Verify arguments.
            if (length1 == 0)
            {
                return length2;
            }

            if (length2 == 0)
            {
                return length1;
            }

            // Initialize arrays.
            for (int i = 0; i <= length1; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= length2; d[0, j] = j++)
            {
            }

            // Begin looping.
            for (int i = 1; i <= length1; i++)
            {
                for (int j = 1; j <= length2; j++)
                {
                    // Compute cost.
                    int cost = (s2[j - 1] == s1[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
                }
            }
            // Return cost.
            return d[length1, length2];
        }


        public static bool IsNumber(this string s)
        {
            return long.TryParse(s, out _);
        }
    }
}
