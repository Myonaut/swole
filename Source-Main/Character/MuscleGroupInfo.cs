using System;
using System.Runtime.InteropServices;

namespace Swole
{

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleGroupInfo
    {

        public int mirroredIndex;
        public float mass;
        public float flex;
        public float pump;

        public MuscleGroupInfo(int mirroredIndex, float mass, float flex, float pump)
        {

            this.mirroredIndex = mirroredIndex;
            this.mass = mass;
            this.flex = flex;
            this.pump = pump;

        }

    }

}
