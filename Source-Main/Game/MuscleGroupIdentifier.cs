using System;

namespace Swole
{

    [Serializable]
    public struct MuscleGroupIdentifier
    {

        public MuscleGroup muscleGroup;

        public Side side;

        public override readonly string ToString()
        {

            return muscleGroup.DefaultName() + side.AsSuffix();

        }

    }

}
