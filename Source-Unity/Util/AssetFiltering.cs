#if (UNITY_STANDALONE || UNITY_EDITOR)

namespace Swole
{
    public static class AssetFiltering
    {

        public static bool ContainsStartString(string target, string startString)
        {
            return target.StartsWith(startString) || target.Contains($" {startString}") || target.Contains($"_{startString}") || target.Contains($".{startString}");
        }
        public static bool ContainsCapitalizedWord(string target, string word)
        {
            if (string.IsNullOrWhiteSpace(word) || word.Length <= 1 || !char.IsLetter(word[0])) return false;
            word = word.Substring(0, 1).ToUpper() + word.Substring(1);

            int ind = target.IndexOf(word);
            if (ind > 0) return  !char.IsLetter(target[ind - 1]) || char.IsLower(target[ind - 1]); 

            return ind >= 0;
        }

    }
}

#endif