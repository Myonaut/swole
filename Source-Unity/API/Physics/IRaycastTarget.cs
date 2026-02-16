using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

namespace Swole
{
    public interface IRaycastTarget
    {
        public bool RaycastAgainst(int lod, float3 rayOrigin, float3 rayOffset, out Maths.RaycastHitResult hit, float errorMargin = 0.01f);

        public int DefaultRaycastLOD { get; set; }
    }
}
