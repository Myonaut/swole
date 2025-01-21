using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Swole
{

    public static class SwoleUtil
    {

        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameBiceps = "Biceps";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameTriceps = "Triceps";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameOuterForearm = "Outer_Forearm";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameInnerForearm = "Inner_Forearm";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNamePecs = "Pecs";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameNeck = "Neck";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameTraps = "Traps";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameShoulders = "Shoulders";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameScapula = "Scapula";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameLowerTraps = "Lower_Traps";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameLats = "Lats";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameTLF = "TLF";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameAbsA = "Abs_A";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameAbsB = "Abs_B";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameAbsC = "Abs_C";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameAbsD = "Abs_D";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNamePelvic = "Pelvic";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameLowerObliques = "Lower_Obliques";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameUpperObliques = "Upper_Oblqiues";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameSerratus = "Serratus";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameQuads = "Quads";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameOuterLeg = "Outer_Leg";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameHamstrings = "Hamstrings";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameInnerLeg = "Inner_Leg";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameGlutes = "Glutes";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameCalves = "Calves";
        /// <summary>
        /// Corresponds to the muscle's material property name in the character muscle shader, minus any suffixes.
        /// </summary>
        public const string defaultMuscleNameTFL = "TFL";

        public static string DefaultName(this MuscleGroup group)
        {

            switch (group)
            {

                case MuscleGroup.Biceps:
                    return defaultMuscleNameBiceps;

                case MuscleGroup.Triceps:
                    return defaultMuscleNameTriceps;

                case MuscleGroup.OuterForearm:
                    return defaultMuscleNameOuterForearm;

                case MuscleGroup.InnerForearm:
                    return defaultMuscleNameInnerForearm;

                case MuscleGroup.Pecs:
                    return defaultMuscleNamePecs;

                case MuscleGroup.Neck:
                    return defaultMuscleNameNeck;

                case MuscleGroup.Traps:
                    return defaultMuscleNameTraps;

                case MuscleGroup.Shoulders:
                    return defaultMuscleNameShoulders;

                case MuscleGroup.Scapula:
                    return defaultMuscleNameScapula;

                case MuscleGroup.LowerTraps:
                    return defaultMuscleNameLowerTraps;

                case MuscleGroup.Lats:
                    return defaultMuscleNameLats;

                case MuscleGroup.TLF:
                    return defaultMuscleNameTLF;

                case MuscleGroup.AbsA:
                    return defaultMuscleNameAbsA;

                case MuscleGroup.AbsB:
                    return defaultMuscleNameAbsB;

                case MuscleGroup.AbsC:
                    return defaultMuscleNameAbsC;

                case MuscleGroup.AbsD:
                    return defaultMuscleNameAbsD;

                case MuscleGroup.Pelvic:
                    return defaultMuscleNamePelvic;

                case MuscleGroup.LowerObliques:
                    return defaultMuscleNameLowerObliques;

                case MuscleGroup.UpperObliques:
                    return defaultMuscleNameUpperObliques;

                case MuscleGroup.Serratus:
                    return defaultMuscleNameSerratus;

                case MuscleGroup.Quads:
                    return defaultMuscleNameQuads;

                case MuscleGroup.OuterLeg:
                    return defaultMuscleNameOuterLeg;

                case MuscleGroup.Hamstrings:
                    return defaultMuscleNameHamstrings;

                case MuscleGroup.InnerLeg:
                    return defaultMuscleNameInnerLeg;

                case MuscleGroup.Glutes:
                    return defaultMuscleNameGlutes;

                case MuscleGroup.Calves:
                    return defaultMuscleNameCalves;

                case MuscleGroup.TFL:
                    return defaultMuscleNameTFL;

            }

            return "";

        }

        public const string sideSuffixBoth = "";
        /// <summary>
        /// Should be appended to a muscle group name when accessing a left muscle material property in the character muscle shader.
        /// </summary>
        public const string sideSuffixLeft = "_Left";
        /// <summary>
        /// Should be appended to a muscle group name when accessing a right muscle material property in the character muscle shader.
        /// </summary>
        public const string sideSuffixRight = "_Right";

        public static string AsSuffix(this Side side)
        {

            switch (side)
            {

                case Side.Both:
                    return sideSuffixBoth;

                case Side.Left:
                    return sideSuffixLeft;

                case Side.Right:
                    return sideSuffixRight;

            }

            return "";

        }

        public static Version AsVersion(this string str)
        {

            if (!string.IsNullOrEmpty(str))
            {

                try
                {
                    var ver = new Version(str);
                    return ver;
                }
                catch { }

            }

            return new Version("0.0.0");

        }

        /// <summary>
        /// Equivalent to the MiniScript implementation of a boolean
        /// </summary>
        public static bool AsBool(this double val) => val != 0;

        /// <summary>
        /// Equivalent to the MiniScript implementation of a boolean
        /// </summary>
        public static bool AsBool(this float val) => val != 0;


        private static List<int> available = new List<int>();
        private static List<int> restricted = new List<int>();

        public delegate int GetIdInCollectionDelegate(int index);

        /// <summary>
        /// Generates a unique id based on a collection of existing ids.
        /// </summary>
        public static int GetUniqueId(GetIdInCollectionDelegate getIdInCollection, int collectionSize)
        {

            if (getIdInCollection == null) return -1;

            available.Clear();
            restricted.Clear();

            void AddAvailableID(int id)
            {

                if (id >= 0 && !restricted.Contains(id)) available.Add(id);
            }

            for (int ind = 0; ind < collectionSize; ind++)
            {

                int id = getIdInCollection(ind);
                if (id < 0) continue;

                restricted.Add(id);
                available.RemoveAll(i => i == id);

                AddAvailableID(id - 1);
                AddAvailableID(id + 1);

            }

            if (available.Count > 1)
            {
                available.Sort();
                return available[0];
            }
            else if (available.Count == 1)
            {
                return available[0];
            }

            return collectionSize;

        }

        public static bool IsSpaceConvertible(this CreationVariable.Type type)
        {
            return

                type == CreationVariable.Type.Vector3 ||
                type == CreationVariable.Type.PositionLocal || type == CreationVariable.Type.PositionRoot || type == CreationVariable.Type.PositionWorld ||
                type == CreationVariable.Type.DirectionLocal || type == CreationVariable.Type.DirectionRoot || type == CreationVariable.Type.DirectionWorld ||
                type == CreationVariable.Type.ScaleLocal ||
                type == CreationVariable.Type.EulerAngles || type == CreationVariable.Type.RotationEulerLocal || type == CreationVariable.Type.RotationEulerRoot || type == CreationVariable.Type.RotationEulerWorld

                ||

                type == CreationVariable.Type.Vector4 ||
                type == CreationVariable.Type.TangentLocal || type == CreationVariable.Type.TangentRoot || type == CreationVariable.Type.TangentWorld ||
                type == CreationVariable.Type.Quaternion;
        }

        #region String Extensions

        public static string RemoveWhitespace(this string str)
        {
            return string.Join("", str.Split(default(string[]), System.StringSplitOptions.RemoveEmptyEntries));
        }

        public static string AsID(this string str) => str.ToLower().Trim();

        public static bool IsURL(this string str) => Uri.IsWellFormedUriString(str, UriKind.Absolute);
        public static bool IsWebURL(this string str) => Uri.TryCreate(str, UriKind.Absolute, out Uri uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        /// <summary>
        /// source: https://stackoverflow.com/questions/1546419/convert-file-path-to-a-file-uri/74852300#74852300
        /// </summary>
        public static string FilePathToFileUrl(string path)
        {
            return new UriBuilder("file", string.Empty)
            {
                Path = path
                        .Replace("%", $"%{(int)'%':X2}")
                        .Replace("[", $"%{(int)'[':X2}")
                        .Replace("]", $"%{(int)']':X2}"),
            }
                .Uri
                .AbsoluteUri;
        }

        /* Regular Expressions Cheat Sheet https://regexr.com/
        * ^ - Starts with
        * $ - Ends with
        * [] - Range
        * () - Group
        * . - Single character once
        * + - one or more characters in a row
        * ? - optional preceding character match
        * \ - escape character
        * \n - New line
        * \d - Digit
        * \D - Non-digit
        * \s - White space
        * \S - non-white space
        * \w - alphanumeric/underscore character (word chars)
        * \W - non-word characters
        * {x,y} - Repeat low (x) to high (y) (no "y" means at least x, no ",y" means that many)
        * (x|y) - Alternative - x or y
        * 
        * [^x] - Anything but x (where x is whatever character you want)
        */

        public static readonly Regex rgAlphabetic = new Regex(@"^[a-zA-Z\s]*$");
        public static readonly Regex rgAlphabeticFilter = new Regex(@"[^a-zA-Z\s]");
        public static bool IsAlphabetic(this string str) => rgAlphabetic.IsMatch(str);
        public static string AsAlphabetic(this string str) => rgAlphabeticFilter.Replace(str, "");

        public static readonly Regex rgAlphabeticNoWhitespace = new Regex(@"^[a-zA-Z]*$");
        public static readonly Regex rgAlphabeticFilterNoWhitespace = new Regex(@"[^a-zA-Z]");
        public static bool IsAlphabeticNoWhitespace(this string str) => rgAlphabeticNoWhitespace.IsMatch(str);
        public static string AsAlphabeticNoWhitespace(this string str) => rgAlphabeticFilterNoWhitespace.Replace(str, "");

        public static readonly Regex rgAlphaNumeric = new Regex(@"^[a-zA-Z0-9\s]*$");
        public static readonly Regex rgAlphaNumericFilter = new Regex(@"[^a-zA-Z0-9\s]");
        public static bool IsAlphaNumeric(this string str) => rgAlphaNumeric.IsMatch(str);
        public static string AsAlphaNumeric(this string str) => rgAlphaNumericFilter.Replace(str, "");

        public static readonly Regex rgAlphaNumericWithSpacesAndUnderscores = new Regex(@"^[a-zA-Z0-9\s_]*$");
        public static readonly Regex rgAlphaNumericWithSpacesAndUnderscoresFilter = new Regex(@"[^a-zA-Z0-9\s_]");
        public static bool IsAlphaNumericWithSpacesAndUnderscores(this string str) => rgAlphaNumeric.IsMatch(str);
        public static string AsAlphaNumericWithSpacesAndUnderscores(this string str) => rgAlphaNumericFilter.Replace(str, "");

        public static readonly Regex rgAlphaNumericNoWhitespace = new Regex(@"^[a-zA-Z0-9]*$");
        public static readonly Regex rgAlphaNumericNoWhitespaceFilter = new Regex(@"[^a-zA-Z0-9]");
        public static bool IsAlphaNumericNoWhitespace(this string str) => rgAlphaNumericNoWhitespace.IsMatch(str);
        public static string AsAlphaNumericNoWhitespace(this string str) => rgAlphaNumericNoWhitespaceFilter.Replace(str, "");

        public static readonly Regex rgProjectName = new Regex(@"^[a-zA-Z0-9\s_\-.\s]*$");
        public static readonly Regex rgProjectNameFilter = new Regex(@"[^a-zA-Z0-9_\-.\s]");
        public static bool IsProjectName(this string str) => rgProjectName.IsMatch(str);
        public static string AsProjectName(this string str) => rgProjectNameFilter.Replace(str, "");

        public static readonly Regex rgContentName = new Regex(@"^[a-zA-Z0-9\s_\-\s]*$");
        public static readonly Regex rgContentNameFilter = new Regex(@"[^a-zA-Z0-9_\-\s]");
        public static bool IsContentName(this string str) => rgContentName.IsMatch(str);
        public static string AsContentName(this string str) => rgContentNameFilter.Replace(str, "");

        public static readonly Regex rgTagsString = new Regex(@"^[a-zA-Z0-9\s_\-.,\s]*$");
        public static readonly Regex rgTagsStringFilter = new Regex(@"[^a-zA-Z0-9_\-.,\s]");
        public static bool IsTagsString(this string str) => rgTagsString.IsMatch(str);
        public static string AsTagsString(this string str) => rgTagsStringFilter.Replace(str, "");

        public static readonly Regex rgPackageString = new Regex(@"^[a-zA-Z0-9.]*(@([0-9]\.){1,3}[0-9])?$");
        public static readonly Regex rgPackageStringWithVersion = new Regex(@"^[a-zA-Z0-9.]*(@([0-9]\.){1,3}[0-9])$");
        public static readonly Regex rgPackageNameString = new Regex(@"^[a-zA-Z0-9.]*$");

        public static readonly Regex rgPackageStringFilter = new Regex(@"[^a-zA-Z0-9.@]");
        public static readonly Regex rgPackageNameFilter = new Regex(@"[^a-zA-Z0-9.]"); 
        public static bool IsPackageName(this string str) => rgPackageNameString.IsMatch(str) && !str.StartsWith('.') && !str.EndsWith('.') && (str.Length > 0 ? str.Substring(0, 1).IsAlphabeticNoWhitespace() : false);
        public static bool IsPackageString(this string str) => rgPackageString.IsMatch(str) && !str.StartsWith('.') && !str.EndsWith('.') && (str.Length > 0 ? str.Substring(0, 1).IsAlphabeticNoWhitespace() : false);
        public static bool IsPackageStringWithVersion(this string str) => rgPackageStringWithVersion.IsMatch(str) && !str.StartsWith('.') && !str.EndsWith('.') && (str.Length > 0 ? str.Substring(0, 1).IsAlphabeticNoWhitespace() : false);
        public static string AsPackageString(this string str) 
        { 
            str = rgPackageStringFilter.Replace(str, "");
            while (str.StartsWith('.')) str = str.Length > 1 ? str.Substring(1) : string.Empty;
            while (str.EndsWith('.')) str = str.Length > 1 ? str.Substring(0, str.Length - 1) : string.Empty;
            if (str.Length > 0 && !str.Substring(0, 1).IsAlphabeticNoWhitespace()) str = "x." + str;

            return str;
        }
        public static string AsPackageName(this string str)
        {
            str = rgPackageNameFilter.Replace(str, "");
            while (str.StartsWith('.')) str = str.Length > 1 ? str.Substring(1) : string.Empty;
            while (str.EndsWith('.')) str = str.Length > 1 ? str.Substring(0, str.Length - 1) : string.Empty;
            if (str.Length > 0 && !str.Substring(0, 1).IsAlphabeticNoWhitespace()) str = "x." + str;

            return str;
        }

        public static readonly Regex rgFourComponentVersionNumber = new Regex(@"^([0-9]\.){1,3}[0-9]$");
        public static bool IsNativeVersionString(this string str) => rgFourComponentVersionNumber.IsMatch(str);

        /// <summary>
        /// Source: https://stackoverflow.com/questions/1365407/c-sharp-code-to-validate-email-address
        /// </summary>
        public static readonly Regex rgEmailAddress = new Regex(@"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|" + @"([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)" + @"@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$", RegexOptions.IgnoreCase);
        public static bool IsEmailAddress(this string str) => rgEmailAddress.IsMatch(str);

        #endregion

        #region Generic Extensions

        public static Array DeepClone(this Array array)
        {

            Array newArray = Array.CreateInstance(array.GetType().GetElementType(), array.Length);

            for (int a = 0; a < array.Length; a++)
            {

                object val = array.GetValue(a);

                if (val == null) continue;

                if (val.GetType().IsAssignableFrom(typeof(ICloneable))) val = (val as ICloneable).Clone();

                newArray.SetValue(val, a);

            }

            return newArray;

        }

        public static List<T> DeepClone<T>(this List<T> list)
        {

            List<T> newList = new List<T>(list);

            for (int a = 0; a < list.Count; a++)
            {

                T val = list[a];

                if (val == null) continue;

                if (typeof(ICloneable).IsAssignableFrom(val.GetType())) val = (T)(val as ICloneable).Clone();

                newList[a] = val;

            }

            return newList;

        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type>
        {
            typeof(int),  typeof(double),  typeof(decimal),
            typeof(long), typeof(short),   typeof(sbyte),
            typeof(byte), typeof(ulong),   typeof(ushort),
            typeof(uint), typeof(float)
        };

        public static bool IsNumeric(this Type myType)
        {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }

        public static List<PropertyInfo> GetPropertiesWithAttribute<T>(this System.Type type) where T : System.Attribute
        {

            List<PropertyInfo> properties = new List<PropertyInfo>();

            PropertyInfo[] props = type.GetProperties();

            for (int a = 0; a < props.Length; a++)
            {

                if (System.Attribute.IsDefined(props[a], typeof(T))) properties.Add(props[a]);
            }

            return properties;

        }

        public static List<FieldInfo> GetFieldsWithAttribute<T>(this System.Type type) where T : System.Attribute
        {

            List<FieldInfo> properties = new List<FieldInfo>();

            FieldInfo[] props = type.GetFields();

            for (int a = 0; a < props.Length; a++)
            {

                if (System.Attribute.IsDefined(props[a], typeof(T))) properties.Add(props[a]);
            }

            return properties;

        }

        #endregion

        #region File Handling

        /// <summary>
        /// Source: https://stackoverflow.com/questions/882686/asynchronous-file-copy-move-in-c-sharp
        /// </summary>
        public static async Task CopyFileAsync(string sourceFile, string destinationFile, CancellationToken cancellationToken = default)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream =
                  new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))

            using (var destinationStream =
                  new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

                await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken)
                                  .ConfigureAwait(false); 
        }
        public static void CopyAll(string sourceDir, string targetDir, bool overwriteExistingFiles, ICollection<string> extensionFilter = null) => CopyAll(new DirectoryInfo(sourceDir), new DirectoryInfo(targetDir), overwriteExistingFiles, extensionFilter);
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target, bool overwriteExistingFiles, ICollection<string> extensionFilter = null)
        {
            if (source.FullName.ToLower() == target.FullName.ToLower()) return; 

            if (!Directory.Exists(target.FullName)) Directory.CreateDirectory(target.FullName);
           
            foreach (FileInfo fi in source.GetFiles())
            {
                if (extensionFilter != null)
                {
                    var ext = Path.GetExtension(fi.Name);
                    if (extensionFilter.Contains(ext)) continue;
                    if (extensionFilter.Contains(ext.AsID())) continue;
                    ext = ext.Replace(".", string.Empty);
                    if (extensionFilter.Contains(ext)) continue;
                    if (extensionFilter.Contains(ext.AsID())) continue;
                }
                try
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), overwriteExistingFiles);
                }
                catch(IOException) // if the file already exists dont overwrite it and catch the exception
                {  
                }
            }

            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir, overwriteExistingFiles, extensionFilter);
            }
        }

        /// <summary>
        /// Source: https://stackoverflow.com/questions/6198392/check-whether-a-path-is-valid
        /// </summary>
        public static bool IsValidPath(string path, bool allowRelativePaths = false)
        {
            bool isValid = true;

            try
            {
                string fullPath = Path.GetFullPath(path);

                if (allowRelativePaths)
                {
                    isValid = Path.IsPathRooted(path);
                }
                else
                {
                    string root = Path.GetPathRoot(path);
                    isValid = string.IsNullOrEmpty(root.Trim(new char[] { '\\', '/' })) == false;
                }
            }
            catch
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Source: https://stackoverflow.com/questions/5617320/given-full-path-check-if-path-is-subdirectory-of-some-other-path-or-otherwise
        /// </summary>
        public static bool IsSubPathOf(this string subPath, string basePath)
        {
            var rel = Path.GetRelativePath(
                basePath.Replace('\\', '/'),
                subPath.Replace('\\', '/'));
            return rel != "."
                && rel != ".."
                && !rel.StartsWith("../")
                && !Path.IsPathRooted(rel);
        }
        public static bool IsSubDirectoryOf(this DirectoryInfo subDir, string basePath) => IsSubPathOf(subDir == null ? string.Empty : subDir.FullName, basePath);
        public static bool IsSubDirectoryOf(this DirectoryInfo subDir, DirectoryInfo baseDir) => IsSubPathOf(subDir == null ? string.Empty : subDir.FullName, baseDir == null ? string.Empty : baseDir.FullName);

        /// <summary>
        /// Source: https://stackoverflow.com/questions/2281531/how-can-i-compare-directory-paths-in-c
        /// </summary>
        public static string NormalizePath(this string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        public static string NormalizePathCaseInsensitive(this string path) => path.NormalizePath().ToUpperInvariant();

        public static bool IsIdenticalPath(this string path, string otherPath) => path.NormalizePath() == otherPath.NormalizePath();
        public static bool IsIdenticalPathIgnoreCase(this string path, string otherPath) => path.NormalizePathCaseInsensitive() == otherPath.NormalizePathCaseInsensitive();

        public static string NormalizeDirectorySeparators(this string path, bool useAltSeparatorChar = true) => path.Replace(useAltSeparatorChar ? Path.DirectorySeparatorChar : Path.AltDirectorySeparatorChar, useAltSeparatorChar ? Path.AltDirectorySeparatorChar : Path.DirectorySeparatorChar);

        public static bool IsEmpty(this DirectoryInfo directory) => IsDirectoryEmpty(directory);
        public static bool IsDirectoryEmpty(string path) => IsDirectoryEmpty(new DirectoryInfo(path));
        public static bool IsDirectoryEmpty(DirectoryInfo directory)
        {
            if (directory == null || !directory.Exists) return true;
            if (directory.GetDirectories().Length > 0) return false;
            if (directory.GetFiles().Length > 0) return false;
            return true;
        }

        #endregion

    }

}
