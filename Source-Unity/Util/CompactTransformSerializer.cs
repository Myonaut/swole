using System;
using System.IO;

using UnityEngine;

namespace Swole.API.Unity
{
    /// <summary>
    /// Helpers for compactly serializing transform-like data (position/rotation/scale) to binary.
    /// Components that match their default value are omitted entirely from the stream to save memory:
    /// - position is omitted if it is (0, 0, 0)
    /// - rotation is omitted if it is (extremely close to) Quaternion.identity
    /// - scale is omitted if it is (extremely close to) (1, 1, 1)
    /// On read, any omitted component is assumed to be its default value.
    ///
    /// Rotations that are written are always normalized first and compressed using the well known
    /// "smallest three" quaternion compression scheme: the largest magnitude component (which can always
    /// be reconstructed, since the quaternion is unit length) is dropped, its index (2 bits) and the
    /// remaining three components (10 bits each, sign-adjusted so the dropped component is positive) are
    /// packed into a single 32-bit value instead of 4 raw floats (16 bytes -> 4 bytes).
    /// </summary>
    public static class CompactTransformSerializer
    {
        private const float Tolerance = 0.0001f;

        // 1/sqrt(2): the largest-magnitude component of a unit quaternion is always >= this, so every
        // other component is guaranteed to fall within [-SmallestThreeRange, SmallestThreeRange].
        private const float SmallestThreeRange = 0.70710678f;
        private const int BitsPerComponent = 10;
        private const uint MaxComponentValue = (1u << BitsPerComponent) - 1; // 1023

        [Flags]
        private enum Flags : byte
        {
            None = 0,
            HasPosition = 1 << 0,
            HasRotation = 1 << 1,
            HasScale = 1 << 2
        }

        public static bool IsDefaultPosition(Vector3 position) => position.sqrMagnitude <= (Tolerance * Tolerance);

        public static bool IsDefaultRotation(Quaternion rotation) =>
            (Mathf.Abs(rotation.x - Quaternion.identity.x) <= Tolerance) &&
            (Mathf.Abs(rotation.y - Quaternion.identity.y) <= Tolerance) &&
            (Mathf.Abs(rotation.z - Quaternion.identity.z) <= Tolerance) &&
            (Mathf.Abs(rotation.w - Quaternion.identity.w) <= Tolerance);

        public static bool IsDefaultScale(Vector3 scale) =>
            (Mathf.Abs(scale.x - 1f) <= Tolerance) &&
            (Mathf.Abs(scale.y - 1f) <= Tolerance) &&
            (Mathf.Abs(scale.z - 1f) <= Tolerance);

        public static void WriteVector3(BinaryWriter writer, Vector3 value)
        {
            writer.Write(value.x);
            writer.Write(value.y);
            writer.Write(value.z);
        }

        public static Vector3 ReadVector3(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        /// <summary>Normalizes and packs a quaternion into 32 bits using "smallest three" compression.</summary>
        public static uint PackQuaternionSmallestThree(Quaternion value)
        {
            value = Quaternion.Normalize(value);

            float x = value.x, y = value.y, z = value.z, w = value.w;
            float ax = Mathf.Abs(x), ay = Mathf.Abs(y), az = Mathf.Abs(z), aw = Mathf.Abs(w);

            int largestIndex = 0;
            float largestValue = x;
            float largestAbs = ax;

            if (ay > largestAbs) { largestAbs = ay; largestValue = y; largestIndex = 1; }
            if (az > largestAbs) { largestAbs = az; largestValue = z; largestIndex = 2; }
            if (aw > largestAbs) { largestAbs = aw; largestValue = w; largestIndex = 3; }

            // A quaternion and its negation represent the same rotation. If the dropped (largest)
            // component is negative, negate the whole quaternion so it becomes positive and can be
            // reconstructed unambiguously from the other three components on read.
            if (largestValue < 0f)
            {
                x = -x; y = -y; z = -z; w = -w;
            }

            float c0 = 0f, c1 = 0f, c2 = 0f;
            int ci = 0;
            if (largestIndex != 0) { if (ci == 0) c0 = x; else if (ci == 1) c1 = x; else c2 = x; ci++; }
            if (largestIndex != 1) { if (ci == 0) c0 = y; else if (ci == 1) c1 = y; else c2 = y; ci++; }
            if (largestIndex != 2) { if (ci == 0) c0 = z; else if (ci == 1) c1 = z; else c2 = z; ci++; }
            if (largestIndex != 3) { if (ci == 0) c0 = w; else if (ci == 1) c1 = w; else c2 = w; ci++; }

            uint a = EncodeComponent(c0);
            uint b = EncodeComponent(c1);
            uint c = EncodeComponent(c2);

            return ((uint)largestIndex << 30) | (a << 20) | (b << 10) | c;
        }

        /// <summary>Unpacks a quaternion previously packed by <see cref="PackQuaternionSmallestThree"/>.</summary>
        public static Quaternion UnpackQuaternionSmallestThree(uint packed)
        {
            int largestIndex = (int)((packed >> 30) & 0x3);
            uint a = (packed >> 20) & MaxComponentValue;
            uint b = (packed >> 10) & MaxComponentValue;
            uint c = packed & MaxComponentValue;

            float c0 = DecodeComponent(a);
            float c1 = DecodeComponent(b);
            float c2 = DecodeComponent(c);

            float sumSquares = (c0 * c0) + (c1 * c1) + (c2 * c2);
            float largestValue = Mathf.Sqrt(Mathf.Max(0f, 1f - sumSquares));

            float x = 0f, y = 0f, z = 0f, w = 0f;
            int ci = 0;
            if (largestIndex == 0) x = largestValue; else { if (ci == 0) x = c0; else if (ci == 1) x = c1; else x = c2; ci++; }
            if (largestIndex == 1) y = largestValue; else { if (ci == 0) y = c0; else if (ci == 1) y = c1; else y = c2; ci++; }
            if (largestIndex == 2) z = largestValue; else { if (ci == 0) z = c0; else if (ci == 1) z = c1; else z = c2; ci++; }
            if (largestIndex == 3) w = largestValue; else { if (ci == 0) w = c0; else if (ci == 1) w = c1; else w = c2; ci++; }

            return new Quaternion(x, y, z, w);
        }

        private static uint EncodeComponent(float value)
        {
            float normalized = (value + SmallestThreeRange) / (2f * SmallestThreeRange); // 0..1
            normalized = Mathf.Clamp01(normalized);
            return (uint)Mathf.RoundToInt(normalized * MaxComponentValue);
        }

        private static float DecodeComponent(uint encoded)
        {
            float normalized = encoded / (float)MaxComponentValue;
            return (normalized * (2f * SmallestThreeRange)) - SmallestThreeRange;
        }

        public static void WriteQuaternion(BinaryWriter writer, Quaternion value) => writer.Write(PackQuaternionSmallestThree(value));

        public static Quaternion ReadQuaternion(BinaryReader reader) => UnpackQuaternionSmallestThree(reader.ReadUInt32()); 

        /// <summary>Writes position/rotation/scale, omitting any component that matches its default value.</summary>
        public static void WriteCompactTransform(BinaryWriter writer, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            bool hasPosition = !IsDefaultPosition(position);
            bool hasRotation = !IsDefaultRotation(rotation);
            bool hasScale = !IsDefaultScale(scale);

            Flags flags = Flags.None;
            if (hasPosition) flags |= Flags.HasPosition;
            if (hasRotation) flags |= Flags.HasRotation;
            if (hasScale) flags |= Flags.HasScale;

            writer.Write((byte)flags);

            if (hasPosition) WriteVector3(writer, position);
            if (hasRotation) WriteQuaternion(writer, rotation);
            if (hasScale) WriteVector3(writer, scale);
        }

        /// <summary>Reads position/rotation/scale, defaulting any component omitted by the writer.</summary>
        public static void ReadCompactTransform(BinaryReader reader, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            Flags flags = (Flags)reader.ReadByte();

            position = (flags & Flags.HasPosition) != 0 ? ReadVector3(reader) : Vector3.zero;
            rotation = (flags & Flags.HasRotation) != 0 ? ReadQuaternion(reader) : Quaternion.identity;
            scale = (flags & Flags.HasScale) != 0 ? ReadVector3(reader) : Vector3.one;
        }
    }
}
