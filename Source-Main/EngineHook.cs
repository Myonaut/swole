using Swole.Script;
using System;

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

        public virtual string ToJson(object obj, bool prettyPrint = false) => DefaultJsonSerializer.ToJson(obj, prettyPrint);
        public virtual object FromJson(string json, Type type) => DefaultJsonSerializer.FromJson(json, type);
        public virtual T FromJson<T>(string json) => DefaultJsonSerializer.FromJson<T>(json);

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
        public virtual EngineInternal.Matrix4x4 Mul(EngineInternal.Matrix4x4 mA, EngineInternal.Matrix4x4 mB) => EngineInternal.Matrix4x4.identity;

        public EngineInternal.Quaternion Quaternion_Euler(EngineInternal.Vector3 eulerAngles) => Quaternion_Euler(eulerAngles.x, eulerAngles.y, eulerAngles.z);
        public virtual EngineInternal.Quaternion Quaternion_Euler(float x, float y, float z) => EngineInternal.Quaternion.identity;

        public virtual EngineInternal.Matrix4x4 Matrix4x4_TRS(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation, EngineInternal.Vector3 scale) => EngineInternal.Matrix4x4.identity;

        public virtual EngineInternal.Transform Transform_GetParent(EngineInternal.Transform transform) => default;
        public virtual void Transform_SetParent(EngineInternal.Transform transform, EngineInternal.Transform parent, bool worldPositionStays = true) { }

        public virtual EngineInternal.GameObject GameObject_Create(string name = "") => default;
        public virtual EngineInternal.GameObject GameObject_Instantiate(EngineInternal.GameObject gameObject) => default;
        public virtual void GameObject_Destroy(EngineInternal.GameObject gameObject, float timeDelay = 0) { }

        #endregion

        #region Bulk Out! Specific

        public virtual int GetTileCount(object boObject) => 0;
        public virtual EngineInternal.Tile GetTileFromSet(object boObject, int tileIndex) => default;

        public virtual EngineInternal.TileSet GetTileSet(string tileSetName) => default;

        public virtual EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, int tileIndex, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 scaleInRoot) => default;

        #endregion

    }

}
