using System;
using System.Collections.Generic;

using static Swole.EngineInternal;

namespace Swole
{
    public class ObjectSpawnGroup : SwoleObject<ObjectSpawnGroup, ObjectSpawnGroup.Serialized>
    {
        #region Serialization

        public override string AsJSON(bool prettyPrint = false) => AsSerializableStruct().AsJSON(prettyPrint);

        [Serializable]
        public struct Serialized : ISerializableContainer<ObjectSpawnGroup, ObjectSpawnGroup.Serialized>
        {

            public string type;
            public string assetStringMain;
            public string assetStringSecondary;
            public ObjectSpawner[] objectSpawns;

            private static readonly Type[] _serializedConstructorTypes = new Type[] { typeof(ObjectSpawnGroup.Serialized) };
            private static readonly object[] _serializedConstructorArguments = new object[1];
            public ObjectSpawnGroup AsOriginalType(PackageInfo packageInfo = default) 
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        Type t = Type.GetType(type);
                        if (t != null)
                        {
                            var constructor = t.GetConstructor(_serializedConstructorTypes);
                            if (constructor != null)
                            {
                                _serializedConstructorArguments[0] = this;
                                constructor.Invoke(_serializedConstructorArguments);
                            }
                        }
                    }
                }
                catch(Exception ex)
                {
                    swole.LogError($"[{nameof(ObjectSpawnGroup.Serialized)}] Encountered exception while converting serialized type to original type: '{type}'");
                    swole.LogError(ex);
                }

                return new ObjectSpawnGroup(this);
            }

            public string AsJSON(bool prettyPrint = false) => swole.Engine.ToJson(this, prettyPrint);

            public object AsNonserializableObject(PackageInfo packageInfo = default) => AsOriginalType(packageInfo);
        }

        public static implicit operator Serialized(ObjectSpawnGroup obj) => new ObjectSpawnGroup.Serialized() { type = obj.GetType().FullName, assetStringMain = obj.assetStringMain, objectSpawns = obj.objectSpawns };

        public override ObjectSpawnGroup.Serialized AsSerializableStruct() => this;

        public ObjectSpawnGroup(ObjectSpawnGroup.Serialized serializable) : base(serializable)
        {
            this.assetStringMain = serializable.assetStringMain;
            this.assetStringSecondary = serializable.assetStringSecondary;
            this.objectSpawns = serializable.objectSpawns;
        }

        #endregion

        protected string assetStringMain;
        public string AssetStringMain => assetStringMain;

        protected string assetStringSecondary;
        public string AssetStringSecondary => assetStringSecondary;

        protected ObjectSpawner[] objectSpawns;
        public int ObjectSpawnCount => objectSpawns == null ? 0 : objectSpawns.Length;
        public ObjectSpawner GetObjectSpawner(int index) => objectSpawns == null || index < 0 || index >= objectSpawns.Length ? default : GetObjectSpawnerUnsafe(index);
        public ObjectSpawner GetObjectSpawnerUnsafe(int index) => objectSpawns[index];

        /// <summary>
        /// id and index must match existing spawner
        /// </summary>
        public void ForceSetObjectSpawner(int index, ObjectSpawner spawner)
        {
            if (index < 0 || index >= ObjectSpawnCount) return;

            var current = objectSpawns[index];
            if (current.id != spawner.id || current.index != spawner.index) return;

            objectSpawns[index] = spawner;
        }

        public virtual void Spawn(EngineInternal.ITransform environmentRoot, bool useRealTransforms) => SpawnIntoList(environmentRoot, useRealTransforms, null);
        public virtual void SpawnIntoList(EngineInternal.ITransform environmentRoot, bool useRealTransforms, List<EngineInternal.ITransform> instanceOutputList)
        {
        }

    }
}
