#if (UNITY_EDITOR || UNITY_STANDALONE) && BULKOUT_ENV
#define FOUND_BULKOUT
using UnityEngine;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

namespace Swolescript
{

    public class BulkOutHook : UnityEngineHook
    {

        public override string Name => "BulkOut+Unity";

#if FOUND_BULKOUT

        #region Conversions | Swole -> Bulk Out!

        public static Tile AsBulkOutTile(EngineInternal.Tile tile)
        {
            if (tile.instance is Tile boTile) return boTile;
            return null;
        }

        public static TileSet AsBulkOutTileSet(EngineInternal.TileSet tileSet)
        {
            if (tileSet.instance is TileSet boTileSet) return boTileSet;
            return null;
        }

        public static TileInstance AsBulkOutTileInstance(EngineInternal.TileInstance tileInstance)
        {
            if (tileInstance.instance is TileInstance boTileInstance) return boTileInstance;
            return null;
        }

        #endregion

        #region Conversions | Bulk Out! -> Swole

        public static EngineInternal.Tile AsSwoleTile(Tile tile)
        {
            if (tile == null) return default;
            return new EngineInternal.Tile(tile, tile.name, tile.isDynamic, AsSwoleVector(tile.positionOffset), AsSwoleVector(tile.initialRotationEuler), AsSwoleVector(tile.initialScale));
        }

        public static EngineInternal.TileSet AsSwoleTileSet(TileSet tileSet)
        {
            if (tileSet == null) return default;
            return new EngineInternal.TileSet(tileSet, tileSet.name, tileSet.mesh == null ? "" : tileSet.mesh.name, tileSet.material == null ? "" : tileSet.material.name);
        }

        public static EngineInternal.TileInstance AsSwoleTileInstance(TileInstance tileInstance)
        {
            if (tileInstance == null) return default;
            return new EngineInternal.TileInstance(tileInstance, tileInstance.tileSetName, tileInstance.tileIndex, AsSwoleGameObject(tileInstance.rootInstance));
        }

        #endregion

        public static Tile AsTile(object boObject)
        {
            if (boObject is Tile boTile)
            {
                return boTile;
            }
            return null;
        }

        public static TileSet AsTileSet(object boObject)
        {
            if (boObject is TileSet boTileSet)
            {
                return boTileSet;
            }
            return null;
        }

        public static TileInstance AsTileInstance(object boObject)
        {
            if (boObject is TileInstance boTileInstance)
            {
                return boTileInstance;
            }
            return null;
        }

        public override int GetTileCount(object boObject) 
        {
            var tileSet = AsTileSet(boObject);
            if (tileSet == null) return 0;
            return tileSet.TileCount;
        }
        public override EngineInternal.Tile GetTileFromSet(object boObject, int tileIndex) 
        {
            var tileSet = AsTileSet(boObject);
            if (tileSet == null) return default;
            return AsSwoleTile(tileSet[tileIndex]);
        }

        public override EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, int tileIndex, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 scaleInRoot)
        {

            var tile = tileSet[tileIndex];

            var boTileSet = AsBulkOutTileSet(tileSet);
            var boTile = AsBulkOutTile(tile);

            if (boTileSet == null || boTile == null) return default;

            TileInstance tileInstance = new TileInstance(boTileSet.name, tileIndex);

            if (!boTile.RenderOnly)
            {

                tileInstance.rootInstance = boTileSet.CreateNewTileInstance(tileIndex);

                if (tileInstance.rootInstance != null)
                {

                    Transform tileTransform = tileInstance.rootInstance.transform;

                    tileTransform.position = AsUnityVector(rootWorldPosition + (rootWorldRotation * positionInRoot));
                    tileTransform.rotation = AsUnityQuaternion(rootWorldRotation * rotationInRoot);
                    tileTransform.localScale = AsUnityVector(scaleInRoot);

                    GameObject tileProxy = new GameObject("tile");
                    Transform tileProxyTransform = tileProxy.transform;

                    tileProxyTransform.SetParent(tileTransform, false);
                    tileProxyTransform.localPosition = boTile.positionOffset;
                    tileProxyTransform.localRotation = Quaternion.Euler(boTile.initialRotationEuler);
                    tileProxyTransform.localScale = boTile.initialScale;

                }

            }

            return AsSwoleTileInstance(tileInstance);

        }

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
