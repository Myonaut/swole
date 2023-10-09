#if (UNITY_STANDALONE || UNITYEDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public static class MeshUtils
    {

        public static List<Vector2> GetUVsByChannelAsList(this Mesh mesh, int uvChannel)
        {

            List<Vector2> uv = new List<Vector2>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector2[] GetUVsByChannel(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsList(mesh, uvChannel).ToArray();

        }

        public static List<Vector3> GetUVsByChannelAsListV3(this Mesh mesh, int uvChannel)
        {

            List<Vector3> uv = new List<Vector3>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector3[] GetUVsByChannelV3(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsListV3(mesh, uvChannel).ToArray();

        }

        public static List<Vector4> GetUVsByChannelAsListV4(this Mesh mesh, int uvChannel)
        {

            List<Vector4> uv = new List<Vector4>();

            mesh.GetUVs(uvChannel, uv);

            return uv;

        }

        public static Vector4[] GetUVsByChannelV4(this Mesh mesh, int uvChannel)
        {

            return GetUVsByChannelAsListV4(mesh, uvChannel).ToArray();

        }

    }

}

#endif
