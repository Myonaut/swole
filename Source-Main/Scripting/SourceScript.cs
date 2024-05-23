using System;
using System.Collections.Generic;

using static Swole.swole;
using static Swole.Script.SwoleScriptSemantics;

namespace Swole.Script
{

    [Serializable]
    public struct SourceScript : IContent, ISwoleSerialization<SourceScript, SourceScript.Serialized>
    {

        #region Serialization

        public string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<SourceScript, SourceScript.Serialized>
        {

            public ContentInfo contentInfo; 
            public string source;

            public SourceScript AsOriginalType(PackageInfo packageInfo = default) => new SourceScript(this, packageInfo);
            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public SourceScript.Serialized AsSerializableStruct() => new SourceScript.Serialized() { contentInfo = contentInfo, source = source };
        public object AsSerializableObject() => AsSerializableStruct();

        public SourceScript(SourceScript.Serialized serializable, PackageInfo packageInfo = default)
        {
            originPath = relativePath = string.Empty;
            this.packageInfo = packageInfo;

            this.contentInfo = serializable.contentInfo;
            this.source = serializable.source;

        }

        #endregion

        public string originPath;
        public string OriginPath => originPath;
        public IContent SetOriginPath(string path)
        {
            var content = this;
            content.originPath = path;
            return content;
        }
        public string relativePath;
        public string RelativePath => relativePath;
        public IContent SetRelativePath(string path)
        {
            var content = this;
            content.relativePath = path;
            return content;
        }

        public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
        {

            if (dependencies == null) dependencies = new List<PackageIdentifier>();

            #if SWOLE_ENV
            if (!string.IsNullOrEmpty(source)) dependencies = swole.ExtractPackageDependencies(source, dependencies);
            #endif

            return dependencies;

        }

        public override string ToString()
        {
             
            return $"{Name}{(string.IsNullOrEmpty(Author) ? "" : " created by" + Author)}{(this.HasPackage() ? " - imported from '" + packageInfo.GetIdentityString() + "'" : "")}";

        }

        public string Name => contentInfo.name;
        public bool NameIsValid => ValidateScriptName(Name);

        public string Author => contentInfo.author;
        public string CreationDate => contentInfo.creationDate;
        public string LastEditDate => contentInfo.lastEditDate;
        public string Description => contentInfo.description;

        public PackageInfo packageInfo;
        public PackageInfo PackageInfo => packageInfo;

        public ContentInfo contentInfo;
        public ContentInfo ContentInfo => contentInfo;

        public string source;

        public SourceScript(string name, string author, DateTime creationDate, DateTime lastEditDate, string description, string source, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate.ToString(IContent.dateFormat), lastEditDate = lastEditDate.ToString(IContent.dateFormat), description = description }, source, packageInfo) {}

        public SourceScript(string name, string author, string creationDate, string lastEditDate, string description, string source, PackageInfo packageInfo = default) : this(new ContentInfo() { name = name, author = author, creationDate = creationDate, lastEditDate = lastEditDate, description = description }, source, packageInfo) { }

        public SourceScript(ContentInfo contentInfo, string source, PackageInfo packageInfo = default)
        {
            originPath = relativePath = string.Empty;
            this.contentInfo = contentInfo;
            this.source = source;
            this.packageInfo = packageInfo;

        }

        /// <summary>
        /// Outputs a parsed source string that is ready to be embedded in a source that references this script.
        /// </summary>
        public string GetSourceEmbed(ref List<PackageIdentifier> dependencyList, string topAuthor = null, PackageManifest workingPackage = default, int autoIndentation = 2, int indentation = 0, ICollection<SourceScript> localScripts = null, bool addInitialLineBreak = false, bool addAdditionalEndLineBreak = false)
        {

            if (string.IsNullOrEmpty(source)) return "";

            if (dependencyList == null) dependencyList = new List<PackageIdentifier>();

            if (indentation < 0) indentation = 0;
            string indent = new string(' ', indentation);

            bool isLocalPackage = this.HasPackage() && packageInfo.GetIdentityString() == workingPackage.GetIdentityString();
            if (!isLocalPackage && this.HasPackage())
            {
                if (swole.FindSourcePackage(packageInfo.name, packageInfo.version, out var pkg, out _) == PackageActionResult.Success)
                {
                    workingPackage = pkg.Manifest;
                    if (localScripts == null) localScripts = new List<SourceScript>();
                    localScripts.Clear();

                    if (localScripts is List<SourceScript> list)
                    {
                        for (int a = 0; a < pkg.ScriptCount; a++) list.Add(pkg.GetScript(a));
                    }
                } 
                else
                {
                    localScripts?.Clear();
                }
            }

            string embedSource = source.StandardizeNewLines();

#if SWOLE_ENV
            swole.ParseSource(embedSource, ref dependencyList, topAuthor, workingPackage, autoIndentation, indentation, localScripts);
#endif

            string[] splitSource = embedSource.Split(ssNewLine);
            embedSource = "";
            for (int a = 0; a < splitSource.Length; a++) embedSource = embedSource + indent + splitSource[a] + (a == splitSource.Length - 1 ? "" : ssNewLine); // Add indentation to each line


            string header = $"{indent}// {(isLocalPackage || !this.HasPackage() ? "" : packageInfo.GetIdentityString() + ".") + Name} ";
            string auth = $"{indent}// ©{this.LastEditDate().Year} {(string.IsNullOrEmpty(Author) ? "" : Author)}";

            bool isSameAuthor = Author == topAuthor;

            int boundLength = Math.Max(header.Length, auth.Length);

            return (addInitialLineBreak ? ssNewLine : "") +
                                $"{indent}//" + new string('+', boundLength) + ssNewLine +
                                header + ssNewLine +
                                (isSameAuthor || string.IsNullOrEmpty(Author) ? "" : (auth + ssNewLine)) +
                                $"{indent}//" + new string('+', boundLength) + ssNewLine +
                                embedSource + ssNewLine +
                                $"{indent}//" + new string('+', boundLength) + (addAdditionalEndLineBreak ? ssNewLine : "") + ssNewLine;

        }

        public const string metaDataTag_URL = "URL:";
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

            string desc = Description.StandardizeNewLines();
            desc = desc.Replace(descriptionMarker + "", "");

            string l0 = string.IsNullOrEmpty(packageInfo.url) ? "" : $"// {metaDataTag_URL} {packageInfo.url}" + ssNewLine;
            string l1 = string.IsNullOrEmpty(Name) ? "" : $"// {metaDataTag_Name} {Name}" + ssNewLine;
            string l2 = string.IsNullOrEmpty(Author) ? "" : $"// {metaDataTag_Author} {Author}" + ssNewLine;
            string l3 = string.IsNullOrEmpty(CreationDate) ? "" : $"// {metaDataTag_CreationDate} {CreationDate}" + ssNewLine;
            string l4 = string.IsNullOrEmpty(LastEditDate) ? "" : $"// {metaDataTag_LastEditDate} {LastEditDate}" + ssNewLine;

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
            originPath = relativePath = string.Empty;
            packageInfo = default;

            contentInfo = new ContentInfo();

            contentInfo.name = "null";
            contentInfo.author = contentInfo.description = "";
            contentInfo.creationDate = DateTime.Now.ToString(IContent.dateFormat);
            contentInfo.lastEditDate = null;

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
                    contentInfo.description = contentInfo.description + line;
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
                    if (tagIndex + metaDataTag_Name.Length < line.Length) contentInfo.name = line.Substring(tagIndex + metaDataTag_Name.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_Author);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_Author.Length < line.Length) contentInfo.author = line.Substring(tagIndex + metaDataTag_Author.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_CreationDate);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_CreationDate.Length < line.Length) contentInfo.creationDate = line.Substring(tagIndex + metaDataTag_CreationDate.Length).Trim();
                    continue;
                }

                tagIndex = line.IndexOf(metaDataTag_LastEditDate);
                if (tagIndex >= 0)
                {
                    if (tagIndex + metaDataTag_LastEditDate.Length < line.Length) contentInfo.lastEditDate = line.Substring(tagIndex + metaDataTag_LastEditDate.Length).Trim();
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
                    contentInfo.description = contentInfo.description + line;
                    continue;
                }

            }
            source = standaloneSource;

            if (string.IsNullOrEmpty(contentInfo.lastEditDate)) contentInfo.lastEditDate = contentInfo.creationDate;
        }

        public IContent CreateCopyAndReplaceContentInfo(ContentInfo info) => new SourceScript(info, source, packageInfo);

    }

}
