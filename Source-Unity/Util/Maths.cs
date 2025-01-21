#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Runtime.CompilerServices;

using Unity.Mathematics;
using UnityEngine;

namespace Swole
{

    public static class Maths
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countTrue(bool4 b4)
        {
            int4 c = 0;
            c = math.select(c, c + 1, b4);
            return c.x + c.y + c.z + c.w;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countTrue(bool3 b3)
        {
            int3 c = 0;
            c = math.select(c, c + 1, b3);
            return c.x + c.y + c.z;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int countTrue(bool2 b2)
        {
            int2 c = 0;
            c = math.select(c, c + 1, b2);
            return c.x + c.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetValueOnSegment(float3 worldPosition, float3 pointA, float3 pointB)
        {

            float3 v = pointB - pointA;
            float3 u = pointA - worldPosition;

            float vu = v.x * u.x + v.y * u.y + v.z * u.z;
            float vv = v.x * v.x + v.y * v.y + v.z * v.z;
            float t = -vu / vv;

            return math.clamp(t, 0, 1);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetPointOnSegment(float3 worldPosition, float3 pointA, float3 pointB)
        {

            return math.lerp(pointA, pointB, GetValueOnSegment(worldPosition, pointA, pointB));

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int AsPowerOf2(int value)
        {
            int newVal = 1;
            while (value > newVal) newVal *= 2;
            return newVal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ObjectSpaceToTangentSpaceOffset(float3 OSoffset, float3 OSnormal, float4 OStangent, bool normalizeVectors = true)
        {

            if (OSoffset.x == 0 && OSoffset.y == 0 && OSoffset.z == 0) return float3.zero;

            float3 OStangentXYZ = new float3(OStangent.x, OStangent.y, OStangent.z);

            if (normalizeVectors)
            {

                OSnormal = math.normalize(OSnormal);
                OStangent = new float4(math.normalize(OStangentXYZ), OStangent.w);

                OStangentXYZ = new float3(OStangent.x, OStangent.y, OStangent.z);

            }

            float3 OSbitangent = math.cross(OSnormal, OStangentXYZ) * OStangent.w;

            float offset = math.length(OSoffset);
            float3 offsetDirection = OSoffset / offset;

            float outwardDot = math.dot(offsetDirection, OSnormal);
            float horizontalDot = math.dot(offsetDirection, OStangentXYZ);
            float verticalDot = math.dot(offsetDirection, OSbitangent);

            float outwardVal = offset * outwardDot;
            float horizontalVal = offset * horizontalDot;
            float verticalVal = offset * verticalDot;

            return new float3(horizontalVal, verticalVal, outwardVal);

        }

        /// <summary>
        /// Converts a quaternion to euler angles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 ToEuler(this quaternion quaternion)
        {
            var q = quaternion.value;

            // roll (x-axis rotation)
            var sinRCosP = 2 * ((q.w * q.x) + (q.y * q.z));
            var cosRCosP = 1 - (2 * ((q.x * q.x) + (q.y * q.y)));
            var roll = math.atan2(sinRCosP, cosRCosP);

            // pitch (y-axis rotation)
            var sinP = 2 * ((q.w * q.y) - (q.z * q.x));
            var pitch = math.select(math.asin(sinP), math.sign(sinP) * math.PI / 2, math.abs(sinP) >= 1);

            // yaw (z-axis rotation)
            var sinYCosP = 2 * ((q.w * q.z) + (q.x * q.y));
            var cosYCosP = 1 - (2 * ((q.y * q.y) + (q.z * q.z)));
            var yaw = math.atan2(sinYCosP, cosYCosP);

            return new float3(roll, pitch, yaw);
        }

        #region Source: https://forum.unity.com/threads/rotate-towards-c-jobs.836356/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion RotateTowards(
        quaternion from,
        quaternion to,
        float maxDegreesDelta)
        {
            float num = Angle(from, to);
            return num < float.Epsilon ? to : math.slerp(from, to, math.min(1f, maxDegreesDelta / num));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(this quaternion q1, quaternion q2)
        {
            var dot = math.dot(q1, q2);
            return !(dot > 0.999998986721039) ? (float)(math.acos(math.min(math.abs(dot), 1f)) * 2.0) : 0.0f;
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion FromToRotation(float3 aFrom, float3 aTo)
        {
            float3 axis = math.cross(aFrom, aTo);
            float angle = Angle(aFrom, aTo);
            return AngleAxis(angle, axis); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion AngleAxis(float aAngle, float3 aAxis)
        {
            aAxis = math.normalizesafe(aAxis);
            float rad = aAngle * 0.5f;
            aAxis *= math.sin(rad);
            return new quaternion(aAxis.x, aAxis.y, aAxis.z, math.cos(rad));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Angle(float3 from, float3 to)
        {
            float denominator = (float)math.sqrt(math.lengthsq(from) * math.lengthsq(to));
            if (denominator < Vector3.kEpsilonNormalSqrt)
                return 0F;

            float dot = math.clamp(math.dot(from, to) / denominator, -1F, 1F);
            return math.acos(dot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion GetRotation(this float4x4 matrix)
        {

            float3 forward = new float3(matrix.c2.x, matrix.c2.y, matrix.c2.z);

            float3 up = new float3(matrix.c1.x, matrix.c1.y, matrix.c1.z);

            return quaternion.LookRotation(forward, up);

        }

        #region Source: https://gamedev.stackexchange.com/questions/23743/whats-the-most-efficient-way-to-find-barycentric-coordinates
        /// <summary>
        /// Compute barycentric coordinates (u, v, w) for point p with respect to triangle (a, b, c)
        /// </summary>
        public static float3 BarycentricCoords(float3 p, float3 a, float3 b, float3 c)
        {
            float3 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = math.dot(v0, v0);
            float d01 = math.dot(v0, v1);
            float d11 = math.dot(v1, v1);
            float d20 = math.dot(v2, v0);
            float d21 = math.dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;

            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return new float3(u, v, w);

        }
        #endregion

        #region Source: https://gist.github.com/keenanwoodall/c37ce12e0b7c08bd59f7235ec9614562
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float repeat(float t, float length)
        {
            return math.clamp(t - math.floor(t / length) * length, 0, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float pingpong(float t, float length)
        {
            t = repeat(t, length * 2f);
            return length - math.abs(t - length);
        }
        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int wrap(int index, int length)
        {
            return (index % length + length) % length;
        }

        public static float Wrap(float value, float max) => value - (Mathf.Floor(value / max) * max);

        public static float NormalizeDegrees(float degrees)
        {

            while (degrees < 0) degrees += 360;
            while (degrees >= 360) degrees -= 360;

            return degrees;

        }

        /// <summary>
        /// Find the smallest distance between two angles. (in degrees)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleDifference(float angle1, float angle2)
        {

            float diff = (angle2 - angle1 + 180) % 360 - 180;

            return diff < -180 ? diff + 360 : diff;

        }

        public const float _exp_sRGBtoLinear = 2.2f;
        public const float _exp_LinearTosRGB = (1.0f / 2.2f);

        /// <summary>
        /// Convert an sRGB color to Linear space
        /// </summary>
        public static Vector4 AsLinearColorVector(this Color color)
        {

            return (Vector4)(math.pow(new float4(color.r, color.g, color.b, color.a), _exp_sRGBtoLinear));

        }

        /// <summary>
        /// Convert a Linear space color value to an sRGB color
        /// </summary>
        public static Color AsSRGBColor(this Vector4 linearColor)
        {

            return (Color)(Vector4)(math.pow((float4)linearColor, _exp_LinearTosRGB));

        }

        public static float NormalizeAngle(float angle)
        {
            angle = angle % 360;
            if (angle < 0) angle += 360;
            return angle;
        }
        public static float GetMinDeltaAngle(float ang1, float ang2)
        {
            ang1 = NormalizeAngle(ang1);
            ang2 = NormalizeAngle(ang2);

            float delta = ang2 - ang1;
            if (math.abs(delta) > 180)
            {
                delta = math.sign(delta) >= 0 ? (delta - 360) : (delta + 360);

            }

            return delta;
        }

        public static float AngleOffsetAroundAxis(Vector3 v, Vector3 forward, Vector3 axis)
        {
            Vector3 right = Vector3.Cross(axis, forward);//.normalized;
            forward = Vector3.Cross(right, axis);//.normalized;
            return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * Mathf.Rad2Deg;
        }
        public static Vector2 RotateByArc(Vector2 Center, Vector2 A, float arc)
        {
            //calculate radius
            float radius = Vector2.Distance(Center, A);
            //calculate angle from arc
            float angle = arc / radius;
            Vector2 B = RotateByRadians(Center, A, angle);
            return B;
        }
        public static Vector2 RotateByArc(Vector2 Center, Vector2 A, float arc, float radius)
        {
            //calculate angle from arc
            float angle = arc / radius;
            Vector2 B = RotateByRadians(Center, A, angle);
            return B;
        }
        public static Vector2 RotateByRadians(Vector2 Center, Vector2 A, float angle)
        {
            //Move calculation to 0,0
            Vector2 v = A - Center;
            //rotate x and y
            float x = v.x * Mathf.Cos(angle) + v.y * Mathf.Sin(angle);
            float y = v.y * Mathf.Cos(angle) - v.x * Mathf.Sin(angle);
            //move back to center
            Vector2 B = new Vector2(x, y) + Center;
            return B;
        }
        public static Vector2 RotateByDegrees(Vector2 Center, Vector2 A, float angle) => RotateByRadians(Center, A, Mathf.Deg2Rad * angle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Rotate(this float2 v, float radians)
        {
            float sin = math.sin(radians);
            float cos = math.cos(radians);

            float tx = v.x;
            float ty = v.y;

            return new float2(cos * tx - sin * ty, sin * tx + cos * ty);
        }

        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);

            float tx = v.x;
            float ty = v.y;

            return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
        }

        public static float InverseSafe(float f)
        {
            if (Mathf.Abs(f) > Vector3.kEpsilon)
                return 1.0F / f;
            else
                return 0.0F;
        }

        public static Vector3 InverseSafe(Vector3 v)
        {
            return new Vector3(InverseSafe(v.x), InverseSafe(v.y), InverseSafe(v.z));
        }

        public static Matrix4x4 GetWorldRotationAndScale(EngineInternal.ITransform transform)
        {
            Matrix4x4 ret = new Matrix4x4();
            ret.SetTRS(new Vector3(0, 0, 0), UnityEngineHook.AsUnityQuaternion(transform.localRotation), UnityEngineHook.AsUnityVector(transform.localScale));
            if (transform.parent != null)
            {
                Matrix4x4 parentTransform = GetWorldRotationAndScale(transform.parent);
                ret = parentTransform * ret;
            }
            return ret;
        }

        public static Vector3 CalcNormal(Vector3 v1, Vector3 v2, Vector3 v3) => Vector3.Cross((v2 - v1), (v3 - v1));

        public static void QuaternionToMatrix(Quaternion q, ref Matrix4x4 m)
        {
            float x = q.x * 2.0F;
            float y = q.y * 2.0F;
            float z = q.z * 2.0F;
            float xx = q.x * x;
            float yy = q.y * y;
            float zz = q.z * z;
            float xy = q.x * y;
            float xz = q.x * z;
            float yz = q.y * z;
            float wx = q.w * x;
            float wy = q.w * y;
            float wz = q.w * z;

            m[0] = 1.0f - (yy + zz);
            m[1] = xy + wz;
            m[2] = xz - wy;
            m[3] = 0.0F;

            m[4] = xy - wz;
            m[5] = 1.0f - (xx + zz);
            m[6] = yz + wx;
            m[7] = 0.0F;

            m[8] = xz + wy;
            m[9] = yz - wx;
            m[10] = 1.0f - (xx + yy);
            m[11] = 0.0F;

            m[12] = 0.0F;
            m[13] = 0.0F;
            m[14] = 0.0F;
            m[15] = 1.0F;
        }

        /// <summary>
        /// source: https://forum.unity.com/threads/shortest-rotation-between-two-quaternions.812346/
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ShortestRotationLocal(Quaternion from, Quaternion to)
        {
            if (Quaternion.Dot(from, to) < 0)
            {
                return to * Quaternion.Inverse(Multiply(from, -1));
            }
            else return to * Quaternion.Inverse(from);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ShortestRotationLocal(quaternion from, quaternion to)
        {
            return math.select(math.mul(to, math.inverse(from)).value, math.mul(to, math.inverse(Multiply(from, -1))).value, math.dot(from, to) < 0);  
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Quaternion ShortestRotationGlobal(Quaternion from, Quaternion to)
        {
            if (Quaternion.Dot(to, from) < 0)
            {
                return Quaternion.Inverse(Multiply(from, -1)) * to;
            }
            else return Quaternion.Inverse(from) * to;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion ShortestRotationGlobal(quaternion from, quaternion to)
        {
            return math.select(math.mul(math.inverse(from), to).value, math.mul(math.inverse(Multiply(from, -1)), to).value, math.dot(to, from) < 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion LongestRotationLocal(Quaternion from, Quaternion to)
        {
            if (Quaternion.Dot(from, to) >= 0)
            {
                return to * Quaternion.Inverse(Multiply(from, -1));
            }
            else return to * Quaternion.Inverse(from);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion LongestRotationLocal(quaternion from, quaternion to)
        {
            return math.select(math.mul(to, math.inverse(from)).value, math.mul(to, math.inverse(Multiply(from, -1))).value, math.dot(from, to) >= 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        public static Quaternion LongestRotationGlobal(Quaternion from, Quaternion to)
        {
            if (Quaternion.Dot(to, from) >= 0)
            {
                return Quaternion.Inverse(Multiply(from, -1)) * to;
            }
            else return Quaternion.Inverse(from) * to;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion LongestRotationGlobal(quaternion from, quaternion to)
        {
            return math.select(math.mul(math.inverse(from), to).value, math.mul(math.inverse(Multiply(from, -1)), to).value, math.dot(to, from) >= 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Multiply(Quaternion input, float scalar) => Multiply((quaternion)input, scalar);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static quaternion Multiply(quaternion input, float scalar)
        {
            return input.value * scalar;
        }

        #region Mirroring
        public static Quaternion ReflectX(this Quaternion quat)
        {
            return new Quaternion(quat.x,
                -quat.y,
                -quat.z,
                quat.w);
        }
        public static quaternion ReflectX(this quaternion quat)
        {
            return new quaternion(quat.value.x,
                -quat.value.y,
                -quat.value.z,
                quat.value.w);
        }
        public static void MirrorPositionAndRotationX(Vector3 position, Quaternion rotation, out Vector3 outputPosition, out Quaternion outputRotation)
        {
            outputPosition = new Vector3(-position.x, position.y, position.z);
            outputRotation = rotation.ReflectX();
        }
        #endregion

        // intersect a ray with a 3D triangle
        //    Input:  a ray R, and 3 vector3 forming a triangle

        //  -1 = triangle is degenerate (a segment or point)
        //             0 = disjoint (no intersect)
        //             1 = intersect in unique point I1
        //             2 = are in the same plane
        public static bool IntersectRayTriangle(Ray ray, Vector3 v0, Vector3 v1, Vector3 v2, bool bidirectional, out RaycastHit hit)
        {
            hit = default;

            Vector3 ab = v1 - v0;
            Vector3 ac = v2 - v0;

            // Compute triangle normal. Can be precalculated or cached if
            // intersecting multiple segments against the same triangle
            Vector3 n = Vector3.Cross(ab, ac);

            // Compute denominator d. If d <= 0, segment is parallel to or points
            // away from triangle, so exit early
            float d = Vector3.Dot(-ray.direction, n);
            if (d <= 0.0f) return false;

            // Compute intersection t value of pq with plane of triangle. A ray
            // intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
            // dividing by d until intersection has been found to pierce triangle
            Vector3 ap = ray.origin - v0;
            float t = Vector3.Dot(ap, n);
            if ((t < 0.0f) && (!bidirectional)) return false;
            //if (t > d) return null; // For segment; exclude this code line for a ray test

            // Compute barycentric coordinate components and test if within bounds
            Vector3 e = Vector3.Cross(-ray.direction, ap);
            float v = Vector3.Dot(ac, e);
            if (v < 0.0f || v > d) return false;

            float w = -Vector3.Dot(ab, e);
            if (w < 0.0f || v + w > d) return false;

            // Segment/ray intersects triangle. Perform delayed division and
            // compute the last barycentric coordinate component
            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
            float u = 1.0f - v - w;

            hit = new RaycastHit();
            hit.point = ray.origin + t * ray.direction;
            hit.distance = t;
            hit.barycentricCoordinate = new Vector3(u, v, w);
            hit.normal = Vector3.Normalize(n);

            return true;
        }

        public static bool IntersectSegmentTriangle(Vector3 s0, Vector3 s1, Vector3 v0, Vector3 v1, Vector3 v2, out RaycastHit hit)
        {
            hit = default;

            Vector3 segment = (s1 - s0);

            Vector3 ab = v1 - v0;
            Vector3 ac = v2 - v0;

            // Compute triangle normal. Can be precalculated or cached if
            // intersecting multiple segments against the same triangle
            Vector3 n = Vector3.Cross(ab, ac);

            // Compute denominator d. If d <= 0, segment is parallel to or points
            // away from triangle, so exit early
            float d = Vector3.Dot(-segment, n);
            if (d <= 0.0f) return false;

            // Compute intersection t value of pq with plane of triangle. A ray
            // intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
            // dividing by d until intersection has been found to pierce triangle
            Vector3 ap = s0 - v0;
            float t = Vector3.Dot(ap, n);
            if ((t < 0.0f) || (t > d)) return false;

            // Compute barycentric coordinate components and test if within bounds
            Vector3 e = Vector3.Cross(-segment, ap);
            float v = Vector3.Dot(ac, e);
            if (v < 0.0f || v > d) return false;

            float w = -Vector3.Dot(ab, e);
            if (w < 0.0f || v + w > d) return false;

            float h = t; 
            // Segment/ray intersects triangle. Perform delayed division and
            // compute the last barycentric coordinate component
            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
            float u = 1.0f - v - w;

            hit = new RaycastHit();
            hit.point = s0 + t * segment; 
            //hit.point = s0 + (-Vector3.Dot(ap, n) / Vector3.Dot((s1 - s0), n)) * segment;  
            hit.distance = t * segment.magnitude;
            hit.barycentricCoordinate = new Vector3(u, v, w);

            hit.normal = Vector3.Normalize(n);

            return true;
        }
        public struct RaycastHitResult
        {
            /// <summary>
            /// The impact point in world space where the ray hit the collider.
            /// </summary>
            public float3 point;

            /// <summary>
            /// The normal of the surface the ray hit.
            /// </summary>
            public float3 normal;

            /// <summary>
            /// The index of the triangle that was hit.
            /// </summary>
            public int triangleIndex;

            /// <summary>
            /// The distance from the ray's origin to the impact point.
            /// </summary>
            public float distance;

            public float2 UV;

            /// <summary>
            /// The barycentric coordinate of the triangle we hit.
            /// </summary>
            public float3 barycentricCoordinate
            {
                get
                {
                    return new float3(1f - (UV.y + UV.x), UV.x, UV.y);
                }
                set
                {
                    UV = value.xy;
                }
            }
        }

        public static bool IntersectSegmentTriangle(float3 s0, float3 s1, float3 v0, float3 v1, float3 v2, out RaycastHitResult hit) => IntersectSegmentTriangle(s0, s1, v0, v1, v2, out _, out _, out _, out hit);        
        public static bool IntersectSegmentTriangle(float3 s0, float3 s1, float3 v0, float3 v1, float3 v2, out float3 ab, out float3 ac, out float3 triNormal, out RaycastHitResult hit)
        {
            ab = v1 - v0;
            ac = v2 - v0;

            // Compute triangle normal. Can be precalculated or cached if
            // intersecting multiple segments against the same triangle
            triNormal = math.cross(ab, ac);

            return IntersectSegmentTriangle(s0, s1, v0, v1, v2, ab, ac, triNormal, out hit);
        }
        public static bool IntersectSegmentTriangle(float3 s0, float3 s1, float3 v0, float3 v1, float3 v2, float3 ab, float3 ac, float3 triNormal, out RaycastHitResult hit)
        {
            hit = default;

            float3 segment = (s1 - s0);

            // Compute denominator d. If d <= 0, segment is parallel to or points
            // away from triangle, so exit early
            float d = math.dot(-segment, triNormal);
            if (d <= 0.0f) return false;

            // Compute intersection t value of pq with plane of triangle. A ray
            // intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
            // dividing by d until intersection has been found to pierce triangle
            float3 ap = s0 - v0;
            float t = math.dot(ap, triNormal);
            if ((t < 0.0f) || (t > d)) return false;

            // Compute barycentric coordinate components and test if within bounds
            float3 e = math.cross(-segment, ap);
            float v = math.dot(ac, e);
            if (v < 0.0f || v > d) return false;

            float w = -math.dot(ab, e);
            if (w < 0.0f || v + w > d) return false;

            float h = t;
            // Segment/ray intersects triangle. Perform delayed division and
            // compute the last barycentric coordinate component
            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
            float u = 1.0f - v - w;

            hit = new RaycastHitResult();
            hit.point = s0 + t * segment;
            hit.distance = t * math.length(segment);
            hit.barycentricCoordinate = new float3(u, v, w);

            hit.normal = math.normalizesafe(triNormal);

            return true;
        }

    }

}

#endif