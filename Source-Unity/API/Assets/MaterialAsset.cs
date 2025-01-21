using Swole.Script;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public class MaterialAsset : IMaterialAsset
    {

        public System.Type AssetType => typeof(Material);
        public object Asset => material;
        public static implicit operator Material(MaterialAsset asset) => asset.Material;

        protected bool isInternalAsset;
        public bool IsInternalAsset
        {
            get => isInternalAsset;
            set => isInternalAsset = value;
        }

        public bool IsValid => material != null;
        public void Dispose() 
        { 
            if (!isInternalAsset && material != null)
            {
                GameObject.Destroy(material);
            }
            material = null;
        }

        protected Material material;
        public Material Material => material;
        public object Instance => Material;

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
