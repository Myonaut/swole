#if (UNITY_EDITOR || UNITY_STANDALONE)

#define FOUND_UNITY
using UnityEngine;
using Swole.API.Unity;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

using Swole.Animation;
using Swole.Script;
using Swole.API.Unity.Animation;

namespace Swole
{

    public class BulkOutIntermediaryHook : UnityEngineHook
    {

        public override string Name => "BulkOutLite+Unity";

#if FOUND_UNITY

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Initialize()
        {
            if (!(typeof(BulkOutIntermediaryHook).IsAssignableFrom(swole.Engine.GetType())))
            {
                activeHook = new BulkOutIntermediaryHook();
                swole.SetEngine(activeHook);
            }
        }

        #region Conversions | Swole -> Bulk Out!

        public static Tile AsBulkOutTile(ITile tile)
        {
            if (tile.Instance is Tile boTile) return boTile;
            return null;
        }

        public static TileSet AsBulkOutTileSet(ITileSet tileSet)
        {
            if (tileSet.Instance is TileSet boTileSet) return boTileSet;
            return null;
        }

        public static TileInstance AsBulkOutTileInstance(EngineInternal.TileInstance tileInstance)
        {
            if (tileInstance.instance is TileInstance boTileInstance) return boTileInstance;
            return null;
        }

        public static CreationBehaviour AsBulkOutCreationInstance(EngineInternal.CreationInstance creationInstance)
        {
            if (creationInstance.instance is CreationBehaviour boCreationInstance) return boCreationInstance;
            return null;
        }

        #endregion

        #region Conversions | Bulk Out! -> Swole

        public static EngineInternal.Tile AsSwoleTile(Tile tile)
        {
            if (tile == null) return default;
            return new EngineInternal.Tile(tile); 
        }

        public static EngineInternal.TileSet AsSwoleTileSet(TileSet tileSet)
        {
            if (tileSet == null) return default;
            return new EngineInternal.TileSet(tileSet);
        }

        public static EngineInternal.TileInstance AsSwoleTileInstance(TileInstance tileInstance)
        {
            if (tileInstance == null) return default;
            return new EngineInternal.TileInstance(tileInstance);
        }

        public static EngineInternal.CreationInstance AsSwoleCreationInstance(CreationBehaviour creationInstance)
        {
            if (creationInstance == null) return default;
            return new EngineInternal.CreationInstance(creationInstance); 
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
            return AsSwoleTile((Tile)tileSet[tileIndex]);
        }
        public override EngineInternal.TileSet GetTileSet(string tileSetId, string tileCollectionId = null, bool caseSensitive = false) => AsSwoleTileSet(ResourceLib.FindTileSet(tileSetId, tileCollectionId, caseSensitive));

        public override EngineInternal.TileInstance CreateNewTileInstance(EngineInternal.TileSet tileSet, int tileIndex, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 localScale)
        {
            var tile = tileSet[tileIndex];

            var boTileSet = AsBulkOutTileSet(tileSet);
            var boTile = AsBulkOutTile(tile);

            if (boTileSet == null || boTile == null) return default;

            Transform tileTransform = null;
            if (!boTile.RenderOnly)
            {
                var tileGO = boTileSet.CreateNewTileInstance(tileIndex);
                tileTransform = tileGO == null ? null : tileGO.transform;
            }

            TileInstance tileInstance = new TileInstance(boTileSet, tileIndex, tileTransform);
            tileInstance.SetPositionAndRotation(rootWorldPosition + (rootWorldRotation * positionInRoot), rootWorldRotation * rotationInRoot);
            tileInstance.localScale = localScale;

            tileInstance.baseRenderingMatrix = Matrix4x4.TRS(boTile.positionOffset, Quaternion.Euler(boTile.initialRotationEuler), boTile.initialScale);
            tileInstance.ReevaluateRendering();

            return AsSwoleTileInstance(tileInstance);
        }
         
        public override EngineInternal.CreationInstance CreateNewCreationInstance(Creation creation, bool useRealTransformsOnly, EngineInternal.Vector3 rootWorldPosition, EngineInternal.Quaternion rootWorldRotation, EngineInternal.Vector3 positionInRoot, EngineInternal.Quaternion rotationInRoot, EngineInternal.Vector3 localScale, bool autoInitialize = true, SwoleLogger logger = null)
        {
            if (creation == null) return default;
            
            CreationBehaviour instance = CreationBehaviour.New(creation, useRealTransformsOnly, AsUnityVector(rootWorldPosition + (rootWorldRotation * positionInRoot)), AsUnityQuaternion(rootWorldRotation * rotationInRoot), null, autoInitialize, 0, logger == null ? swole.DefaultLogger : logger);
            if (instance != null)
            {
                var transform = instance.Root.transform;
                transform.localScale = localScale;
                return AsSwoleCreationInstance(instance);
            }

            return default;
        }

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
