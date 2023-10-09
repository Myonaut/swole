using System;

using static Swole.Swole;
using static Swole.Script.SwoleScriptSemantics;

namespace Swole.Script
{

    [Serializable]
    public struct SourceScript : IContent
    {

        public override string ToString()
        {

            return $"{name}{(string.IsNullOrEmpty(author) ? "" : " created by" + author)}{(this.HasPackage() ? " - imported from '" + packageInfo.GetIdentityString() + "'" : "")}";

        }

        public string name;
        public string Name => name;
        public bool NameIsValid => ValidateScriptName(name);

        public string author;
        public string Author => author;

        public string creationDate;
        public string CreationDateString => creationDate;

        public string lastEditDate;
        public string LastEditDateString => lastEditDate;

        public string description;
        public string Description => description;

        public PackageManifest packageInfo;
        public PackageManifest PackageInfo => packageInfo;

        public string source;

        public SourceScript(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, string source, PackageManifest packageInfo = default)
        {

            this.name = name;
            this.author = author;
            this.creationDate = creationDate.ToString(IContent.dateFormat);
            this.lastEditDate = lastEditDate.ToString(IContent.dateFormat);
            this.description = description;
            this.source = source;
            this.packageInfo = packageInfo;

        }

        public SourceScript(string name, string author, string creationDate, string lastEditDate, string description, string source, PackageManifest packageInfo = default)
        {

            this.name = name;
            this.author = author;
            this.creationDate = creationDate;
            this.lastEditDate = lastEditDate;
            this.description = description;
            this.source = source;
            this.packageInfo = packageInfo;

        }

        /// <summary>
        /// Outputs a source string that is ready to be embedded in a source that references this script.
        /// </summary>
        public string GetSourceEmbed(string topAuthor = null, PackageManifest workingPackage = default, int indentation = 0, bool addInitialLineBreak = false, bool addAdditionalEndLineBreak = false)
        {

            if (string.IsNullOrEmpty(source)) return "";

            if (indentation < 0) indentation = 0;
            string indent = new string(' ', indentation);

            string embedSource = source.StandardizeNewLines();

            string[] splitSource = embedSource.Split(ssNewLine);
            embedSource = "";
            for (int a = 0; a < splitSource.Length; a++) embedSource = embedSource + indent + splitSource[a] + (a == splitSource.Length - 1 ? "" : ssNewLine); // Add indentation to each line

            bool isLocalPackage = this.HasPackage() && packageInfo.GetIdentityString() == workingPackage.GetIdentityString();

            string header = $"{indent}// {(isLocalPackage || !this.HasPackage() ? "" : packageInfo.GetIdentityString() + ".") + name} ";
            string auth = $"{indent}// ©{this.LastEditDate().Year} {(string.IsNullOrEmpty(author) ? "" : author)}";

            bool isSameAuthor = author == topAuthor;

            int boundLength = Math.Max(header.Length, auth.Length);

            return (addInitialLineBreak ? ssNewLine : "") +
                                $"{indent}//" + new string('+', boundLength) + ssNewLine +
                                header + ssNewLine +
                                (isSameAuthor || string.IsNullOrEmpty(author) ? "" : auth + ssNewLine) +
                                $"{indent}//" + new string('+', boundLength) + ssNewLine +
                                embedSource + ssNewLine +
                                $"{indent}//" + new string('+', boundLength) + (addAdditionalEndLineBreak ? ssNewLine : "") + ssNewLine;

        }

        public const string metaDataTag_URL = "PULLED_FROM:";
        public const string metaDataTag_Name = "NAME:";
        public const string metaDataTag_Author = "AUTHOR:";
        public const string metaDataTag_CreationDate = "CREATED:";
        public const string metaDataTag_LastEditDate = "LAST_EDIT:";
        public const string metaDataTag_Description = "INFO:";
        public const char descriptionMarker = '`';

        public const int defaultMaxDescCharsPerLine = 64;

        /// <summary>
        /// Returns an altered source; such that its origin can be identified without being wrapped in this struct.
        /// </summary>
        public string GetStandaloneSourceWithMetaData(int maxDescCharsPerLine = defaultMaxDescCharsPerLine)
        {

            string sourceEdit = source.StandardizeNewLines();

            string desc = description.StandardizeNewLines();
            desc = desc.Replace(descriptionMarker + "", "");

            string l0 = string.IsNullOrEmpty(packageInfo.url) ? "" : $"// {metaDataTag_URL} {packageInfo.url}" + ssNewLine;
            string l1 = string.IsNullOrEmpty(name) ? "" : $"// {metaDataTag_Name} {name}" + ssNewLine;
            string l2 = string.IsNullOrEmpty(author) ? "" : $"// {metaDataTag_Author} {author}" + ssNewLine;
            string l3 = string.IsNullOrEmpty(creationDate) ? "" : $"// {metaDataTag_CreationDate} {creationDate}" + ssNewLine;
            string l4 = string.IsNullOrEmpty(lastEditDate) ? "" : $"// {metaDataTag_LastEditDate} {lastEditDate}" + ssNewLine;

            int descLineCount = string.IsNullOrEmpty(desc) ? 0 : Math.Max(1, (int)Math.Ceiling(desc.Length / (float)maxDescCharsPerLine));
            string descStr = "";
            int i = 0;
            int j = 0;
            for (int a = 0; a < descLineCount; a++)
            {
                if (j >= desc.Length) break;
                descStr = descStr + $"// {(a == 0 ? metaDataTag_Description + $" {descriptionMarker}" : "")}";
                while (i < maxDescCharsPerLine || j < desc.Length && desc[j] != ' ') // Avoid splitting words onto two lines
                {
                    if (j >= desc.Length) break;
                    descStr = descStr + desc[j];

                    i++;
                    j++;

                }

                while (j < desc.Length && desc[j] == ' ') // Avoid leading a line with a space
                {
                    descStr = descStr + desc[j];
                    j++;
                }
                descStr = descStr + (a == descLineCount - 1 || j >= desc.Length ? $"{descriptionMarker}{ssNewLine}" : ssNewLine);
                i = 0;

            }

            int boundLength = Math.Max(Math.Min(maxDescCharsPerLine, desc.Length), l1.Length);
            boundLength = Math.Max(boundLength, l2.Length);
            boundLength = Math.Max(boundLength, l3.Length);
            boundLength = Math.Max(boundLength, l4.Length);

            sourceEdit =
                "// " + new string('~', boundLength) + ssNewLine
                + l0 + l1 + l2 + l3 + l4 + descStr
                + "// " + new string('~', boundLength) + ssNewLine
                + sourceEdit;

            return sourceEdit;

        }

        /// <summary>
        /// Takes a standalone source string with embedded meta data and constructs a script using said data.
        /// </summary>
        public SourceScript(string standaloneSource)
        {

            packageInfo = default;

            name = "null";
            author = description = "";
            creationDate = DateTime.Now.ToString(IContent.dateFormat);
            lastEditDate = null;

            standaloneSource = standaloneSource.StandardizeNewLines();

            string[] lines = standaloneSource.Split(ssNewLine);

            standaloneSource = "";
            bool isDesc = false;
            for (int l = 0; l < lines.Length; l++)
            {
                var line = lines[l];

                int tagIndex = line.IndexOf(msBeginComment);

                if (tagIndex < 0)
                {
                    standaloneSource = standaloneSource + line + (l < lines.Length - 1 ? ssNewLine : "");
                    continue;
                }

                if (tagIndex + 1 >= line.Length) continue;

                line = line.Substring(tagIndex + msBeginComment.Length);

                if (isDesc)
                {
                    line = line.TrimStart();
                    tagIndex = line.IndexOf(descriptionMarker);
                    if (tagIndex >= 0)
                    {
                        isDesc = false;
                        line = line.Substring(0, tagIndex);
                    }
                    description = description + line;
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_URL);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_URL.Length < line.Length) packageInfo.url = line.Substring(tagIndex + metaDataTag_URL.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_Name);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_Name.Length < line.Length) name = line.Substring(tagIndex + metaDataTag_Name.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_Author);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_Author.Length < line.Length) author = line.Substring(tagIndex + metaDataTag_Author.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_CreationDate);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_CreationDate.Length < line.Length) creationDate = line.Substring(tagIndex + metaDataTag_CreationDate.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_LastEditDate);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_LastEditDate.Length < line.Length) lastEditDate = line.Substring(tagIndex + metaDataTag_LastEditDate.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_Description);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_Description.Length < line.Length) line = line.Substring(tagIndex + metaDataTag_Description.Length).TrimStart(); else continue;
                    tagIndex = line.IndexOf(descriptionMarker);
                    if (tagIndex >= 0)
                    {
                        isDesc = true;
                        line = line.Substring(tagIndex + 1);
                    }
                    description = description + line;
                    continue;
                }

            }
            source = standaloneSource;

            if (string.IsNullOrEmpty(lastEditDate)) lastEditDate = creationDate;

        }

    }

}
