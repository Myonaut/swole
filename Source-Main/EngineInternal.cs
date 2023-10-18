using System;
using System.Runtime.CompilerServices;

using Swole.Script;

namespace Swole
{

    public static class EngineInternal
    {

        [Serializable]
        public struct Vector2
        {

            public override string ToString() => $"({x}, {y})";

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

        }

        [Serializable]
        public struct Vector3
        {

            #region Proxy Implementations

            public static Vector3 operator *(Quaternion q, Vector3 v) => Swole.Engine.Mul(q, v);

            #endregion

            public override string ToString() => $"({x}, {y}, {z})";

            public static Vector3 operator +(Vector3 vA, Vector3 vB) => new Vector3(vA.x + vB.x, vA.y + vB.y, vA.z + vB.z);
            public static Vector3 operator *(Vector3 v, float scalar) => new Vector3(v.x * scalar, v.y * scalar, v.z * scalar);
            public static Vector3 operator /(Vector3 v, float scalar) => new Vector3(v.x / scalar, v.y / scalar, v.z * scalar);

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

            public override string ToString() => $"({x}, {y}, {z}, {w})";

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

        }

        [Serializable]
        public struct Quaternion
        {

            #region Proxy Implementations

            public static Quaternion operator *(Quaternion qA, Quaternion qB) => Swole.Engine.Mul(qA, qB);

            public static Quaternion Euler(Vector3 eulerAngles) => Swole.Engine.Quaternion_Euler(eulerAngles);
            public static Quaternion Euler(float x, float y, float z) => Swole.Engine.Quaternion_Euler(x, y, z);

            public static Quaternion Inverse(Quaternion q) => Swole.Engine.Quaternion_Inverse(q);

            public Vector3 EulerAngles => Swole.Engine.Quaternion_EulerAngles(this);

            #endregion

            public override string ToString() => $"({x}, {y}, {z}, {w})";

            public static readonly Quaternion identity = new Quaternion(0, 0, 0, 1);

            public Quaternion inverse => Swole.Engine.Quaternion_Inverse(this);

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

        }

        [Serializable]
        public struct Matrix4x4
        {

            #region Proxy Implementations

            public static Matrix4x4 operator *(Matrix4x4 mA, Matrix4x4 mB) => Swole.Engine.Mul(mA, mB);

            public static Matrix4x4 TRS(Vector3 position, Quaternion rotation, Vector3 scale) => Swole.Engine.Matrix4x4_TRS(position, rotation, scale);
            public static Matrix4x4 Scale(Vector3 vector) => Swole.Engine.Matrix4x4_Scale(vector);
            public static Matrix4x4 Translate(Vector3 vector) => Swole.Engine.Matrix4x4_Translate(vector);
            public static Matrix4x4 Rotate(Quaternion q) => Swole.Engine.Matrix4x4_Rotate(q);

            public Vector3 MultiplyPoint(Matrix4x4 m, Vector3 point) => Swole.Engine.Mul(m, point);
            public Vector3 MultiplyPoint3x4(Matrix4x4 m, Vector3 point) => Swole.Engine.Mul3x4(m,  point);
            public Vector3 MultiplyVector(Matrix4x4 m, Vector3 vector) => Swole.Engine.Rotate(m, vector);

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

        [Serializable]
        public enum RotationOrder
        {
            XYZ, XZY,YZX, YXZ, ZXY, ZYX
        }

        [Serializable]
        public enum Space
        {
            World, Self
        }

        public struct GameObject : IVolatile
        {

            #region Proxy Implementations

            public string name => Swole.Engine.GetName(instance);

            public static GameObject Create(string name = "") => Swole.Engine.GameObject_Create(name);

            public static GameObject Instantiate(GameObject referenceObject) => Swole.Engine.GameObject_Instantiate(referenceObject);

            public GameObject Instantiate() => Instantiate(this);

            public static void Destroy(GameObject gameObject, float timeDelay = 0) => Swole.Engine.GameObject_Destroy(gameObject, timeDelay);

            public void Destroy(float timeDelay = 0) => Destroy(this, timeDelay);

            #endregion

            public object instance;

            public Transform transform;

            public GameObject(object instance, Transform transform)
            {
                this.instance = instance;
                this.transform = transform;
            }

            public static bool operator ==(GameObject lhs, GameObject rhs) => lhs.instance == rhs.instance;
            public static bool operator !=(GameObject lhs, GameObject rhs) => lhs.instance != rhs.instance;

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

        }

        public struct Transform : IVolatile
        {

            #region Proxy Implementations

            public Transform(object instance)
            {
                this.instance = instance;
            }

            public Transform parent => Swole.Engine.Transform_GetParent(this);

            public Vector3 position
            {
                get => Swole.Engine.GetWorldPosition(instance);
                set => Swole.Engine.SetWorldPosition(instance, value);
            }

            public Quaternion rotation
            {
                get => Swole.Engine.GetWorldRotation(instance);
                set => Swole.Engine.SetWorldRotation(instance, value);
            }

            public Vector3 localPosition
            {
                get => Swole.Engine.GetLocalPosition(instance);
                set => Swole.Engine.SetLocalPosition(instance, value);
            }

            public Quaternion localRotation
            {
                get => Swole.Engine.GetLocalRotation(instance);
                set => Swole.Engine.SetLocalRotation(instance, value);
            }

            public Vector3 localScale
            {
                get => Swole.Engine.GetLocalScale(instance);
                set => Swole.Engine.SetLocalScale(instance, value);
            }

            public Vector3 lossyScale => Swole.Engine.Transform_lossyScale(this);

            public Vector3 eulerAngles
            {
                get => Swole.Engine.Transform_eulerAnglesGet(this);
                set => Swole.Engine.Transform_eulerAnglesSet(this, value);
            }

            public Vector3 localEulerAngles
            {
                get => Swole.Engine.Transform_localEulerAnglesGet(this);
                set => Swole.Engine.Transform_localEulerAnglesSet(this, value);
            }

            public Vector3 right
            {
                get => Swole.Engine.Transform_rightGet(this);
                set => Swole.Engine.Transform_rightSet(this, value);
            }

            public Vector3 up
            {
                get => Swole.Engine.Transform_upGet(this);
                set => Swole.Engine.Transform_upSet(this, value);
            }

            public Vector3 forward
            {
                get => Swole.Engine.Transform_forwardGet(this);
                set => Swole.Engine.Transform_forwardSet(this, value);
            }

            public Matrix4x4 worldToLocalMatrix => Swole.Engine.Transform_worldToLocalMatrix(this);

            public Matrix4x4 localToWorldMatrix => Swole.Engine.Transform_localToWorldMatrix(this);

            public Transform root => Swole.Engine.Transform_root(this);

            public int childCount => Swole.Engine.Transform_childCount(this);

            public bool hasChanged
            {
                get => Swole.Engine.Transform_hasChangedGet(this);
                set => Swole.Engine.Transform_hasChangedSet(this, value);
            }

            public int hierarchyCapacity
            {
                get => Swole.Engine.Transform_hierarchyCapacityGet(this);
                set => Swole.Engine.Transform_hierarchyCapacitySet(this, value);
            }

            public int hierarchyCount => Swole.Engine.Transform_hierarchyCount(this);

            public Transform GetParent() => Swole.Engine.Transform_GetParent(this);

            public void SetParent(Transform p) => Swole.Engine.Transform_SetParent(this, p);

            public void SetParent(Transform parent, bool worldPositionStays) => Swole.Engine.Transform_SetParent(this, parent, worldPositionStays);

            public void SetPositionAndRotation(Vector3 position, Quaternion rotation) => Swole.Engine.Transform_SetPositionAndRotation(this, position, rotation);

            public void SetLocalPositionAndRotation(Vector3 localPosition, Quaternion localRotation) => Swole.Engine.Transform_SetLocalPositionAndRotation(this, localPosition, localRotation);

            public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation) => Swole.Engine.Transform_GetPositionAndRotation(this, out position, out rotation);

            public void GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation) => Swole.Engine.Transform_GetLocalPositionAndRotation(this, out localPosition, out localRotation);

            public void Translate(Vector3 translation, Space relativeTo = Space.Self) => Swole.Engine.Transform_Translate(this, translation, relativeTo);

            public void Translate(Vector3 translation) => Swole.Engine.Transform_Translate(this, translation);

            public void Translate(float x, float y, float z, Space relativeTo = Space.Self) => Swole.Engine.Transform_Translate(this, x, y, z, relativeTo);

            public void Translate(float x, float y, float z) => Swole.Engine.Transform_Translate(this, x, y, z);

            public void Translate(Vector3 translation, Transform relativeTo) => Swole.Engine.Transform_Translate(this, translation, relativeTo);

            public void Translate(float x, float y, float z, Transform relativeTo) => Swole.Engine.Transform_Translate(this, x, y, z, relativeTo);

            public void Rotate(Vector3 eulers, Space relativeTo = Space.Self) => Swole.Engine.Transform_Rotate(this, eulers, relativeTo);

            public void Rotate(Vector3 eulers) => Swole.Engine.Transform_Rotate(this, eulers);

            public void Rotate(float xAngle, float yAngle, float zAngle, Space relativeTo = Space.Self) => Swole.Engine.Transform_Rotate(this, xAngle, yAngle, zAngle, relativeTo);

            public void Rotate(float xAngle, float yAngle, float zAngle) => Swole.Engine.Transform_Rotate(this, xAngle, yAngle, zAngle);

            public void Rotate(Vector3 axis, float angle, Space relativeTo = Space.Self) => Swole.Engine.Transform_Rotate(this, axis, angle, relativeTo);

            public void Rotate(Vector3 axis, float angle) => Swole.Engine.Transform_Rotate(this, axis, angle);

            public void RotateAround(Vector3 point, Vector3 axis, float angle) => Swole.Engine.Transform_RotateAround(this, point, axis, angle);

            public void LookAt(Transform target, Vector3 worldUp) => Swole.Engine.Transform_LookAt(this, target, worldUp);

            public void LookAt(Transform target) => Swole.Engine.Transform_LookAt(this, target);

            public void LookAt(Vector3 worldPosition, Vector3 worldUp) => Swole.Engine.Transform_LookAt(this, worldPosition, worldUp);

            public void LookAt(Vector3 worldPosition) => Swole.Engine.Transform_LookAt(this, worldPosition);

            public Vector3 TransformDirection(Vector3 direction) => Swole.Engine.Transform_TransformDirection(this, direction);

            public Vector3 TransformDirection(float x, float y, float z) => Swole.Engine.Transform_TransformDirection(this, x, y, z);

            public Vector3 InverseTransformDirection(Vector3 direction) => Swole.Engine.Transform_InverseTransformDirection(this, direction);

            public Vector3 InverseTransformDirection(float x, float y, float z) => Swole.Engine.Transform_InverseTransformDirection(this, x, y, z);

            public Vector3 TransformVector(Vector3 vector) => Swole.Engine.Transform_TransformVector(this, vector);

            public Vector3 TransformVector(float x, float y, float z) => Swole.Engine.Transform_TransformVector(this, x, y, z);

            public Vector3 InverseTransformVector(Vector3 vector) => Swole.Engine.Transform_InverseTransformVector(this, vector);

            public Vector3 InverseTransformVector(float x, float y, float z) => Swole.Engine.Transform_InverseTransformVector(this, x, y, z);

            public Vector3 TransformPoint(Vector3 position) => Swole.Engine.Transform_TransformPoint(this, position);

            public Vector3 TransformPoint(float x, float y, float z) => Swole.Engine.Transform_TransformPoint(this, x, y, z);

            public Vector3 InverseTransformPoint(Vector3 position) => Swole.Engine.Transform_InverseTransformPoint(this, position);

            public Vector3 InverseTransformPoint(float x, float y, float z) => Swole.Engine.Transform_InverseTransformPoint(this, x, y, z);

            public void DetachChildren() => Swole.Engine.Transform_DetachChildren(this);

            public void SetAsFirstSibling() => Swole.Engine.Transform_SetAsFirstSibling(this);

            public void SetAsLastSibling() => Swole.Engine.Transform_SetAsLastSibling(this);

            public void SetSiblingIndex(int index) => Swole.Engine.Transform_SetSiblingIndex(this, index);

            public int GetSiblingIndex() => Swole.Engine.Transform_GetSiblingIndex(this);

            public Transform Find(string n) => Swole.Engine.Transform_Find(this, n);

            public bool IsChildOf(Transform parent) => Swole.Engine.Transform_IsChildOf(this, parent);

            public Transform GetChild(int index) => Swole.Engine.Transform_GetChild(this, index);

            #endregion

            public object instance;

            public static bool operator ==(Transform lhs, Transform rhs) => lhs.instance == rhs.instance;
            public static bool operator !=(Transform lhs, Transform rhs) => lhs.instance != rhs.instance;

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

        }

        public struct Tile : IVolatile
        {

            public Tile(object instance, string name, bool isDynamic, Vector3 positionOffset, Vector3 initialRotationEuler, Vector3 initialScale)
            {
                this.instance = instance;
                this.name = name;
                this.isDynamic = isDynamic;
                this.positionOffset = positionOffset;
                this.initialRotationEuler = initialRotationEuler;
                this.initialScale = initialScale;
            }

            public object instance;

            public string name;

            /// <summary>
            /// Can the tile change position after being spawned?
            /// </summary>
            public bool isDynamic;

            public Vector3 positionOffset;
            public Vector3 initialRotationEuler;
            public Vector3 initialScale;

            public static bool operator ==(Tile lhs, Tile rhs) => lhs.instance == rhs.instance;
            public static bool operator !=(Tile lhs, Tile rhs) => lhs.instance != rhs.instance;

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
        public struct TileSet
        {

            #region Proxy Implementations

            public int TileCount => Swole.Engine.GetTileCount(instance);

            public Tile this[int tileIndex] => Swole.Engine.GetTileFromSet(instance, tileIndex);

            #endregion

            public TileSet(object instance, string name, string tileMeshName, string tileMaterialName)
            {
                this.instance = instance;
                this.name = name;
                this.tileMeshName = tileMeshName;
                this.tileMaterialName = tileMaterialName;
            }

            [NonSerialized]
            public object instance;

            public string name;

            public string tileMeshName;
            public string tileMaterialName;

            public static bool operator ==(TileSet lhs, TileSet rhs) => lhs.instance == rhs.instance;
            public static bool operator !=(TileSet lhs, TileSet rhs) => lhs.instance != rhs.instance;

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
        public struct TileInstance
        {

            public TileInstance(object instance, string tileSetName, int tileIndex, GameObject rootInstance)
            {
                this.instance = instance;
                this.tileSetName = tileSetName;
                this.tileIndex = tileIndex;
                this.rootInstance = rootInstance;
            }

            [NonSerialized]
            public object instance;

            public string tileSetName;
            public int tileIndex;

            [NonSerialized]
            public GameObject rootInstance;

            public static bool operator ==(TileInstance lhs, TileInstance rhs) => lhs.instance == rhs.instance;
            public static bool operator !=(TileInstance lhs, TileInstance rhs) => lhs.instance != rhs.instance;

            public override bool Equals(object obj)
            {
                return instance == null ? obj == null : instance.Equals(obj);
            }
            public override int GetHashCode()
            {
                return instance == null ? base.GetHashCode() : instance.GetHashCode();
            }

        }

    }

}
