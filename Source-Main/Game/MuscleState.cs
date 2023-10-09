using System;

namespace Swole
{

    /// <summary>
    /// A struct used to represent the state of a muscle in terms of mass, flex, and pump.
    /// </summary>
    [Serializable]
    public struct MuscleState
    {

        public const int fullMassThreshold = ushort.MaxValue / 8;
        public const int fullFlexThreshold = 43690;
        public const int fullPumpThreshold = ushort.MaxValue;

        public static ushort FloatToMass(float mass) => (ushort)Math.Clamp(Math.Round(mass * fullMassThreshold), 0, ushort.MaxValue);
        public static ushort FloatToFlex(float flex) => (ushort)Math.Clamp(Math.Round(flex * fullFlexThreshold), 0, ushort.MaxValue);
        public static ushort FloatToPump(float pump) => (ushort)Math.Clamp(Math.Round(pump * fullPumpThreshold), 0, ushort.MaxValue);

        public static float MassToFloat(ushort mass) => mass / (float)fullMassThreshold;
        public static float FlexToFloat(ushort flex) => flex / (float)fullFlexThreshold;
        public static float PumpToFloat(ushort pump) => pump / (float)fullPumpThreshold;

        public MuscleGroupIdentifier muscleGroup;

        /// <summary>
        /// The size and presence of the muscle.
        /// </summary>
        public ushort mass;
        /// <summary>
        /// How flexed the muscle is.
        /// </summary>
        public ushort flex;
        /// <summary>
        /// How veiny the area near the muscle is.
        /// </summary>
        public ushort pump;

        /// <summary>
        /// The size and presence of the muscle. Normalized to a range where 1 is the controlled maximum, and values beyond it extrapolate mesh morph data.
        /// </summary>
        public float NormalizedMass => MassToFloat(mass);
        /// <summary>
        /// How flexed the muscle is. Normalized to a range where 1 is the controlled maximum, and values beyond it extrapolate mesh morph data.
        /// </summary>
        public float NormalizedFlex => MassToFloat(flex);
        /// <summary>
        /// How veiny the area near the muscle is. Normalized to a range where 1 is the controlled maximum, and values beyond it extrapolate mesh morph data.
        /// </summary>
        public float NormalizedPump => MassToFloat(pump);

    }

}
