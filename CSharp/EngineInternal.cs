using System;

namespace Swolescript
{

    public static class EngineInternal
    {

        [Serializable]
        public struct Vector2
        {

            public static readonly Vector2 zero = new Vector2(0, 0);
            public static readonly Vector2 one = new Vector2(1, 1);

            public float x, y;

            public Vector2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }

            public static implicit operator Vector3(Vector2 v2) => new(v2.x, v2.y, 0);
            public static implicit operator Vector4(Vector2 v2) => new(v2.x, v2.y, 0, 0);

        }

        [Serializable]
        public struct Vector3
        {

            public static readonly Vector3 zero = new Vector3(0, 0, 0);
            public static readonly Vector3 one = new Vector3(1, 1, 1);

            public float x, y, z;

            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public static implicit operator Vector2(Vector3 v3) => new(v3.x, v3.y);
            public static implicit operator Vector4(Vector3 v3) => new(v3.x, v3.y, v3.z, 0);

        }

        [Serializable]
        public struct Vector4
        {

            public static readonly Vector4 zero = new Vector4(0, 0, 0, 0);
            public static readonly Vector4 one = new Vector4(1, 1, 1, 1);

            public float x, y, z, w;

            public Vector4(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public static implicit operator Vector2(Vector4 v4) => new(v4.x, v4.y);
            public static implicit operator Vector3(Vector4 v4) => new(v4.x, v4.y, v4.z);
            public static implicit operator Quaternion(Vector4 v4) => new(v4.x, v4.y, v4.z, v4.w);

        }

        [Serializable]
        public struct Quaternion
        {

            public static readonly Quaternion identity = new Quaternion(0, 0, 0, 1);

            public float x, y, z, w;

            public Quaternion(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public static implicit operator Vector4(Quaternion q) => new(q.x, q.y, q.z, q.w);

        }

    }

}
