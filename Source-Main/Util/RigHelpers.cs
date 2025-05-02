using System;
using System.Collections.Generic;
using System.Linq;

namespace Swole
{

    public class RigHelpers
    {
        public static List<BoneMapping> MapBones(IEnumerable<string> rig1Bones, IEnumerable<string> rig2Bones)
        {
            var mappings = new List<BoneMapping>();

            foreach (var bone1 in rig1Bones)
            {
                var bone1Cleaned = CleanBoneName(bone1);

                string leftOrRight = BoneIsLeftOrRight(bone1);

                BoneMapping? bestMatch = null;
                foreach (var bone2 in rig2Bones)
                {
                    string leftOrRight2 = BoneIsLeftOrRight(bone2);

                    if (leftOrRight != leftOrRight2) continue;           

                    var bone2Cleaned = CleanBoneName(bone2);
                    var cost = LevenshteinDistance(bone1Cleaned, bone2Cleaned);
                    if (bestMatch == null || cost < bestMatch.Value.cost)
                    {
                        bestMatch = new BoneMapping { rig1Bone = bone1, rig2Bone = bone2, cost = cost, cleanName1 = bone1Cleaned, cleanName2 = bone2Cleaned };
                    }
                }

                if (bestMatch != null)
                {
                    mappings.Add(bestMatch.Value); 
                }
            }

            return mappings;
        }

        private static string CleanBoneName(string boneName)
        {
            boneName = boneName.ToLower().Trim();
            if (boneName.EndsWith(".l") || boneName.EndsWith("_l")) boneName = boneName.Substring(0, boneName.Length - 2) + "left";
            if (boneName.EndsWith(".r") || boneName.EndsWith("_r")) boneName = boneName.Substring(0, boneName.Length - 2) + "right";

            return boneName.ToLower()
                           .Replace("mixamorig:", "")
                           .Replace(".l.", "left")
                           .Replace(".r.", "right")
                           .Replace("_l_", "left")
                           .Replace("_r_", "right")
                           .Replace("left", "<")
                           .Replace("right", ">")
                           .Replace(".", "")
                           .Replace("_", "");
        }

        private static string BoneIsLeftOrRight(string boneName)
        {
            boneName = boneName.ToLower().Trim();

            if (boneName.Contains("left") || boneName.EndsWith(".l") || boneName.EndsWith("_l") || boneName.Contains(".l.") || boneName.Contains("_l_"))
            {
                return "left";
            }
            else if (boneName.Contains("right") || boneName.EndsWith(".r") || boneName.EndsWith("_r") || boneName.Contains(".r.") || boneName.Contains("_r_"))
            {
                return "right";
            }
            return "";
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;

            var costs = new int[b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
            {
                int lastValue = i;
                for (int j = 0; j <= b.Length; j++)
                {
                    if (i == 0)
                    {
                        costs[j] = j;
                    }
                    else if (j > 0)
                    {
                        int newValue = costs[j - 1];
                        if (a[i - 1] != b[j - 1])
                        {
                            newValue = Math.Min(Math.Min(newValue, lastValue), costs[j]) + 1;
                        }
                        costs[j - 1] = lastValue;
                        lastValue = newValue;
                    }
                }
                if (i > 0) costs[b.Length] = lastValue;
            }
            return costs[b.Length];
        }
    }

    [Serializable]
    public struct BoneMapping
    {
        public string rig1Bone;
        public string rig2Bone;
        public float cost;
        public string cleanName1;
        public string cleanName2;
    }

}