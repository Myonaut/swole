using System;

using static Swolescript.SwoleScript;

namespace Swolescript
{
    public static class SwoleScriptSemantics
    {

        public const string ssVersionPrefix = "@";
        public static string GetFullPackageString(string packageName, Version version) => GetFullPackageString(packageName, version == null ? "" : version.ToString());
        public static string GetFullPackageString(string packageName, string version)
        {

            if (!ValidatePackageName(packageName)) return "";

            if (!ValidateVersionString(version) || version == "0.0" || version == "0.0.0" || version == "0.0.0.0") return packageName;

            return $"{packageName}{ssVersionPrefix}{version}";
        }
        public static void SplitFullPackageString(string packageString, out string packageName, out string packageVersion)
        {

            packageName = packageString;
            packageVersion = "";

            int versionPrefix = packageString.IndexOf(ssVersionPrefix);
            if (versionPrefix >= 0)
            {

                if ((versionPrefix + ssVersionPrefix.Length + 1) < packageName.Length) packageVersion = packageName.Substring(versionPrefix + ssVersionPrefix.Length + 1);
                packageName = packageString.Substring(versionPrefix);

            }

        }

        public const int ssDefaultAutoIndentation = 2;
        public const string ssMimicNewLine = "\n";

        public static string MimicNewLines(this string sourceString)
        {
            sourceString = string.IsNullOrEmpty(sourceString) ? "" : sourceString;
            sourceString = sourceString.Replace("\n", ssMimicNewLine);
            sourceString = sourceString.Replace("\r\n", ssMimicNewLine);
            return sourceString;
        }

        public const string ssMsgPrefix_Info = ">>";
        public const string ssMsgPrefix_Warning = "!!";
        public const string ssMsgPrefix_Error = "!!!";

        public const string ssKeyword_Import = "import";
        public const string ssKeyword_Insert = "insert";

        public const string msKeyword_If = "if";
        public const string msKeyword_Then = "then";
        public const string msKeyword_While = "while";
        public const string msKeyword_For = "for";

        public const string msKeyword_Else = "else";
        public const string msKeyword_ElseIf = "else if";

        public const string msKeyword_EndIf = "end if";
        public const string msKeyword_EndFor = "end for";
        public const string msKeyword_EndWhile = "end while";

        public const string msBeginComment = "//";

        /// <summary>
        /// A section of source code string.
        /// </summary>
        [Serializable]
        public struct CodeSection
        {
            public int startIndex;
            public int endIndex;

            public CodeSection(int startIndex, int endIndex)
            {
                this.startIndex = startIndex;
                this.endIndex = endIndex;
            }
        }

        /// <summary>
        /// A section of a source code string that should be replaced with source code from another script.
        /// </summary>
        [Serializable]
        public struct ScriptEmbed
        {
            public CodeSection section;
            public string packageName;
            public string scriptName;

            public ScriptEmbed(CodeSection section, string packageName, string scriptName)
            {
                this.section = section;
                this.packageName = packageName;
                this.scriptName = scriptName;
            }
        }

    }

}
