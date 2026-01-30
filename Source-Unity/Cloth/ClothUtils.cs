#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using UnityEngine;

using Unity.Mathematics;
using Unity.Collections;

namespace Swole.Cloth
{
    public static class ClothUtils
    {

        /*public static NativeList<SharedEdge> FindSharedEdges()
        {

        }*/

    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct SharedEdge
    {
        public int localEdgeA;
        public int localEdgeB;
        public int globalEdge;
    }
}

#endif