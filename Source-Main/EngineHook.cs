using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Swole.Animation;
using Swole.Script;

namespace Swole
{

    public class EngineHook
    {

        protected static EngineHook activeHook;

        public virtual string Name => "No Engine";
        public virtual bool HookWasSuccessful => true;

        public class ConsoleLogger : SwoleLogger
        {
            protected override void LogInternal(string message) => Console.WriteLine(message);

            protected override void LogErrorInternal(string error) => Console.Error.WriteLine(error);

            protected override void LogWarningInternal(string warning) => Console.WriteLine($"[WARN]: {warning}");

        }

        protected SwoleLogger logger;
        public virtual SwoleLogger Logger
        {
            get
            {
                if (logger == null) logger = new ConsoleLogger();

                return logger;
            }
        }

        public virtual string WorkingDirectory => Environment.CurrentDirectory;

        public virtual string ParseSource(string source) => source;

        protected RuntimeEnvironment runtimeEnvironment;
        public virtual RuntimeEnvironment RuntimeEnvironment
        {
            get
            {
                if (runtimeEnvironment == null)
                {
                    runtimeEnvironment = new RuntimeEnvironment($"{Name} Main");
                }

                return runtimeEnvironment;
            }
        }

        #region JSON Serialization

        protected virtual string ToJsonInternal(object obj, bool prettyPrint = false) => DefaultJsonSerializer.ToJson(obj, prettyPrint);
        protected virtual object FromJsonInternal(string json, Type type) => DefaultJsonSerializer.FromJson(json, type);
        protected virtual T FromJsonInternal<T>(string json) => DefaultJsonSerializer.FromJson<T>(json);

        public string ToJson(object obj, bool prettyPrint = false) 
        {
            if (obj == null) return "{}";

            Type t = obj.GetType();

            if (typeof(ISwoleSerialization).IsAssignableFrom(t)) return ToJsonInternal(((ISwoleSerialization)obj).AsSerializableObject(), prettyPrint);

            return ToJsonInternal(obj, prettyPrint); 

        }
        public object FromJson(string json, Type type) 
        {
            return FromJsonInternal(json, type);
        }
        public T FromJson<T>(string json) 
        {
            return FromJsonInternal<T>(json);
        }

        #endregion

        #region File Compression

        [Serializable]
        public struct FileDescr
        {
            public string fileName;
            public ulong fileSize;
        }

        public struct DecompressionResult
        {
            public List<FileDescr> fileDescs;
            public List<byte[]> fileData;
        }

        public void DecompressZIP(string filePath, ref List<FileDescr> fileNames, ref List<byte[]> fileData) => DecompressZIP(string.IsNullOrEmpty(filePath) ? null : File.ReadAllBytes(filePath), ref fileNames, ref fileData);
        async public Task<DecompressionResult> DecompressZIPAsync(string filePath, List<FileDescr> fileNames = null, List<byte[]> fileData = null)
        {
            if (string.IsNullOrEmpty(filePath)) return new DecompressionResult() { fileDescs = fileNames, fileData = fileData };

            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            void Decompress() => DecompressZIP(bytes, ref fileNames, ref fileData);
            await Task.Run(Decompress);

            return new DecompressionResult() { fileDescs = fileNames, fileData = fileData };
        }

        public virtual void DecompressZIP(byte[] data, ref List<FileDescr> fileNames, ref List<byte[]> fileData)
        {
            if (fileNames == null) fileNames = new List<FileDescr>();
            if (fileData == null) fileData = new List<byte[]>();

            if (data == null) return;
            // TODO: Custom Zip implementation
        }

        public bool CompressZIP(DirectoryInfo sourceDirectory, FileInfo destinationPath) => CompressZIP(sourceDirectory == null ? null : sourceDirectory.FullName, destinationPath == null ? null : destinationPath.FullName);
        public bool CompressZIP(string sourceDirectoryPath, FileInfo destinationPath) => CompressZIP(sourceDirectoryPath, destinationPath == null ? null : destinationPath.FullName);
        public bool CompressZIP(DirectoryInfo sourceDirectory, string destinationPath) => CompressZIP(sourceDirectory == null ? null : sourceDirectory.FullName, destinationPath);
        public virtual bool CompressZIP(string sourceDirectoryPath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourceDirectoryPath) || string.IsNullOrEmpty(destinationPath)) return false;
            // TODO: Custom Zip implementation
            return false;
        }
        async public Task<bool> CompressZIPAsync(string sourceDirectoryPath, string destinationPath) 
        { 
            bool Compress()
            {
                return CompressZIP(sourceDirectoryPath, destinationPath);
            }
            return await Task<bool>.Run(Compress); 
        }

        #endregion

        /// <summary>
        /// Executes the singleton callstack.
        /// </summary>
        public virtual void FrameUpdate() 
        { 
            SingletonCallStack.Execute(); 
        }
        /// <summary>
        /// Executes the singleton late callstack.
        /// </summary>
        public virtual void FrameUpdateLate() 
        { 
            SingletonCallStack.ExecuteLate(); 
        }
        /// <summary>
        /// Executes the singleton physics callstack.
        /// </summary>
        public virtual void PhysicsUpdate() 
        { 
            SingletonCallStack.ExecuteFixed(); 
        }

        #region RNG

        public virtual EngineInternal.RNG RNG_Global() => default;

        public virtual EngineInternal.RNG RNG_New(int seed) => default;
        public virtual EngineInternal.RNG RNG_New(EngineInternal.RNGState initialState) => default;
        public virtual EngineInternal.RNG RNG_New(EngineInternal.RNGState initialState, EngineInternal.RNGState currentState) => default;

        public virtual EngineInternal.RNG RNG_Reset(EngineInternal.RNG rng) => default;

        public virtual EngineInternal.RNG RNG_Fork(EngineInternal.RNG rng) => default;

        public virtual int RNG_Seed(EngineInternal.RNG rng) => 0;

        public virtual EngineInternal.RNGState RNG_State(EngineInternal.RNG rng) => default;

        public virtual float RNG_NextValue(EngineInternal.RNG rng) => 0;
        public virtual bool RNG_NextBool(EngineInternal.RNG rng) => false;

        public virtual EngineInternal.Vector4 RNG_NextColor(EngineInternal.RNG rng) => EngineInternal.Vector4.one;

        public virtual EngineInternal.Quaternion RNG_NextRotation(EngineInternal.RNG rng) => EngineInternal.Quaternion.identity;
        public virtual EngineInternal.Quaternion RNG_NextRotationUniform(EngineInternal.RNG rng) => EngineInternal.Quaternion.identity;

        public virtual float RNG_Range(EngineInternal.RNG rng, float minInclusive = 0, float maxInclusive = 1) => 0;
        public virtual int RNG_RangeInt(EngineInternal.RNG rng, int minInclusive, int maxExclusive) => 0;

        #endregion

        #region Engine Specific

        public virtual bool IsNull(object engineObject)
        {

            if (engineObject == null) return true;
            if (engineObject is EngineInternal.IEngineObject eo) return eo.Instance == null || eo.IsDestroyed;

            return false;
        }
        public bool IsNotNull(object engineObject) => !IsNull(engineObject);

        public virtual void InvokeOverTime(VoidParameterlessDelegate func, SwoleCancellationToken token, float duration, float step = 0) { } 

        #region Audio

        public virtual IAudioAsset GetAudioAsset(string assetId, string audioCollectionId = null, bool caseSensitive = false) => null;

        #endregion

        public virtual int Object_GetInstanceID(object engineObject) => -1;

        public virtual EngineInternal.EngineObject Object_Instantiate(object engineObject) => default;
        public virtual void Object_Destroy(object engineObject, float timeDelay = 0) { }
        public virtual void Object_AdminDestroy(object engineObject, float timeDelay = 0) { }

        #region Math

        public virtual EngineInternal.Vector2 lerp(EngineInternal.Vector2 vA, EngineInternal.Vector2 vB, float t) => default;
        public virtual EngineInternal.Vector3 lerp(EngineInternal.Vector3 vA, EngineInternal.Vector3 vB, float t) => default;
        public virtual EngineInternal.Vector4 lerp(EngineInternal.Vector4 vA, EngineInternal.Vector4 vB, float t) => default;
        public virtual EngineInternal.Vector4 slerp(EngineInternal.Vector4 vA, EngineInternal.Vector4 vB, float t) => default;
        public virtual EngineInternal.Quaternion slerp(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB, float t) => default;

        #endregion

        public virtual EngineInternal.Vector3 GetLocalPosition(object engineObject) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Vector3 GetLocalScale(object engineObject) => EngineInternal.Vector3.one;
        public virtual EngineInternal.Quaternion GetLocalRotation(object engineObject) => EngineInternal.Quaternion.identity;

        public virtual EngineInternal.Vector3 GetWorldPosition(object engineObject) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Vector3 GetLossyScale(object engineObject) => EngineInternal.Vector3.one;
        public virtual EngineInternal.Quaternion GetWorldRotation(object engineObject) => EngineInternal.Quaternion.identity;

        public virtual void SetLocalPosition(object engineObject, EngineInternal.Vector3 localPosition) { }
        public virtual void SetLocalRotation(object engineObject, EngineInternal.Quaternion localRotation) { }
        public virtual void SetLocalScale(object engineObject, EngineInternal.Vector3 localScale) { }

        public virtual void SetWorldPosition(object engineObject, EngineInternal.Vector3 position) { }
        public virtual void SetWorldRotation(object engineObject, EngineInternal.Quaternion rotation) { }

        public virtual string GetName(object engineObject) => string.Empty;

        public virtual EngineInternal.Quaternion Mul(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB) => EngineInternal.Quaternion.identity;
        public virtual EngineInternal.Vector3 Mul(EngineInternal.Quaternion q, EngineInternal.Vector3 v) => v;
        public virtual EngineInternal.Vector3 Rotate(EngineInternal.Quaternion q, EngineInternal.Vector3 v) => Mul(q, v);
        public virtual EngineInternal.Matrix4x4 Mul(EngineInternal.Matrix4x4 mA, EngineInternal.Matrix4x4 mB) => EngineInternal.Matrix4x4.identity;

        public virtual EngineInternal.Vector3 Mul(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => point;
        public virtual EngineInternal.Vector3 Mul3x4(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => point;
        public virtual EngineInternal.Vector3 Rotate(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 vector) => vector;

        public virtual float Vector3_SignedAngle(EngineInternal.Vector3 vA, EngineInternal.Vector3 vB, EngineInternal.Vector3 axis) => 0;

        public EngineInternal.Quaternion Quaternion_Euler(EngineInternal.Vector3 eulerAngles) => Quaternion_Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        public virtual EngineInternal.Quaternion Quaternion_Euler(float x, float y, float z) => EngineInternal.Quaternion.identity;
        public virtual EngineInternal.Vector3 Quaternion_EulerAngles(EngineInternal.Quaternion quaternion) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Quaternion Quaternion_Inverse(EngineInternal.Quaternion q) => q;
        public virtual float Quaternion_Dot(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB) => 0;
        public virtual EngineInternal.Quaternion Quaternion_FromToRotation(EngineInternal.Vector3 vA, EngineInternal.Vector3 vB) => EngineInternal.Quaternion.identity;
        public virtual EngineInternal.Quaternion Quaternion_LookRotation(EngineInternal.Vector3 forward, EngineInternal.Vector3 upward) => EngineInternal.Quaternion.identity;

        public virtual EngineInternal.Matrix4x4 Matrix4x4_Inverse(EngineInternal.Matrix4x4 m) => m;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_TRS(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation, EngineInternal.Vector3 scale) => EngineInternal.Matrix4x4.identity;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_Scale(EngineInternal.Vector3 vector) => EngineInternal.Matrix4x4.identity;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_Translate(EngineInternal.Vector3 vector) => EngineInternal.Matrix4x4.identity;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_Rotate(EngineInternal.Quaternion q) => EngineInternal.Matrix4x4.identity;

        #region Transforms

        public virtual EngineInternal.ITransform Transform_GetParent(EngineInternal.ITransform transform) => default;
        public virtual void Transform_SetParent(EngineInternal.ITransform transform, EngineInternal.ITransform parent, bool worldPositionStays = true) { }
        public virtual EngineInternal.Vector3 Transform_lossyScale(EngineInternal.ITransform transform) => default;
        public virtual EngineInternal.Vector3 Transform_eulerAnglesGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_eulerAnglesSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_localEulerAnglesGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_localEulerAnglesSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_rightGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_rightSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_upGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_upSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_forwardGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_forwardSet(EngineInternal.ITransform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Matrix4x4 Transform_worldToLocalMatrix(EngineInternal.ITransform transform) => default;
        public virtual EngineInternal.Matrix4x4 Transform_localToWorldMatrix(EngineInternal.ITransform transform) => default;
        public virtual EngineInternal.ITransform Transform_root(EngineInternal.ITransform transform) => default;
        public virtual int Transform_childCount(EngineInternal.ITransform transform) => default;
        public virtual bool Transform_hasChangedGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_hasChangedSet(EngineInternal.ITransform transform, bool val) { }
        public virtual int Transform_hierarchyCapacityGet(EngineInternal.ITransform transform) => default;
        public virtual void Transform_hierarchyCapacitySet(EngineInternal.ITransform transform, int val) { }
        public virtual int Transform_hierarchyCount(EngineInternal.ITransform transform) => default;
        public virtual void Transform_SetPositionAndRotation(EngineInternal.ITransform transform, EngineInternal.Vector3 position, EngineInternal.Quaternion rotation) { }
        public virtual void Transform_SetLocalPositionAndRotation(EngineInternal.ITransform transform, EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation) { }
        public virtual void Transform_GetPositionAndRotation(EngineInternal.ITransform transform, out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation) { position = default; rotation = default; }
        public virtual void Transform_GetLocalPositionAndRotation(EngineInternal.ITransform transform, out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation) { localPosition = default; localRotation = default; }
        public virtual void Transform_Translate(EngineInternal.ITransform transform, EngineInternal.Vector3 translation, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Translate(EngineInternal.ITransform transform, EngineInternal.Vector3 translation) { }
        public virtual void Transform_Translate(EngineInternal.ITransform transform, float x, float y, float z, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Translate(EngineInternal.ITransform transform, float x, float y, float z) { }
        public virtual void Transform_Translate(EngineInternal.ITransform transform, EngineInternal.Vector3 translation, EngineInternal.ITransform relativeTo) { }
        public virtual void Transform_Translate(EngineInternal.ITransform transform, float x, float y, float z, EngineInternal.ITransform relativeTo) { }
        public virtual void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 eulers, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 eulers) { }
        public virtual void Transform_Rotate(EngineInternal.ITransform transform, float xAngle, float yAngle, float zAngle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Rotate(EngineInternal.ITransform transform, float xAngle, float yAngle, float zAngle) { }
        public virtual void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 axis, float angle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Rotate(EngineInternal.ITransform transform, EngineInternal.Vector3 axis, float angle) { }
        public virtual void Transform_RotateAround(EngineInternal.ITransform transform, EngineInternal.Vector3 point, EngineInternal.Vector3 axis, float angle) { }
        public virtual void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.ITransform target, EngineInternal.Vector3 worldUp) { }
        public virtual void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.ITransform target) { }
        public virtual void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.Vector3 worldPosition, EngineInternal.Vector3 worldUp) { }
        public virtual void Transform_LookAt(EngineInternal.ITransform transform, EngineInternal.Vector3 worldPosition) { }
        public virtual EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.ITransform transform, EngineInternal.Vector3 direction) => default;
        public virtual EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.ITransform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.ITransform transform, EngineInternal.Vector3 direction) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.ITransform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_TransformVector(EngineInternal.ITransform transform, EngineInternal.Vector3 vector) => default;
        public virtual EngineInternal.Vector3 Transform_TransformVector(EngineInternal.ITransform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.ITransform transform, EngineInternal.Vector3 vector) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.ITransform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.ITransform transform, EngineInternal.Vector3 position) => default;
        public virtual EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.ITransform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.ITransform transform, EngineInternal.Vector3 position) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.ITransform transform, float x, float y, float z) => default;
        public virtual void Transform_DetachChildren(EngineInternal.ITransform transform) { }
        public virtual void Transform_SetAsFirstSibling(EngineInternal.ITransform transform) { }
        public virtual void Transform_SetAsLastSibling(EngineInternal.ITransform transform) { }
        public virtual void Transform_SetSiblingIndex(EngineInternal.ITransform transform, int index) { }
        public virtual int Transform_GetSiblingIndex(EngineInternal.ITransform transform) => default;
        public virtual EngineInternal.ITransform Transform_Find(EngineInternal.ITransform transform, string n) => default;
        public virtual bool Transform_IsChildOf(EngineInternal.ITransform transform, EngineInternal.ITransform parent) => default;
        public virtual EngineInternal.ITransform Transform_GetChild(EngineInternal.ITransform transform, int index) => default;

        #endregion

        #region GameObjects

        public virtual void GameObject_SetActive(EngineInternal.GameObject gameObject, bool active) { }

        public virtual EngineInternal.IComponent GameObject_GetComponent(EngineInternal.GameObject gameObject, Type type) => default;
        public virtual EngineInternal.IComponent GameObject_AddComponent(EngineInternal.GameObject gameObject, Type type) => default;

        public virtual EngineInternal.GameObject GameObject_Create(string name = "") => default;
        public virtual EngineInternal.GameObject GameObject_Instantiate(EngineInternal.GameObject gameObject) => default;
        public virtual void GameObject_Destroy(EngineInternal.GameObject gameObject, float timeDelay = 0) { }
        public virtual void GameObject_AdminDestroy(EngineInternal.GameObject gameObject, float timeDelay = 0) { }

        #endregion

        #region Components

        public virtual EngineInternal.GameObject Component_gameObject(EngineInternal.IComponent component) => default;

        #endregion

        #region Cameras

        public virtual void Camera_fieldOfViewSet(EngineInternal.Camera camera, float fieldOfView) { }
        public virtual float Camera_fieldOfViewGet(EngineInternal.Camera camera) => 60;

        public virtual void Camera_orthographicSet(EngineInternal.Camera camera, bool isOrthographic) { }
        public virtual bool Camera_orthographicGet(EngineInternal.Camera camera) => false;

        public virtual void Camera_orthographicSizeSet(EngineInternal.Camera camera, float orthographicSize) { } 
        public virtual float Camera_orthographicSizeGet(EngineInternal.Camera camera) => 5;

        public virtual void Camera_nearClipPlaneSet(EngineInternal.Camera camera, float nearClipPlane) { }
        public virtual float Camera_nearClipPlaneGet(EngineInternal.Camera camera) => 0.01f;
        public virtual void Camera_farClipPlaneSet(EngineInternal.Camera camera, float farClipPlane) { }
        public virtual float Camera_farClipPlaneGet(EngineInternal.Camera camera) => 1000;

        public virtual EngineInternal.Camera Camera_main() => default;
        public virtual EngineInternal.Camera PlayModeCamera => default;

        #endregion

        #endregion

        #region Swole Specific

        public virtual IInputManager InputManager { get { return null; } }

        protected readonly Dictionary<string, int> trackedTransforms = new Dictionary<string, int>();
        public virtual void TrackTransform(EngineInternal.ITransform transform)
        {
            if (transform == null) return;
            string id = transform.ID;
            if (string.IsNullOrWhiteSpace(id))
            {
                swole.LogWarning("Tried to track transform with empty ID");
                return;
            }
            trackedTransforms.TryGetValue(id, out int trackers);
            trackedTransforms[id] = trackers + 1;
        }
        public virtual void UntrackTransform(EngineInternal.ITransform transform)
        {
            if (transform == null) return;
            string id = transform.ID;
            if (string.IsNullOrWhiteSpace(id))
            {
                swole.LogWarning("Tried to untrack transform with empty ID");
                return;
            }
            trackedTransforms.TryGetValue(id, out int trackers);
            trackers = trackers - 1;
            if (trackers <= 0) trackedTransforms.Remove(id);
        }

        /// <summary>
        /// Is the object a root transform for a gameplay experience?
        /// </summary>
        public virtual bool IsExperienceRoot(object obj) => false;

        public virtual EngineInternal.CreationInstance GetGameplayExperienceRoot(object obj) => default;
        public virtual EngineInternal.CreationInstance GetRootCreationInstance(object obj) => default;

        public virtual int GetTileCount(object boObject) => 0;
        public virtual EngineInternal.Tile GetTileFromSet(object boObject, int tileIndex) => default;

        public virtual EngineInternal.TileSet GetTileSet(string tileSetId, string tileCollectionId = null, bool caseSensitive = false) => default;

        public virtual void SetSwoleId(object obj, int id) { }
        public virtual int GetSwoleId(object obj) => -1;

        public virtual EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, int tileIndex, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 localScale) => default;
        public virtual EngineInternal.CreationInstance CreateNewCreationInstance(Creation creation, bool useRealTransformsOnly, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 localScale, bool autoInitialize=true, SwoleLogger logger = null) => default;

        public virtual IRuntimeEventHandler GetEventHandler(object obj)
        {
            if (obj is IRuntimeEventHandler eventHandler) return eventHandler; 
            return default;
        }

        public virtual T FindAsset<T>(string assetPath, IRuntimeHost host, bool caseSensitive = false) where T : ISwoleAsset
        {
            TryFindAsset<T>(assetPath, host, out T asset, caseSensitive);
            return asset;
        }
        public virtual ISwoleAsset FindAsset(string assetPath, Type type, IRuntimeHost host, bool caseSensitive = false)
        {
            TryFindAsset(assetPath, type, host, out ISwoleAsset asset, caseSensitive);
            return asset;
        }
        public enum FindAssetResult
        {
            Success, InvalidType, EmptyPath, InvalidHost, EmptyPackageName, InvalidPackage, PackageNotImported, NotFound, NotFoundLocally
        }
        public virtual FindAssetResult TryFindAsset<T>(string assetPath, IRuntimeHost host, out T asset, bool caseSensitive = false) where T : ISwoleAsset
        {
            asset = default;
            var res = TryFindAsset(assetPath, typeof(T), host, out var _asset, caseSensitive);
            if (res == FindAssetResult.Success)
            {
                if (_asset is T)
                {
                    asset = (T)_asset;
                }
                else res = FindAssetResult.NotFound; 
            }

            return res;
        }
        public virtual FindAssetResult TryFindAsset(string assetPath, Type type, IRuntimeHost host, out ISwoleAsset asset, bool caseSensitive = false)
        {
            asset = default;
            if (type == null || !typeof(ISwoleAsset).IsAssignableFrom(type)) return FindAssetResult.InvalidType;
            if (string.IsNullOrWhiteSpace(assetPath)) return FindAssetResult.EmptyPath;

            if (host == null) return FindAssetResult.InvalidHost;

            string packageName = null;

            int delimiterIndex = assetPath.IndexOf('/');
            if (delimiterIndex < 0) delimiterIndex = assetPath.LastIndexOf('.'); 
            if (delimiterIndex >= 0)
            {
                packageName = assetPath.Substring(0, delimiterIndex);
                assetPath = assetPath.Substring(delimiterIndex + 1);
            }

            PackageIdentifier pkgIdentifier = default;
            ContentPackage package = null;
            bool isLocalContent = false;
            if (string.IsNullOrWhiteSpace(packageName)) 
            {
                package = host.LocalContent;
                isLocalContent = true;

                if (package == null) return FindAssetResult.EmptyPackageName; 
            }

            if (package == null)
            {
                SwoleScriptSemantics.SplitFullPackageString(packageName, out string pkgName, out string pkgVer);
                if (string.IsNullOrWhiteSpace(pkgName)) return FindAssetResult.InvalidPackage;
                if (string.IsNullOrWhiteSpace(pkgVer))
                {
                    host.TryGetReferencePackage(pkgName, out package);
                }
                else
                {
                    pkgIdentifier = new PackageIdentifier(pkgName, pkgVer);
                    if (!pkgIdentifier.VersionIsValid) return FindAssetResult.InvalidPackage;
                    host.TryGetReferencePackage(pkgIdentifier, out package);
                }
                if (package == null) return FindAssetResult.PackageNotImported;
            }

            if (package.TryFind(out IContent content, assetPath, type, caseSensitive))
            {
                asset = content;
                return FindAssetResult.Success;
            }

            return isLocalContent ? FindAssetResult.NotFoundLocally : FindAssetResult.NotFound;
        }

        #region Animation

        public virtual IAnimationController CreateNewAnimationController(string name, IRuntimeHost host)
        {
            return null;
        }
        public virtual IAnimationLayer CreateNewAnimationLayer(string name)
        {
            return null;
        }
        public virtual IAnimationStateMachine CreateNewStateMachine(string name, int motionControllerIndex, Transition[] transitions = null)
        {
            return null;
        }
        public virtual IAnimationReference CreateNewAnimationReference(string name, IAnimationAsset asset, AnimationLoopMode loopMode)
        {
            return null;
        }

        #endregion

        #endregion

    }

}
