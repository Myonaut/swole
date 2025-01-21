#if (UNITY_STANDALONE || UNITY_EDITOR)

using Swole.Script;
using System;
using UnityEngine;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used to place a tile in an editing environment
    /// </summary>
    public class TilePrototype : MonoBehaviour, ITileInstance
    {

        public TileSet tileSet;

        public int tileIndex;

        public int SwoleId 
        { 
            get 
            {
                var sgo = gameObject.GetComponent<SwoleGameObject>();
                if (sgo == null) return -1;
                return sgo.id;
            } 
            set { } 
        }

        public string TileSetId => tileSet == null ? null : tileSet.ID;

        public int TileIndex => tileIndex; 

        public bool IsRenderOnly => false;

        public bool visible 
        { 
            get => gameObject.activeInHierarchy;
            set => gameObject.SetActive(value); 
        }

        #region ITileInstance

        public EngineInternal.GameObject Root => baseGameObject;

        public string ID => $"tile_prototype[{GetInstanceID()}]";

        public EngineInternal.TransformEventHandler TransformEventHandler => null;

        public int LastParent { get => -1; set { } }
        public EngineInternal.Vector3 LastPosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.Quaternion LastRotation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.Vector3 LastScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.ITransform parent { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.Vector3 position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.Quaternion rotation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public EngineInternal.Vector3 lossyScale => throw new NotImplementedException();

        public EngineInternal.Vector3 localPosition { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.Quaternion localRotation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public EngineInternal.Vector3 localScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public EngineInternal.Matrix4x4 worldToLocalMatrix => throw new NotImplementedException();

        public EngineInternal.Matrix4x4 localToWorldMatrix => throw new NotImplementedException();

        public int childCount => transform.childCount;

        public EngineInternal.GameObject baseGameObject => UnityEngineHook.AsSwoleGameObject(gameObject);

        public object Instance => this;

        public int InstanceID => GetInstanceID();

        public bool IsDestroyed => this == null;

        public bool HasEventHandler => false;

        public IRuntimeEventHandler EventHandler => null;

        public Type EngineComponentType => GetType();

        public void AdminDestroy(float timeDelay = 0)
        {
            throw new NotImplementedException();
        }

        public void Destroy(float timeDelay = 0)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public EngineInternal.ITransform Find(string n)
        {
            throw new NotImplementedException();
        }

        public void ForceUseRealTransform()
        {
            throw new NotImplementedException();
        }

        public EngineInternal.ITransform GetChild(int index)
        {
            throw new NotImplementedException();
        }

        public void GetLocalPositionAndRotation(out EngineInternal.Vector3 localPosition, out EngineInternal.Quaternion localRotation)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.ITransform GetParent()
        {
            throw new NotImplementedException();
        }

        public void GetPositionAndRotation(out EngineInternal.Vector3 position, out EngineInternal.Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 InverseTransformDirection(EngineInternal.Vector3 direction)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 InverseTransformDirection(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 InverseTransformPoint(EngineInternal.Vector3 position)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 InverseTransformPoint(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 InverseTransformVector(EngineInternal.Vector3 vector)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 InverseTransformVector(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public bool IsChildOf(EngineInternal.ITransform parent)
        {
            throw new NotImplementedException();
        }

        public void ReevaluateRendering()
        {
            throw new NotImplementedException();
        }

        public void SetLocalPositionAndRotation(EngineInternal.Vector3 localPosition, EngineInternal.Quaternion localRotation)
        {
            throw new NotImplementedException();
        }

        public void SetParent(EngineInternal.ITransform newParent, bool worldPositionStays, bool forceRealTransformConversion)
        {
            throw new NotImplementedException();
        }

        public void SetParent(EngineInternal.ITransform p)
        {
            throw new NotImplementedException();
        }

        public void SetParent(EngineInternal.ITransform parent, bool worldPositionStays)
        {
            throw new NotImplementedException();
        }

        public void SetPositionAndRotation(EngineInternal.Vector3 position, EngineInternal.Quaternion rotation)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 TransformDirection(EngineInternal.Vector3 direction)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 TransformDirection(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 TransformPoint(EngineInternal.Vector3 position)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 TransformPoint(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 TransformVector(EngineInternal.Vector3 vector)
        {
            throw new NotImplementedException();
        }

        public EngineInternal.Vector3 TransformVector(float x, float y, float z)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}

#endif
