using System;

using static Swolescript.EngineInternal;

namespace Swolescript
{

    [Serializable]
    public struct TileSpawner
    {

        public int tileIndex;

        public Vector3 positionInRoot;
        public Quaternion rotationInRoot;
        public Vector3 scaleInRoot;

        public Matrix4x4 EnvironmentMatrixTRS => Matrix4x4.TRS(positionInRoot, rotationInRoot, scaleInRoot);

        public Matrix4x4 GetWorldMatrixTRS(Transform environmentRoot)
        {

            return GetWorldMatrixTRS(environmentRoot == null ? Vector3.zero : environmentRoot.position, environmentRoot == null ? Quaternion.identity : environmentRoot.rotation);

        }

        public Matrix4x4 GetWorldMatrixTRS(Vector3 rootWorldPosition, Quaternion rootWorldRotation)
        {

            return Matrix4x4.TRS(rootWorldPosition + (rootWorldRotation * positionInRoot), rootWorldRotation * rotationInRoot, scaleInRoot);

        }

        public Matrix4x4 GetTileLocalMatrixTRS(EngineInternal.TileSet tileSet)
        {

            if (tileSet == null) return Matrix4x4.identity;

            var tile = tileSet[tileIndex];
            if (tile == null) return Matrix4x4.identity;

            return Matrix4x4.TRS(tile.positionOffset, Quaternion.Euler(tile.initialRotationEuler), tile.initialScale);

        }

        public Matrix4x4 GetTileWorldMatrixTRS(EngineInternal.TileSet tileSet, Transform environmentRoot)
        {

            return GetTileWorldMatrixTRS(tileSet, environmentRoot == null ? Vector3.zero : environmentRoot.position, environmentRoot == null ? Quaternion.identity : environmentRoot.rotation);

        }

        public Matrix4x4 GetTileWorldMatrixTRS(EngineInternal.TileSet tileSet, Vector3 rootWorldPosition, Quaternion rootWorldRotation)
        {

            return GetWorldMatrixTRS(rootWorldPosition, rootWorldRotation) * GetTileLocalMatrixTRS(tileSet);

        }
        
        public EngineInternal.TileInstance CreateNewInstance(EngineInternal.TileSet tileSet, Transform environmentRoot)
        {

            var instance = CreateNewInstance(tileSet, environmentRoot == null ? Vector3.zero : environmentRoot.position, environmentRoot == null ? Quaternion.identity : environmentRoot.rotation);

            if (instance != null && environmentRoot != null && instance.rootInstance != null) instance.rootInstance.transform.SetParent(environmentRoot, true);

            return instance;

        }

        public EngineInternal.TileInstance CreateNewInstance(EngineInternal.TileSet tileSet, Vector3 rootWorldPosition, Quaternion rootWorldRotation) => SwoleScript.Engine.CreateNewTileInstance(tileSet, tileIndex, rootWorldPosition, rootWorldRotation, positionInRoot, rotationInRoot, scaleInRoot);
        
    }

}
