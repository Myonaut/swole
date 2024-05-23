#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [Serializable]
    public struct SerializedAnimationCurve : ISerializableContainer<AnimationCurve, SerializedAnimationCurve>
    {

        public SerializedKeyframe[] keyframes;

        public Keyframe[] DeserializeKeyframes()
        {
            if (keyframes == null) return null;

            Keyframe[] deserializedKeyframes = new Keyframe[keyframes.Length];
            for (int a = 0; a < keyframes.Length; a++) deserializedKeyframes[a] = keyframes[a];

            return deserializedKeyframes;
        }

        public AnimationCurve AsOriginalType(PackageInfo packageInfo = default) 
        {
            var keyframes = DeserializeKeyframes();
            if (keyframes == null || keyframes.Length <= 0) return null; // Treat empty curves as null
            return new AnimationCurve(keyframes);  
        }
        public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

        public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

        public static implicit operator AnimationCurve(SerializedAnimationCurve inCurve) => inCurve.AsOriginalType();

        public static implicit operator SerializedAnimationCurve(AnimationCurve inCurve)
        {
            if (inCurve != null)
            {
                var serializedCurve = new SerializedAnimationCurve();

                var keyframes = inCurve.keys;
                if (keyframes != null)
                {
                    serializedCurve.keyframes = new SerializedKeyframe[keyframes.Length];
                    for (int a = 0; a < serializedCurve.keyframes.Length; a++) serializedCurve.keyframes[a] = keyframes[a];
                }

                return serializedCurve;
            }

            return default;
        }

    }

    [Serializable]
    public struct SerializedKeyframe : ISerializableContainer<Keyframe, SerializedKeyframe>
    {

        public float time;

        public float value;

        public float inTangent;

        public float outTangent;

        public int weightedMode;

        public float inWeight;

        public float outWeight;

        public static implicit operator Keyframe(SerializedKeyframe inKey)
        {
            Keyframe key = new Keyframe() { inTangent = inKey.inTangent, inWeight = inKey.inWeight, outTangent = inKey.outTangent, outWeight = inKey.outWeight, time = inKey.time, value = inKey.value, weightedMode = (WeightedMode)inKey.weightedMode };

            return key;
        }

        public static implicit operator SerializedKeyframe(Keyframe inKey)
        {
            SerializedKeyframe key = new SerializedKeyframe() { inTangent = inKey.inTangent, inWeight = inKey.inWeight, outTangent = inKey.outTangent, outWeight = inKey.outWeight, time = inKey.time, value = inKey.value, weightedMode = (int)inKey.weightedMode };

            return key;
        }

        public Keyframe AsOriginalType(PackageInfo packageInfo = default) => this;
        public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);
        public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);

    }

    public static class AnimationCurveSerialization
    {

        public static SerializedAnimationCurve AsSerializableStruct(this AnimationCurve curve) => curve;

    }

}

#endif