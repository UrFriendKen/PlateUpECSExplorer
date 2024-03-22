using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KitchenECSExplorer.Utils
{
    public static class StringUtils
    {
        public static int DamerauLevenshteinDistance(string firstText, string secondText)
        {
            var n = firstText.Length + 1;
            var m = secondText.Length + 1;
            var arrayD = new int[n, m];

            for (var i = 0; i < n; i++)
            {
                arrayD[i, 0] = i;
            }

            for (var j = 0; j < m; j++)
            {
                arrayD[0, j] = j;
            }

            for (var i = 1; i < n; i++)
            {
                for (var j = 1; j < m; j++)
                {
                    var cost = firstText[i - 1] == secondText[j - 1] ? 0 : 1;

                    arrayD[i, j] = Mathf.Min(
                        arrayD[i - 1, j] + 1, // delete
                        arrayD[i, j - 1] + 1, // insert
                        arrayD[i - 1, j - 1] + cost); // replacement

                    if (i > 1 && j > 1
                       && firstText[i - 1] == secondText[j - 2]
                       && firstText[i - 2] == secondText[j - 1])
                    {
                        arrayD[i, j] = Mathf.Min(arrayD[i, j],
                            arrayD[i - 2, j - 2] + cost); // permutation
                    }
                }
            }

            return arrayD[n - 1, m - 1];
        }

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

        [Flags]
        public enum FuzzyMatchStrategy
        {
            Standard = 0,
            ByWord = 1 << 0,
            IgnoreCase = 1 << 1,
            IgnoreLength = 1 << 2,
            ByWordIgnoreCase = ByWord | IgnoreCase,
            ByWordIgnoreLength = ByWord | IgnoreLength,
            IgnoreCaseAndLength = IgnoreCase | IgnoreLength,
            ByWordIgnoreCaseAndLength = ByWord | IgnoreCase | IgnoreLength

        }

        static HashSet<int> _isFuzzyMatchUsedWords = new HashSet<int>();
        public static bool IsFuzzyMatch(string s1, string s2, out int editDistance, FuzzyMatchStrategy matchStrategy = FuzzyMatchStrategy.Standard, int maxDistance = 2)
        {
            if (matchStrategy.HasFlag(FuzzyMatchStrategy.IgnoreCase))
            {
                s1 = s1.ToLowerInvariant();
                s2 = s2.ToLowerInvariant();
            }

            bool ignoreLength = matchStrategy.HasFlag(FuzzyMatchStrategy.IgnoreLength);

            int GetLength(string subS1, string subS2)
            {
                int editDistance = StringUtils.DamerauLevenshteinDistance(subS1, subS2);
                if (ignoreLength)
                    editDistance -= Mathf.Abs(subS1.Length - subS2.Length);
                return editDistance;
            }

            int distance = 0;
            if (matchStrategy.HasFlag(FuzzyMatchStrategy.ByWord))
            {
                _isFuzzyMatchUsedWords.Clear();
                string[] wordsS1 = s1.Split();
                string[] wordsS2 = s2.Split();

                string[] lessWords = wordsS1.Length < wordsS2.Length ? wordsS1 : wordsS2;
                string[] moreWords = wordsS1.Length >= wordsS2.Length ? wordsS1 : wordsS2;


                for (int i = 0; i < moreWords.Length && distance < maxDistance; i++)
                {
                    string word1 = moreWords[i];
                    if (_isFuzzyMatchUsedWords.Count >= lessWords.Length)
                    {
                        distance += word1.Length;
                        continue;
                    }
                    var bestMatch = lessWords.Select((word, i) => new { length = GetLength(word1, word), index = i })
                        .Where(item => !_isFuzzyMatchUsedWords.Contains(item.index)).OrderByDescending(item => item.length).FirstOrDefault();

                    _isFuzzyMatchUsedWords.Add(bestMatch.index);
                    distance += bestMatch.length;
                }
            }
            else
            {
                distance = GetLength(s1, s2);
            }

            editDistance = distance;
            return distance <= maxDistance;
        }


        public static bool IsNumber(this string s)
        {
            return long.TryParse(s, out _);
        }
    }
}
