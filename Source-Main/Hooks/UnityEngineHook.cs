#if (UNITY_EDITOR || UNITY_STANDALONE)
#define FOUND_UNITY
using UnityEngine;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole
{

    public class UnityEngineHook : EngineHook
    {

        public override string Name => "Unity";

#if FOUND_UNITY

        protected static EngineHook activeHook;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Initialize()
        {
            if (!(typeof(UnityEngineHook).IsAssignableFrom(Swole.Engine.GetType()))) 
            {
                activeHook = new UnityEngineHook();
                Swole.SetEngine(activeHook); 
            }
        }

        protected class PersistentBehaviour : MonoBehaviour
        {

            [NonSerialized]
            private EngineHook hook;

            public static PersistentBehaviour New(EngineHook hook)
            {
                var b = new GameObject($"_{nameof(Swole)}").AddComponent<PersistentBehaviour>();
                DontDestroyOnLoad(b.gameObject);
                b.hook = hook;
                return b;
            }

            protected void Update()
            {
                hook?.UpdateSingletons(); 
            }
            protected void LateUpdate()
            {
                hook?.UpdateSingletonsLate();
            }
            protected void FixedUpdate()
            {
                hook?.UpdateSingletonsPhysics();
            }

        }

        protected static PersistentBehaviour behaviour;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartRunning()
        {
            if (behaviour == null) behaviour = PersistentBehaviour.New(activeHook);
        }

        public class UnityLogger : SwoleLogger
        {
            protected override void LogInternal(string message) => Debug.Log(message);

            protected override void LogErrorInternal(string error) => Debug.LogError(error);

            protected override void LogWarningInternal(string warning) => Debug.LogWarning(warning);

        }

        public override SwoleLogger Logger
        {

            get
            {

                if (logger == null) logger = new UnityLogger();

                return logger;

            }

        }

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

        public static Matrix4x4 AsUnityMatrix(EngineInternal.Matrix4x4 m) => new Matrix4x4(AsUnityVector(m.GetColumn(0)), AsUnityVector(m.GetColumn(1)), AsUnityVector(m.GetColumn(2)), AsUnityVector(m.GetColumn(3)));

        public static GameObject AsUnityGameObject(EngineInternal.GameObject gameObject)
        {
            if (gameObject.instance is GameObject unityGameObject) return unityGameObject;
            return null;
        }

        public static Transform AsUnityTransform(EngineInternal.Transform transform)
        {
            if (transform.instance is Transform unityTransform) return unityTransform;
            return null;
        }

        #endregion

        #region Conversions | Unity -> Swole

        public static EngineInternal.Vector2 AsSwoleVector(Vector2 v2) => new EngineInternal.Vector2(v2.x, v2.y);
        public static EngineInternal.Vector3 AsSwoleVector(Vector3 v3) => new EngineInternal.Vector3(v3.x, v3.y, v3.z);
        public static EngineInternal.Vector4 AsSwoleVector(Vector4 v4) => new EngineInternal.Vector4(v4.x, v4.y, v4.z, v4.w);

        public static EngineInternal.Quaternion AsSwoleQuaternion(Quaternion q) => new EngineInternal.Quaternion(q.x, q.y, q.z, q.w);

        public static EngineInternal.Matrix4x4 AsSwoleMatrix(Matrix4x4 m) => new EngineInternal.Matrix4x4(AsSwoleVector(m.GetColumn(0)), AsSwoleVector(m.GetColumn(1)), AsSwoleVector(m.GetColumn(2)), AsSwoleVector(m.GetColumn(3)));

        public static EngineInternal.GameObject AsSwoleGameObject(GameObject gameObject)
        {
            if (gameObject == null) return default;
            return new EngineInternal.GameObject(gameObject, AsSwoleTransform(gameObject.transform));
        }

        public static EngineInternal.Transform AsSwoleTransform(Transform transform)
        {
            if (transform == null) return default;
            return new EngineInternal.Transform(transform);
        }

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

        public override void SetLocalPosition(object engineObject, EngineInternal.Vector3 localPosition) 
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return;
            transform.localPosition = AsUnityVector(localPosition);
        }
        public override void SetLocalRotation(object engineObject, EngineInternal.Quaternion localRotation) 
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return;
            transform.localRotation = AsUnityQuaternion(localRotation);
        }
        public override void SetLocalScale(object engineObject, EngineInternal.Vector3 localScale)
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return;
            transform.localScale = AsUnityVector(localScale);
        }

        public override void SetWorldPosition(object engineObject, EngineInternal.Vector3 position) 
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return;
            transform.position = AsUnityVector(position);
        }
        public override void SetWorldRotation(object engineObject, EngineInternal.Quaternion rotation) 
        {
            var transform = AsTransform(engineObject);
            if (transform == null) return;
            transform.rotation = AsUnityQuaternion(rotation);
        }

        public override string GetName(object engineObject) 
        {

            if (engineObject is UnityEngine.Object unityObject)
            {
                return unityObject.name;
            }

            return "";

        }

        public override EngineInternal.Quaternion Mul(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB) => AsSwoleQuaternion(AsUnityQuaternion(qA) * AsUnityQuaternion(qB));
        public override EngineInternal.Vector3 Mul(EngineInternal.Quaternion q, EngineInternal.Vector3 v) => AsSwoleVector(AsUnityQuaternion(q) * AsUnityVector(v));
        public override EngineInternal.Matrix4x4 Mul(EngineInternal.Matrix4x4 mA, EngineInternal.Matrix4x4 mB) => AsSwoleMatrix(AsUnityMatrix(mA) * AsUnityMatrix(mB));

        public override EngineInternal.Quaternion Quaternion_Euler(float x, float y, float z) => AsSwoleQuaternion(Quaternion.Euler(x, y, z));

        public override EngineInternal.Matrix4x4 Matrix4x4_TRS(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation, EngineInternal.Vector3 scale) => AsSwoleMatrix(Matrix4x4.TRS(AsUnityVector(position), AsUnityQuaternion(rotation), AsUnityVector(scale)));

        public override EngineInternal.Transform Transform_GetParent(EngineInternal.Transform transform) 
        {
            var unityTransform = AsUnityTransform(transform);
            if (unityTransform == null) return default;
            return AsSwoleTransform(unityTransform.parent);

        }
        public override void Transform_SetParent(EngineInternal.Transform transform, EngineInternal.Transform parent, bool worldPositionStays = true) 
        {
            var unityTransform = AsUnityTransform(transform);
            if (unityTransform == null) return;
            unityTransform.SetParent(AsUnityTransform(parent), worldPositionStays);
        }

        public override EngineInternal.GameObject GameObject_Create(string name = "") => AsSwoleGameObject(new GameObject(name));

        public override EngineInternal.GameObject GameObject_Instantiate(EngineInternal.GameObject gameObject) 
        {
            var unityGameObject = AsUnityGameObject(gameObject);
            if (unityGameObject == null) return default;
            return AsSwoleGameObject(GameObject.Instantiate(unityGameObject));
        }
        public override void GameObject_Destroy(EngineInternal.GameObject gameObject, float timeDelay = 0) 
        {
            var unityGameObject = AsUnityGameObject(gameObject);
            if (unityGameObject == null) return;
            GameObject.Destroy(unityGameObject, timeDelay);
        }

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
