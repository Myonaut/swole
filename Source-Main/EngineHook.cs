using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Swole.Script;

namespace Swole
{

    public class EngineHook
    {

        public virtual string Name => "No Engine";
        public virtual bool HookWasSuccessful => true;

        protected SwoleLogger logger;
        public virtual SwoleLogger Logger
        {
            get
            {
                if (logger == null) logger = new SwoleLogger();

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
            public List<FileDescr> fileNames;
            public List<byte[]> fileData;
        }

        public void DecompressZIP(string filePath, ref List<FileDescr> fileNames, ref List<byte[]> fileData) => DecompressZIP(string.IsNullOrEmpty(filePath) ? null : File.ReadAllBytes(filePath), ref fileNames, ref fileData);
        async public Task<DecompressionResult> DecompressZIPAsync(string filePath, List<FileDescr> fileNames = null, List<byte[]> fileData = null)
        {
            if (string.IsNullOrEmpty(filePath)) return new DecompressionResult() { fileNames = fileNames, fileData = fileData };

            byte[] bytes = await File.ReadAllBytesAsync(filePath);

            void Decompress() => DecompressZIP(bytes, ref fileNames, ref fileData);
            await Task.Run(Decompress);

            return new DecompressionResult() { fileNames = fileNames, fileData = fileData };
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
        public virtual void UpdateSingletons() => SingletonCallStack.Execute();
        /// <summary>
        /// Executes the singleton late callstack.
        /// </summary>
        public virtual void UpdateSingletonsLate() => SingletonCallStack.ExecuteLate();
        /// <summary>
        /// Executes the singleton physics callstack.
        /// </summary>
        public virtual void UpdateSingletonsPhysics() => SingletonCallStack.ExecuteFixed();

        #region Engine Specific

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

        public virtual string GetName(object engineObject) => "";

        public virtual EngineInternal.Quaternion Mul(EngineInternal.Quaternion qA, EngineInternal.Quaternion qB) => EngineInternal.Quaternion.identity;
        public virtual EngineInternal.Vector3 Mul(EngineInternal.Quaternion q, EngineInternal.Vector3 v) => v;
        public virtual EngineInternal.Vector3 Rotate(EngineInternal.Quaternion q, EngineInternal.Vector3 v) => Mul(q, v);
        public virtual EngineInternal.Matrix4x4 Mul(EngineInternal.Matrix4x4 mA, EngineInternal.Matrix4x4 mB) => EngineInternal.Matrix4x4.identity;

        public virtual EngineInternal.Vector3 Mul(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => point;
        public virtual EngineInternal.Vector3 Mul3x4(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 point) => point;
        public virtual EngineInternal.Vector3 Rotate(EngineInternal.Matrix4x4 m, EngineInternal.Vector3 vector) => vector;

        public EngineInternal.Quaternion Quaternion_Euler(EngineInternal.Vector3 eulerAngles) => Quaternion_Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        public virtual EngineInternal.Quaternion Quaternion_Euler(float x, float y, float z) => EngineInternal.Quaternion.identity;
        public virtual EngineInternal.Vector3 Quaternion_EulerAngles(EngineInternal.Quaternion quaternion) => EngineInternal.Vector3.zero;
        public virtual EngineInternal.Quaternion Quaternion_Inverse(EngineInternal.Quaternion q) => q;

        public virtual EngineInternal.Matrix4x4 Matrix4x4_TRS(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation, EngineInternal.Vector3 scale) => EngineInternal.Matrix4x4.identity;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_Scale(EngineInternal.Vector3 vector) => EngineInternal.Matrix4x4.identity;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_Translate(EngineInternal.Vector3 vector) => EngineInternal.Matrix4x4.identity;
        public virtual EngineInternal.Matrix4x4 Matrix4x4_Rotate(EngineInternal.Quaternion q) => EngineInternal.Matrix4x4.identity;

        #region Transforms

        public virtual EngineInternal.Transform Transform_GetParent(EngineInternal.Transform transform) => default;
        public virtual void Transform_SetParent(EngineInternal.Transform transform, EngineInternal.Transform parent, bool worldPositionStays = true) { }
        public virtual EngineInternal.Vector3 Transform_lossyScale(EngineInternal.Transform transform) => default;
        public virtual EngineInternal.Vector3 Transform_eulerAnglesGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_eulerAnglesSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_localEulerAnglesGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_localEulerAnglesSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_rightGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_rightSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_upGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_upSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Vector3 Transform_forwardGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_forwardSet(EngineInternal.Transform transform, EngineInternal.Vector3 val) { }
        public virtual EngineInternal.Matrix4x4 Transform_worldToLocalMatrix(EngineInternal.Transform transform) => default;
        public virtual EngineInternal.Matrix4x4 Transform_localToWorldMatrix(EngineInternal.Transform transform) => default;
        public virtual EngineInternal.Transform Transform_root(EngineInternal.Transform transform) => default;
        public virtual int Transform_childCount(EngineInternal.Transform transform) => default;
        public virtual bool Transform_hasChangedGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_hasChangedSet(EngineInternal.Transform transform, bool val) { }
        public virtual int Transform_hierarchyCapacityGet(EngineInternal.Transform transform) => default;
        public virtual void Transform_hierarchyCapacitySet(EngineInternal.Transform transform, int val) { }
        public virtual int Transform_hierarchyCount(EngineInternal.Transform transform) => default;
        public virtual void Transform_SetPositionAndRotation(EngineInternal.Transform transform, EngineInternal.Vector3 position, EngineInternal.Quaternion rotation) { }
        public virtual void Transform_SetLocalPositionAndRotation(EngineInternal.Transform transform, EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation) { }
        public virtual void Transform_GetPositionAndRotation(EngineInternal.Transform transform, out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation) { position = default; rotation = default; }
        public virtual void Transform_GetLocalPositionAndRotation(EngineInternal.Transform transform, out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation) { localPosition = default; localRotation = default; }
        public virtual void Transform_Translate(EngineInternal.Transform transform, EngineInternal.Vector3 translation, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Translate(EngineInternal.Transform transform, EngineInternal.Vector3 translation) { }
        public virtual void Transform_Translate(EngineInternal.Transform transform, float x, float y, float z, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Translate(EngineInternal.Transform transform, float x, float y, float z) { }
        public virtual void Transform_Translate(EngineInternal.Transform transform, EngineInternal.Vector3 translation, EngineInternal.Transform relativeTo) { }
        public virtual void Transform_Translate(EngineInternal.Transform transform, float x, float y, float z, EngineInternal.Transform relativeTo) { }
        public virtual void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 eulers, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 eulers) { }
        public virtual void Transform_Rotate(EngineInternal.Transform transform, float xAngle, float yAngle, float zAngle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Rotate(EngineInternal.Transform transform, float xAngle, float yAngle, float zAngle) { }
        public virtual void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 axis, float angle, EngineInternal.Space relativeTo = EngineInternal.Space.Self) { }
        public virtual void Transform_Rotate(EngineInternal.Transform transform, EngineInternal.Vector3 axis, float angle) { }
        public virtual void Transform_RotateAround(EngineInternal.Transform transform, EngineInternal.Vector3 point, EngineInternal.Vector3 axis, float angle) { }
        public virtual void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Transform target, EngineInternal.Vector3 worldUp) { }
        public virtual void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Transform target) { }
        public virtual void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Vector3 worldPosition, EngineInternal.Vector3 worldUp) { }
        public virtual void Transform_LookAt(EngineInternal.Transform transform, EngineInternal.Vector3 worldPosition) { }
        public virtual EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.Transform transform, EngineInternal.Vector3 direction) => default;
        public virtual EngineInternal.Vector3 Transform_TransformDirection(EngineInternal.Transform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.Transform transform, EngineInternal.Vector3 direction) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformDirection(EngineInternal.Transform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_TransformVector(EngineInternal.Transform transform, EngineInternal.Vector3 vector) => default;
        public virtual EngineInternal.Vector3 Transform_TransformVector(EngineInternal.Transform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.Transform transform, EngineInternal.Vector3 vector) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformVector(EngineInternal.Transform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.Transform transform, EngineInternal.Vector3 position) => default;
        public virtual EngineInternal.Vector3 Transform_TransformPoint(EngineInternal.Transform transform, float x, float y, float z) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.Transform transform, EngineInternal.Vector3 position) => default;
        public virtual EngineInternal.Vector3 Transform_InverseTransformPoint(EngineInternal.Transform transform, float x, float y, float z) => default;
        public virtual void Transform_DetachChildren(EngineInternal.Transform transform) { }
        public virtual void Transform_SetAsFirstSibling(EngineInternal.Transform transform) { }
        public virtual void Transform_SetAsLastSibling(EngineInternal.Transform transform) { }
        public virtual void Transform_SetSiblingIndex(EngineInternal.Transform transform, int index) { }
        public virtual int Transform_GetSiblingIndex(EngineInternal.Transform transform) => default;
        public virtual EngineInternal.Transform Transform_Find(EngineInternal.Transform transform, string n) => default;
        public virtual bool Transform_IsChildOf(EngineInternal.Transform transform, EngineInternal.Transform parent) => default;
        public virtual EngineInternal.Transform Transform_GetChild(EngineInternal.Transform transform, int index) => default;

        #endregion

        #region GameObjects

        public virtual EngineInternal.GameObject GameObject_Create(string name = "") => default;
        public virtual EngineInternal.GameObject GameObject_Instantiate(EngineInternal.GameObject gameObject) => default;
        public virtual void GameObject_Destroy(EngineInternal.GameObject gameObject, float timeDelay = 0) { }

        #endregion

        #endregion

        #region Bulk Out! Specific

        public virtual int GetTileCount(object boObject) => 0;
        public virtual EngineInternal.Tile GetTileFromSet(object boObject, int tileIndex) => default;

        public virtual EngineInternal.TileSet GetTileSet(string tileSetName) => default;

        public virtual EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, int tileIndex, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 scaleInRoot) => default;

        #endregion

    }

}
