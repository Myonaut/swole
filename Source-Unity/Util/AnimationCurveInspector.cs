#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.Debugging
{
    [ExecuteInEditMode]
    public class AnimationCurveInspector : MonoBehaviour
    {

        public bool printKeys;

        public AnimationCurve curve;

        private Keyframe[] keys;

        [Serializable]
        public struct CurveView
        {
            public string name;
            public AnimationCurve curve;
        }
        public CurveView[] curveViews;

        public void Update() 
        {
            if (printKeys)
            {
                printKeys = false; 
                PrintKeys();
            }
        }

        public void PrintKeys()
        {
            if (curve == null) return;
            if (keys == null || keys.Length != curve.length) keys = curve.keys;

            for (int a = 0; a < keys.Length; a++)
            {
                var oldKey = keys[a];
                var newKey = curve[a];

                if (oldKey.inTangent != newKey.inTangent || oldKey.inWeight != newKey.inWeight || oldKey.outTangent != newKey.outTangent || oldKey.outWeight != newKey.outWeight || oldKey.time != newKey.time || oldKey.value != newKey.value || oldKey.weightedMode != newKey.weightedMode)
                {
                    keys[a] = newKey;

                    Debug.Log("==============================");
                    Debug.Log($"{a}.inTangent: {newKey.inTangent}");
                    Debug.Log($"{a}.inWeight: {newKey.inWeight}");
                    Debug.Log("-----------------------");
                    Debug.Log($"{a}.outTangent: {newKey.outTangent}");
                    Debug.Log($"{a}.outWeight: {newKey.outWeight}");
                    Debug.Log("-----------------------");
                    Debug.Log($"{a}.time: {newKey.time}");
                    Debug.Log($"{a}.value: {newKey.value}");
                    Debug.Log("-----------------------");
                    Debug.Log($"{a}.weightedMode: {newKey.weightedMode}");
                    Debug.Log("==============================");
                }
            }
        }
    }
}

#endif