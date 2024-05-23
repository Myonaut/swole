#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;

using static Swole.DataStructures;

namespace Swole
{

    public static class Data
    {

        /// <summary>
        ///SOURCE: https://stackoverflow.com/questions/20752344/storing-2-single-floats-in-one
        /// </summary>
        /* yourBiggestNumber * scaleFactor < cp */
        private const double scaleFactor = 65530.0;
        private const double cp = 256.0 * 256.0;

        /// <summary>
        /// packs given two floats into one float (range 0..1)
        /// </summary>
        public static float FloatPack(float2 values)
        {
            return FloatPack(values.x, values.y);
        }

        /// <summary>
        /// packs given two floats into one float (range 0..1)
        /// </summary>
        public static float FloatPack(float x, float y)
        {
            int x1 = (int)(x * scaleFactor);
            int y1 = (int)(y * scaleFactor);
            float f = (float)((y1 * cp) + x1);
            return f;
        }

        /// <summary>
        /// unpacks given float to two floats (range 0..1)
        /// </summary>
        public static float2 FloatUnpack(float f)
        {
            double dy = math.floor(f / cp);
            double dx = f - (dy * cp);
            return new float2((float)(dx / scaleFactor), (float)(dy / scaleFactor));
        }

        public static float GetChannel(this Color color, RGBAChannel channel, bool average = true)
        {

            float val = 0;

            int i = 0;

            if (channel.HasFlag(RGBAChannel.R)) { val += color.r; i++; }
            if (channel.HasFlag(RGBAChannel.G)) { val += color.g; i++; }
            if (channel.HasFlag(RGBAChannel.B)) { val += color.b; i++; }
            if (channel.HasFlag(RGBAChannel.A)) { val += color.a; i++; }

            if (average && i > 1) val = val / i;

            return val;

        }

    }

}

#endif