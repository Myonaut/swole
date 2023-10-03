using System.Text.RegularExpressions;
using System;

namespace Swolescript
{

    public static class SwoleScriptUtil
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

        public static readonly Regex rgAlphaNumeric = new Regex(@"^[a-zA-Z0-9\s,]*$");
        public static bool IsAlphaNumeric(this string str) => rgAlphaNumeric.IsMatch(str);

    }

}
