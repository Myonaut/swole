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

        public static unsafe void AddReplicated<T>(this NativeList<T> list, T value, int amount) where T : unmanaged
        {
#if UNITY_2022_OR_NEWER
            list.AddReplicate(value, amount);
#else
            for(int a = 0; a < amount; a++) list.Add(value);
#endif
        }

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