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

        protected override string ToJsonInternal(object obj, bool prettyPrint = false) => JsonUtility.ToJson(obj, prettyPrint);
        protected override object FromJsonInternal(string json, Type type) => JsonUtility.FromJson(json, type);
        protected override T FromJsonInternal<T>(string json) => JsonUtility.FromJson<T>(json);

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

        public static bool IsUnityType(System.Type type)
        {

            if (type == null) return false;

            if (typeof(UnityEngine.Object).IsAssignableFrom(type)) return true;

            if (type == typeof(Vector2)) return true;
            if (type == typeof(Vector3)) return true;
            if (type == typeof(Vector4)) return true;
            if (type == typeof(Quaternion)) return true;
            if (type == typeof(Matrix4x4)) return true;

            return false;

        }

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

        public override EngineInternal.Vector3 Mul(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => AsSwoleVector(AsUnityMatrix(m).MultiplyPoint(AsUnityVector(point)));
        public override EngineInternal.Vector3 Mul3x4(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => AsSwoleVector(AsUnityMatrix(m).MultiplyPoint3x4(AsUnityVector(point)));
        public override EngineInternal.Vector3 Rotate(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 vector) => AsSwoleVector(AsUnityMatrix(m).MultiplyVector(AsUnityVector(vector)));

        public override EngineInternal.Quaternion Quaternion_Euler(float x, float y, float z) => AsSwoleQuaternion(Quaternion.Euler(x, y, z));
        public override EngineInternal.Vector3 Quaternion_EulerAngles(EngineInternal.Quaternion quaternion) => AsSwoleVector(AsUnityQuaternion(quaternion).eulerAngles);
        public override EngineInternal.Quaternion Quaternion_Inverse(EngineInternal.Quaternion q) => AsSwoleQuaternion(Quaternion.Inverse(AsUnityQuaternion(q)));

        public override EngineInternal.Matrix4x4 Matrix4x4_TRS(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation, EngineInternal.Vector3 scale) => AsSwoleMatrix(Matrix4x4.TRS(AsUnityVector(position), AsUnityQuaternion(rotation), AsUnityVector(scale)));
        public override EngineInternal.Matrix4x4 Matrix4x4_Scale(EngineInternal.Vector3 vector) => AsSwoleMatrix(Matrix4x4.Scale(AsUnityVector(vector)));
        public override EngineInternal.Matrix4x4 Matrix4x4_Translate(EngineInternal.Vector3 vector) => AsSwoleMatrix(Matrix4x4.Translate(AsUnityVector(vector)));
        public override EngineInternal.Matrix4x4 Matrix4x4_Rotate(EngineInternal.Quaternion q) => AsSwoleMatrix(Matrix4x4.Rotate(AsUnityQuaternion(q)));

        #region Transforms

        public override EngineInternal.Transform Transform_GetParent(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleTransform(AsUnityTransform(transform).parent);
        public override void Transform_SetParent(EngineInternal.Transform transform, EngineInternal.Transform parent, bool worldPositionStays = true) { if (transform.instance != null) AsUnityTransform(transform).SetParent(AsUnityTransform(parent), worldPositionStays); }
        public override EngineInternal.Vector3 Transform_lossyScale(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).lossyScale);
        public override EngineInternal.Vector3 Transform_eulerAnglesGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).eulerAngles);
        public override void Transform_eulerAnglesSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { if (transform.instance != null) AsUnityTransform(transform).eulerAngles = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_localEulerAnglesGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).localEulerAngles);
        public override void Transform_localEulerAnglesSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { if (transform.instance != null) AsUnityTransform(transform).localEulerAngles = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_rightGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).right);
        public override void Transform_rightSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { if (transform.instance != null) AsUnityTransform(transform).right = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_upGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).up);
        public override void Transform_upSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { if (transform.instance != null) AsUnityTransform(transform).up = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_forwardGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).forward);
        public override void Transform_forwardSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { if (transform.instance != null) AsUnityTransform(transform).forward = AsUnityVector(val); }
        public override EngineInternal.Matrix4x4 Transform_worldToLocalMatrix(EngineInternal.Transform transform) => transform.instance == null ? EngineInternal.Matrix4x4.identity : AsSwoleMatrix(AsUnityTransform(transform).worldToLocalMatrix);
        public override EngineInternal.Matrix4x4 Transform_localToWorldMatrix(EngineInternal.Transform transform) => transform.instance == null ? EngineInternal.Matrix4x4.identity : AsSwoleMatrix(AsUnityTransform(transform).localToWorldMatrix);
        public override EngineInternal.Transform Transform_root(EngineInternal.Transform transform) => transform.instance == null ? default : AsSwoleTransform(AsUnityTransform(transform).root);
        public override int Transform_childCount(EngineInternal.Transform transform) => transform.instance == null ? default : AsUnityTransform(transform).childCount;
        public override bool Transform_hasChangedGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsUnityTransform(transform).hasChanged;
        public override void Transform_hasChangedSet(EngineInternal.Transform transform, bool val) { if (transform.instance != null) AsUnityTransform(transform).hasChanged = val; }
        public override int Transform_hierarchyCapacityGet(EngineInternal.Transform transform) => transform.instance == null ? default : AsUnityTransform(transform).hierarchyCapacity;
        public override void Transform_hierarchyCapacitySet(EngineInternal.Transform transform, int val) { if (transform.instance != null) AsUnityTransform(transform).hierarchyCapacity = val; }
        public override int Transform_hierarchyCount(EngineInternal.Transform transform) => transform.instance == null ? default : AsUnityTransform(transform).hierarchyCount;
        public override void Transform_SetPositionAndRotation(EngineInternal.Transform transform, EngineInternal.Vector3 position, EngineInternal.Quaternion rotation) { if (transform.instance != null) AsUnityTransform(transform).SetPositionAndRotation(AsUnityVector(position), AsUnityQuaternion(rotation)); }
        public override void Transform_SetLocalPositionAndRotation(EngineInternal.Transform transform, EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation) { if (transform.instance != null) AsUnityTransform(transform).SetLocalPositionAndRotation(AsUnityVector(localPosition), AsUnityQuaternion(localRotation)); }
        public override void Transform_GetPositionAndRotation(EngineInternal.Transform transform, out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation) { position = default; rotation = default; }
        public override void Transform_GetLocalPositionAndRotation(EngineInternal.Transform transform, out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation) { localPosition = default; localRotation = default; }
        public override void Transform_Translate(EngineInternal.Transform transform, EngineInternal.Vector3 translation, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (transform.instance != null) AsUnityTransform(transform).Translate(AsUnityVector(translation), (Space)(int)relativeTo); }
        public override void Transform_Translate(EngineInternal.Transform transform, EngineInternal.Vector3 translation) { if (transform.instance != null) AsUnityTransform(transform).Translate(AsUnityVector(translation)); }
        public override void Transform_Translate(EngineInternal.Transform transform, float x, float y, float z, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (transform.instance != null) AsUnityTransform(transform).Translate(x, y, z, (Space)(int)relativeTo); }
        public override void Transform_Translate(EngineInternal.Transform transform, float x, float y, float z) { if (transform.instance != null) AsUnityTransform(transform).Translate(x, y, z); }
        public override void Transform_Translate(EngineInternal.Transform transform, EngineInternal.Vector3 translation, EngineInternal.Transform relativeTo) { if (transform.instance != null) AsUnityTransform(transform).Translate(AsUnityVector(translation), AsUnityTransform(relativeTo)); }
        public override void Transform_Translate(EngineInternal.Transform transform, float x, float y, float z, EngineInternal.Transform relativeTo) { if (transform.instance != null) AsUnityTransform(transform).Translate(x, y, z, AsUnityTransform(relativeTo)); }
        public override void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 eulers, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (transform.instance != null) AsUnityTransform(transform).Rotate(AsUnityVector(eulers), (Space)(int)relativeTo); }
        public override void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 eulers) { if (transform.instance != null) AsUnityTransform(transform).Rotate(AsUnityVector(eulers)); }
        public override void Transform_Rotate(EngineInternal.Transform transform, float xAngle, float yAngle, float zAngle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (transform.instance != null) AsUnityTransform(transform).Rotate(xAngle, yAngle, zAngle, (Space)(int)relativeTo); }
        public override void Transform_Rotate(EngineInternal.Transform transform, float xAngle, float yAngle, float zAngle) { if (transform.instance != null) AsUnityTransform(transform).Rotate(xAngle, yAngle, zAngle); }
        public override void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 axis, float angle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (transform.instance != null) AsUnityTransform(transform).Rotate(AsUnityVector(axis), angle, (Space)(int)relativeTo); }
        public override void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 axis, float angle) { if (transform.instance != null) AsUnityTransform(transform).Rotate(AsUnityVector(axis), angle); }
        public override void Transform_RotateAround(EngineInternal.Transform transform, EngineInternal.Vector3 point, EngineInternal.Vector3 axis, float angle) { if (transform.instance != null) AsUnityTransform(transform).RotateAround(AsUnityVector(point), AsUnityVector(axis), angle); }
        public override void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Transform target, EngineInternal.Vector3 worldUp) { if (transform.instance != null) AsUnityTransform(transform).LookAt(AsUnityTransform(target), AsUnityVector(worldUp)); }
        public override void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Transform target) { if (transform.instance != null) AsUnityTransform(transform).LookAt(AsUnityTransform(target)); }
        public override void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Vector3 worldPosition, EngineInternal.Vector3 worldUp) { if (transform.instance != null) AsUnityTransform(transform).LookAt(AsUnityVector(worldPosition), AsUnityVector(worldUp)); }
        public override void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Vector3 worldPosition) { if (transform.instance != null) AsUnityTransform(transform).LookAt(AsUnityVector(worldPosition)); }
        public override EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.Transform transform, EngineInternal.Vector3 direction) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).TransformDirection(AsUnityVector(direction)));
        public override EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.Transform transform, float x, float y, float z) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).TransformDirection(x, y, z));
        public override EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.Transform transform, EngineInternal.Vector3 direction) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformDirection(AsUnityVector(direction)));
        public override EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.Transform transform, float x, float y, float z) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformDirection(x, y, z));
        public override EngineInternal.Vector3 Transform_TransformVector(EngineInternal.Transform transform, EngineInternal.Vector3 vector) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).TransformVector(AsUnityVector(vector)));
        public override EngineInternal.Vector3 Transform_TransformVector(EngineInternal.Transform transform, float x, float y, float z) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).TransformVector(x, y, z));
        public override EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.Transform transform, EngineInternal.Vector3 vector) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformVector(AsUnityVector(vector)));
        public override EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.Transform transform, float x, float y, float z) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformVector(x, y, z));
        public override EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.Transform transform, EngineInternal.Vector3 position) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).TransformPoint(AsUnityVector(position)));
        public override EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.Transform transform, float x, float y, float z) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).TransformPoint(x, y, z));
        public override EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.Transform transform, EngineInternal.Vector3 position) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformPoint(AsUnityVector(position)));
        public override EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.Transform transform, float x, float y, float z) => transform.instance == null ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformPoint(x, y, z));
        public override void Transform_DetachChildren(EngineInternal.Transform transform) { if (transform.instance != null) AsUnityTransform(transform).DetachChildren(); }
        public override void Transform_SetAsFirstSibling(EngineInternal.Transform transform) { if (transform.instance != null) AsUnityTransform(transform).SetAsFirstSibling(); }
        public override void Transform_SetAsLastSibling(EngineInternal.Transform transform) { if (transform.instance != null) AsUnityTransform(transform).SetAsLastSibling(); }
        public override void Transform_SetSiblingIndex(EngineInternal.Transform transform, int index) { if (transform.instance != null) AsUnityTransform(transform).SetSiblingIndex(index); }
        public override int Transform_GetSiblingIndex(EngineInternal.Transform transform) => transform.instance == null ? default : AsUnityTransform(transform).GetSiblingIndex();
        public override EngineInternal.Transform Transform_Find(EngineInternal.Transform transform, string n) => transform.instance == null ? default : AsSwoleTransform(AsUnityTransform(transform).Find(n));
        public override bool Transform_IsChildOf(EngineInternal.Transform transform, EngineInternal.Transform parent) => transform.instance == null ? default : AsUnityTransform(transform).IsChildOf(AsUnityTransform(parent));
        public override EngineInternal.Transform Transform_GetChild(EngineInternal.Transform transform, int index) => transform.instance == null ? default : AsSwoleTransform(AsUnityTransform(transform).GetChild(index));

        #endregion

        #region GameObjects

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

        #endregion

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
