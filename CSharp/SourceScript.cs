using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using static Swolescript.SwoleScriptSemantics;

namespace Swolescript
{

    [Serializable]
    public struct SourceScript
    {

        public static bool ValidateScriptName(SourceScript script) => ValidateScriptName(script.name);
        public static bool ValidateScriptName(string name)
        {

            if (string.IsNullOrEmpty(name)) return false;

            return name.IsAlphaNumeric();

        }

        public bool NameIsValid => ValidateScriptName(name);

        public override string ToString()
        {

            return $"{name} created by {(string.IsNullOrEmpty(author) ? "?" : author)}";

        }

        public string name;

        public string author;

        public string creationDate;

        public string lastEditDate;

        public string description;

        public string source;

        public const string dateFormat = "MM/dd/yyyy";

        public SourceScript(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, string source)
        {

            this.name = name;
            this.author = author;
            this.creationDate = creationDate.ToString(dateFormat);
            this.lastEditDate = lastEditDate.ToString(dateFormat);
            this.description = description;
            this.source = source;

        }

        public SourceScript(string name, string author, string creationDate, string lastEditDate, string description, string source)
        {

            this.name = name;
            this.author = author;
            this.creationDate = creationDate;
            this.lastEditDate = lastEditDate;
            this.description = description;
            this.source = source;

        }

        public DateTime LastEditDate
        {

            get
            {

                if (!string.IsNullOrEmpty(lastEditDate) && DateTime.TryParse(lastEditDate, new CultureInfo("en-us"), DateTimeStyles.None, out DateTime date)) return date;

                return CreationDate;

            }

        }

        public DateTime CreationDate
        {

            get
            {

                DateTime date = DateTime.Now;

                if (!string.IsNullOrEmpty(creationDate) && DateTime.TryParse(creationDate, new CultureInfo("en-us"), DateTimeStyles.None, out DateTime result)) date = result;

                return date;

            }

        }

        /// <summary>
        /// Outputs a source string that is ready to be embedded in a source that references this script.
        /// </summary>
        public string GetSourceEmbed(string packageName, int indentation = 0, bool addInitialLineBreak = false, bool addAdditionalEndLineBreak = false)
        {

            if (string.IsNullOrEmpty(source)) return "";

            if (indentation < 0) indentation = 0;
            string indent = new string(' ', indentation);

            string embedSource = SwoleScriptSemantics.MimicNewLines(source);

            string[] splitSource = embedSource.Split(ssMimicNewLine);
            embedSource = "";
            for (int a = 0; a < splitSource.Length; a++) embedSource = embedSource + indent + splitSource[a] + (a == splitSource.Length - 1 ? "" : ssMimicNewLine); // Add indentation to each line

            string header = $"{indent}// {((string.IsNullOrEmpty(packageName) ? "" : (packageName + ".")) + name)} ";
            string auth = $"{indent}// ©{LastEditDate.Year} {(string.IsNullOrEmpty(author) ? "" : author)}";
             
            int boundLength = (int)Math.Max(header.Length, auth.Length);

            return (addInitialLineBreak ? ssMimicNewLine : "") +
                                $"{indent}//" + (new string('+', boundLength)) + ssMimicNewLine +
                                header + ssMimicNewLine +
                                (string.IsNullOrEmpty(author) ? "" : (auth + ssMimicNewLine)) +
                                $"{indent}//" + (new string('+', boundLength)) + ssMimicNewLine +
                                embedSource + ssMimicNewLine +
                                $"{indent}//" + (new string('+', boundLength)) + (addAdditionalEndLineBreak ? ssMimicNewLine : "") + ssMimicNewLine;

        }

        public const string metaDataTag_Name = "NAME:";
        public const string metaDataTag_Author = "AUTHOR:";
        public const string metaDataTag_CreationDate = "CREATED:";
        public const string metaDataTag_LastEditDate = "LASTEDIT:";
        public const string metaDataTag_Description = "INFO:";
        public const char descriptionMarker = '`';

        public const int defaultMaxDescCharsPerLine = 64;

        /// <summary>
        /// Returns an altered source; such that its origin can be identified without being wrapped in this struct.
        /// </summary>
        public string GetStandaloneSourceWithMetaData(int maxDescCharsPerLine = defaultMaxDescCharsPerLine)
        {

            string sourceEdit = SwoleScriptSemantics.MimicNewLines(source);

            string desc = SwoleScriptSemantics.MimicNewLines(description);
            desc = desc.Replace(descriptionMarker + "", "");

            string l1 = string.IsNullOrEmpty(name) ? "" : ($"// {metaDataTag_Name} {name}" + ssMimicNewLine);
            string l2 = string.IsNullOrEmpty(author) ? "" : ($"// {metaDataTag_Author} {author}" + ssMimicNewLine);
            string l3 = string.IsNullOrEmpty(creationDate) ? "" : ($"// {metaDataTag_CreationDate} {creationDate}" + ssMimicNewLine);
            string l4 = string.IsNullOrEmpty(lastEditDate) ? "" : ($"// {metaDataTag_LastEditDate} {lastEditDate}" + ssMimicNewLine);

            int descLineCount = string.IsNullOrEmpty(desc) ? 0 : Math.Max(1, (int)Math.Ceiling(desc.Length / (float)maxDescCharsPerLine));
            string descStr = "";
            int i = 0;
            int j = 0;
            for(int a = 0; a < descLineCount; a++)
            {
                if ((j >= desc.Length)) break;
                descStr = descStr + $"// {(a == 0 ? (metaDataTag_Description + $" {descriptionMarker}") : "")}";
                while (i < maxDescCharsPerLine || (j < desc.Length && desc[j] != ' ')) // Avoid splitting words onto two lines
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
                descStr = descStr + ((a == descLineCount - 1) || (j >= desc.Length) ? $"{descriptionMarker}{ssMimicNewLine}" : ssMimicNewLine);              
                i = 0;

            }

            int boundLength = Math.Max(Math.Min(maxDescCharsPerLine, desc.Length), l1.Length);
            boundLength = Math.Max(boundLength, l2.Length);
            boundLength = Math.Max(boundLength, l3.Length);
            boundLength = Math.Max(boundLength, l4.Length);

            sourceEdit =
                "// " + (new string('~', boundLength)) + ssMimicNewLine
                + l1 + l2 + l3 + l4 + descStr
                + "// " + (new string('~', boundLength)) + ssMimicNewLine
                + sourceEdit;

            return sourceEdit;

        }

        /// <summary>
        /// Takes a standalone source string with embedded meta data and constructs a script using said data.
        /// </summary>
        public SourceScript(string standaloneSource)
        {

            name = "null";
            author = description = "";
            creationDate = DateTime.Now.ToString(dateFormat);
            lastEditDate = null;

            standaloneSource = SwoleScriptSemantics.MimicNewLines(standaloneSource);

            string[] lines = standaloneSource.Split(ssMimicNewLine);

            standaloneSource = "";
            bool isDesc = false;
            for(int l = 0; l < lines.Length; l++)
            {
                var line = lines[l];

                int tagIndex = line.IndexOf(msBeginComment);

                if (tagIndex < 0)
                {
                    standaloneSource = standaloneSource + line + (l < lines.Length - 1 ? ssMimicNewLine : "");
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
