#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace Swole
{
    public static class JobUtils
    {
    }

    [BurstCompile]
    public struct MemsetNativeArray<T> : IJobParallelFor where T : struct
    {

        public T value;

        [NativeDisableParallelForRestriction]
        public NativeArray<T> array;

        public void Execute(int index)
        {
            array[index] = value;
        }
    }
}

#endif