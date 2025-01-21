#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    [Serializable]
    public struct SerializedAnimationCurve : ISerializableContainer<AnimationCurve, SerializedAnimationCurve>, ISerializableContainer<EditableAnimationCurve, SerializedAnimationCurve>
    {

        public string name;
        public string SerializedName => name;

        public SerializedKeyframe[] keyframes;
        public AnimationCurveEditor.State keyframeStates;

        public Keyframe[] DeserializeKeyframes()
        {
            if (keyframes == null && keyframeStates.keyframes == null) return null;

            Keyframe[] deserializedKeyframes = null;

            if (keyframeStates.keyframes != null && (keyframeStates.keyframes.Length > 0 || keyframes == null)) 
            {
                deserializedKeyframes = new Keyframe[keyframeStates.keyframes.Length];
                for (int a = 0; a < keyframeStates.keyframes.Length; a++) deserializedKeyframes[a] = keyframeStates.keyframes[a];
            } 
            else
            {
                deserializedKeyframes = new Keyframe[keyframes.Length];
                for (int a = 0; a < keyframes.Length; a++) deserializedKeyframes[a] = keyframes[a];
            }

            return deserializedKeyframes;
        }

        AnimationCurve ISerializableContainer<AnimationCurve, SerializedAnimationCurve>.AsOriginalType(PackageInfo packageInfo)  
        {
            var keyframes = DeserializeKeyframes();
            if (keyframes == null || keyframes.Length <= 0) return null; // Treat empty curves as null

            var curve = new AnimationCurve(keyframes);
            curve.preWrapMode = (WrapMode)(keyframeStates.preWrapMode == 0 ? (int)WrapMode.Clamp : keyframeStates.preWrapMode);
            curve.postWrapMode = (WrapMode)(keyframeStates.postWrapMode == 0 ? (int)WrapMode.Clamp : keyframeStates.postWrapMode);    
            return curve;
        }
        public AnimationCurve AsAnimationCurve(PackageInfo packageInfo = default) => (this as ISerializableContainer<AnimationCurve, SerializedAnimationCurve>).AsOriginalType(packageInfo); 
        EditableAnimationCurve ISerializableContainer<EditableAnimationCurve, SerializedAnimationCurve>.AsOriginalType(PackageInfo packageInfo) => ((keyframeStates.keyframes != null && keyframeStates.keyframes.Length > 0) || (keyframes == null || keyframes.Length <= 0)) ? ((keyframeStates.keyframes == null || keyframeStates.keyframes.Length <= 0) ? null : new EditableAnimationCurve(this)) : new EditableAnimationCurve(AsAnimationCurve(), name); // Treat empty curves as null
        public EditableAnimationCurve AsEditableAnimationCurve(PackageInfo packageInfo = default) => (this as ISerializableContainer<EditableAnimationCurve, SerializedAnimationCurve>).AsOriginalType(packageInfo);    

        public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);  

        public object AsNonserializableObject(PackageInfo packageInfo = default) => AsEditableAnimationCurve(); 


        public static implicit operator AnimationCurve(SerializedAnimationCurve inCurve) => inCurve.AsAnimationCurve();  

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
        public static implicit operator SerializedAnimationCurve(AnimationCurveEditor.State inState)
        {
            var serializedCurve = new SerializedAnimationCurve();
            serializedCurve.keyframeStates = inState;
            return serializedCurve;
        }
        public static implicit operator SerializedAnimationCurve(EditableAnimationCurve inCurve)
        {
            if (inCurve != null) 
            {
                SerializedAnimationCurve data = inCurve.State; 
                data.name = inCurve.Name; 
                return data;
            }
            return default;
        }

    }

    [Serializable]
    public struct SerializedKeyframe : ISerializableContainer<Keyframe, SerializedKeyframe>
    {

        public string SerializedName => nameof(SerializedKeyframe);

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