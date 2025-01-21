#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using UnityEngine;

namespace Swole
{

    /// <summary>
    /// source: https://discussions.unity.com/t/getting-a-primitive-mesh-without-creating-a-new-gameobject/78809/6
    /// </summary>
    public static class UnityPrimitiveMesh
    {
        public static Mesh Get(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.Sphere:
                    return GetCached(ref _unitySphereMesh, primitiveType);
                case PrimitiveType.Capsule:
                    return GetCached(ref _unityCapsuleMesh, primitiveType);
                case PrimitiveType.Cylinder:
                    return GetCached(ref _unityCylinderMesh, primitiveType);
                case PrimitiveType.Cube:
                    return GetCached(ref _unityCubeMesh, primitiveType);
                case PrimitiveType.Plane:
                    return GetCached(ref _unityPlaneMesh, primitiveType);
                case PrimitiveType.Quad:
                    return GetCached(ref _unityQuadMesh, primitiveType);
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
            }
        }

        private static Mesh GetCached(ref Mesh primMesh, PrimitiveType primitiveType)
        {
            if (primMesh == null)
            {
                Debug.Log("Getting Unity Primitive Mesh: " + primitiveType);
                primMesh = Resources.GetBuiltinResource<Mesh>(GetPath(primitiveType));

                if (primMesh == null)
                {
                    Debug.LogError("Couldn't load Unity Primitive Mesh: " + primitiveType);
                }
            }

            return primMesh;
        }

        private static string GetPath(PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.Sphere:
                    return "New-Sphere.fbx";
                case PrimitiveType.Capsule:
                    return "New-Capsule.fbx";
                case PrimitiveType.Cylinder:
                    return "New-Cylinder.fbx";
                case PrimitiveType.Cube:
                    return "Cube.fbx";
                case PrimitiveType.Plane:
                    return "New-Plane.fbx";
                case PrimitiveType.Quad:
                    return "Quad.fbx";
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitiveType), primitiveType, null);
            }
        }

        private static Mesh _unityCapsuleMesh = null;
        private static Mesh _unityCubeMesh = null;
        private static Mesh _unityCylinderMesh = null;
        private static Mesh _unityPlaneMesh = null;
        private static Mesh _unitySphereMesh = null;
        private static Mesh _unityQuadMesh = null;
    }
}

#endif