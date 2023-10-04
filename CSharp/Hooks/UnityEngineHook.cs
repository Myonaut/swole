#if (UNITY_EDITOR || UNITY_STANDALONE)
#define FOUND_UNITY
using UnityEngine;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

namespace Swolescript
{

    public class UnityEngineHook : EngineHook
    {

        public override string Name => "Unity";

#if FOUND_UNITY

        public override string WorkingDirectory => Application.persistentDataPath;

        #region JSON Serialization

        public override string ToJson(object obj, bool prettyPrint = false) => JsonUtility.ToJson(obj, prettyPrint);
        public override object FromJson(string json, Type type) => JsonUtility.FromJson(json, type);
        public override T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);

        #endregion

        #region Conversions | Swole -> Unity

        public static Vector2 AsUnityVector(EngineInternal.Vector2 v2) => new Vector2(v2.x, v2.y);
        public static Vector3 AsUnityVector(EngineInternal.Vector3 v3) => new Vector3(v3.x, v3.y, v3.z);
        public static Vector4 AsUnityVector(EngineInternal.Vector4 v4) => new Vector4(v4.x, v4.y, v4.z, v4.w);

        public static Quaternion AsUnityQuaternion(EngineInternal.Quaternion q) => new Quaternion(q.x, q.y, q.z, q.w);

        #endregion

        #region Conversions | Unity -> Swole

        public static EngineInternal.Vector2 AsSwoleVector(Vector2 v2) => new EngineInternal.Vector2(v2.x, v2.y);
        public static EngineInternal.Vector3 AsSwoleVector(Vector3 v3) => new EngineInternal.Vector3(v3.x, v3.y, v3.z);
        public static EngineInternal.Vector4 AsSwoleVector(Vector4 v4) => new EngineInternal.Vector4(v4.x, v4.y, v4.z, v4.w);

        public static EngineInternal.Quaternion AsSwoleQuaternion(Quaternion q) => new EngineInternal.Quaternion(q.x, q.y, q.z, q.w);

        #endregion

        public static Transform AsTransform(object engineObject)
        {
            if (engineObject is Transform transform)
            {
                return transform;
            }
            else if (engineObject is GameObject gameObject)
            {
                return gameObject.transform;
            }
            else if (engineObject is Component component)
            {
                gameObject = component.gameObject;
                if (gameObject == null) return null;
                return gameObject.transform;
            }
            return null;
        }

        public static GameObject AsGameObject(object engineObject)
        {
            if (engineObject is GameObject gameObject)
            {
                return gameObject;
            }
            else if (engineObject is Component component)
            {
                return component.gameObject;
            }
            return null;
        }

        public override EngineInternal.Vector3 GetLocalPosition(object engineObject) 
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.zero;
            return AsSwoleVector(transform.localPosition);
        }
        public override EngineInternal.Vector3 GetLocalScale(object engineObject)
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.one;
            return AsSwoleVector(transform.localScale);
        }
        public override EngineInternal.Quaternion GetLocalRotation(object engineObject)
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return EngineInternal.Quaternion.identity;
            return AsSwoleQuaternion(transform.localRotation);
        }

        public override EngineInternal.Vector3 GetWorldPosition(object engineObject)
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.zero;
            return AsSwoleVector(transform.position);
        }
        public override EngineInternal.Vector3 GetLossyScale(object engineObject)
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.one;
            return AsSwoleVector(transform.lossyScale);
        }
        public override EngineInternal.Quaternion GetWorldRotation(object engineObject)
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return EngineInternal.Quaternion.identity;
            return AsSwoleQuaternion(transform.rotation);
        }

#endif

    }

}
