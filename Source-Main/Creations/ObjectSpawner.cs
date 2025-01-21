using System;

using static Swole.EngineInternal;

namespace Swole
{

    [Serializable]
    public struct ObjectSpawnOverride
    {
        public string overrideId;
        public string overrideValue;
    }

    [Serializable]
    public struct ObjectSpawner
    {

        /// <summary>
        /// Add 1 when storing. Subtract 1 when querying. Zero represents no id.
        /// </summary>
        public int id;
        public int ID => id - 1;

        public string name;

        public int index;

        public int parentSpawnerIndex;
        public int parentInstanceIndex;

        public Vector3 positionInRoot;
        public Quaternion rotationInRoot;
        public Vector3 localScale;

        public ObjectSpawnOverride[] overrides;

        public Matrix4x4 RootSpaceTRS => Matrix4x4.TRS(positionInRoot, rotationInRoot, localScale);

        public Matrix4x4 GetWorldMatrixTRS(Transform environmentRoot)
        {

            return GetWorldMatrixTRS(environmentRoot == null ? Vector3.zero : environmentRoot.position, environmentRoot == null ? Quaternion.identity : environmentRoot.rotation);

        }

        public Matrix4x4 GetWorldMatrixTRS(Vector3 rootWorldPosition, Quaternion rootWorldRotation)
        {

            return Matrix4x4.TRS(rootWorldPosition + (rootWorldRotation * positionInRoot), rootWorldRotation * rotationInRoot, localScale);

        }

        #region Tile

        public Matrix4x4 GetTileLocalMatrixTRS(EngineInternal.TileSet tileSet)
        {

            if (tileSet == null) return Matrix4x4.identity;

            var tile = tileSet[index];
            if (tile == null) return Matrix4x4.identity;

            return Matrix4x4.TRS(tile.PositionOffset, Quaternion.Euler(tile.InitialRotationEuler), tile.InitialScale);

        }

        public Matrix4x4 GetTileWorldMatrixTRS(EngineInternal.TileSet tileSet, EngineInternal.ITransform environmentRoot)
        {

            return GetTileWorldMatrixTRS(tileSet, (environmentRoot == null || environmentRoot.IsDestroyed) ? Vector3.zero : environmentRoot.position, (environmentRoot == null || environmentRoot.IsDestroyed) ? Quaternion.identity : environmentRoot.rotation);

        }

        public Matrix4x4 GetTileWorldMatrixTRS(EngineInternal.TileSet tileSet, Vector3 rootWorldPosition, Quaternion rootWorldRotation)
        {

            return GetWorldMatrixTRS(rootWorldPosition, rootWorldRotation) * GetTileLocalMatrixTRS(tileSet); 

        }
        
        public EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, EngineInternal.ITransform environmentRoot, bool useRealTransforms)
        {
            var instance = CreateNewTileInstance(tileSet, (environmentRoot == null || environmentRoot.IsDestroyed) ? Vector3.zero : environmentRoot.position, (environmentRoot == null || environmentRoot.IsDestroyed) ? Quaternion.identity : environmentRoot.rotation);
            if (environmentRoot != null && !environmentRoot.IsDestroyed) instance.SetParent(environmentRoot, true, useRealTransforms);

            return instance;
        }
        public EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, Vector3 rootWorldPosition, Quaternion rootWorldRotation) 
        { 
            var inst = swole.Engine.CreateNewTileInstance(tileSet, index, rootWorldPosition, rootWorldRotation, positionInRoot, rotationInRoot, localScale);
            if (!string.IsNullOrEmpty(name)) inst.baseGameObject.SetName(name);
            if (id > 0) swole.Engine.SetSwoleId(inst, ID);
            return inst;
        }

        #endregion

        #region Creation

        public EngineInternal.CreationInstance CreateNewCreationInstance(Creation creation, bool useRealTransformsOnly, EngineInternal.ITransform environmentRoot)
        {

            var instance = CreateNewCreationInstance(creation, useRealTransformsOnly, environmentRoot == null ? Vector3.zero : environmentRoot.position, environmentRoot == null ? Quaternion.identity : environmentRoot.rotation);

            if (instance != null && environmentRoot != null && instance.Root != null) instance.Root.transform.SetParent(environmentRoot, true); 

            return instance;
             
        }
        public EngineInternal.CreationInstance CreateNewCreationInstance(Creation creation, bool useRealTransformsOnly, Vector3 rootWorldPosition, Quaternion rootWorldRotation) 
        { 
            var inst = swole.Engine.CreateNewCreationInstance(creation, useRealTransformsOnly, rootWorldPosition, rootWorldRotation, positionInRoot, rotationInRoot, localScale);
            if (!string.IsNullOrEmpty(name)) inst.baseGameObject.SetName(name); 
            if (id > 0) swole.Engine.SetSwoleId(inst, ID);
            return inst;
        }


        #endregion
         
    }

}
