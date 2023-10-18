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

        public static bool AsBool(this double val) => val != 0; // Equivalent to MiniScript implementation of a boolean

        public static string RemoveWhitespace(this string str)
        {
            return string.Join("", str.Split(default(string[]), System.StringSplitOptions.RemoveEmptyEntries));
        }

        public static string AsID(this string str) => str.ToLower().Trim();


        private static List<int> available = new List<int>();
        private static List<int> restricted = new List<int>();

        public delegate int GetIdFromArrayDelegate(int index);

        public static int GetUniqueId(GetIdFromArrayDelegate getIdInCollection, int count)
        {

            if (getIdInCollection == null) return -1;

            available.Clear();
            restricted.Clear();

            void AddAvailableID(int id)
            {

                if (id >= 0 && !restricted.Contains(id)) available.Add(id);
            }

            for (int ind = 0; ind < count; ind++)
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

            return count;

        }

        #region Regular Expressions

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

        public static readonly Regex rgAlphaNumeric = new Regex(@"^[a-zA-Z0-9\s]*$");
        public static readonly Regex rgAlphaNumericFilter = new Regex(@"[^a-zA-Z0-9\s]");
        public static bool IsAlphaNumeric(this string str) => rgAlphaNumeric.IsMatch(str);
        public static string AsAlphaNumeric(this string str) => rgAlphaNumericFilter.Replace(str, "");

        public static readonly Regex rgAlphaNumericNoWhitespace = new Regex(@"^[a-zA-Z0-9]*$");
        public static readonly Regex rgAlphaNumericNoWhitespaceFilter = new Regex(@"[^a-zA-Z0-9]");
        public static bool IsAlphaNumericNoWhitespace(this string str) => rgAlphaNumericNoWhitespace.IsMatch(str);
        public static string AsAlphaNumericNoWhitespace(this string str) => rgAlphaNumericNoWhitespaceFilter.Replace(str, "");

        public static readonly Regex rgPackageString = new Regex(@"^[a-zA-Z0-9.]*$");
        public static readonly Regex rgPackageStringFilter = new Regex(@"[^a-zA-Z0-9.]");
        public static bool IsPackageString(this string str) => rgPackageString.IsMatch(str);
        public static string AsPackageString(this string str) => rgPackageStringFilter.Replace(str, "");

        public static readonly Regex rgFourComponentVersionNumber = new Regex(@"^([0-9]\.){1,3}[0-9]$");
        public static bool IsNativeVersionString(this string str) => rgFourComponentVersionNumber.IsMatch(str);

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

        #endregion

    }

}
