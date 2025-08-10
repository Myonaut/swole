#if (UNITY_EDITOR || UNITY_STANDALONE)
#define FOUND_UNITY
using UnityEngine;
using Swole.Unity;
using Swole.API.Unity;
using Swole.API.Unity.Animation;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

using Swole.Animation;
using Swole.Script;

namespace Swole
{

    public class UnityEngineHook : EngineHook
    {

        public override string Name => "Unity";

#if FOUND_UNITY

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            deviceID = SystemInfo.deviceUniqueIdentifier;

            if (!(typeof(UnityEngineHook).IsAssignableFrom(swole.Engine.GetType())))
            {
                activeHook = new UnityEngineHook();
                swole.SetEngine(activeHook);
            }
        }

        protected class PersistentBehaviour : MonoBehaviour
        {

            [NonSerialized]
            private EngineHook hook; 

            public static PersistentBehaviour New(EngineHook hook)
            {
                var b = new GameObject($"~{nameof(swole)}").AddComponent<PersistentBehaviour>();
                DontDestroyOnLoad(b.gameObject);
                b.hook = hook; 
                return b;
            }

            protected void Update()
            {
                hook?.FrameUpdate();
            }
            protected void LateUpdate()
            {
                hook?.FrameUpdateLate();
            }
            protected void FixedUpdate()
            {
                if (hook != null) 
                {
                    hook.PrePhysicsUpdate();
                    hook.PhysicsUpdate(); 
                    hook.PostPhysicsUpdate();
                }
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

                if (logger == null || !typeof(UnityLogger).IsAssignableFrom(logger.GetType())) logger = new UnityLogger();

                return logger;

            }

        }

        private static string deviceID;
        public override string DeviceID => deviceID;
        public override string AppDataDirectory => Application.persistentDataPath;
        public override string WorkingDirectory => Application.dataPath;

        private readonly List<string> toRemove = new List<string>();
        public override void FrameUpdateLate()
        { 
            try
            {
                SubstepPhysicsManager.AutoSimulate = swole.IsInPlayMode; 
            }
            catch(Exception ex)
            {
                swole.LogError(ex);
            }

            try
            {
                if (transforms != null)
                {
                    foreach (var transformPair in trackedTransforms)
                    {
                        string transformId = transformPair.Key;
                        if (transformPair.Value <= 0 || !transforms.TryGetValue(transformId, out var transform) || transform == null || transform.IsDestroyed)
                        {
                            toRemove.Add(transformId);
                            continue;
                        }

                        if (!transform.HasEventHandler) continue;
                        var eventHandler = transform.TransformEventHandler;

                        var pos = transform.position;
                        var rot = transform.rotation;
                        var sca = transform.lossyScale;
                        var par = Object_GetInstanceID(transform.parent);

                        bool flagPos = pos != transform.LastPosition;
                        bool flagRot = rot != transform.LastRotation;
                        bool flagSca = sca != transform.LastScale;
                        bool flagPar = par != transform.LastParent;

                        if (flagPos) eventHandler.NotifyPositionChanged(!(flagRot || flagSca || flagPar));
                        if (flagRot) eventHandler.NotifyRotationChanged(!(flagSca || flagPar));
                        if (flagSca) eventHandler.NotifyScaleChanged(!flagPar);
                        if (flagPar) eventHandler.NotifyParentChanged(true); 
                    }

                    if (toRemove.Count > 0)
                    {
                        foreach (var id in toRemove)
                        {
                            transforms.Remove(id);
                            trackedTransforms.Remove(id);
                        }
                        toRemove.Clear();
                    }
                }
            } 
            catch(Exception ex)
            {
                swole.LogError($"[{nameof(UnityEngineHook)}:{nameof(FrameUpdateLate)}] Encountered exception while tracking transforms");
                swole.LogError(ex);

                trackedTransforms.Clear();
                toRemove.Clear();
            }
            base.FrameUpdateLate();
        }

        #region JSON Serialization

        protected override string ToJsonInternal(object obj, bool prettyPrint = false) => JsonUtility.ToJson(obj, prettyPrint);
        protected override object FromJsonInternal(string json, Type type) => JsonUtility.FromJson(json, type);
        protected override T FromJsonInternal<T>(string json) => JsonUtility.FromJson<T>(json);

        #endregion

        #region Conversions | Swole -> Unity

        public static bool CanConvertToUnityType(Type type, out Type conversionType)
        {
            conversionType = type;

            if (typeof(EngineInternal.Vector2).IsAssignableFrom(type))
            {
                conversionType = typeof(Vector2);
                return true;
            }
            else if (typeof(EngineInternal.Vector3).IsAssignableFrom(type))
            {
                conversionType = typeof(Vector3);
                return true;
            }
            else if (typeof(EngineInternal.Vector4).IsAssignableFrom(type))
            {
                conversionType = typeof(Vector4);
                return true;
            }
            else if (typeof(EngineInternal.Quaternion).IsAssignableFrom(type))
            {
                conversionType = typeof(Quaternion);
                return true;
            }
            else if (typeof(EngineInternal.IEngineObject).IsAssignableFrom(type))
            {
                conversionType = typeof(UnityEngine.Object);
                return true;
            } 
            else if (type.IsArray && CanConvertToUnityType(type.GetElementType(), out var elementType))
            {
                conversionType = elementType.MakeArrayType();
                return true;
            }

            return false;
        }
        public static object ConvertToUnityType(object obj)
        {
            TryConvertToUnityType(obj, out var conversion);
            return conversion;
        }
        public static bool TryConvertToUnityType(object obj, out object conversion)
        {
            conversion = obj;
            if (ReferenceEquals(obj, null)) return false;

            var type = obj.GetType();
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (CanConvertToUnityType(elementType, out var conversionType))
                {
                    var oldArray = (Array)obj;
                    var newArray = Array.CreateInstance(conversionType, oldArray.Length);
                    for(int a = 0; a < oldArray.Length; a++)
                    {
                        if (TryConvertToUnityType(oldArray.GetValue(a), out var elem)) newArray.SetValue(elem, a);
                    }
                }
            }
            else
            {
                if (typeof(EngineInternal.Vector2).IsAssignableFrom(type))
                {
                    conversion = AsUnityVector((EngineInternal.Vector2)obj);  
                    return true;
                }
                else if (typeof(EngineInternal.Vector3).IsAssignableFrom(type))
                {
                    conversion = AsUnityVector((EngineInternal.Vector3)obj);
                    return true;
                }
                else if (typeof(EngineInternal.Vector4).IsAssignableFrom(type))
                {
                    conversion = AsUnityVector((EngineInternal.Vector4)obj);
                    return true;
                }
                else if (typeof(EngineInternal.Quaternion).IsAssignableFrom(type))
                {
                    conversion = AsUnityVector((EngineInternal.Quaternion)obj);
                    return true;
                }
                else if (typeof(EngineInternal.IEngineObject).IsAssignableFrom(type))
                {
                    conversion = AsUnityObject((EngineInternal.IEngineObject)obj);
                    return true;
                }
            }

            return false;
        }

        public static Vector2 AsUnityVector(EngineInternal.Vector2 v2) => new Vector2(v2.x, v2.y);
        public static Vector3 AsUnityVector(EngineInternal.Vector3 v3) => new Vector3(v3.x, v3.y, v3.z);
        public static Vector4 AsUnityVector(EngineInternal.Vector4 v4) => new Vector4(v4.x, v4.y, v4.z, v4.w);
        public static Vector4 AsUnityVector(EngineInternal.Quaternion v4) => new Vector4(v4.x, v4.y, v4.z, v4.w);

        public static Quaternion AsUnityQuaternion(EngineInternal.Quaternion q) => new Quaternion(q.x, q.y, q.z, q.w);
        public static Quaternion AsUnityQuaternion(EngineInternal.Vector4 q) => new Quaternion(q.x, q.y, q.z, q.w);

        public static Matrix4x4 AsUnityMatrix(EngineInternal.Matrix4x4 m) => new Matrix4x4(AsUnityVector(m.GetColumn(0)), AsUnityVector(m.GetColumn(1)), AsUnityVector(m.GetColumn(2)), AsUnityVector(m.GetColumn(3)));

        public static UnityEngine.Object AsUnityObject(EngineInternal.IEngineObject obj)
        {
            if (obj == null) return null;
            if (obj.Instance is UnityEngine.Object unityObject) return unityObject; 
            if (obj is ITileInstance tile)
            {
                tile.ForceUseRealTransform();
                if (tile.Instance is Transform uT) return uT;  
            }
            return null;
        }

        public static GameObject AsUnityGameObject(EngineInternal.IEngineObject gameObject)
        {
            if (gameObject == null) return null;
            if (gameObject.Instance is GameObject unityGameObject) return unityGameObject;
            else if (gameObject.Instance is Component unityComponent) return unityComponent.gameObject;
            else if (gameObject is ITileInstance tile)
            {
                tile.ForceUseRealTransform();
                if (tile.Instance is Transform uT) return uT.gameObject;  
            } 

            return null;
        }

        public static Component AsUnityComponent(EngineInternal.IComponent comp)
        {
            if (comp == null) return null;
            if (comp.Instance is Component component) return component;

            return null;
        }

        public static Transform AsUnityTransform(EngineInternal.IEngineObject transform)
        {
            if (transform == null) return null;
            if (transform.Instance is Transform unityTransform) return unityTransform;
            else if (transform.Instance is Component unityComponent) return unityComponent.transform;
            else if (transform.Instance is GameObject unityGO) return unityGO.transform;
            else if (transform is ITileInstance tile)
            {
                tile.ForceUseRealTransform();
                if (tile.Instance is Transform uT) return uT;
            }

            return null;
        }

        public static Camera AsUnityCamera(EngineInternal.IEngineObject camera)
        {
            if (camera == null) return null;
            if (camera.Instance is Camera cam) return cam;

            var go = AsUnityGameObject(camera);
            if (go != null) return go.GetComponent<Camera>();
             
            return null;
        }

        #endregion

        #region Conversions | Unity -> Swole

        public static bool CanConvertToSwoleType(Type type, out Type conversionType)
        {
            conversionType = type;

            if (typeof(Vector2).IsAssignableFrom(type))
            {
                conversionType = typeof(EngineInternal.Vector2);
                return true;
            }
            else if (typeof(Vector3).IsAssignableFrom(type))
            {
                conversionType = typeof(EngineInternal.Vector3);
                return true;
            }
            else if (typeof(Vector4).IsAssignableFrom(type))
            {
                conversionType = typeof(EngineInternal.Vector4);
                return true;
            }
            else if (typeof(Quaternion).IsAssignableFrom(type))
            {
                conversionType = typeof(EngineInternal.Quaternion);
                return true;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                conversionType = typeof(EngineInternal.IEngineObject);
                return true;
            }
            else if (type.IsArray && CanConvertToSwoleType(type.GetElementType(), out var elementType))
            {
                conversionType = elementType.MakeArrayType();
                return true;
            }

            return false;
        }
        public static object ConvertToSwoleType(object obj)
        {
            TryConvertToSwoleType(obj, out var conversion); 
            return conversion;
        }
        public static bool TryConvertToSwoleType(object obj, out object conversion)
        {
            conversion = obj;
            if (ReferenceEquals(obj, null)) return false;

            var type = obj.GetType();
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                if (CanConvertToSwoleType(elementType, out var conversionType))
                {
                    var oldArray = (Array)obj;
                    var newArray = Array.CreateInstance(conversionType, oldArray.Length);
                    for (int a = 0; a < oldArray.Length; a++)
                    {
                        if (TryConvertToSwoleType(oldArray.GetValue(a), out var elem)) newArray.SetValue(elem, a);
                    }
                }
            }
            else
            {
                if (typeof(Vector2).IsAssignableFrom(type))
                {
                    conversion = AsSwoleVector((Vector2)obj);
                    return true;
                }
                else if (typeof(Vector3).IsAssignableFrom(type))
                {
                    conversion = AsSwoleVector((Vector3)obj);
                    return true;
                }
                else if (typeof(Vector4).IsAssignableFrom(type))
                {
                    conversion = AsSwoleVector((Vector4)obj);
                    return true;
                }
                else if (typeof(Quaternion).IsAssignableFrom(type))
                {
                    conversion = AsSwoleVector((Quaternion)obj);
                    return true;
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    conversion = AsEngineObject((UnityEngine.Object)obj);
                    return true;
                }
            }

            return false;
        }

        public static EngineInternal.Vector2 AsSwoleVector(Vector2 v2) => new EngineInternal.Vector2(v2.x, v2.y);
        public static EngineInternal.Vector3 AsSwoleVector(Vector3 v3) => new EngineInternal.Vector3(v3.x, v3.y, v3.z);
        public static EngineInternal.Vector4 AsSwoleVector(Vector4 v4) => new EngineInternal.Vector4(v4.x, v4.y, v4.z, v4.w);
        public static EngineInternal.Vector4 AsSwoleVector(Quaternion v4) => new EngineInternal.Vector4(v4.x, v4.y, v4.z, v4.w);

        public static EngineInternal.Quaternion AsSwoleQuaternion(Quaternion q) => new EngineInternal.Quaternion(q.x, q.y, q.z, q.w);
        public static EngineInternal.Quaternion AsSwoleQuaternion(Vector4 q) => new EngineInternal.Quaternion(q.x, q.y, q.z, q.w);

        public static EngineInternal.Matrix4x4 AsSwoleMatrix(Matrix4x4 m) => new EngineInternal.Matrix4x4(AsSwoleVector(m.GetColumn(0)), AsSwoleVector(m.GetColumn(1)), AsSwoleVector(m.GetColumn(2)), AsSwoleVector(m.GetColumn(3)));

        public static EngineInternal.EngineObject AsEngineObject(UnityEngine.Object obj)
        {
            if (obj == null) return default;
            return new EngineInternal.EngineObject(obj);
        }

        public static EngineInternal.GameObject AsSwoleGameObject(GameObject gameObject)
        {
            if (gameObject == null) return default;
            var transform = gameObject.GetComponent<EngineInternal.ITransform>();
            if (swole.IsNull(transform)) transform = AsSwoleTransform(gameObject.transform);
            return new EngineInternal.GameObject(gameObject, transform); 
        }
        public static EngineInternal.GameObject AsSwoleGameObject(EngineInternal.ITransform t)
        {
            if (t == null) return default;
            if (t is Transform unityTransform) return AsSwoleGameObject(unityTransform.gameObject); 

            return default;
        }
        public static EngineInternal.GameObject AsSwoleGameObject(ITileInstance tile)
        {
            if (tile == null) return default;
             
            GameObject gameObject = null;
            if (tile.IsRenderOnly)
            {
                tile.SetParent(tile.parent, true, true); // Force tile and its parent to become real objects

                if (tile.Instance is Transform t) gameObject = t.gameObject; else if (tile.Instance is GameObject go) gameObject = go;
            } 
            else
            {
                if (tile.Instance is Transform t) gameObject = t.gameObject; else if (tile.Instance is GameObject go) gameObject = go;
            }

            if (gameObject != null) return new EngineInternal.GameObject(gameObject, tile);        

            return default;
        }
         
        public static EngineInternal.IComponent AsSwoleComponent(Component component)
        {
            if (component == null) return default;
            if (component is EngineInternal.IComponent comp) return comp;
            if (component is RectTransform rt) return AsSwoleRectTransform(rt);
            if (component is Transform t) return AsSwoleTransform(t);
            if (component is Camera cam) return AsSwoleCamera(cam);
            if (component is AudioSource source) return AsSwoleAudioSource(source);
            if (component is Rigidbody rigidbody) return AsSwoleRigidbody(rigidbody);

            return default;
        }

        protected static readonly Dictionary<string, EngineInternal.ITransform> transforms = new Dictionary<string, EngineInternal.ITransform>();
        public static string GetUnityObjectIDString(UnityEngine.Object obj) => obj == null ? null : $"unity_{obj.GetInstanceID()}";
        public static EngineInternal.Transform AsSwoleTransform(Transform transform)
        {
            if (transform == null) return default;
            string id = GetUnityObjectIDString(transform);
            if (transforms.TryGetValue(id, out var swoleTransform) && ReferenceEquals(swoleTransform.Instance, transform)) return (EngineInternal.Transform)swoleTransform;

            if (swoleTransform != null && swoleTransform.HasEventHandler) swoleTransform.TransformEventHandler.Dispose();

            swoleTransform = transform is RectTransform rt ? new EngineInternal.RectTransform(id, rt) : new EngineInternal.Transform(id, transform);
            transforms[id] = swoleTransform; 
            return (EngineInternal.Transform)swoleTransform; 
        }
        public static EngineInternal.RectTransform AsSwoleRectTransform(RectTransform rectTransform)
        {
            var transform = AsSwoleTransform(rectTransform);

            EngineInternal.RectTransform output = null;
            if (transform is EngineInternal.RectTransform rt)
            {
                output = rt;
            } 
            else
            {
                output = new EngineInternal.RectTransform(transform);
                transforms[output.ID] = output;
            }

            return output;
        }
        public static EngineInternal.ITransform AsSwoleTransform(ITileInstance tile)
        {
            if (tile == null) return default;
            string id = tile.ID;
            if (transforms.TryGetValue(id, out var swoleTransform) && ReferenceEquals(swoleTransform.Instance, tile.Instance)) return swoleTransform;

            if (swoleTransform != null && swoleTransform.HasEventHandler) swoleTransform.TransformEventHandler.Dispose();

            transforms[id] = tile;
            return swoleTransform;
        }

        public static EngineInternal.Camera AsSwoleCamera(Camera camera)
        {
            return new EngineInternal.Camera(camera);
        }

        public static AudioSourceProxy AsSwoleAudioSource(AudioSource source)
        {
            return new AudioSourceProxy(source);
        }

        #region Physics

        public static RigidbodyProxy AsSwoleRigidbody(Rigidbody rigidbody)
        {
            return new RigidbodyProxy(rigidbody);
        }

        #endregion

        #endregion

        #region RNG

        public override EngineInternal.RNG RNG_Global() => new EngineInternal.RNG(RNG.Global);

        public override EngineInternal.RNG RNG_New(int seed) => new EngineInternal.RNG(new RNG(seed));
        public override EngineInternal.RNG RNG_New(EngineInternal.RNGState initialState) => new EngineInternal.RNG(new RNG((UnityEngine.Random.State)initialState.instance));
        public override EngineInternal.RNG RNG_New(EngineInternal.RNGState initialState, EngineInternal.RNGState currentState) => new EngineInternal.RNG(new RNG((UnityEngine.Random.State)initialState.instance, (UnityEngine.Random.State)currentState.instance));

        public override EngineInternal.RNG RNG_Reset(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return new EngineInternal.RNG(rng_.Reset()); else return rng; }

        public override EngineInternal.RNG RNG_Fork(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return new EngineInternal.RNG(rng_.Fork); else return rng; }

        public override int RNG_Seed(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return rng_.Seed; else return 0; }
        
        public override EngineInternal.RNGState RNG_State(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return new EngineInternal.RNGState(rng_.State); else return default; }

        public override float RNG_NextValue(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return rng_.NextValue; else return default; }
        public override bool RNG_NextBool(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return rng_.NextBool; else return default; }

        public override EngineInternal.Vector4 RNG_NextColor(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return AsSwoleVector(rng_.NextColor); else return default; }

        public override EngineInternal.Quaternion RNG_NextRotation(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return AsSwoleQuaternion(rng_.NextRotation); else return EngineInternal.Quaternion.identity; }
        public override EngineInternal.Quaternion RNG_NextRotationUniform(EngineInternal.RNG rng) { if (rng.instance is RNG rng_) return AsSwoleQuaternion(rng_.NextRotationUniform); else return EngineInternal.Quaternion.identity; }

        public override float RNG_Range(EngineInternal.RNG rng, float minInclusive = 0, float maxInclusive = 1) { if (rng.instance is RNG rng_) return rng_.Range(minInclusive, maxInclusive); else return default; }
        public override int RNG_RangeInt(EngineInternal.RNG rng, int minInclusive, int maxExclusive) { if (rng.instance is RNG rng_) return rng_.RangeInt(minInclusive, maxExclusive); else return default; }

        #endregion

        #region Swole

        public override IInputManager InputManager => InputProxy.Manager;

        public override bool IsNull(object engineObject)
        {

            if (engineObject == null) return true;
            if (engineObject is UnityEngine.Object unityObject && unityObject == null) return true;
            if (engineObject is EngineInternal.IEngineObject eo) 
            {
                if (eo.Instance == null) return true;
                if (!ReferenceEquals(eo, eo.Instance) && IsNull(eo.Instance)) return true; 
            }

            return false;
        }

        public override void InvokeOverTime(VoidParameterlessDelegate func, SwoleCancellationToken token, float duration, float step = 0) 
        {
            if (func == null) return;

            float startTime = Time.time;
            float lastTime = startTime;
            float t = 0;
            IEnumerator Invoker()
            {
                while (token == null ? true : !token.IsCancelled)
                {
                    var time_ = Time.time;
                    if (time_ - startTime > duration) break;
                    t += time_ - lastTime;
                    lastTime = time_;

                    if (t >= step)
                    {
                        t = 0;
                        func.Invoke();
                    }

                    yield return null;
                }
            }

            CoroutineProxy.Start(Invoker());
        }

        public override void SetSwoleId(object obj, int id)
        {
            if (obj is EngineInternal.SwoleGameObject sgo) 
            {
                sgo.id = id; 
                return;
            }

            if (obj is EngineInternal.GameObject ego)
            {
                var go = AsUnityGameObject(ego);
                if (go == null) return;

                var swoleObj = go.GetComponent<SwoleGameObject>();
                if (swoleObj == null) swoleObj = go.AddComponent<SwoleGameObject>();

                swoleObj.id = id;
                return;
            }
            if (obj is GameObject ugo)
            {
                var swoleObj = ugo.GetComponent<SwoleGameObject>();
                if (swoleObj == null) return;

                swoleObj.id = id;
                return;
            }
            if (obj is ITileInstance tileInstance)
            {
                tileInstance.SwoleId = id; 
                return; 
            }
            
        }
        public override int GetSwoleId(object obj) 
        {
            if (obj is EngineInternal.SwoleGameObject sgo) return sgo.id;

            if (obj is EngineInternal.GameObject ego)
            {
                var go = AsUnityGameObject(ego);
                if (go == null) return -1;

                var swoleObj = go.GetComponent<SwoleGameObject>();
                if (swoleObj == null) return -1;

                return swoleObj.id;
            }
            if (obj is GameObject ugo)
            {
                var swoleObj = ugo.GetComponent<SwoleGameObject>();
                if (swoleObj == null) return -1;

                return swoleObj.id;
            }
            if (obj is ITileInstance tileInstance) return tileInstance.SwoleId;

            return -1;
        }

        public override bool IsExperienceRoot(object obj)
        {
            if (obj is EngineInternal.ITransform it) obj = it.Instance;
            if (obj is Component unityComponent) obj = unityComponent.gameObject;

            if (obj is GameObject unityGameObject)
            {
                var cb = unityGameObject.GetComponent<CreationBehaviour>();
                if (cb != null)
                {
                    return unityGameObject.transform.parent == null;
                }
            }

            return false;
        }

        public override EngineInternal.CreationInstance GetGameplayExperienceRoot(object obj) 
        {

            Transform t = null;
            if (obj is ITileInstance tile)
            {
                return GetGameplayExperienceRoot(tile.parent); // Avoid forcing tiles to use real transforms
            }
            else if (obj is EngineInternal.IEngineObject engObj)
            {
                t = AsUnityTransform(engObj); 
            } 
            else if (obj is GameObject unityGO)
            {
                t = unityGO.transform;
            } 
            else if (obj is Component unityComp)
            {
                t = unityComp.transform;
            }

            if (t != null)
            {
                var creation = t.GetComponentInParent<CreationBehaviour>();
                if (creation != null) t = creation.transform;

                while(t.parent != null)
                {
                    t = t.parent;
                    var parentCreation = t.GetComponentInParent<CreationBehaviour>();
                    if (parentCreation != null) 
                    { 
                        creation = parentCreation;
                        t = creation.transform;
                    } else break;
                }

                if (creation != null) return new EngineInternal.CreationInstance(creation);
            }

            return default;
        }

        public override EngineInternal.CreationInstance GetRootCreationInstance(object obj)
        {

            Transform t = null;
            if (obj is ITileInstance tile)
            {
                return GetRootCreationInstance(tile.parent); // Avoid forcing tiles to use real transforms
            }
            else if (obj is EngineInternal.IEngineObject engObj)
            {
                t = AsUnityTransform(engObj);
            }
            else if (obj is GameObject unityGO)
            {
                t = unityGO.transform;
            }
            else if (obj is Component unityComp)
            {
                t = unityComp.transform;
            }

            if (t != null)
            {
                var creation = t.GetComponent<CreationBehaviour>(); // Will only get used if there is not a parent creation behaviour

                if (t.parent != null)
                {
                    var parentCreation = t.parent.GetComponentInParent<CreationBehaviour>();
                    if (parentCreation != null) creation = parentCreation;
                }

                if (creation != null) return new EngineInternal.CreationInstance(creation);
            }

            return default;
        }

        public override FindAssetResult TryFindAsset(string assetPath, Type type, IRuntimeHost host, out ISwoleAsset asset, bool caseSensitive = false)
        {
            var res = base.TryFindAsset(assetPath, type, host, out asset, caseSensitive);
            if (res == FindAssetResult.EmptyPackageName || res == FindAssetResult.NotFoundLocally)
            {
                if (ResourceLib.TryFindAsset(out asset, assetPath, type, caseSensitive)) res = FindAssetResult.Success; else res = FindAssetResult.NotFound;
            }
             
            return res;
        }

        protected T CreateTemporaryScriptableObject<T>(string name, IRuntimeHost host) where T : ScriptableObject
        {
            if (host == null || !host.Scope.HasFlag(PermissionScope.CreateAssets)) return null;
            var asset = ScriptableObject.CreateInstance<T>();
            void DisposeTempAsset()
            {
                if (asset == null) return;
                GameObject.Destroy(asset);
                asset = null;
            }
            host.ListenForQuit(DisposeTempAsset);
            asset.name = name;
             
            return asset;
        }

        #region Animation

        public override IAnimationController CreateNewAnimationController(string name, IRuntimeHost host)
        {
            var asset = CreateTemporaryScriptableObject<CustomAnimationController>(name, host);
            return asset;
        }
        public override IAnimationLayer CreateNewAnimationLayer(string name)
        {
            return new CustomAnimationLayer() { name = name };
        }
        public override IAnimationLayerState CreateNewLayerState(string name, int motionControllerIndex, Transition[] transitions = null)
        {
            return new CustomAnimationLayerState() { name = name, motionControllerIndex = motionControllerIndex, transitions = transitions };
        }

        public override IAnimationReference CreateNewAnimationReference(string name, IAnimationAsset asset, AnimationLoopMode loopMode)
        {
            return new AnimationReference(name, asset, loopMode);
        }

        #endregion

        #endregion

        #region Audio

        public override IAudioAsset GetAudioAsset(string assetId, string audioCollectionId = null, bool caseSensitive = false) => ResourceLib.FindAudioClip(assetId, audioCollectionId, caseSensitive);

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

        public override int Object_GetInstanceID(object engineObject)
        {
            if (engineObject is UnityEngine.Object obj) return obj.GetInstanceID();
            if (engineObject is EngineInternal.GameObject go && !ReferenceEquals(go, go.instance)) return Object_GetInstanceID(go.instance);
            if (engineObject is EngineInternal.ITransform t && !ReferenceEquals(t, t.Instance)) return Object_GetInstanceID(t.Instance); 

            return 0;
        }

        public static Component AsUnityComponent(object engineObject)
        {

            if (engineObject is Component component)
            {
                return component;
            }
            else if (engineObject is EngineInternal.IEngineObject eo)
            {
                if (eo.Instance != eo) return AsUnityComponent(eo.Instance);
            }

            return null;
        }

        public static Transform AsUnityTransform(object engineObject)
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
                return component.transform;
            }
            else if (engineObject is EngineInternal.IEngineObject eo)
            {
                if (eo.Instance != eo) return AsUnityTransform(eo.Instance);
            }

            return null;
        }
        public static RectTransform AsUnityRectTransform(object engineObject)
        {
            if (engineObject is RectTransform transform)
            {
                return transform;
            }
            else if (engineObject is GameObject gameObject)
            {
                return gameObject.GetComponent<RectTransform>();
            }
            else if (engineObject is Component component)
            {
                return component.GetComponent<RectTransform>();
            }
            else if (engineObject is EngineInternal.IEngineObject eo)
            {
                if (eo.Instance != eo) return AsUnityRectTransform(eo.Instance);
            }

            return null;
        }

        /// <summary>
        /// Converts an engine object to a unity gameobject, if applicable
        /// </summary>
        public static GameObject AsUnityGameObject(object engineObject)
        {
            if (engineObject is GameObject gameObject)
            {
                return gameObject;
            }
            else if (engineObject is Component component)
            {
                return component.gameObject;
            } 
            else if (engineObject is EngineInternal.IEngineObject eo)
            {
                if (eo.Instance != eo) return AsUnityGameObject(eo.Instance);
            }

            return null;
        }

        public override EngineInternal.Vector3 GetLocalPosition(object engineObject) 
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.zero;
            return AsSwoleVector(transform.localPosition);
        }
        public override EngineInternal.Vector3 GetLocalScale(object engineObject)
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.one;
            return AsSwoleVector(transform.localScale);
        }
        public override EngineInternal.Quaternion GetLocalRotation(object engineObject)
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return EngineInternal.Quaternion.identity;
            return AsSwoleQuaternion(transform.localRotation);
        }

        public override EngineInternal.Vector3 GetWorldPosition(object engineObject)
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.zero;
            return AsSwoleVector(transform.position);
        }
        public override EngineInternal.Vector3 GetLossyScale(object engineObject)
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return EngineInternal.Vector3.one;
            return AsSwoleVector(transform.lossyScale);
        }
        public override EngineInternal.Quaternion GetWorldRotation(object engineObject)
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return EngineInternal.Quaternion.identity;
            return AsSwoleQuaternion(transform.rotation);
        }

        public override void SetLocalPosition(object engineObject, EngineInternal.Vector3 localPosition) 
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return;
            transform.localPosition = AsUnityVector(localPosition);
        }
        public override void SetLocalRotation(object engineObject, EngineInternal.Quaternion localRotation) 
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return;
            transform.localRotation = AsUnityQuaternion(localRotation);
        }
        public override void SetLocalScale(object engineObject, EngineInternal.Vector3 localScale)
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return;
            transform.localScale = AsUnityVector(localScale);
        }

        public override void SetWorldPosition(object engineObject, EngineInternal.Vector3 position) 
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return;
            transform.position = AsUnityVector(position);
        }
        public override void SetWorldRotation(object engineObject, EngineInternal.Quaternion rotation) 
        {
            var transform = AsUnityTransform(engineObject);
            if (transform == null) return;
            transform.rotation = AsUnityQuaternion(rotation);
        }

        public override string GetName(object engineObject) 
        {
            if (engineObject is UnityEngine.Object unityObject) return unityObject.name;
           
            return string.Empty;
        }

        public override EngineInternal.EngineObject Object_Instantiate(object engineObject) 
        {
            if (engineObject is UnityEngine.Object unityObject) 
            { 
                try
                {
                    return new EngineInternal.EngineObject(UnityEngine.Object.Instantiate(unityObject));
                }
                catch(Exception ex)
                {
                    swole.LogError($"[{nameof(UnityEngineHook)}:{nameof(Object_Instantiate)}] Encountered exception while trying to instantiate engine object '{unityObject}'");
                    swole.LogError(ex);
                }
            }

            return default;
        }

        public override void Object_SetName(object engineObject, string name) => Object_SetNameInternal(engineObject, name, false);
        public override void Object_SetNameAdmin(object engineObject, string name) => Object_SetNameInternal(engineObject, name, true);
        protected virtual void Object_SetNameInternal(object engineObject, string name, bool admin)
        {
            UnityEngine.Object unityObject = null;
            if (engineObject is EngineInternal.IEngineObject eo)
            {
                if (eo.Instance is UnityEngine.Object) unityObject = (UnityEngine.Object)eo.Instance;
            } 
            else if (engineObject is UnityEngine.Object)
            {
                unityObject = (UnityEngine.Object)engineObject; 
            }

            if (unityObject != null) unityObject.name = name;
        }

        public override void Object_Destroy(object engineObject, float timeDelay = 0) => Object_DestroyInternal(engineObject, timeDelay, false);
        public override void Object_AdminDestroy(object engineObject, float timeDelay = 0) => Object_DestroyInternal(engineObject, timeDelay, true);
        protected virtual void Object_DestroyInternal(object engineObject, float timeDelay, bool admin)
        {
            if (engineObject is ISwoleAsset asset) 
            {
                if (admin)
                {
                    if (asset.IsInternalAsset) swole.LogWarning($"Tried to destroy internal asset '{asset.Name}'!"); else asset.Dispose();
                }
                else 
                { 
                    swole.LogWarning($"Tried to destroy swole asset '{asset.Name}' without permission!");  
                }

                return;
            }

            if (engineObject is ITileInstance tileInst)
            {
                try
                {
                    tileInst.Dispose();
                }
                catch (Exception ex)
                {
                    swole.LogError($"[{nameof(UnityEngineHook)}:{nameof(Object_Instantiate)}] Encountered exception while trying to destroy tile instance '{tileInst}'");
                    swole.LogError(ex);
                }
            }
            else if (engineObject is UnityEngine.Object unityObject)
            {
                if (!admin && RuntimeHandler.IsALoadedExperience(AsEngineObject(unityObject))) // Don't allow loaded gameplay experiences to be destroyed by scripts
                {
                    swole.LogWarning("Tried to destroy gameplay experience object!");
                    return;
                }
                try
                {
                    UnityEngine.Object.Destroy(unityObject, timeDelay);
                }
                catch (Exception ex)
                {
                    swole.LogError($"[{nameof(UnityEngineHook)}:{nameof(Object_Instantiate)}] Encountered exception while trying to destroy engine object '{unityObject}'");
                    swole.LogError(ex);
                }
            }
            else if (engineObject is EngineInternal.IEngineObject eo)
            {
                if (!admin && RuntimeHandler.IsALoadedExperience(eo)) // Don't allow loaded gameplay experiences to be destroyed by scripts
                {
                    swole.LogWarning("Tried to destroy gameplay experience object!");
                    return;
                }

                if (!ReferenceEquals(eo, eo.Instance)) Object_DestroyInternal(eo.Instance, timeDelay, admin);
            }
        }

        #region Math

        public override EngineInternal.Vector2 lerp(EngineInternal.Vector2 vA, EngineInternal.Vector2 vB, float t) => AsSwoleVector(Vector2.LerpUnclamped(AsUnityVector(vA), AsUnityVector(vB), t));
        public override EngineInternal.Vector3 lerp(EngineInternal.Vector3 vA, EngineInternal.Vector3 vB, float t) => AsSwoleVector(Vector3.LerpUnclamped(AsUnityVector(vA), AsUnityVector(vB), t));
        public override EngineInternal.Vector4 lerp(EngineInternal.Vector4 vA, EngineInternal.Vector4 vB, float t) => AsSwoleVector(Vector4.LerpUnclamped(AsUnityVector(vA), AsUnityVector(vB), t));
        public override EngineInternal.Vector4 slerp(EngineInternal.Vector4 vA, EngineInternal.Vector4 vB, float t) => AsSwoleVector(Quaternion.SlerpUnclamped(AsUnityQuaternion(vA), AsUnityQuaternion(vB), t));
        public override EngineInternal.Quaternion slerp(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB, float t) => AsSwoleQuaternion(Quaternion.SlerpUnclamped(AsUnityQuaternion(qA), AsUnityQuaternion(qB), t));

        #endregion

        public override EngineInternal.Quaternion Mul(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB) => AsSwoleQuaternion(AsUnityQuaternion(qA) * AsUnityQuaternion(qB));
        public override EngineInternal.Vector3 Mul(EngineInternal.Quaternion q, EngineInternal.Vector3 v) => AsSwoleVector(AsUnityQuaternion(q) * AsUnityVector(v));
        public override EngineInternal.Matrix4x4 Mul(EngineInternal.Matrix4x4 mA, EngineInternal.Matrix4x4 mB) => AsSwoleMatrix(AsUnityMatrix(mA) * AsUnityMatrix(mB));

        public override EngineInternal.Vector3 Mul(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => AsSwoleVector(AsUnityMatrix(m).MultiplyPoint(AsUnityVector(point)));
        public override EngineInternal.Vector3 Mul3x4(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => AsSwoleVector(AsUnityMatrix(m).MultiplyPoint3x4(AsUnityVector(point)));
        public override EngineInternal.Vector3 Rotate(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 vector) => AsSwoleVector(AsUnityMatrix(m).MultiplyVector(AsUnityVector(vector)));

        public override float Vector3_SignedAngle(EngineInternal.Vector3 vA, EngineInternal.Vector3 vB, EngineInternal.Vector3 axis) => Vector3.SignedAngle(AsUnityVector(vA), AsUnityVector(vB), AsUnityVector(axis));

        public override EngineInternal.Quaternion Quaternion_Euler(float x, float y, float z) => AsSwoleQuaternion(Quaternion.Euler(x, y, z));
        public override EngineInternal.Vector3 Quaternion_EulerAngles(EngineInternal.Quaternion quaternion) => AsSwoleVector(AsUnityQuaternion(quaternion).eulerAngles);
        public override EngineInternal.Quaternion Quaternion_Inverse(EngineInternal.Quaternion q) => AsSwoleQuaternion(Quaternion.Inverse(AsUnityQuaternion(q)));
        public override float Quaternion_Dot(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB) => Quaternion.Dot(AsUnityQuaternion(qA), AsUnityQuaternion(qB));
        public override EngineInternal.Quaternion Quaternion_FromToRotation(EngineInternal.Vector3 vA, EngineInternal.Vector3 vB) => AsSwoleQuaternion(Quaternion.FromToRotation(AsUnityVector(vA), AsUnityVector(vB)));
        public override EngineInternal.Quaternion Quaternion_LookRotation(EngineInternal.Vector3 forward, EngineInternal.Vector3 upward) => AsSwoleQuaternion(Quaternion.LookRotation(AsUnityVector(forward), AsUnityVector(upward)));

        public override EngineInternal.Matrix4x4 Matrix4x4_Inverse(EngineInternal.Matrix4x4 m) => AsSwoleMatrix(Matrix4x4.Inverse(AsUnityMatrix(m)));
        public override EngineInternal.Matrix4x4 Matrix4x4_TRS(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation, EngineInternal.Vector3 scale) => AsSwoleMatrix(Matrix4x4.TRS(AsUnityVector(position), AsUnityQuaternion(rotation), AsUnityVector(scale)));
        public override EngineInternal.Matrix4x4 Matrix4x4_Scale(EngineInternal.Vector3 vector) => AsSwoleMatrix(Matrix4x4.Scale(AsUnityVector(vector)));
        public override EngineInternal.Matrix4x4 Matrix4x4_Translate(EngineInternal.Vector3 vector) => AsSwoleMatrix(Matrix4x4.Translate(AsUnityVector(vector)));
        public override EngineInternal.Matrix4x4 Matrix4x4_Rotate(EngineInternal.Quaternion q) => AsSwoleMatrix(Matrix4x4.Rotate(AsUnityQuaternion(q)));

        #region Transforms

        public override EngineInternal.ITransform Transform_GetParent(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleTransform(AsUnityTransform(transform).parent);
        public override void Transform_SetParent(EngineInternal.ITransform transform, EngineInternal.ITransform parent, bool worldPositionStays = true) 
        {
            if (IsNull(transform)) return;

            if (RuntimeHandler.IsALoadedExperience(transform.baseGameObject)) // Don't allow loaded gameplay experiences to be parented by scripts
            {
                swole.LogWarning("Tried to parent gameplay experience transform!");
                return;
            }
            
            AsUnityTransform(transform).SetParent(AsUnityTransform(parent), worldPositionStays); 
        }
        public override EngineInternal.Vector3 Transform_lossyScale(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).lossyScale);
        public override EngineInternal.Vector3 Transform_eulerAnglesGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).eulerAngles);
        public override void Transform_eulerAnglesSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { if (IsNotNull(transform)) AsUnityTransform(transform).eulerAngles = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_localEulerAnglesGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).localEulerAngles);
        public override void Transform_localEulerAnglesSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { if (IsNotNull(transform)) AsUnityTransform(transform).localEulerAngles = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_rightGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).right);
        public override void Transform_rightSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { if (IsNotNull(transform)) AsUnityTransform(transform).right = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_upGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).up);
        public override void Transform_upSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { if (IsNotNull(transform)) AsUnityTransform(transform).up = AsUnityVector(val); }
        public override EngineInternal.Vector3 Transform_forwardGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).forward);
        public override void Transform_forwardSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { if (IsNotNull(transform)) AsUnityTransform(transform).forward = AsUnityVector(val); }
        public override EngineInternal.Matrix4x4 Transform_worldToLocalMatrix(EngineInternal.ITransform transform) => IsNull(transform) ? EngineInternal.Matrix4x4.identity : AsSwoleMatrix(AsUnityTransform(transform).worldToLocalMatrix);
        public override EngineInternal.Matrix4x4 Transform_localToWorldMatrix(EngineInternal.ITransform transform) => IsNull(transform) ? EngineInternal.Matrix4x4.identity : AsSwoleMatrix(AsUnityTransform(transform).localToWorldMatrix);
        public override EngineInternal.ITransform Transform_root(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsSwoleTransform(AsUnityTransform(transform).root);
        public override int Transform_childCount(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsUnityTransform(transform).childCount;
        public override bool Transform_hasChangedGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsUnityTransform(transform).hasChanged;
        public override void Transform_hasChangedSet(EngineInternal.ITransform transform, bool val) { if (IsNotNull(transform)) AsUnityTransform(transform).hasChanged = val; }
        public override int Transform_hierarchyCapacityGet(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsUnityTransform(transform).hierarchyCapacity;
        public override void Transform_hierarchyCapacitySet(EngineInternal.ITransform transform, int val) { if (IsNull(transform)) AsUnityTransform(transform).hierarchyCapacity = val; }
        public override int Transform_hierarchyCount(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsUnityTransform(transform).hierarchyCount;
        public override void Transform_SetPositionAndRotation(EngineInternal.ITransform transform, EngineInternal.Vector3 position, EngineInternal.Quaternion rotation) { if (IsNotNull(transform)) AsUnityTransform(transform).SetPositionAndRotation(AsUnityVector(position), AsUnityQuaternion(rotation)); }
        public override void Transform_SetLocalPositionAndRotation(EngineInternal.ITransform transform, EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation) { if (IsNotNull(transform)) AsUnityTransform(transform).SetLocalPositionAndRotation(AsUnityVector(localPosition), AsUnityQuaternion(localRotation)); }
        public override void Transform_GetPositionAndRotation(EngineInternal.ITransform transform, out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation) 
        { 
            position = default; rotation = default;
            if (IsNull(transform)) return;

            AsUnityTransform(transform).GetPositionAndRotation(out var unity_position, out var unity_rotation); 
            position = AsSwoleVector(unity_position);
            rotation = AsSwoleQuaternion(unity_rotation);
        }
        public override void Transform_GetLocalPositionAndRotation(EngineInternal.ITransform transform, out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation) 
        { 
            localPosition = default; localRotation = default;
            if (IsNull(transform)) return;

            AsUnityTransform(transform).GetPositionAndRotation(out var unity_position, out var unity_rotation);
            localPosition = AsSwoleVector(unity_position);
            localRotation = AsSwoleQuaternion(unity_rotation);
        }
        public override void Transform_Translate(EngineInternal.ITransform transform, EngineInternal.Vector3 translation, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (IsNotNull(transform)) AsUnityTransform(transform).Translate(AsUnityVector(translation), (Space)(int)relativeTo); }
        public override void Transform_Translate(EngineInternal.ITransform transform, EngineInternal.Vector3 translation) { if (IsNotNull(transform)) AsUnityTransform(transform).Translate(AsUnityVector(translation)); }
        public override void Transform_Translate(EngineInternal.ITransform transform, float x, float y, float z, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (IsNotNull(transform)) AsUnityTransform(transform).Translate(x, y, z, (Space)(int)relativeTo); }
        public override void Transform_Translate(EngineInternal.ITransform transform, float x, float y, float z) { if (IsNotNull(transform)) AsUnityTransform(transform).Translate(x, y, z); }
        public override void Transform_Translate(EngineInternal.ITransform transform, EngineInternal.Vector3 translation, EngineInternal.ITransform relativeTo) { if (IsNotNull(transform)) AsUnityTransform(transform).Translate(AsUnityVector(translation), AsUnityTransform(relativeTo)); }
        public override void Transform_Translate(EngineInternal.ITransform transform, float x, float y, float z, EngineInternal.ITransform relativeTo) { if (IsNotNull(transform)) AsUnityTransform(transform).Translate(x, y, z, AsUnityTransform(relativeTo)); }
        public override void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 eulers, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (IsNotNull(transform)) AsUnityTransform(transform).Rotate(AsUnityVector(eulers), (Space)(int)relativeTo); }
        public override void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 eulers) { if (IsNotNull(transform)) AsUnityTransform(transform).Rotate(AsUnityVector(eulers)); }
        public override void Transform_Rotate(EngineInternal.ITransform transform, float xAngle, float yAngle, float zAngle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (IsNotNull(transform)) AsUnityTransform(transform).Rotate(xAngle, yAngle, zAngle, (Space)(int)relativeTo); }
        public override void Transform_Rotate(EngineInternal.ITransform transform, float xAngle, float yAngle, float zAngle) { if (IsNotNull(transform)) AsUnityTransform(transform).Rotate(xAngle, yAngle, zAngle); }
        public override void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 axis, float angle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { if (IsNotNull(transform)) AsUnityTransform(transform).Rotate(AsUnityVector(axis), angle, (Space)(int)relativeTo); }
        public override void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 axis, float angle) { if (IsNotNull(transform)) AsUnityTransform(transform).Rotate(AsUnityVector(axis), angle); }
        public override void Transform_RotateAround(EngineInternal.ITransform transform, EngineInternal.Vector3 point, EngineInternal.Vector3 axis, float angle) { if (IsNotNull(transform)) AsUnityTransform(transform).RotateAround(AsUnityVector(point), AsUnityVector(axis), angle); }
        public override void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.ITransform target, EngineInternal.Vector3 worldUp) { if (IsNotNull(transform)) AsUnityTransform(transform).LookAt(AsUnityTransform(target), AsUnityVector(worldUp)); }
        public override void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.ITransform target) { if (IsNotNull(transform)) AsUnityTransform(transform).LookAt(AsUnityTransform(target)); }
        public override void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.Vector3 worldPosition, EngineInternal.Vector3 worldUp) { if (IsNotNull(transform)) AsUnityTransform(transform).LookAt(AsUnityVector(worldPosition), AsUnityVector(worldUp)); }
        public override void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.Vector3 worldPosition) { if (IsNotNull(transform)) AsUnityTransform(transform).LookAt(AsUnityVector(worldPosition)); }
        public override EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.ITransform transform, EngineInternal.Vector3 direction) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).TransformDirection(AsUnityVector(direction)));
        public override EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.ITransform transform, float x, float y, float z) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).TransformDirection(x, y, z));
        public override EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.ITransform transform, EngineInternal.Vector3 direction) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformDirection(AsUnityVector(direction)));
        public override EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.ITransform transform, float x, float y, float z) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformDirection(x, y, z));
        public override EngineInternal.Vector3 Transform_TransformVector(EngineInternal.ITransform transform, EngineInternal.Vector3 vector) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).TransformVector(AsUnityVector(vector)));
        public override EngineInternal.Vector3 Transform_TransformVector(EngineInternal.ITransform transform, float x, float y, float z) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).TransformVector(x, y, z));
        public override EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.ITransform transform, EngineInternal.Vector3 vector) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformVector(AsUnityVector(vector)));
        public override EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.ITransform transform, float x, float y, float z) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformVector(x, y, z));
        public override EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.ITransform transform, EngineInternal.Vector3 position) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).TransformPoint(AsUnityVector(position)));
        public override EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.ITransform transform, float x, float y, float z) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).TransformPoint(x, y, z));
        public override EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.ITransform transform, EngineInternal.Vector3 position) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformPoint(AsUnityVector(position)));
        public override EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.ITransform transform, float x, float y, float z) => IsNull(transform) ? default : AsSwoleVector(AsUnityTransform(transform).InverseTransformPoint(x, y, z));
        public override void Transform_DetachChildren(EngineInternal.ITransform transform) { if (IsNotNull(transform)) AsUnityTransform(transform).DetachChildren(); }
        public override void Transform_SetAsFirstSibling(EngineInternal.ITransform transform) { if (IsNotNull(transform)) AsUnityTransform(transform).SetAsFirstSibling(); }
        public override void Transform_SetAsLastSibling(EngineInternal.ITransform transform) { if (IsNotNull(transform)) AsUnityTransform(transform).SetAsLastSibling(); }
        public override void Transform_SetSiblingIndex(EngineInternal.ITransform transform, int index) { if (IsNotNull(transform)) AsUnityTransform(transform).SetSiblingIndex(index); }
        public override int Transform_GetSiblingIndex(EngineInternal.ITransform transform) => IsNull(transform) ? default : AsUnityTransform(transform).GetSiblingIndex();
        public override EngineInternal.ITransform Transform_Find(EngineInternal.ITransform transform, string n) => IsNull(transform) ? default : AsSwoleTransform(AsUnityTransform(transform).Find(n));
        public override bool Transform_IsChildOf(EngineInternal.ITransform transform, EngineInternal.ITransform parent) 
        {
            if (IsNull(transform)) return false;
            var pT = AsUnityTransform(parent);
            if (pT == null) return false;
            return AsUnityTransform(transform).IsChildOf(pT); 
        }
        public override EngineInternal.ITransform Transform_GetChild(EngineInternal.ITransform transform, int index) => IsNull(transform) ? default : AsSwoleTransform(AsUnityTransform(transform).GetChild(index));
         
        #region Rect Transforms

        public override EngineInternal.Vector2 RectTransform_anchorMinGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).anchorMin);
        public override void RectTransform_anchorMinSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).anchorMin = AsUnityVector(val); }
        public override EngineInternal.Vector2 RectTransform_anchorMaxGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).anchorMax);
        public override void RectTransform_anchorMaxSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).anchorMax = AsUnityVector(val); }

        public override EngineInternal.Vector2 RectTransform_sizeDeltaGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).sizeDelta);
        public override void RectTransform_sizeDeltaSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).sizeDelta = AsUnityVector(val); }
        public override EngineInternal.Vector2 RectTransform_offsetMinGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).offsetMin);
        public override void RectTransform_offsetMinSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).offsetMin = AsUnityVector(val); }
        public override EngineInternal.Vector2 RectTransform_offsetMaxGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).offsetMax);
        public override void RectTransform_offsetMaxSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).offsetMax = AsUnityVector(val); }

        public override EngineInternal.Vector2 RectTransform_pivotGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).pivot);
        public override void RectTransform_pivotSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).pivot = AsUnityVector(val); }
         
        public override EngineInternal.Vector2 RectTransform_anchoredPositionGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).anchoredPosition);
        public override void RectTransform_anchoredPositionSet(EngineInternal.IRectTransform transform, EngineInternal.Vector2 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).anchoredPosition = AsUnityVector(val); }
        public override EngineInternal.Vector3 RectTransform_anchoredPosition3DGet(EngineInternal.IRectTransform transform) => IsNull(transform) ? default : AsSwoleVector(AsUnityRectTransform(transform).anchoredPosition3D);
        public override void RectTransform_anchoredPosition3DSet(EngineInternal.IRectTransform transform, EngineInternal.Vector3 val) { if (IsNotNull(transform)) AsUnityRectTransform(transform).anchoredPosition3D = AsUnityVector(val); }

        #endregion

        #endregion

        #region GameObjects

        public override void GameObject_SetActive(EngineInternal.GameObject gameObject, bool active)
        {
            if (IsNull(gameObject)) return;

            if (gameObject.Instance is GameObject go) go.SetActive(active); 
        }

        public override EngineInternal.IComponent GameObject_GetComponent(EngineInternal.GameObject gameObject, Type type) 
        {
            if (IsNull(gameObject)) return default;

            if (gameObject.Instance is GameObject go)
            {

                if (typeof(ICreationInstance).IsAssignableFrom(type)) return go.GetComponent<ICreationInstance>();
                if (typeof(EngineInternal.ITransform).IsAssignableFrom(type)) return go.GetComponent<EngineInternal.ITransform>(); 
                if (typeof(Transform).IsAssignableFrom(type)) // keep after ITransform
                { 
                    var proxy = go.GetComponent<EngineInternal.ITransform>();
                    if (proxy != null) return proxy;
                    return AsSwoleTransform(go.transform);
                }
                if (typeof(Component).IsAssignableFrom(type)) return AsSwoleComponent(go.GetComponent(type)); 

                if (typeof(IAnimator).IsAssignableFrom(type)) return go.GetComponent<IAnimator>(); 

                if (typeof(IAudibleObject).IsAssignableFrom(type)) return go.GetComponent<IAudibleObject>(); 
                if (typeof(IAudioSource).IsAssignableFrom(type)) return go.GetComponent<IAudioSource>();

                if (typeof(IRigidbody).IsAssignableFrom(type)) return go.GetComponent<IRigidbody>();
            }

            return default;
        } 
        public override EngineInternal.IComponent GameObject_AddComponent(EngineInternal.GameObject gameObject, Type type)
        {
            if (IsNull(gameObject)) return default;

            if (gameObject.Instance is GameObject go)
            {
                if (typeof(Component).IsAssignableFrom(type)) 
                {
                    Component comp = go.GetComponent(type);
                    if (comp == null) comp = go.AddComponent(type); 
                    return AsSwoleComponent(comp);
                }

                if (typeof(IAnimator).IsAssignableFrom(type)) return go.AddOrGetComponent<CustomAnimator>();

                if (typeof(IAudibleObject).IsAssignableFrom(type)) return go.AddOrGetComponent<AudibleObject>();
                if (typeof(IAudioSource).IsAssignableFrom(type)) 
                {
                    var proxy = go.GetComponent<IAudioSourceProxy>();
                    var orig = proxy == null ? null : proxy.Source; 
                    if (orig == null) orig = go.AddOrGetComponent<AudioSource>();
                    if (proxy == null) proxy = go.AddComponent<ExternalAudioSource>();
                    proxy.Source = orig;
                    return proxy;
                }

                if (typeof(IRigidbody).IsAssignableFrom(type))
                {
                    var proxy = go.GetComponent<IRigidbodyProxy>();
                    var orig = proxy == null ? null : proxy.Rigidbody;
                    if (orig == null) orig = go.AddOrGetComponent<Rigidbody>();
                    if (proxy == null) proxy = go.AddComponent<ExternalRigidbody>();
                    proxy.Rigidbody = orig;
                    return proxy;
                }

            }

            return default;
        }

        public override EngineInternal.GameObject GameObject_Create(string name = "") => AsSwoleGameObject(new GameObject(name));

        public override EngineInternal.GameObject GameObject_Instantiate(EngineInternal.GameObject gameObject) 
        {
            var unityGameObject = AsUnityGameObject(gameObject);
            if (unityGameObject == null) return default;

            return AsSwoleGameObject(GameObject.Instantiate(unityGameObject));
        }

        public override void GameObject_Destroy(EngineInternal.GameObject gameObject, float timeDelay = 0) => GameObject_DestroyInternal(gameObject, timeDelay, false);
        public override void GameObject_AdminDestroy(EngineInternal.GameObject gameObject, float timeDelay=0) => GameObject_DestroyInternal(gameObject, timeDelay, true);
        protected void GameObject_DestroyInternal(EngineInternal.GameObject gameObject, float timeDelay, bool admin) 
        {
            if (!admin && RuntimeHandler.IsALoadedExperience(gameObject)) // Don't allow loaded gameplay experiences to be destroyed by scripts
            {
                swole.LogWarning("Tried to destroy gameplay experience object!");
                return;
            }

            var unityGameObject = AsUnityGameObject(gameObject);
            if (unityGameObject == null) return;

            GameObject.Destroy(unityGameObject, timeDelay); 
        }

        #endregion

        #region Components

        public override EngineInternal.GameObject Component_gameObject(EngineInternal.IComponent component) 
        {
            if (component == null) return default;
            if (component.Instance is Component comp) return AsSwoleGameObject(comp.gameObject);

            return default;
        }

        #endregion

        #region Cameras

        public override void Camera_fieldOfViewSet(EngineInternal.Camera camera, float fieldOfView) 
        {
            if (camera.instance is Camera cam) cam.fieldOfView = fieldOfView;
        }
        public override float Camera_fieldOfViewGet(EngineInternal.Camera camera) 
        {
            if (camera.instance is Camera cam) return cam.fieldOfView;

            return 0;
        }

        public override void Camera_orthographicSet(EngineInternal.Camera camera, bool isOrthographic)
        {
            if (camera.instance is Camera cam) cam.orthographic = isOrthographic;
        }
        public override bool Camera_orthographicGet(EngineInternal.Camera camera)
        {
            if (camera.instance is Camera cam) return cam.orthographic;

            return false;
        }


        public override void Camera_orthographicSizeSet(EngineInternal.Camera camera, float orthographicSize)
        {
            if (camera.instance is Camera cam) cam.orthographicSize = orthographicSize;
        }
        public override float Camera_orthographicSizeGet(EngineInternal.Camera camera)
        {
            if (camera.instance is Camera cam) return cam.orthographicSize;

            return 0;
        }


        public override void Camera_nearClipPlaneSet(EngineInternal.Camera camera, float nearClipPlane)
        {
            if (camera.instance is Camera cam) cam.nearClipPlane = nearClipPlane;
        }
        public override float Camera_nearClipPlaneGet(EngineInternal.Camera camera)
        {
            if (camera.instance is Camera cam) return cam.nearClipPlane;

            return 0.01f;
        }

        public override void Camera_farClipPlaneSet(EngineInternal.Camera camera, float farClipPlane)
        {
            if (camera.instance is Camera cam) cam.farClipPlane = farClipPlane;
        }
        public override float Camera_farClipPlaneGet(EngineInternal.Camera camera)
        {
            if (camera.instance is Camera cam) return cam.farClipPlane;

            return 1000;
        }


        public override EngineInternal.Camera Camera_main() => new EngineInternal.Camera(Camera.main);

        public override EngineInternal.Camera PlayModeCamera => swole.IsInPlayMode && Swole.API.Unity.PlayModeCamera.HasActiveInstance ? new EngineInternal.Camera(Swole.API.Unity.PlayModeCamera.activeInstance.camera) : default;

        #endregion

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
