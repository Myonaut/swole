using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnityEngine;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace swolescr.andywiecko.BurstTriangulator
{

    public static class TriangulatorExtensions
    {

        public static unsafe Span<T> ToSpan<T>(this NativeArray<T> array) where T : struct => new Span<T>(array.GetUnsafePtr(), array.Length);

        public static unsafe ReadOnlySpan<T> ToReadOnlySpan<T>(this NativeArray<T> array) where T : struct => new ReadOnlySpan<T>(array.GetUnsafeReadOnlyPtr(), array.Length);

        public static unsafe Span<T> ToSpan<T>(this NativeList<T> list) where T : unmanaged => list.AsArray().ToSpan();

        public static unsafe ReadOnlySpan<T> ToReadOnlySpan<T>(this NativeList<T> list) where T : unmanaged => list.AsArray().ToReadOnlySpan();

    }

}