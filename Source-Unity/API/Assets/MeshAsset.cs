using Swole.Script;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swole.API.Unity
{
    public class MeshAsset : IMeshAsset
    {

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
