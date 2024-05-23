using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if SWOLE_ENV
using Miniscript;
#endif

using Swole.Script;
using Swole.Animation;

namespace Swole
{

    public static class EngineInternal
    {
         
        [Serializable]
        public enum RotationOrder
        {
            XYZ, XZY, YZX, YXZ, ZXY, ZYX
        }

        [Serializable]
        public enum Space
        {
            World, Self
        }

        #region Data Types

        [Serializable]
        public struct Vector2 : IEquatable<Vector2>
        {

            public override string ToString() => $"({x}, {y})";

            public bool IsZero => (x == 0 && y == 0);

            public static Vector2 operator +(Vector2 vA, Vector2 vB) => new Vector2(vA.x + vB.x, vA.y + vB.y);
            public static Vector2 operator *(Vector2 v, float scalar) => new Vector2(v.x * scalar, v.y * scalar);
            public static Vector2 operator /(Vector2 v, float scalar) => new Vector2(v.x / scalar, v.y / scalar);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object other)
            {
                if (!(other is Vector2))
                {
                    return false;
                }

                return Equals((Vector2)other);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Vector2 other)
            {
                return x == other.x && y == other.y;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return x.GetHashCode() ^ (y.GetHashCode() << 2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Vector2 lhs, Vector2 rhs)
            {
                float num = lhs.x - rhs.x;
                float num2 = lhs.y - rhs.y;
                return num * num + num2 * num2 < 9.99999944E-11f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Vector2 lhs, Vector2 rhs)
            {
                return !(lhs == rhs);
            }

        }

        [Serializable]
        public struct Vector3 : IEquatable<Vector3>
        {

            #region Proxy Implementations

            public static Vector3 operator *(Quaternion q, Vector3 v) => swole.Engine.Mul(q, v);

            #endregion

            public override string ToString() => $"({x}, {y}, {z})";

            public bool IsZero => (x == 0 && y == 0 && z == 0);

            public static Vector3 operator +(Vector3 vA, Vector3 vB) => new Vector3(vA.x + vB.x, vA.y + vB.y, vA.z + vB.z);
            public static Vector3 operator *(Vector3 v, float scalar) => new Vector3(v.x * scalar, v.y * scalar, v.z * scalar);
            public static Vector3 operator /(Vector3 v, float scalar) => new Vector3(v.x / scalar, v.y / scalar, v.z * scalar);

            public static readonly Vector3 zero = new Vector3(0, 0, 0);
            public static readonly Vector3 one = new Vector3(1, 1, 1);

            public static readonly Vector3 forward = new Vector3(0, 0, 1);
            public static readonly Vector3 up = new Vector3(0, 1, 0);
            public static readonly Vector3 down = new Vector3(0, -1, 0);

            public float x, y, z;

            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            public static implicit operator Vector2(Vector3 v3) => new(v3.x, v3.y);
            public static implicit operator Vector4(Vector3 v3) => new(v3.x, v3.y, v3.z, 0);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object other)
            {
                if (!(other is Vector3))
                {
                    return false;
                }

                return Equals((Vector3)other);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Vector3 other)
            {
                return x == other.x && y == other.y && z == other.z;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Vector3 lhs, Vector3 rhs)
            {
                float num = lhs.x - rhs.x;
                float num2 = lhs.y - rhs.y;
                float num3 = lhs.z - rhs.z;
                float num4 = num * num + num2 * num2 + num3 * num3;
                return num4 < 9.99999944E-11f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Vector3 lhs, Vector3 rhs)
            {
                return !(lhs == rhs);
            }

        }

        [Serializable]
        public struct Vector4 : IEquatable<Vector4>
        {

            public override string ToString() => $"({x}, {y}, {z}, {w})";

            public bool IsZero => (x == 0 && y == 0 && z == 0 && w == 0);

            public static Vector4 operator +(Vector4 vA, Vector4 vB) => new Vector4(vA.x + vB.x, vA.y + vB.y, vA.z + vB.z, vA.w + vB.w);
            public static Vector4 operator *(Vector4 v, float scalar) => new Vector4(v.x * scalar, v.y * scalar, v.z * scalar, v.w * scalar);
            public static Vector4 operator /(Vector4 v, float scalar) => new Vector4(v.x / scalar, v.y / scalar, v.z / scalar, v.w / scalar);

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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object other)
            {
                if (!(other is Vector4))
                {
                    return false;
                }

                return Equals((Vector4)other);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Vector4 other)
            {
                return x == other.x && y == other.y && z == other.z && w == other.w;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Vector4 lhs, Vector4 rhs)
            {
                float num = lhs.x - rhs.x;
                float num2 = lhs.y - rhs.y;
                float num3 = lhs.z - rhs.z;
                float num4 = lhs.w - rhs.w;
                float num5 = num * num + num2 * num2 + num3 * num3 + num4 * num4;
                return num5 < 9.99999944E-11f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Vector4 lhs, Vector4 rhs)
            {
                return !(lhs == rhs);
            }

        }

        [Serializable]
        public struct Quaternion : IEquatable<Quaternion>
        {

            #region Proxy Implementations

            public static Quaternion operator *(Quaternion qA, Quaternion qB) => swole.Engine.Mul(qA, qB);

            public static Quaternion Euler(Vector3 eulerAngles) => swole.Engine.Quaternion_Euler(eulerAngles);
            public static Quaternion Euler(float x, float y, float z) => swole.Engine.Quaternion_Euler(x, y, z);

            public static Quaternion Inverse(Quaternion q) => swole.Engine.Quaternion_Inverse(q);

            public static float Dot(Quaternion qA, Quaternion qB) => swole.Engine.Quaternion_Dot(qA, qB);

            public static Quaternion FromToRotation(Vector3 vA, Vector3 vB) => swole.Engine.Quaternion_FromToRotation(vA, vB);

            public static Quaternion LookRotation(Vector3 forward, Vector3 upward) => swole.Engine.Quaternion_LookRotation(forward, upward);

            public Vector3 EulerAngles => swole.Engine.Quaternion_EulerAngles(this);

            #endregion

            public override string ToString() => $"({x}, {y}, {z}, {w})";

            public static readonly Quaternion identity = new Quaternion(0, 0, 0, 1);

            public Quaternion inverse => swole.Engine.Quaternion_Inverse(this);

            public bool IsZero => (x == 0 && y == 0 && z == 0 && w == 0);

            public float x, y, z, w;

            public Quaternion(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }

            public static implicit operator Vector4(Quaternion q) => new(q.x, q.y, q.z, q.w);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override bool Equals(object other)
            {
                if (!(other is Quaternion))
                {
                    return false;
                }

                return Equals((Quaternion)other);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Quaternion other)
            {
                return x == other.x && y == other.y && z == other.z && w == other.w;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2) ^ (w.GetHashCode() >> 1);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsEqualUsingDot(float dot)
            {
                return dot > 0.999999f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(Quaternion lhs, Quaternion rhs)
            {
                return IsEqualUsingDot(Dot(lhs, rhs));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(Quaternion lhs, Quaternion rhs)
            {
                return !(lhs == rhs);
            }

        }

        [Serializable]
        public struct Matrix4x4
        {

            #region Proxy Implementations

            public static Matrix4x4 operator *(Matrix4x4 mA, Matrix4x4 mB) => swole.Engine.Mul(mA, mB);

            public static Matrix4x4 Inverse(Matrix4x4 m) => swole.Engine.Matrix4x4_Inverse(m);
            public static Matrix4x4 TRS(Vector3 position, Quaternion rotation, Vector3 scale) => swole.Engine.Matrix4x4_TRS(position, rotation, scale);
            public static Matrix4x4 Scale(Vector3 vector) => swole.Engine.Matrix4x4_Scale(vector);
            public static Matrix4x4 Translate(Vector3 vector) => swole.Engine.Matrix4x4_Translate(vector);
            public static Matrix4x4 Rotate(Quaternion q) => swole.Engine.Matrix4x4_Rotate(q);

            public Vector3 MultiplyPoint(Vector3 point) => swole.Engine.Mul(this, point);
            public Vector3 MultiplyPoint3x4(Vector3 point) => swole.Engine.Mul3x4(this, point);
            public Vector3 MultiplyVector(Vector3 vector) => swole.Engine.Rotate(this, vector);

            #endregion

            public Matrix4x4(Vector4 column0, Vector4 column1, Vector4 column2, Vector4 column3)
            {
                m00 = column0.x;
                m01 = column1.x;
                m02 = column2.x;
                m03 = column3.x;
                m10 = column0.y;
                m11 = column1.y;
                m12 = column2.y;
                m13 = column3.y;
                m20 = column0.z;
                m21 = column1.z;
                m22 = column2.z;
                m23 = column3.z;
                m30 = column0.w;
                m31 = column1.w;
                m32 = column2.w;
                m33 = column3.w;
            }
             
            public Matrix4x4(float m00, float m01, float m02, float m03, float m10, float m11, float m12, float m13, float m20, float m21, float m22, float m23, float m30, float m31, float m32, float m33)
            {
                this.m00 = m00;
                this.m01 = m01;
                this.m02 = m02;
                this.m03 = m03;
                this.m10 = m10;
                this.m11 = m11;
                this.m12 = m12;
                this.m13 = m13;
                this.m20 = m20;
                this.m21 = m21;
                this.m22 = m22;
                this.m23 = m23;
                this.m30 = m30;
                this.m31 = m31;
                this.m32 = m32;
                this.m33 = m33;
            }

            public float m00;

            public float m10;

            public float m20;

            public float m30;

            public float m01;

            public float m11;

            public float m21;

            public float m31;

            public float m02;

            public float m12;

            public float m22;

            public float m32;

            public float m03;

            public float m13;

            public float m23;

            public float m33;

            public bool IsZero => (
                m00 == 0 && m01 == 0 && m02 == 0 && m03 == 0 &&
                m10 == 0 && m11 == 0 && m12 == 0 && m13 == 0 &&
                m20 == 0 && m21 == 0 && m22 == 0 && m23 == 0 &&
                m30 == 0 && m31 == 0 && m32 == 0 && m33 == 0);

            private static readonly Matrix4x4 zeroMatrix = new Matrix4x4(new Vector4(0f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 0f), new Vector4(0f, 0f, 0f, 0f));

            private static readonly Matrix4x4 identityMatrix = new Matrix4x4(new Vector4(1f, 0f, 0f, 0f), new Vector4(0f, 1f, 0f, 0f), new Vector4(0f, 0f, 1f, 0f), new Vector4(0f, 0f, 0f, 1f));

            public float this[int row, int column]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this[row + column * 4];
                }
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                set
                {
                    this[row + column * 4] = value;
                }
            }

            public float this[int index]
            {
                get
                {
                    return index switch
                    {
                        0 => m00,
                        1 => m10,
                        2 => m20,
                        3 => m30,
                        4 => m01,
                        5 => m11,
                        6 => m21,
                        7 => m31,
                        8 => m02,
                        9 => m12,
                        10 => m22,
                        11 => m32,
                        12 => m03,
                        13 => m13,
                        14 => m23,
                        15 => m33,
                        _ => 0,
                    };
                }
                set
                {
                    switch (index)
                    {
                        case 0:
                            m00 = value;
                            break;
                        case 1:
                            m10 = value;
                            break;
                        case 2:
                            m20 = value;
                            break;
                        case 3:
                            m30 = value;
                            break;
                        case 4:
                            m01 = value;
                            break;
                        case 5:
                            m11 = value;
                            break;
                        case 6:
                            m21 = value;
                            break;
                        case 7:
                            m31 = value;
                            break;
                        case 8:
                            m02 = value;
                            break;
                        case 9:
                            m12 = value;
                            break;
                        case 10:
                            m22 = value;
                            break;
                        case 11:
                            m32 = value;
                            break;
                        case 12:
                            m03 = value;
                            break;
                        case 13:
                            m13 = value;
                            break;
                        case 14:
                            m23 = value;
                            break;
                        case 15:
                            m33 = value;
                            break;
                        default:
                            break;
                    }
                }
            }

            public static Matrix4x4 zero => zeroMatrix;

            public static Matrix4x4 identity
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return identityMatrix;
                }
            }

            public Matrix4x4 inverse => swole.Engine.Matrix4x4_Inverse(this);

            public Vector4 GetColumn(int index)
            {
                return index switch
                {
                    0 => new Vector4(m00, m10, m20, m30),
                    1 => new Vector4(m01, m11, m21, m31),
                    2 => new Vector4(m02, m12, m22, m32),
                    3 => new Vector4(m03, m13, m23, m33),
                    _ => Vector4.zero
                };
            }

            public Vector4 GetRow(int index)
            {
                return index switch
                {
                    0 => new Vector4(m00, m01, m02, m03),
                    1 => new Vector4(m10, m11, m12, m13),
                    2 => new Vector4(m20, m21, m22, m23),
                    3 => new Vector4(m30, m31, m32, m33),
                    _ => Vector4.zero
                };
            }

            public Vector3 GetPosition()
            {
                return new Vector3(m03, m13, m23);
            }

            public void SetColumn(int index, Vector4 column)
            {
                this[0, index] = column.x;
                this[1, index] = column.y;
                this[2, index] = column.z;
                this[3, index] = column.w;
            }

            public void SetRow(int index, Vector4 row)
            {
                this[index, 0] = row.x;
                this[index, 1] = row.y;
                this[index, 2] = row.z;
                this[index, 3] = row.w;
            }

        }

        #endregion

        #region RNG

        public struct RNG
        {
             
            [SwoleScriptIgnore]
            public object instance;

            public RNG(object instance)
            {
                this.instance = instance;
            }

            #region Proxy Implementation

            public static EngineInternal.RNG Global => swole.Engine.RNG_Global(); 

            public static EngineInternal.RNG New(int seed) => swole.Engine.RNG_New(seed);
            public static EngineInternal.RNG New(EngineInternal.RNGState initialState) => swole.Engine.RNG_New(initialState);
            public static EngineInternal.RNG New(EngineInternal.RNGState initialState, EngineInternal.RNGState currentState) => swole.Engine.RNG_New(initialState, currentState);

            public EngineInternal.RNG Reset() => swole.Engine.RNG_Reset(this);

            public EngineInternal.RNG Fork() => swole.Engine.RNG_Fork(this);

            public int Seed => swole.Engine.RNG_Seed(this);

            public EngineInternal.RNGState State => swole.Engine.RNG_State(this);

            public float NextValue => swole.Engine.RNG_NextValue(this);
            public bool NextBool => swole.Engine.RNG_NextBool(this);

            public EngineInternal.Vector4 NextColor => swole.Engine.RNG_NextColor(this);

            public EngineInternal.Quaternion NextRotation => swole.Engine.RNG_NextRotation(this);
            public EngineInternal.Quaternion NextRotationUniform => swole.Engine.RNG_NextRotationUniform(this);

            public float Range(float minInclusive = 0, float maxInclusive = 1) => swole.Engine.RNG_Range(this, minInclusive, maxInclusive);
            public int RangeInt(int minInclusive, int maxExclusive) => swole.Engine.RNG_RangeInt(this, minInclusive, maxExclusive);

            #endregion

            public static bool operator ==(RNG lhs, object rhs)
            {
                if (rhs is RNG ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(RNG lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

        }

        public struct RNGState
        {
            [SwoleScriptIgnore]
            public object instance;

            public RNGState(object instance)
            {
                this.instance = instance;
            }

            public static bool operator ==(RNGState lhs, object rhs)
            {
                if (rhs is RNGState ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(RNGState lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

        }

        #endregion

        #region Objects

        public interface IEngineObject : IVolatile
        {

            public string name { get; }

            public object Instance { get; }

            public int InstanceID { get; }

            public bool IsDestroyed { get; }

            public void Destroy(float timeDelay = 0);
            public void AdminDestroy(float timeDelay = 0);


            public bool HasEventHandler { get; }
            public IRuntimeEventHandler EventHandler { get; }


        }

        public struct EngineObject : IEngineObject
        {
            #region Proxy Implementations

            public string name => swole.Engine.GetName(instance);

            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            #endregion

            public object instance;
            public object Instance => instance;
            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public EngineObject(object instance)
            {
                this.instance = instance;
            }

            public static bool operator ==(EngineObject lhs, object rhs)
            {
                if (rhs is EngineObject ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(EngineObject lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

        }

        #region GameObjects

        public struct GameObject : IEngineObject
        {

            #region Proxy Implementations

            public string name => swole.Engine.GetName(instance);

            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);

            public static GameObject Create(string name = "") => swole.Engine.GameObject_Create(name);

            public static GameObject Instantiate(GameObject referenceObject) => swole.Engine.GameObject_Instantiate(referenceObject);

            public GameObject Instantiate() => Instantiate(this);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public void SetActive(bool active) => swole.Engine.GameObject_SetActive(this, active);

            public EngineInternal.IComponent GetComponent(Type type) => swole.Engine.GameObject_GetComponent(this, type);
            public EngineInternal.IComponent AddComponent(Type type) => swole.Engine.GameObject_AddComponent(this, type);

            #endregion

            public object instance;
            public object Instance => instance;
            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public ITransform transform;

            public GameObject(object instance, ITransform transform)
            {
                this.instance = instance;
                this.transform = transform;
            }

            public static bool operator ==(GameObject lhs, object rhs)
            {
                if (rhs is GameObject ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(GameObject lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

        }

        public struct SwoleGameObject : IEngineObject
        {
            #region Proxy Implementations

            public string name => instance.name;

            public int InstanceID => instance.InstanceID;

            public GameObject Instantiate() => instance.Instantiate();

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            #endregion

            public GameObject instance;
            public object Instance => instance.Instance;
            public bool IsDestroyed => instance.IsDestroyed;

            public ITransform transform => instance.transform;

            public int id;

            public SwoleGameObject(GameObject instance, int id)
            {
                this.instance = instance;
                this.id = id;
            }

            public static bool operator ==(SwoleGameObject lhs, object rhs)
            {
                if (rhs is SwoleGameObject ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(SwoleGameObject lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public bool HasEventHandler => instance.HasEventHandler;
            public IRuntimeEventHandler EventHandler => instance.EventHandler;

        }

        #endregion

        #endregion

        #region Components

        public static bool TryGetComponent<T>(EngineInternal.IEngineObject engObj, out T comp) where T : IComponent
        {
            comp = default;
            if (engObj is T)
            {
                comp = (T)engObj;
                return true;
            }
            else if (engObj is IComponent c)
            {
                engObj = c.baseGameObject;
            }

            if (engObj is EngineInternal.GameObject go)
            {
                var temp = go.GetComponent(typeof(T));
                if (temp is T) comp = (T)temp;
            }
            else if (engObj is EngineInternal.SwoleGameObject sgo)
            {
                var temp = sgo.instance.GetComponent(typeof(IAnimator));
                if (temp is T) comp = (T)temp;
            }

            return swole.IsNotNull(comp);
        }

        public interface IComponent : IEngineObject, IEngineComponentProxy
        {

            public EngineInternal.GameObject baseGameObject { get; }

        }

        #region Transforms

        public static bool TryGetTransform(EngineInternal.IEngineObject engObj, out EngineInternal.ITransform transform)
        {
            transform = null;
            if (engObj is EngineInternal.ITransform)
            {
                transform = (EngineInternal.ITransform)engObj;
            }
            else if (engObj is EngineInternal.GameObject go)
            {
                transform = go.transform;
            }
            else if (engObj is EngineInternal.SwoleGameObject sgo)
            {
                transform = sgo.transform;
            }
            else if (engObj is EngineInternal.IComponent comp)
            {
                transform = comp.baseGameObject.transform; 
            }

            return transform != null;
        }
        public interface ITransform : IComponent
        {

            public string ID { get; }

            public TransformEventHandler TransformEventHandler { get; }

            public int LastParent { get; set; }
            public Vector3 LastPosition { get; set; }
            public Quaternion LastRotation { get; set; }
            public Vector3 LastScale { get; set; }

            public ITransform parent { get; set; }

            public Vector3 position { get; set; }

            public Quaternion rotation { get; set; }

            public Vector3 lossyScale { get; }

            public Vector3 localPosition { get; set; }

            public Quaternion localRotation { get; set; }

            public Vector3 localScale { get; set; }

            public Matrix4x4 worldToLocalMatrix { get; }

            public Matrix4x4 localToWorldMatrix { get; }

            public int childCount { get; }

            public ITransform GetParent();

            public void SetParent(ITransform p);

            public void SetParent(ITransform parent, bool worldPositionStays);

            public void SetPositionAndRotation(Vector3 position, Quaternion rotation);

            public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation);

            public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation);

            public void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);

            public Vector3 TransformDirection(Vector3 direction);

            public Vector3 TransformDirection(float x, float y, float z);

            public Vector3 InverseTransformDirection(Vector3 direction);

            public Vector3 InverseTransformDirection(float x, float y, float z);

            public Vector3 TransformVector(Vector3 vector);

            public Vector3 TransformVector(float x, float y, float z);

            public Vector3 InverseTransformVector(Vector3 vector);

            public Vector3 InverseTransformVector(float x, float y, float z);

            public Vector3 TransformPoint(Vector3 position);

            public Vector3 TransformPoint(float x, float y, float z);

            public Vector3 InverseTransformPoint(Vector3 position);

            public Vector3 InverseTransformPoint(float x, float y, float z);

            public ITransform Find(string n);

            public bool IsChildOf(ITransform parent);

            public ITransform GetChild(int index);

        }

        public class Transform : ITransform
        {

            public Vector3 lastPosition;
            public Quaternion lastRotation;
            public Vector3 lastScale;
            public int lastParent;

            public Vector3 LastPosition
            {
                get => lastPosition;
                set => lastPosition = value;
            }
            public Quaternion LastRotation
            {
                get => lastRotation;
                set => lastRotation = value;
            }
            public Vector3 LastScale
            {
                get => lastScale;
                set => lastScale = value;
            }
            public int LastParent
            {
                get => lastParent;
                set => lastParent = value;
            }

            protected string id;
            public string ID => id;
            public Transform(string id, object instance)
            {
                this.instance = instance;
                this.id = id;

                lastPosition = position;
                lastRotation = rotation;
                lastScale = lossyScale;
                lastParent = 0;

                var par = parent;
                if (par != null) lastParent = swole.Engine.Object_GetInstanceID(par);
            }

            #region Proxy Implementations

            public Type EngineComponentType => instance is IEngineComponentProxy prox ? prox.EngineComponentType : null;

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);
            public EngineInternal.GameObject baseGameObject => swole.Engine.Component_gameObject(this);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public ITransform parent
            {
                get => swole.Engine.Transform_GetParent(this);
                set => swole.Engine.Transform_SetParent(this, value);
            }

            public Vector3 position
            {
                get => swole.Engine.GetWorldPosition(instance);
                set => swole.Engine.SetWorldPosition(instance, value);
            }

            public Quaternion rotation
            {
                get => swole.Engine.GetWorldRotation(instance);
                set => swole.Engine.SetWorldRotation(instance, value);
            }

            public Vector3 localPosition
            {
                get => swole.Engine.GetLocalPosition(instance);
                set => swole.Engine.SetLocalPosition(instance, value);
            }

            public Quaternion localRotation
            {
                get => swole.Engine.GetLocalRotation(instance);
                set => swole.Engine.SetLocalRotation(instance, value);
            }

            public Vector3 localScale
            {
                get => swole.Engine.GetLocalScale(instance);
                set => swole.Engine.SetLocalScale(instance, value);
            }

            public Vector3 lossyScale => swole.Engine.Transform_lossyScale(this);

            public Vector3 eulerAngles
            {
                get => swole.Engine.Transform_eulerAnglesGet(this);
                set => swole.Engine.Transform_eulerAnglesSet(this, value);
            }

            public Vector3 localEulerAngles
            {
                get => swole.Engine.Transform_localEulerAnglesGet(this);
                set => swole.Engine.Transform_localEulerAnglesSet(this, value);
            }

            public Vector3 right
            {
                get => swole.Engine.Transform_rightGet(this);
                set => swole.Engine.Transform_rightSet(this, value);
            }

            public Vector3 up
            {
                get => swole.Engine.Transform_upGet(this);
                set => swole.Engine.Transform_upSet(this, value);
            }

            public Vector3 forward
            {
                get => swole.Engine.Transform_forwardGet(this);
                set => swole.Engine.Transform_forwardSet(this, value);
            }

            public Matrix4x4 worldToLocalMatrix => swole.Engine.Transform_worldToLocalMatrix(this);

            public Matrix4x4 localToWorldMatrix => swole.Engine.Transform_localToWorldMatrix(this);

            public ITransform root => swole.Engine.Transform_root(this);

            public int childCount => swole.Engine.Transform_childCount(this);

            public bool hasChanged
            {
                get => swole.Engine.Transform_hasChangedGet(this);
                set => swole.Engine.Transform_hasChangedSet(this, value);
            }

            public int hierarchyCapacity
            {
                get => swole.Engine.Transform_hierarchyCapacityGet(this);
                set => swole.Engine.Transform_hierarchyCapacitySet(this, value);
            }

            public int hierarchyCount => swole.Engine.Transform_hierarchyCount(this);

            public ITransform GetParent() => swole.Engine.Transform_GetParent(this);

            public void SetParent(ITransform p) => swole.Engine.Transform_SetParent(this, p);

            public void SetParent(ITransform parent, bool worldPositionStays) => swole.Engine.Transform_SetParent(this, parent, worldPositionStays);

            public void SetPositionAndRotation(Vector3 position, Quaternion rotation) => swole.Engine.Transform_SetPositionAndRotation(this, position, rotation);

            public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation) => swole.Engine.Transform_SetLocalPositionAndRotation(this, localPosition, localRotation);

            public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation) => swole.Engine.Transform_GetPositionAndRotation(this, out position, out rotation);

            public void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation) => swole.Engine.Transform_GetLocalPositionAndRotation(this, out localPosition, out localRotation);

            public void Translate(Vector3 translation, Space relativeTo = Space.Self) => swole.Engine.Transform_Translate(this, translation, relativeTo);

            public void Translate(Vector3 translation) => swole.Engine.Transform_Translate(this, translation);

            public void Translate(float x, float y, float z, Space relativeTo = Space.Self) => swole.Engine.Transform_Translate(this, x, y, z, relativeTo);

            public void Translate(float x, float y, float z) => swole.Engine.Transform_Translate(this, x, y, z);

            public void Translate(Vector3 translation, ITransform relativeTo) => swole.Engine.Transform_Translate(this, translation, relativeTo);

            public void Translate(float x, float y, float z, ITransform relativeTo) => swole.Engine.Transform_Translate(this, x, y, z, relativeTo);

            public void Rotate(Vector3 eulers, Space relativeTo = Space.Self) => swole.Engine.Transform_Rotate(this, eulers, relativeTo);

            public void Rotate(Vector3 eulers) => swole.Engine.Transform_Rotate(this, eulers);

            public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo = Space.Self) => swole.Engine.Transform_Rotate(this, xAngle, yAngle, zAngle, relativeTo);

            public void Rotate(float xAngle, float yAngle, float zAngle) => swole.Engine.Transform_Rotate(this, xAngle, yAngle, zAngle);

            public void Rotate(Vector3 axis, float angle, Space relativeTo = Space.Self) => swole.Engine.Transform_Rotate(this, axis, angle, relativeTo);

            public void Rotate(Vector3 axis, float angle) => swole.Engine.Transform_Rotate(this, axis, angle);

            public void RotateAround(Vector3 point, Vector3 axis, float angle) => swole.Engine.Transform_RotateAround(this, point, axis, angle);

            public void LookAt(ITransform target, Vector3 worldUp) => swole.Engine.Transform_LookAt(this, target, worldUp);

            public void LookAt(ITransform target) => swole.Engine.Transform_LookAt(this, target);

            public void LookAt(Vector3 worldPosition, Vector3 worldUp) => swole.Engine.Transform_LookAt(this, worldPosition, worldUp);

            public void LookAt(Vector3 worldPosition) => swole.Engine.Transform_LookAt(this, worldPosition);

            public Vector3 TransformDirection(Vector3 direction) => swole.Engine.Transform_TransformDirection(this, direction);

            public Vector3 TransformDirection(float x, float y, float z) => swole.Engine.Transform_TransformDirection(this, x, y, z);

            public Vector3 InverseTransformDirection(Vector3 direction) => swole.Engine.Transform_InverseTransformDirection(this, direction);

            public Vector3 InverseTransformDirection(float x, float y, float z) => swole.Engine.Transform_InverseTransformDirection(this, x, y, z);

            public Vector3 TransformVector(Vector3 vector) => swole.Engine.Transform_TransformVector(this, vector);

            public Vector3 TransformVector(float x, float y, float z) => swole.Engine.Transform_TransformVector(this, x, y, z);

            public Vector3 InverseTransformVector(Vector3 vector) => swole.Engine.Transform_InverseTransformVector(this, vector);

            public Vector3 InverseTransformVector(float x, float y, float z) => swole.Engine.Transform_InverseTransformVector(this, x, y, z);

            public Vector3 TransformPoint(Vector3 position) => swole.Engine.Transform_TransformPoint(this, position);

            public Vector3 TransformPoint(float x, float y, float z) => swole.Engine.Transform_TransformPoint(this, x, y, z);

            public Vector3 InverseTransformPoint(Vector3 position) => swole.Engine.Transform_InverseTransformPoint(this, position);

            public Vector3 InverseTransformPoint(float x, float y, float z) => swole.Engine.Transform_InverseTransformPoint(this, x, y, z);

            public void DetachChildren() => swole.Engine.Transform_DetachChildren(this);

            public void SetAsFirstSibling() => swole.Engine.Transform_SetAsFirstSibling(this);

            public void SetAsLastSibling() => swole.Engine.Transform_SetAsLastSibling(this);

            public void SetSiblingIndex(int index) => swole.Engine.Transform_SetSiblingIndex(this, index);

            public int GetSiblingIndex() => swole.Engine.Transform_GetSiblingIndex(this);

            public ITransform Find(string n) => swole.Engine.Transform_Find(this, n);

            public bool IsChildOf(ITransform parent) => swole.Engine.Transform_IsChildOf(this, parent);

            public ITransform GetChild(int index) => swole.Engine.Transform_GetChild(this, index);

            #endregion

            protected object instance;
            public object Instance => instance;
            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            protected TransformEventHandler eventHandler;
            public bool HasEventHandler => eventHandler != null;
            public TransformEventHandler TransformEventHandler
            {
                get
                {
                    if (!IsDestroyed && (eventHandler == null || !eventHandler.IsValid))
                    {
                        eventHandler = new TransformEventHandler(this);
                    }
                    return eventHandler;
                }
            }
            public IRuntimeEventHandler EventHandler => TransformEventHandler;

            public static bool operator ==(Transform lhs, object rhs)
            {
                if (ReferenceEquals(lhs, null)) return rhs == null;
                if (rhs is Transform ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(Transform lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

        }

        public delegate void Vector3Delegate(Vector3 vec, bool isFinal);
        public delegate void QuaternionDelegate(Quaternion quat, bool isFinal);
        public delegate void TransformDelegate(ITransform t, bool isFinal);
        public class TransformEventHandler : IDisposable, IRuntimeEventHandler
        {

            public ITransform transform;
            public bool IsValid => transform != null;

            public TransformEventHandler(ITransform transform)
            {
                this.transform = transform;
            }

            public event Vector3Delegate onPositionChange;
            public event QuaternionDelegate onRotationChange;
            public event Vector3Delegate onScaleChange;
            public event TransformDelegate onParentChange;

            public event VoidParameterlessDelegate onChanged;
            public event VoidParameterlessDelegate onDestroy; 

            public void NotifyPositionChanged(bool isFinal)
            {
                var pos = transform.position;

                try
                {
                    OnPreEventCall(nameof(onPositionChange), 0);
                    onPositionChange?.Invoke(pos, isFinal);
                    onChanged?.Invoke();
                    OnPostEventCall(nameof(onPositionChange), 0);

                    for (int a = 0; a < transform.childCount; a++)
                    {
                        var child = transform.GetChild(a);
                        if (child == null || !child.HasEventHandler) continue;
                        child.TransformEventHandler.NotifyPositionChanged(isFinal);
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                transform.LastPosition = pos;
            }
            public void NotifyRotationChanged(bool isFinal)
            {
                var rot = transform.rotation;

                try
                {
                    OnPreEventCall(nameof(onRotationChange), 1);
                    onRotationChange?.Invoke(rot, isFinal);
                    onChanged?.Invoke();
                    OnPostEventCall(nameof(onRotationChange), 1);

                    for (int a = 0; a < transform.childCount; a++)
                    {
                        var child = transform.GetChild(a);
                        if (child == null || !child.HasEventHandler) continue;
                        child.TransformEventHandler.NotifyRotationChanged(isFinal);
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                transform.LastRotation = rot;
            }
            public void NotifyScaleChanged(bool isFinal)
            {
                var scale = transform.lossyScale;

                try
                {
                    OnPreEventCall(nameof(onScaleChange), 2);
                    onScaleChange?.Invoke(scale, isFinal);
                    onChanged?.Invoke();
                    OnPostEventCall(nameof(onScaleChange), 2);

                    for (int a = 0; a < transform.childCount; a++)
                    {
                        var child = transform.GetChild(a);
                        if (child == null || !child.HasEventHandler) continue;
                        child.TransformEventHandler.NotifyScaleChanged(isFinal);
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                transform.LastScale = scale;
            }
            public void NotifyParentChanged(bool isFinal)
            {
                var parent = transform.parent;
                int parentId = swole.Engine.Object_GetInstanceID(parent);

                try
                {
                    OnPreEventCall(nameof(onParentChange), 3);
                    onParentChange?.Invoke(parent, isFinal);
                    onChanged?.Invoke();
                    OnPostEventCall(nameof(onParentChange), 3);

                    for (int a = 0; a < transform.childCount; a++)
                    {
                        var child = transform.GetChild(a);
                        if (child == null || !child.HasEventHandler) continue;
                        child.TransformEventHandler.NotifyParentChanged(isFinal); 
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                transform.LastParent = parentId;
            }
            public void NotifyDestroyed()
            {
                try
                {
                    OnPreEventCall(nameof(onDestroy), 4);
                    onDestroy?.Invoke();
                    OnPostEventCall(nameof(onDestroy), 4);

                    ClearListeners();

                    for (int a = 0; a < transform.childCount; a++)
                    {
                        var child = transform.GetChild(a);
                        if (child == null || !child.HasEventHandler) continue;
                        child.TransformEventHandler.NotifyDestroyed();
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                Dispose();
            }

            public void ClearListeners()
            {
                onPositionChange = null;
                onRotationChange = null;
                onScaleChange = null;
                onParentChange = null;
                onChanged = null;
                onDestroy = null;
                if (runtimePreListeners != null) runtimePreListeners.Clear();
                runtimePreListeners = null;
                if (runtimePostListeners != null) runtimePostListeners.Clear();
                runtimePostListeners = null;
            }
            public void Dispose()
            {
                ClearListeners();
                transform = null;
            }


            protected List<RuntimeEventListenerDelegate> runtimePreListeners;
            protected void OnPreEventCall(string eventName, int id)
            {
                if (runtimePreListeners == null) return;
                foreach (var listener in runtimePreListeners) listener(eventName, id);
            }
            protected List<RuntimeEventListenerDelegate> runtimePostListeners;
            protected void OnPostEventCall(string eventName, int id)
            {
                if (runtimePostListeners == null) return;
                foreach (var listener in runtimePostListeners) listener(eventName, id);
            }
            public void SubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                if (runtimePreListeners == null) runtimePreListeners = new List<RuntimeEventListenerDelegate>(); else if (runtimePreListeners.Contains(listener)) return;
                runtimePreListeners.Add(listener);
            }
            public void UnsubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                if (runtimePreListeners == null) return;
                runtimePreListeners.Remove(listener);
            }
            public void SubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                if (runtimePostListeners == null) runtimePostListeners = new List<RuntimeEventListenerDelegate>(); else if (runtimePostListeners.Contains(listener)) return;
                runtimePostListeners.Add(listener);
            }
            public void UnsubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                if (runtimePostListeners == null) return;
                runtimePostListeners.Remove(listener);
            }
        }

        #endregion

        #region Cameras

        public struct Camera : IComponent
        {

            #region Proxy Implementation

            public Type EngineComponentType => instance is IEngineComponentProxy prox ? prox.EngineComponentType : null;

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);
            public EngineInternal.GameObject baseGameObject => swole.Engine.Component_gameObject(this);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public static Camera main => swole.Engine.Camera_main();

            public float fieldOfView
            {
                get => swole.Engine.Camera_fieldOfViewGet(this);
                set => swole.Engine.Camera_fieldOfViewSet(this, value);
            }

            public bool orthographic
            {
                get => swole.Engine.Camera_orthographicGet(this);
                set => swole.Engine.Camera_orthographicSet(this, value);
            }

            public float orthographicSize
            {
                get => swole.Engine.Camera_orthographicSizeGet(this);
                set => swole.Engine.Camera_orthographicSizeSet(this, value);
            }

            public float nearClipPlane
            {
                get => swole.Engine.Camera_nearClipPlaneGet(this);
                set => swole.Engine.Camera_nearClipPlaneSet(this, value);
            }

            public float farClipPlane
            {
                get => swole.Engine.Camera_farClipPlaneGet(this);
                set => swole.Engine.Camera_farClipPlaneSet(this, value);
            }

            #endregion

            public object instance;
            public object Instance => instance;

            public Camera(object instance)
            {
                this.instance = instance;
            }

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public static bool operator ==(Camera lhs, object rhs)
            {
                if (rhs is Camera ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(Camera lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

        }

        #endregion

        #region Audio
/*
        public struct AudioSource : IAudioSource
        {

            #region Proxy Implementation

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);
            public EngineInternal.GameObject baseGameObject => swole.Engine.Component_gameObject(this);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            #endregion

            public object instance;
            public object Instance => instance;

            public AudioSource(object instance)
            {
                this.instance = instance;
            }

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public static bool operator ==(AudioSource lhs, object rhs)
            {
                if (rhs is AudioSource ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AudioSource lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

        }

        public struct AudibleObject : IAudibleObject
        {

            #region Proxy Implementation

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);
            public EngineInternal.GameObject baseGameObject => swole.Engine.Component_gameObject(this);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);
            #endregion

            public object instance;
            public object Instance => instance;

            public AudibleObject(object instance)
            {
                this.instance = instance;
            }

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public static bool operator ==(AudibleObject lhs, object rhs)
            {
                if (rhs is AudibleObject ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AudibleObject lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

        }
    */
        #endregion

        #region Physics

        #endregion

        #endregion

        #region Tiles

        public struct Tile : ITile
        {

            #region ICloneable

            public object Clone() => instance == null ? this : new Tile((ITile)instance.Clone());

            #endregion

            #region ITile

            public IImageAsset PreviewTexture
            {
                get => instance == null ? null : instance.PreviewTexture;
                set
                {
                    if (instance == null) return;
                    instance.PreviewTexture = value;
                }
            }
            public SubModelID SubModelId
            {
                get => instance == null ? 0 : instance.SubModelId;
                set
                {
                    if (instance == null) return;
                    instance.SubModelId = value;
                }
            }
            public bool IsGameObject
            {
                get => instance == null ? false : instance.IsGameObject;
                set
                {
                    if (instance == null) return;
                    instance.IsGameObject = value;
                }
            }
            public bool CanToggleOffGameObject
            {
                get => instance == null ? false : instance.CanToggleOffGameObject;
                set
                {
                    if (instance == null) return;
                    instance.CanToggleOffGameObject = value;
                }
            }
            public Vector3 PositionOffset
            {
                get => instance == null ? Vector3.zero : instance.PositionOffset;
                set
                {
                    if (instance == null) return;
                    instance.PositionOffset = value;
                }
            }
            public Vector3 InitialRotationEuler
            {
                get => instance == null ? Vector3.zero : instance.InitialRotationEuler;
                set
                {
                    if (instance == null) return;
                    instance.InitialRotationEuler = value;
                }
            }
            public Vector3 InitialScale
            {
                get => instance == null ? Vector3.zero : instance.InitialScale;
                set
                {
                    if (instance == null) return;
                    instance.InitialScale = value;
                }
            }
            public GameObject PrefabBase
            {
                get => instance == null ? default : instance.PrefabBase;
                set
                {
                    if (instance == null) return;
                    instance.PrefabBase = value;
                }
            }

            #endregion

            public Tile(ITile instance)
            {
                this.instance = instance;
            }

            public ITile instance;
            public ITile Instance => instance;

            public bool RenderOnly => throw new NotImplementedException(); 

            public string Name => throw new NotImplementedException();

            public static bool operator ==(Tile lhs, object rhs)
            {
                if (rhs is Tile ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(Tile lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();  
            }

        }

        public struct TileSet : ITileSet
        {
             
            #region IEngineObject

            public string Name => instance == null ? string.Empty : instance.Name;
            public string name => Name;

            public int InstanceID => instance == null ? 0 : instance.InstanceID;

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public bool HasEventHandler => instance == null ? false : instance.HasEventHandler;
            public IRuntimeEventHandler EventHandler => instance == null ? null : instance.EventHandler;

            #endregion

            #region ITileSet

            public bool IgnoreMeshMasking
            {
                get
                {
                    if (instance == null) return false;
                    return instance.IgnoreMeshMasking;
                }
                set
                {
                    if (instance == null) return;
                    instance.IgnoreMeshMasking = value;
                }
            }

            public IMeshAsset TileMesh 
            { 
                get
                {
                    if (instance == null) return null;
                    return instance.TileMesh;
                }
                set
                {
                    if (instance == null) return;
                    instance.TileMesh = value;
                }
            }
            public IMaterialAsset TileMaterial
            {
                get
                {
                    if (instance == null) return null;
                    return instance.TileMaterial;
                }
                set
                {
                    if (instance == null) return;
                    instance.TileMaterial = value;
                }
            }
            public IMaterialAsset TileOutlineMaterial
            {
                get
                {
                    if (instance == null) return null;
                    return instance.TileOutlineMaterial;
                }
                set
                {
                    if (instance == null) return;
                    instance.TileOutlineMaterial = value;
                }
            }

            public int TileCount => instance == null ? 0 : instance.TileCount;
            public ITile this[int tileIndex] => instance == null ? default : instance[tileIndex];
            public ITile[] Tiles
            {
                get
                {
                    if (instance == null) return null;
                    return instance.Tiles;
                }
                set
                {
                    if (instance == null) return;
                    instance.Tiles = value;
                }
            }

            #endregion

            public TileSet(ITileSet instance)
            {
                this.instance = instance;
            }

            public ITileSet instance;
            public object Instance => instance;

            public static bool operator ==(TileSet lhs, object rhs) 
            {
                if (rhs is TileSet ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs; 
            }
            public static bool operator !=(TileSet lhs, object rhs) => !(lhs == rhs); 

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }
        }

        [Serializable]
        public struct TileInstance : ITileInstance
        {

            public Type EngineComponentType => IsDestroyed ? null : instance.EngineComponentType;

            public int InstanceID => instance == null ? 0 : instance.InstanceID;
            public EngineInternal.GameObject baseGameObject => instance == null ? default : instance.baseGameObject;

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public int SwoleId
            {
                get => IsDestroyed ? -1 : instance.SwoleId;
                set
                {
                    if (IsDestroyed) return;
                    instance.SwoleId = value;
                }
            }

            public string ID => IsDestroyed ? null : instance.ID;

            public string TileSetId => IsDestroyed ? string.Empty : instance.TileSetId;
            public int TileIndex => IsDestroyed ? -1 : instance.TileIndex;

            public bool IsRenderOnly => IsDestroyed ? true : instance.IsRenderOnly; 

            public bool visible
            {
                get
                {
                    if (IsDestroyed) return false;
                    return instance.IsDestroyed;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.visible = value;
                }
            }

            public TileInstance(ITileInstance instance)
            {
                this.instance = instance;
            }

            [NonSerialized]
            public ITileInstance instance;
            public bool IsDestroyed => swole.Engine.IsNull(Instance);
            public void Dispose()
            {
                if (IsDestroyed) return;
                instance.Dispose();
                instance = null;   
            }

            public void ForceUseRealTransform()
            {
                if (IsDestroyed) return;
                instance.ForceUseRealTransform();
            }
            public void ReevaluateRendering()
            {
                if (IsDestroyed) return;
                instance.ReevaluateRendering();
            }

            public GameObject Root => IsDestroyed ? default : instance.Root;

            public object Instance => instance == null ? null : instance.Instance;

            public bool HasEventHandler => IsDestroyed ? false : instance.HasEventHandler;

            public TransformEventHandler TransformEventHandler => IsDestroyed ? null : instance.TransformEventHandler;

            public IRuntimeEventHandler EventHandler => TransformEventHandler;

            public int LastParent { get => IsDestroyed ? 0 : instance.LastParent;
                set { if (!IsDestroyed) instance.LastParent = value; } }
            public Vector3 LastPosition { get => IsDestroyed ? default : instance.LastPosition;
                set { if (!IsDestroyed) instance.LastPosition = value; }
            }
            public Quaternion LastRotation { get => IsDestroyed ? default : instance.LastRotation;
                set { if (!IsDestroyed) instance.LastRotation = value; }
            }
            public Vector3 LastScale { get => IsDestroyed ? default : instance.LastScale;
                set { if (!IsDestroyed) instance.LastScale = value; }
            }

            public string name => IsDestroyed ? string.Empty : instance.name;

            public ITransform parent { get => IsDestroyed ? default : instance.parent;
                set { if (!IsDestroyed) instance.parent = value; }
            }
            public Vector3 position { get => IsDestroyed ? default : instance.position;
                set { if (!IsDestroyed) instance.position = value; }
            }
            public Quaternion rotation { get => IsDestroyed ? default : instance.rotation;
                set { if (!IsDestroyed) instance.rotation = value; }
            }
            public Vector3 lossyScale
            {
                get => IsDestroyed ? default : instance.lossyScale;
            }
            public Vector3 localPosition { get => IsDestroyed ? default : instance.localPosition;
                set { if (!IsDestroyed) instance.localPosition = value; }
            }
            public Quaternion localRotation { get => IsDestroyed ? default : instance.localRotation;
                set { if (!IsDestroyed) instance.localRotation = value; }
            }
            public Vector3 localScale { get => IsDestroyed ? default : instance.localScale; 
                set { if (!IsDestroyed) instance.localScale = value; }
            }

            public Matrix4x4 worldToLocalMatrix => IsDestroyed ? Matrix4x4.identity : instance.worldToLocalMatrix;
            public Matrix4x4 localToWorldMatrix => IsDestroyed ? Matrix4x4.identity : instance.localToWorldMatrix;

            public int childCount => IsDestroyed ? 0 : instance.childCount;

            public static bool operator ==(TileInstance lhs, object rhs)
            {
                if (rhs is TileInstance ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(TileInstance lhs, object rhs) => !(lhs == rhs);

            public static implicit operator GameObject(TileInstance inst) => inst.Root;

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public ITransform GetParent() => IsDestroyed ? default : instance.GetParent();

            public void SetParent(ITransform p)
            {
                if (!IsDestroyed) instance.SetParent(p);
            }

            public void SetParent(ITransform parent, bool worldPositionStays)
            {
                if (!IsDestroyed) instance.SetParent(parent, worldPositionStays);
            }

            public void SetParent(EngineInternal.ITransform parent, bool worldPositionStays, bool forceRealTransformConversion)
            {
                if (!IsDestroyed) instance.SetParent(parent, worldPositionStays, forceRealTransformConversion);
            }

            public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
            {
                if (!IsDestroyed) instance.SetPositionAndRotation(position, rotation);
            }

            public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation)
            {
                if (!IsDestroyed) instance.SetLocalPositionAndRotation(localPosition, localRotation); 
            }

            public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
            {
                position = default;
                rotation = default;
                if (!IsDestroyed) instance.GetPositionAndRotation(out position, out rotation);
            }

            public void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation)
            {
                localPosition = default;
                localRotation = default;
                if (!IsDestroyed) instance.GetLocalPositionAndRotation(out localPosition, out localRotation);
            }

            public Vector3 TransformDirection(Vector3 direction) => IsDestroyed ? default : instance.TransformDirection(direction); 

            public Vector3 TransformDirection(float x, float y, float z) => IsDestroyed ? default : instance.TransformDirection(x,y,z);

            public Vector3 InverseTransformDirection(Vector3 direction) => IsDestroyed ? default : instance.InverseTransformDirection(direction);

            public Vector3 InverseTransformDirection(float x, float y, float z) => IsDestroyed ? default : instance.InverseTransformDirection(x, y, z);

            public Vector3 TransformVector(Vector3 vector) => IsDestroyed ? default : instance.TransformVector(vector);

            public Vector3 TransformVector(float x, float y, float z) => IsDestroyed ? default : instance.TransformVector(x, y, z);

            public Vector3 InverseTransformVector(Vector3 vector) => IsDestroyed ? default : instance.InverseTransformVector(vector);

            public Vector3 InverseTransformVector(float x, float y, float z) => IsDestroyed ? default : instance.InverseTransformVector(x, y, z);

            public Vector3 TransformPoint(Vector3 position) => IsDestroyed ? default : instance.TransformPoint(position);

            public Vector3 TransformPoint(float x, float y, float z) => IsDestroyed ? default : instance.TransformPoint(x, y, z);

            public Vector3 InverseTransformPoint(Vector3 position) => IsDestroyed ? default : instance.InverseTransformPoint(position);

            public Vector3 InverseTransformPoint(float x, float y, float z) => IsDestroyed ? default : instance.InverseTransformPoint(x, y, z);

            public ITransform Find(string n) => IsDestroyed ? default : instance.Find(n);

            public bool IsChildOf(ITransform parent) => IsDestroyed ? false : instance.IsChildOf(parent);

            public ITransform GetChild(int index) => IsDestroyed ? default : instance.GetChild(index); 

        }

        #endregion

        #region Creations

        public static bool TryGetCreationInstance(EngineInternal.IEngineObject engObj, out ICreationInstance creation) => TryGetComponent<ICreationInstance>(engObj, out creation);    
        [Serializable]
        public struct CreationInstance : ICreationInstance
        {

            public void Dispose()
            {
                if (IsDestroyed) return;
                instance.Dispose();
            }

            public Type EngineComponentType => IsDestroyed ? null : instance.EngineComponentType;

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);
            public EngineInternal.GameObject baseGameObject => IsDestroyed ? default : instance.baseGameObject;

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public CreationInstance(ICreationInstance instance)
            {
                this.instance = instance;
            }

            [NonSerialized]
            public ICreationInstance instance;
            public object Instance => instance == null ? null : instance.Instance;

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public Creation Asset => IsDestroyed ? null : instance.Asset;
            public PackageIdentifier Package => IsDestroyed ? default : instance.Package;
            public ContentPackage LocalContent => IsDestroyed ? default : instance.LocalContent;
            public string AssetName => IsDestroyed ? null : instance.AssetName;

            public GameObject Root => IsDestroyed ? default : instance.Root;
            public ICreationInstance RootCreation => IsDestroyed ? default : instance.RootCreation;
            public ExecutableBehaviour Behaviour => IsDestroyed ? null : instance.Behaviour;

            public PermissionScope Scope 
            { 
                
                get { return IsDestroyed? PermissionScope.None: instance.Scope; }
                set
                {
                    if (IsDestroyed) return;
                    instance.Scope = value;
                }

            }
            public string Identifier => IsDestroyed ? string.Empty : instance.Identifier;
            public object HostData => IsDestroyed ? null : instance.HostData;

            public bool IsInitialized => IsDestroyed ? false : instance.IsInitialized;

            public bool IsExecuting => IsDestroyed ? false : instance.IsExecuting;

            public static bool operator ==(CreationInstance lhs, object rhs)
            {
                if (rhs is CreationInstance ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(CreationInstance lhs, object rhs) => !(lhs == rhs);

            public static implicit operator GameObject(CreationInstance inst) => inst.Root;

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode(); 
            }

            public bool TryGetEnvironmentVar(string name, out IVar envVar)
            {
                envVar = default;
                if (IsDestroyed) return false;

                return instance.TryGetEnvironmentVar(name, out envVar);
            }

            public GameObject FindGameObject(string name)
            {
                if (IsDestroyed) return default;
                return instance.FindGameObject(name);
            }

            public SwoleGameObject FindSwoleGameObject(int id)
            {
                if (IsDestroyed) return default;
                return instance.FindSwoleGameObject(id);
            }

            public EngineInternal.SwoleGameObject FindSwoleGameObject(string ids)
            {
                if (IsDestroyed) return default;
                return instance.FindSwoleGameObject(ids);
            }

            public bool Initialize(bool startExecuting = true)
            {
                if (IsDestroyed) return false;
                return instance.Initialize(startExecuting);
            }

            public bool StartExecuting()
            {
                if (IsDestroyed) return false;
                return instance.StartExecuting();
            }

            public void StopExecuting()
            {
                if (IsDestroyed) return;
                instance.StopExecuting();
            }

            public IRuntimeEnvironment Environment => IsDestroyed ? null : instance;
            public string EnvironmentName { get => IsDestroyed ? null : instance.EnvironmentName; set { if (!IsDestroyed) instance.EnvironmentName = value; } }
            public bool IsValid => IsDestroyed ? false : instance.IsValid;
#if SWOLE_ENV
            public void SetLocalVar(string identifier, Value value)
            {
                if (IsDestroyed) return;
                instance.SetLocalVar(identifier, value);
            }
            public bool TryGetLocalVar(string identifier, out Value value)
            {
                value = null;
                if (IsDestroyed) return false;
                return instance.TryGetLocalVar(identifier, out value);
            }
            public Value GetLocalVar(string identifier)
            {
                if (IsDestroyed) return null;
                return instance.GetLocalVar(identifier);
            }
            public ExecutionResult RunForPeriod(SwoleLogger logger, Interpreter interpreter, float timeOut = 0.01F, bool restartIfNotRunning = true, bool setGlobalVars = true)
            {
                if (IsDestroyed) return ExecutionResult.InvalidEnvironment;
                return instance.RunForPeriod(logger, interpreter, timeOut, restartIfNotRunning, setGlobalVars);
            }
            public ExecutionResult RunUntilCompletion(SwoleLogger logger, Interpreter interpreter, float timeOut = 10, bool restart = true, bool setGlobalVars = true)
            {
                if (IsDestroyed) return ExecutionResult.InvalidEnvironment;
                return instance.RunUntilCompletion(logger, interpreter, timeOut, restart, setGlobalVars);
            }
#endif

            public void TrackEventListener(RuntimeEventListener listener)
            {
                if (IsDestroyed) return;
                instance.TrackEventListener(listener);
            }
            public bool UntrackEventListener(RuntimeEventListener listener)
            {
                if (IsDestroyed) return false;
                return instance.UntrackEventListener(listener); 
            }

            public bool UntrackEventListener(string trackerId)
            {
                if (IsDestroyed) return false;
                return instance.UntrackEventListener(trackerId);
            }

            public RuntimeEventListener FindPreEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
            {
                if (IsDestroyed) return null;
                return instance.FindPreEventListener(_delegate, handler);
            }

            public RuntimeEventListener FindPostEventListener(RuntimeEventListenerDelegate _delegate, IRuntimeEventHandler handler)
            {
                if (IsDestroyed) return null;
                return instance.FindPostEventListener(_delegate, handler);
            }

            public RuntimeEventListener FindPreEventListener(ValFunction function, IRuntimeEventHandler handler)
            {
                if (IsDestroyed) return null;
                return instance.FindPreEventListener(function, handler);
            }

            public RuntimeEventListener FindPostEventListener(ValFunction function, IRuntimeEventHandler handler)
            {
                if (IsDestroyed) return null;
                return instance.FindPostEventListener(function, handler);
            }

            public void ListenForQuit(VoidParameterlessDelegate listener)
            {
                if (IsDestroyed) return;
                instance.ListenForQuit(listener);
            }

            public void StopListeningForQuit(VoidParameterlessDelegate listener)
            {
                if (IsDestroyed) return;
                instance.StopListeningForQuit(listener);
            }

            public bool TryGetReferencePackage(PackageIdentifier pkgId, out ContentPackage package)
            {
                package = null;
                if (IsDestroyed) return false;
                return instance.TryGetReferencePackage(pkgId, out package);
            }
            public bool TryGetReferencePackage(string pkgString, out ContentPackage package)
            {
                package = null;
                if (IsDestroyed) return false;
                return instance.TryGetReferencePackage(pkgString, out package);
            }

            public SwoleCancellationToken GetNewCancellationToken()
            {
                if (IsDestroyed) return null;
                return instance.GetNewCancellationToken();
            }

            public void RemoveToken(SwoleCancellationToken token)
            {
                if (IsDestroyed) return;
                instance.RemoveToken(token);
            }

            public void CancelAllTokens()
            {
                if (IsDestroyed) return;
                instance.CancelAllTokens(); 
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance); 

        }

        #endregion

        #region Animation

        public static bool TryGetAnimator(EngineInternal.IEngineObject engObj, out IAnimator animator) => TryGetComponent<IAnimator>(engObj, out animator);
        public struct Animator : IAnimator
        {
            #region Proxy Implementation

            public Type EngineComponentType => IsDestroyed ? null : instance.EngineComponentType;

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);
            public EngineInternal.GameObject baseGameObject => swole.Engine.Component_gameObject(this);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            #endregion

            public IAnimator instance;
            public object Instance => instance;

            public Animator(IAnimator instance)
            {
                this.instance = instance;
            }

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public static bool operator ==(Animator lhs, object rhs)
            {
                if (rhs is Animator ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(Animator lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            public void Dispose()
            {
                if (IsDestroyed) return;
                instance.Dispose();
            }

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

            #region IAnimator

            public void Reinitialize()
            {
                if (IsDestroyed) return;
                instance.Reinitialize(); 
            }

            public void ApplyController(IAnimationController controller, bool usePrefix = true, bool incrementDuplicateParameters = false)
            {
                if (IsDestroyed) return;
                instance.ApplyController(controller, usePrefix, incrementDuplicateParameters);
            }

            public bool HasControllerData(IAnimationController controller)
            {
                if (IsDestroyed) return false;
                return instance.HasControllerData(controller);
            }

            public bool HasControllerData(string prefix)
            {
                if (IsDestroyed) return false;
                return instance.HasControllerData(prefix);
            }

            public void RemoveControllerData(IAnimationController controller)
            {
                if (IsDestroyed) return;
                instance.RemoveControllerData(controller);
            }

            public void RemoveControllerData(string prefix)
            {
                if (IsDestroyed) return;
                instance.RemoveControllerData(prefix);
            }

            public int GetBoneIndex(string name)
            {
                if (IsDestroyed) return -1;
                return instance.GetBoneIndex(name);
            }

            public int BoneCount => IsDestroyed ? 0 : instance.BoneCount;

            public EngineInternal.ITransform GetBone(int index)
            {
                if (IsDestroyed) return null;
                return instance.GetBone(index);
            }

            public void ClearControllerData()
            {
                if (IsDestroyed) return;
                instance.ClearControllerData();
            }

            public void SetOverrideUpdateCalls(bool value)
            {
                if (IsDestroyed) return;
                instance.SetOverrideUpdateCalls(value);
            }

            public IAnimationParameter GetParameter(int index)
            {
                if (IsDestroyed) return null;
                return instance.GetParameter(index);
            }

            public void AddParameter(IAnimationParameter parameter, bool initialize = true, object initObject = null, List<IAnimationParameter> outList = null, bool onlyOutputNew = false)
            {
                if (IsDestroyed) return;
                instance.AddParameter(parameter, initialize, initObject, outList, onlyOutputNew);
            }

            public void AddParameters(ICollection<IAnimationParameter> toAdd, bool initialize = true, object initObject = null, List<IAnimationParameter> outList = null, bool onlyOutputNew = false)
            {
                if (IsDestroyed) return;
                instance.AddParameters(toAdd, initialize, initObject, outList, onlyOutputNew);
            }

            public bool RemoveParameter(IAnimationParameter parameter)
            {
                if (IsDestroyed) return false;
                return instance.RemoveParameter(parameter);
            }

            public bool RemoveParameter(int index)
            {
                if (IsDestroyed) return false;
                return instance.RemoveParameter(index);
            }

            public int RemoveParametersStartingWith(string prefix)
            {
                if (IsDestroyed) return 0;
                return instance.RemoveParametersStartingWith(prefix);
            }

            public int FindParameterIndex(string name)
            {
                if (IsDestroyed) return -1;
                return instance.FindParameterIndex(name);
            }

            public IAnimationParameter FindParameter(string name, out int parameterIndex)
            {
                parameterIndex = -1;
                if (IsDestroyed) return null;
                return instance.FindParameter(name, out parameterIndex);
            }

            public IAnimationParameter FindParameter(string name)
            {
                if (IsDestroyed) return null;
                return instance.FindParameter(name);
            }

            public Dictionary<int, int> RecalculateParameterIndices()
            {
                if (IsDestroyed) return null;
                return instance.RecalculateParameterIndices();
            }

            public void AddLayer(IAnimationLayer layer, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
            {
                if (IsDestroyed) return;
                instance.AddLayer(layer, instantiate, prefix, outList, onlyOutputNew, animationController);
            }

            public void InsertLayer(int index, IAnimationLayer layer, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
            {
                if (IsDestroyed) return;
                instance.InsertLayer(index, layer, instantiate, prefix, outList, onlyOutputNew, animationController);
            }

            public void AddLayers(ICollection<IAnimationLayer> toAdd, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null)
            {
                if (IsDestroyed) return;
                instance.AddLayers(toAdd, instantiate, prefix, outList, onlyOutputNew, animationController);
            }

            public int FindLayerIndex(string layerName)
            {
                if (IsDestroyed) return -1;
                return instance.FindLayerIndex(layerName);
            }

            public IAnimationLayer FindLayer(string layerName)
            {
                if (IsDestroyed) return null;
                return instance.FindLayer(layerName);
            }

            public bool RemoveLayer(IAnimationLayer layer)
            {
                if (IsDestroyed) return false;
                return instance.RemoveLayer(layer);
            }

            public bool RemoveLayer(int layerIndex)
            {
                if (IsDestroyed) return false;
                return instance.RemoveLayer(layerIndex);
            }

            public bool RemoveLayer(string layerName)
            {
                if (IsDestroyed) return false;
                return instance.RemoveLayer(layerName);
            }

            public int RemoveLayersStartingWith(string prefix)
            {
                if (IsDestroyed) return 0;
                return instance.RemoveLayersStartingWith(prefix);
            }

            public Dictionary<int, int> RearrangeLayer(int layerIndex, int swapIndex, bool recalculateIndices = true)
            {
                if (IsDestroyed) return null;
                return instance.RearrangeLayer(layerIndex, swapIndex, recalculateIndices);
            }

            public void RearrangeLayerNoRemap(int layerIndex, int swapIndex, bool recalculateIndices = true)
            {
                if (IsDestroyed) return;
                instance.RearrangeLayerNoRemap(layerIndex, swapIndex, recalculateIndices);
            }

            public Dictionary<int, int> RecalculateLayerIndices()
            {
                if (IsDestroyed) return null;
                return instance.RecalculateLayerIndices();
            }

            public void RecalculateLayerIndicesNoRemap()
            {
                if (IsDestroyed) return;
                instance.RecalculateLayerIndicesNoRemap();
            }

            public bool IsLayerActive(int index)
            {
                if (IsDestroyed) return false;
                return instance.IsLayerActive(index);
            }

            public void UpdateStep(float deltaTime)
            {
                if (IsDestroyed) return;
                instance.UpdateStep(deltaTime);
            }

            public void LateUpdateStep(float deltaTime)
            {
                if (IsDestroyed) return;
                instance.LateUpdateStep(deltaTime);
            }

            public TransformHierarchy GetTransformHierarchy(int index)
            {
                if (IsDestroyed) return null;
                return instance.GetTransformHierarchy(index);
            }

            public TransformHierarchy GetTransformHierarchyUnsafe(int index)
            {
                if (IsDestroyed) return null;
                return instance.GetTransformHierarchyUnsafe(index);
            }

            public TransformHierarchy GetTransformHierarchy(int[] transformIndices)
            {
                if (IsDestroyed) return null;
                return instance.GetTransformHierarchy(transformIndices);
            }

            public int GetTransformIndex(ITransform transform)
            {
                if (IsDestroyed) return -1;
                return instance.GetTransformIndex(transform);
            }

            public IAnimationController DefaultController
            {

                get
                {
                    if (IsDestroyed) return null;
                    return instance.DefaultController;
                }

                set
                {
                    if (IsDestroyed) return;
                    instance.DefaultController = value;
                }

            }

            public string AvatarName => IsDestroyed ? string.Empty : instance.AvatarName;

            public int AffectedTransformCount => IsDestroyed ? 0 : instance.AffectedTransformCount;

            public bool UseDynamicBindPose
            {

                get
                {
                    if (IsDestroyed) return false;
                    return instance.UseDynamicBindPose;
                }

                set
                {
                    if (IsDestroyed) return;
                    instance.UseDynamicBindPose = value;
                }

            }
            public bool DisableMultithreading
            {

                get
                {
                    if (IsDestroyed) return false;
                    return instance.DisableMultithreading;
                }

                set
                {
                    if (IsDestroyed) return;
                    instance.DisableMultithreading = value;
                }

            }
            public bool OverrideUpdateCalls
            {

                get
                {
                    if (IsDestroyed) return false;
                    return instance.OverrideUpdateCalls;
                }

                set
                {
                    if (IsDestroyed) return;
                    instance.OverrideUpdateCalls = value;
                }

            }
            public bool ForceFinalTransformUpdate
            {

                get
                {
                    if (IsDestroyed) return false;
                    return instance.ForceFinalTransformUpdate;
                }

                set
                {
                    if (IsDestroyed) return;
                    instance.ForceFinalTransformUpdate = value;
                }

            }

            public int TransformHierarchyCount => IsDestroyed ? 0 : instance.TransformHierarchyCount;

            #endregion

        } 
        
        public struct Animation : IAnimationAsset
        {

            #region IContent

            public PackageInfo PackageInfo => instance == null ? default : instance.PackageInfo;

            public ContentInfo ContentInfo => instance == null ? default : instance.ContentInfo;

            public string Name => instance == null ? default : instance.Name;

            public string Author => instance == null ? default : instance.Author;

            public string CreationDate => instance == null ? default : instance.CreationDate;

            public string LastEditDate => instance == null ? default : instance.LastEditDate;

            public string Description => instance == null ? default : instance.Description;

            public string OriginPath => instance == null ? default : instance.OriginPath;

            public string RelativePath => instance == null ? default : instance.RelativePath;

            public IContent CreateCopyAndReplaceContentInfo(ContentInfo info)
            {
                if (instance == null) return default;
                var copy = this;
                copy.instance = (IAnimationAsset)instance.CreateCopyAndReplaceContentInfo(info); 
                return copy;
            }

            public IContent SetOriginPath(string path)
            {
                if (instance == null) return default;
                var copy = this;
                copy.instance = (IAnimationAsset)instance.SetOriginPath(path);
                return copy;
            }

            public IContent SetRelativePath(string path)
            {
                if (instance == null) return default;
                var copy = this;
                copy.instance = (IAnimationAsset)instance.SetRelativePath(path);
                return copy;
            }

            public List<PackageIdentifier> ExtractPackageDependencies(List<PackageIdentifier> dependencies = null)
            {
                if (instance == null) return dependencies == null ? new List<PackageIdentifier>() : dependencies;
                return instance.ExtractPackageDependencies(dependencies);
            }

            #endregion

            #region ICloneable

            public object Clone()
            {
                var copy = this;
                copy.instance = (IAnimationAsset)instance.Clone();
                return copy;
            }

            #endregion

            public IAnimationAsset instance;
            public object Instance => instance;

            public Animation(IAnimationAsset instance)
            {
                this.instance = instance; 
            }

            public static bool operator ==(Animation lhs, object rhs)
            {
                if (rhs is Animation ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(Animation lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            #region IAnimationAsset

            public string ID => instance == null ? string.Empty : instance.ID;
            public bool HasKeyframes => instance == null ? false : instance.HasKeyframes;

            public float GetClosestKeyframeTime(float referenceTime, bool includeReferenceTime = true, IntFromDecimalDelegate getFrameIndex = null)
            {
                if (instance == null) return 0;
                return instance.GetClosestKeyframeTime(referenceTime, includeReferenceTime, getFrameIndex);
            }

            #endregion
        }

        
        public struct AnimationLayer : IAnimationLayer
        {

            #region IDisposable

            public void Dispose()
            {
                if (instance == null) return;
                instance.Dispose();
            }

            #endregion

            public IAnimationLayer instance;
            public object Instance => instance;

            public AnimationLayer(IAnimationLayer instance)
            {
                this.instance = instance;
            }

            public static bool operator ==(AnimationLayer lhs, object rhs)
            {
                if (rhs is AnimationLayer ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AnimationLayer lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            #region IAnimationLayer

            public bool Valid => instance == null ? false : instance.Valid;

            public IAnimator Animator => instance == null ? null : instance.Animator;

            public string Name
            {
                get
                {
                    if (instance == null) return string.Empty;
                    return instance.Name;
                }
                set
                {
                    if (instance == null) return;
                    instance.Name = value;
                }
            }
            public int IndexInAnimator
            {
                get
                {
                    if (instance == null) return -1;
                    return instance.IndexInAnimator;
                }
                set
                {
                    if (instance == null) return;
                    instance.IndexInAnimator = value;
                }
            }
            public bool IsAdditive
            {
                get
                {
                    if (instance == null) return false;
                    return instance.IsAdditive;
                }
                set
                {
                    if (instance == null) return;
                    instance.IsAdditive = value;
                }
            }
            public float Mix
            {
                get
                {
                    if (instance == null) return 0;
                    return instance.Mix;
                }
                set
                {
                    if (instance == null) return;
                    instance.Mix = value;
                }
            }
            public bool Deactivate
            {
                get
                {
                    if (instance == null) return false;
                    return instance.Deactivate;
                }
                set
                {
                    if (instance == null) return;
                    instance.Deactivate = value;
                }
            }

            public bool IsActive => instance == null ? false : instance.IsActive;

            public int BlendParameterIndex
            {
                get
                {
                    if (instance == null) return -1;
                    return instance.BlendParameterIndex;
                }
                set
                {
                    if (instance == null) return;
                    instance.BlendParameterIndex = value;
                }
            }
            public MotionControllerIdentifier[] MotionControllerIdentifiers
            {
                get
                {
                    if (instance == null) return null;
                    return instance.MotionControllerIdentifiers;
                }
                set
                {
                    if (instance == null) return;
                    instance.MotionControllerIdentifiers = value;
                }
            }

            public int ControllerCount => instance == null ? 0 : instance.ControllerCount;

            public bool IsPrototype => instance == null ? false : instance.IsPrototype;

            public int EntryStateIndex
            {
                get
                {
                    if (instance == null) return -1;
                    return instance.EntryStateIndex;
                }
                set
                {
                    if (instance == null) return;
                    instance.EntryStateIndex = value;
                }
            }
            public IAnimationStateMachine[] StateMachines
            {
                get
                {
                    if (instance == null) return null;
                    return instance.StateMachines;
                }
                set
                {
                    if (instance == null) return;
                    instance.StateMachines = value;
                }
            }

            public int StateCount => instance == null ? 0 : instance.StateCount;

            public int ActiveStateIndex => instance == null ? -1 : instance.ActiveStateIndex;

            public IAnimationStateMachine ActiveState => instance == null ? null : instance.ActiveState;

            public bool HasActiveState => instance == null ? false : instance.HasActiveState;

            public bool DisposeIfHasPrefix(string prefix)
            {
                if (instance == null) return false;
                return instance.DisposeIfHasPrefix(prefix);
            }

            public IAnimationLayer NewInstance(IAnimator animator, IAnimationController animationController = null)
            {
                if (instance == null) return null;
                return instance.NewInstance(animator, animationController);
            }

            public Dictionary<int, int> Rearrange(int swapIndex, bool recalculateIndices = true)
            {
                if (instance == null) return null;
                return instance.Rearrange(swapIndex, recalculateIndices);
            }

            public void RearrangeNoRemap(int swapIndex, bool recalculateIndices = true)
            {
                if (instance == null) return;
                instance.RearrangeNoRemap(swapIndex, recalculateIndices);
            }

            public void SetAdditive(bool isAdditiveLayer)
            {
                if (instance == null) return;
                instance.SetAdditive(isAdditiveLayer);
            }

            public void SetActive(bool active)
            {
                if (instance == null) return;
                instance.SetActive(active);
            }

            public void GetParameterIndices(List<int> indices)
            {
                if (instance == null) return;
                instance.GetParameterIndices(indices);
            }

            public void RemapParameterIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
            {
                if (instance == null) return;
                instance.RemapParameterIndices(remapper, invalidateNonRemappedIndices);
            }

            public IAnimationMotionController GetMotionController(int index)
            {
                if (instance == null) return null;
                return instance.GetMotionController(index);
            }

            public IAnimationMotionController GetMotionControllerUnsafe(int index)
            {
                if (instance == null) return null;
                return instance.GetMotionControllerUnsafe(index);
            }

            public IAnimationStateMachine GetStateMachine(int index)
            {
                if (instance == null) return null;
                return instance.GetStateMachine(index);
            }

            public IAnimationStateMachine GetStateMachineUnsafe(int index)
            {
                if (instance == null) return null;
                return instance.GetStateMachineUnsafe(index);
            }

            public void SetStateMachine(int index, IAnimationStateMachine stateMachine)
            {
                if (instance == null) return;
                instance.SetStateMachine(index, stateMachine);
            }

            public void SetStateMachines(IAnimationStateMachine[] stateMachines)
            {
                if (instance == null) return;
                instance.SetStateMachines(stateMachines);
            }

            public void IteratePlayers(IterateAnimationPlayerDelegate del)
            {
                if (instance == null) return;
                instance.IteratePlayers(del);
            }

            public IAnimationPlayer GetNewAnimationPlayer(IAnimationAsset animation)
            {
                if (instance == null) return null;
                return instance.GetNewAnimationPlayer(animation);
            }

            public bool RemoveAnimationPlayer(IAnimationAsset animation, int playerIndex)
            {
                if (instance == null) return false;
                return instance.RemoveAnimationPlayer(animation, playerIndex);
            }

            public bool RemoveAnimationPlayer(string id, int playerIndex)
            {
                if (instance == null) return false;
                return instance.RemoveAnimationPlayer(id, playerIndex);
            }

            public bool RemoveAnimationPlayer(List<IAnimationPlayer> players, int playerIndex)
            {
                if (instance == null) return false;
                return instance.RemoveAnimationPlayer(players, playerIndex);
            }

            public bool RemoveAnimationPlayer(IAnimationPlayer player)
            {
                if (instance == null) return false;
                return instance.RemoveAnimationPlayer(player);
            }

            public TransformHierarchy GetActiveTransformHierarchy()
            {
                if (instance == null) return null;
                return instance.GetActiveTransformHierarchy();
            }

            #endregion

        }

        public struct AnimationController : IAnimationController
        {

            #region IEngineObject

            public string name => swole.Engine.GetName(instance);
            public int InstanceID => swole.Engine.Object_GetInstanceID(instance);

            public static void Destroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_Destroy(obj, timeDelay);
            public static void AdminDestroy(IEngineObject obj, float timeDelay = 0) => swole.Engine.Object_AdminDestroy(obj, timeDelay);
            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);
            public void AdminDestroy(float timeDelay = 0) => AdminDestroy(this, timeDelay);

            public bool IsDestroyed => swole.Engine.IsNull(Instance);

            public bool HasEventHandler => EventHandler != null;
            public IRuntimeEventHandler EventHandler => swole.Engine.GetEventHandler(instance);

            #endregion

            #region IAnimationController

            public string Prefix
            {
                get => instance == null ? null : instance.Prefix;
                set
                {
                    if (IsDestroyed) return;
                    instance.Prefix = value;
                }
            }

            public IAnimationParameter[] FloatParameters 
            { 
                get
                {
                    if (IsDestroyed) return null;
                    return instance.FloatParameters;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.FloatParameters = value;
                }
            }
            public IAnimationParameterBoolean[] BoolParameters
            {
                get
                {
                    if (IsDestroyed) return null;
                    return instance.BoolParameters;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.BoolParameters = value;
                }
            }
            public IAnimationParameterTrigger[] TriggerParameters
            {
                get
                {
                    if (IsDestroyed) return null;
                    return instance.TriggerParameters;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.TriggerParameters = value;
                }
            }
            public IAnimationReference[] AnimationReferences
            {
                get
                {
                    if (IsDestroyed) return null;
                    return instance.AnimationReferences;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.AnimationReferences = value;
                }
            }
            public IBlendTree1D[] BlendTrees1D
            {
                get
                {
                    if (IsDestroyed) return null;
                    return instance.BlendTrees1D;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.BlendTrees1D = value;
                }
            }

            public IAnimationLayer[] Layers
            {
                get
                {
                    if (IsDestroyed) return null;
                    return instance.Layers;
                }
                set
                {
                    if (IsDestroyed) return;
                    instance.Layers = value;
                }
            }
            public int LayerCount => IsDestroyed ? 0 : instance.LayerCount;
            public IAnimationLayer GetLayer(int index) 
            {
                if (IsDestroyed) return null;
                return instance.GetLayer(index);
            }
            public IAnimationLayer GetLayerUnsafe(int index)
            {
                if (IsDestroyed) return null;
                return instance.GetLayerUnsafe(index);
            }
            public void SetLayer(int index, IAnimationLayer layer)
            {
                if (IsDestroyed) return;
                instance.SetLayer(index, layer);
            }
            public void SetLayerUnsafe(int index, IAnimationLayer layer)
            {
                if (IsDestroyed) return;
                instance.SetLayerUnsafe(index, layer);
            }

            public IAnimationParameter[] Parameters => IsDestroyed ? null : instance.Parameters;

            public IAnimationParameter[] GetParameters(bool instantiate = false)
            {
                if (IsDestroyed) return null;
                return instance.GetParameters(instantiate);
            }

            public IAnimationParameter GetAnimationParameter(AnimationParameterIdentifier identifier)
            {
                if (IsDestroyed) return null;
                return instance.GetAnimationParameter(identifier);
            }

            public IAnimationMotionController GetMotionController(MotionControllerIdentifier identifier)
            {
                if (IsDestroyed) return null;
                return instance.GetMotionController(identifier);
            }

            #endregion
             
            public IAnimationController instance;
            public object Instance => instance;

            public AnimationController(IAnimationController instance)
            {
                this.instance = instance;
            }

            public static bool operator ==(AnimationController lhs, object rhs)
            {
                if (rhs is AnimationController ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AnimationController lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }
             
        }
        
        public struct AnimationStateMachine : IAnimationStateMachine
        {

            public IAnimationStateMachine instance;
            public object Instance => instance;

            public AnimationStateMachine(IAnimationStateMachine instance)
            {
                this.instance = instance;
            }

            public static bool operator ==(AnimationStateMachine lhs, object rhs)
            {
                if (rhs is AnimationStateMachine ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AnimationStateMachine lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode() 
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            #region IAnimationStateMachine

            public string Name
            {
                get
                {
                    if (instance == null) return string.Empty;
                    return instance.Name;
                }
                set
                {
                    if (instance == null) return;
                    instance.Name = value;
                }
            }
            public int Index
            {
                get
                {
                    if (instance == null) return -1;
                    return instance.Index;
                }
                set
                {
                    if (instance == null) return;
                    instance.Index = value;
                }
            }

            public IAnimationLayer Layer => instance == null ? null : instance.Layer;

            public int MotionControllerIndex
            {
                get
                {
                    if (instance == null) return -1;
                    return instance.MotionControllerIndex;
                }
                set
                {
                    if (instance == null) return;
                    instance.MotionControllerIndex = value;
                }
            }
            public Transition[] Transitions
            {
                get
                {
                    if (instance == null) return null;
                    return instance.Transitions;
                }
                set
                {
                    if (instance == null) return;
                    instance.Transitions = value;
                }
            }

            public int TransitionTarget => instance == null ? -1 : instance.TransitionTarget;

            public float TransitionTime => instance == null ? 0 : instance.TransitionTime;

            public float TransitionTimeLeft => instance == null ? 0 : instance.TransitionTimeLeft;

            public bool IsActive()
            {
                if (instance == null) return false;
                return instance.IsActive();
            }

            public void SetWeight(float weight)
            {
                if (instance == null) return;
                instance.SetWeight(weight);
            }

            public float GetWeight()
            {
                if (instance == null) return 0;
                return instance.GetWeight();
            }

            public float GetTime(float addTime = 0)
            {
                if (instance == null) return 0;
                return instance.GetTime(addTime);
            }

            public float GetNormalizedTime(float addTime = 0)
            {
                if (instance == null) return 0;
                return instance.GetNormalizedTime(addTime);
            }

            public void SetTime(float time)
            {
                if (instance == null) return;
                instance.SetTime(time);
            }

            public void SetNormalizedTime(float normalizedTime)
            {
                if (instance == null) return;
                instance.SetNormalizedTime(normalizedTime);
            }

            public float GetEstimatedDuration()
            {
                if (instance == null) return 0;
                return instance.GetEstimatedDuration();
            }

            public void RestartAnims()
            {
                if (instance == null) return;
                instance.RestartAnims();
            }

            public void ResyncAnims()
            {
                if (instance == null) return;
                instance.ResyncAnims();
            }

            public void ResetTransition()
            {
                if (instance == null) return;
                instance.ResetTransition();
            }

            #endregion

        }
        
        public struct AnimationReference : IAnimationReference
        {

            #region ICloneable

            public object Clone()
            {
                var copy = this;
                copy.instance = (IAnimationReference)instance.Clone(); 
                return copy;
            }

            #endregion

            public IAnimationReference instance;
            public object Instance => instance; 

            public AnimationReference(IAnimationReference instance)
            {
                this.instance = instance;
            }

            public static bool operator ==(AnimationReference lhs, object rhs)
            {
                if (rhs is AnimationReference ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AnimationReference lhs, object rhs) => !(lhs == rhs); 

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            #region IAnimationMotionController

            public string Name
            {
                get
                {
                    if (instance == null) return string.Empty;
                    return instance.Name;
                }
                set
                {
                    if (instance == null) return;
                    instance.Name = value;
                }
            }

            public IAnimationMotionController Parent => instance == null ? null : instance.Parent;

            public float BaseSpeed
            {
                get
                {
                    if (instance == null) return 0;
                    return instance.BaseSpeed;
                }
                set
                {
                    if (instance == null) return;
                    instance.BaseSpeed = value;
                }
            }
            public int SpeedMultiplierParameter
            {
                get
                {
                    if (instance == null) return 0;
                    return instance.SpeedMultiplierParameter;
                }
                set
                {
                    if (instance == null) return;
                    instance.SpeedMultiplierParameter = value;
                }
            }

            public bool HasChildControllers => instance == null ? false : instance.HasChildControllers;

            public void Initialize(IAnimationLayer layer, IAnimationMotionController parent = null)
            {
                if (instance == null) return;
                instance.Initialize(layer, parent);
            }

            public float GetSpeed(IAnimator animator)
            {
                if (instance == null) return 0;
                return instance.GetSpeed(animator);
            }

            public AnimationLoopMode GetLoopMode(IAnimationLayer layer)
            {
                if (instance == null) return default;
                return instance.GetLoopMode(layer);
            }

            public void ForceSetLoopMode(IAnimationLayer layer, AnimationLoopMode loopMode)
            {
                if (instance == null) return;
               instance.ForceSetLoopMode(layer, loopMode);
            }

            public void SetWeight(float weight)
            {
                if (instance == null) return;
                instance.SetWeight(weight);
            }

            public float GetWeight()
            {
                if (instance == null) return default;
                return instance.GetWeight();
            }

            public float GetDuration(IAnimationLayer layer)
            {
                if (instance == null) return default;
                return instance.GetDuration(layer);
            }

            public float GetScaledDuration(IAnimationLayer layer)
            {
                if (instance == null) return default;
                return instance.GetScaledDuration(layer);
            }

            public float GetTime(IAnimationLayer layer, float addTime = 0)
            {
                if (instance == null) return default;
                return instance.GetTime(layer, addTime);
            }

            public float GetNormalizedTime(IAnimationLayer layer, float addTime = 0)
            {
                if (instance == null) return default;
                return instance.GetNormalizedTime(layer, addTime);
            }

            public void SetTime(IAnimationLayer layer, float time)
            {
                if (instance == null) return;
                instance.SetTime(layer, time);
            }

            public void SetNormalizedTime(IAnimationLayer layer, float normalizedTime)
            {
                if (instance == null) return;
                instance.SetNormalizedTime(layer, normalizedTime);
            }

            public bool HasAnimationPlayer(IAnimationLayer layer)
            {
                if (instance == null) return default;
                return instance.HasAnimationPlayer(layer);
            }

            public void GetChildIndexIdentifiers(List<MotionControllerIdentifier> identifiers, bool onlyAddIfNotPresent = true)
            {
                if (instance == null) return;
                instance.GetChildIndexIdentifiers(identifiers, onlyAddIfNotPresent);
            }

            public void RemapChildIndices(Dictionary<MotionControllerIdentifier, int> remapper, bool invalidateNonRemappedIndices = false)
            {
                if (instance == null) return;
                instance.RemapChildIndices(remapper, invalidateNonRemappedIndices);
            }

            public void RemapChildIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
            {
                if (instance == null) return;
                instance.RemapChildIndices(remapper, invalidateNonRemappedIndices);
            }

            public void GetParameterIndices(IAnimationLayer layer, List<int> indices)
            {
                if (instance == null) return;
                instance.GetParameterIndices(layer, indices);
            }

            public void RemapParameterIndices(IAnimationLayer layer, Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
            {
                if (instance == null) return;
                instance.RemapParameterIndices(layer, remapper, invalidateNonRemappedIndices);
            }

            public bool HasDerivativeHierarchyOf(IAnimationLayer layer, IAnimationMotionController other)
            {
                if (instance == null) return default;
                return instance.HasDerivativeHierarchyOf(layer, other);
            }

            public int GetLongestHierarchyIndex(IAnimationLayer layer)
            {
                if (instance == null) return default;
                return instance.GetLongestHierarchyIndex(layer);
            }

            #endregion

            #region IAnimationReference

            public AnimationLoopMode LoopMode 
            { 
                get
                {
                    if (instance == null) return default;
                    return instance.LoopMode;
                }
                set
                {
                    if (instance == null) return;
                    instance.LoopMode = value;
                }
            }
            public IAnimationAsset Animation
            {
                get
                {
                    if (instance == null) return default;
                    return instance.Animation;
                }
                set
                {
                    if (instance == null) return;
                    instance.Animation = value;
                }
            }

            public IAnimationPlayer AnimationPlayer
            {
                get
                {
                    if (instance == null) return default;
                    return instance.AnimationPlayer;
                }
            }

            #endregion

        }

        public struct AnimationPlayer : IAnimationPlayer
        {

            #region IRuntimeEventHandler

            public void SubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                if (instance == null) return;
                instance.SubscribePreEvent(listener);
            }

            public void UnsubscribePreEvent(RuntimeEventListenerDelegate listener)
            {
                if (instance == null) return;
                instance.UnsubscribePreEvent(listener);
            }

            public void SubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                if (instance == null) return;
                instance.SubscribePostEvent(listener);
            }

            public void UnsubscribePostEvent(RuntimeEventListenerDelegate listener)
            {
                if (instance == null) return;
                instance.UnsubscribePostEvent(listener);
            }

            #endregion

            #region IDisposable

            public void Dispose()
            {
                if (instance == null) return;
                instance.Dispose();
            }

            #endregion

            public IAnimationPlayer instance; 
            public object Instance => instance;

            public AnimationPlayer(IAnimationPlayer instance)
            {
                this.instance = instance;  
            }

            public static bool operator ==(AnimationPlayer lhs, object rhs)
            {
                if (rhs is AnimationPlayer ts) return lhs.instance == ts.instance;
                return lhs.instance == rhs;
            }
            public static bool operator !=(AnimationPlayer lhs, object rhs) => !(lhs == rhs);

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

            #region IAnimationPlayer

            public IRuntimeEnvironment EventRuntimeEnvironment
            {
                get
                {
                    if (instance == null) return null;
                    return instance.EventRuntimeEnvironment;
                }
                set
                {
                    if (instance == null) return;
                    instance.EventRuntimeEnvironment = value;
                }
            }
            public SwoleLogger EventLogger
            {
                get
                {
                    if (instance == null) return null;
                    return instance.EventLogger;
                }
                set
                {
                    if (instance == null) return;
                    instance.EventLogger = value;
                }
            }
            public int Index
            {
                get
                {
                    if (instance == null) return -1;
                    return instance.Index;
                }
                set
                {
                    if (instance == null) return;
                    instance.Index = value;
                }
            }

            public IAnimator Animator => instance == null ? null : instance.Animator;

            public IAnimationAsset Animation => instance == null ? null : instance.Animation;

            public float LengthInSeconds => instance == null ? 0 : instance.LengthInSeconds;

            public AnimationLoopMode LoopMode
            {
                get
                {
                    if (instance == null) return default;
                    return instance.LoopMode;
                }
                set
                {
                    if (instance == null) return;
                    instance.LoopMode = value;
                }
            }
            public bool IsAdditive
            {
                get
                {
                    if (instance == null) return default;
                    return instance.IsAdditive;
                }
                set
                {
                    if (instance == null) return;
                    instance.IsAdditive = value;
                }
            }
            public bool IsBlend
            {
                get
                {
                    if (instance == null) return default;
                    return instance.IsBlend;
                }
                set
                {
                    if (instance == null) return;
                    instance.IsBlend = value;
                }
            }
            public float Time
            {
                get
                {
                    if (instance == null) return default;
                    return instance.Time;
                }
                set
                {
                    if (instance == null) return;
                    instance.Time = value;
                }
            }
            public float Speed
            {
                get
                {
                    if (instance == null) return default;
                    return instance.Speed;
                }
                set
                {
                    if (instance == null) return;
                    instance.Speed = value;
                }
            }

            public float InternalSpeed => instance == null ? 0 : instance.InternalSpeed;

            public float Mix
            {
                get
                {
                    if (instance == null) return default;
                    return instance.Mix;
                }
                set
                {
                    if (instance == null) return;
                    instance.Mix = value;
                }
            }
            public bool Paused
            {
                get
                {
                    if (instance == null) return default;
                    return instance.Paused;
                }
                set
                {
                    if (instance == null) return;
                    instance.Paused = value;
                }
            }

            public TransformHierarchy Hierarchy => instance == null ? null : instance.Hierarchy;

            public void CallAnimationEvents(float startTime, float endTime)
            {
                if (instance == null) return;
                instance.CallAnimationEvents(startTime, endTime);
            }

            public void ResetLoop()
            {
                if (instance == null) return;
                instance.ResetLoop(); 
            }

            #endregion

        }

        #endregion

    }

}
