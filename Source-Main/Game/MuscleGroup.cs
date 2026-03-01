using System;

namespace Swole
{

    [Serializable]
    public enum MuscleGroup
    {

        Null, Biceps, Triceps, OuterForearm, InnerForearm, Pecs, Neck, Traps, Shoulders, Scapula, LowerTraps, Lats, TLF, AbsA, AbsB, AbsC, AbsD, Pelvic, LowerObliques, UpperObliques, Serratus, Quads, OuterLeg, Hamstrings, InnerLeg, Glutes, Calves, TFL

    }

    public static class MuscleGroupsExtensions
    {
        public static MuscleGroupsDefault GetMuscleGroupSide(this MuscleGroup muscleGroup, Side side)
        {
            if (muscleGroup == MuscleGroup.Neck) return MuscleGroupsDefault.Neck;

            switch (muscleGroup)
            {
                case MuscleGroup.Biceps:
                    return side == Side.Left ? MuscleGroupsDefault.Biceps_Left : MuscleGroupsDefault.Biceps_Right;
                case MuscleGroup.Triceps:
                    return side == Side.Left ? MuscleGroupsDefault.Triceps_Left : MuscleGroupsDefault.Triceps_Right;
                case MuscleGroup.OuterForearm:
                    return side == Side.Left ? MuscleGroupsDefault.Outer_Forearm_Left : MuscleGroupsDefault.Outer_Forearm_Right;
                case MuscleGroup.InnerForearm:
                    return side == Side.Left ? MuscleGroupsDefault.Inner_Forearm_Left : MuscleGroupsDefault.Inner_Forearm_Right;
                case MuscleGroup.Neck:
                    return MuscleGroupsDefault.Neck;
                case MuscleGroup.Pecs:
                    return side == Side.Left ? MuscleGroupsDefault.Pecs_Left : MuscleGroupsDefault.Pecs_Right;
                case MuscleGroup.Traps:
                    return side == Side.Left ? MuscleGroupsDefault.Traps_Left : MuscleGroupsDefault.Traps_Right;
                case MuscleGroup.Shoulders:
                    return side == Side.Left ? MuscleGroupsDefault.Shoulders_Left : MuscleGroupsDefault.Shoulders_Right;
                case MuscleGroup.Scapula:
                    return side == Side.Left ? MuscleGroupsDefault.Scapula_Left : MuscleGroupsDefault.Scapula_Right;
                case MuscleGroup.LowerTraps:
                    return side == Side.Left ? MuscleGroupsDefault.Lower_Traps_Left : MuscleGroupsDefault.Lower_Traps_Right;
                case MuscleGroup.Lats:
                    return side == Side.Left ? MuscleGroupsDefault.Lats_Left : MuscleGroupsDefault.Lats_Right;
                case MuscleGroup.TLF:
                    return side == Side.Left ? MuscleGroupsDefault.TLF_Left : MuscleGroupsDefault.TLF_Right;
                case MuscleGroup.AbsA:
                    return side == Side.Left ? MuscleGroupsDefault.Abs_A_Left : MuscleGroupsDefault.Abs_A_Right;
                case MuscleGroup.AbsB:
                    return side == Side.Left ? MuscleGroupsDefault.Abs_B_Left : MuscleGroupsDefault.Abs_B_Right;
                case MuscleGroup.AbsC:
                    return side == Side.Left ? MuscleGroupsDefault.Abs_C_Left : MuscleGroupsDefault.Abs_C_Right;
                case MuscleGroup.AbsD:
                    return side == Side.Left ? MuscleGroupsDefault.Abs_D_Left : MuscleGroupsDefault.Abs_D_Right;
                case MuscleGroup.Pelvic:
                    return MuscleGroupsDefault.Pelvic;
                case MuscleGroup.LowerObliques:
                    return side == Side.Left ? MuscleGroupsDefault.Lower_Obliques_Left : MuscleGroupsDefault.Lower_Obliques_Right;
                case MuscleGroup.UpperObliques:
                    return side == Side.Left ? MuscleGroupsDefault.Upper_Obliques_Left : MuscleGroupsDefault.Upper_Obliques_Right;
                case MuscleGroup.Serratus:
                    return side == Side.Left ? MuscleGroupsDefault.Serratus_Left : MuscleGroupsDefault.Serratus_Right;
                case MuscleGroup.Quads:
                    return side == Side.Left ? MuscleGroupsDefault.Quads_Left : MuscleGroupsDefault.Quads_Right;
                case MuscleGroup.OuterLeg:
                    return side == Side.Left ? MuscleGroupsDefault.Outer_Leg_Left : MuscleGroupsDefault.Outer_Leg_Right;
                case MuscleGroup.Hamstrings:
                    return side == Side.Left ? MuscleGroupsDefault.Hamstrings_Left : MuscleGroupsDefault.Hamstrings_Right;
                case MuscleGroup.InnerLeg:
                    return side == Side.Left ? MuscleGroupsDefault.Inner_Leg_Left : MuscleGroupsDefault.Inner_Leg_Right;
                case MuscleGroup.Glutes:
                    return side == Side.Left ? MuscleGroupsDefault.Glutes_Left : MuscleGroupsDefault.Glutes_Right;
                case MuscleGroup.Calves:
                    return side == Side.Left ? MuscleGroupsDefault.Calves_Left : MuscleGroupsDefault.Calves_Right;
                case MuscleGroup.TFL:
                    return side == Side.Left ? MuscleGroupsDefault.TFL_Left : MuscleGroupsDefault.TFL_Right;

            } 

            return default;
        }
    }

}
