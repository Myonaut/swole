using Swole.Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{
    public class MeshAsset : IMeshAsset
    {

        public System.Type AssetType => typeof(Mesh);
        public object Asset => mesh;
        public static implicit operator Mesh(MeshAsset asset) => asset.Mesh;

        protected bool isInternalAsset;
        public bool IsInternalAsset 
        {
            get => isInternalAsset;
            set => isInternalAsset = value;
        }

        public bool IsValid => mesh != null;
        public void Dispose()
        {
            if (!isInternalAsset && mesh != null)
            {
                GameObject.Destroy(mesh);
            }
            mesh = null;
        }

        protected Mesh mesh;
        public Mesh Mesh => mesh;
        public object Instance => Mesh;

        public string Name => throw new System.NotImplementedException();

        public string name => throw new System.NotImplementedException();

        public int InstanceID => throw new System.NotImplementedException();

        public bool IsDestroyed => throw new System.NotImplementedException();

        public bool HasEventHandler => throw new System.NotImplementedException();

        public IRuntimeEventHandler EventHandler => throw new System.NotImplementedException();

        public void AdminDestroy(float timeDelay = 0)
        {
            throw new System.NotImplementedException();
        }

        public void Destroy(float timeDelay = 0)
        {
            throw new System.NotImplementedException();
        }
    }
}
