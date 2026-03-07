using System;

namespace Swole
{
    [Serializable]
    public enum MuscleGroupsDefault
    {
        Biceps_Left, Biceps_Right, Triceps_Left, Triceps_Right, Outer_Forearm_Left, Outer_Forearm_Right, Inner_Forearm_Left, Inner_Forearm_Right, Pecs_Left, Pecs_Right, Neck, Traps_Left, Traps_Right, Shoulders_Left, Shoulders_Right, Scapula_Left, Scapula_Right, Lower_Traps_Left, Lower_Traps_Right, Lats_Left, Lats_Right,
        TLF_Left, TLF_Right, Abs_A_Left, Abs_A_Right, Abs_B_Left, Abs_B_Right, Abs_C_Left, Abs_C_Right, Abs_D_Left, Abs_D_Right, Pelvic, Lower_Obliques_Left, Lower_Obliques_Right, Upper_Obliques_Left, Upper_Obliques_Right, Serratus_Left, Serratus_Right, Quads_Left, Quads_Right, Outer_Leg_Left, Outer_Leg_Right, Hamstrings_Left, Hamstrings_Right,
        Inner_Leg_Left, Inner_Leg_Right, Glutes_Left, Glutes_Right, Calves_Left, Calves_Right, TFL_Left, TFL_Right
    }

    public static class MuscleGroupsDefaultExtensions
    {
        public static MuscleGroup GetMuscleGroupBase(this MuscleGroupsDefault muscleGroup)
        {
            return GetMuscleGroupBase(muscleGroup.ToString()); 
        }
        public static MuscleGroup GetMuscleGroupBase(string name)
        {
            name = name.ToLower();

            var indexOfLeft = name.IndexOf(SwoleUtil.sideSuffixLeft.ToLower());
            if (indexOfLeft >= 0)
            {
                name = name.Substring(0, indexOfLeft);
            }

            var indexOfRight = name.IndexOf(SwoleUtil.sideSuffixRight.ToLower());
            if (indexOfRight >= 0)
            {
                name = name.Substring(0, indexOfRight);
            }

            if (Enum.TryParse(name, true, out MuscleGroup result)) 
            {
                return result;
            }

            return MuscleGroup.Null;
        }

        public static bool IsLeft(this MuscleGroupsDefault muscleGroup)
        {
            return muscleGroup.ToString().EndsWith(SwoleUtil.sideSuffixLeft);
        }

        public static bool IsRight(this MuscleGroupsDefault muscleGroup)
        {
            return muscleGroup.ToString().EndsWith(SwoleUtil.sideSuffixRight); 
        }

        public static Side GetSide(this MuscleGroupsDefault muscleGroup)
        {
            if (muscleGroup.IsLeft()) return Side.Left;
            if (muscleGroup.IsRight()) return Side.Right;
            return Side.Both;
        }

        public static bool IsSymmetrical(this MuscleGroupsDefault muscleGroup)
        {
            return muscleGroup.GetSide() == Side.Both;
        }
    }
}
