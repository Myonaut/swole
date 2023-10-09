#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public class TileEnvironment : SingletonBehaviour<TileEnvironment>
    {

        protected List<TileInstance> tileInstances = new List<TileInstance>();

        public override void OnUpdate() { }

        public override void OnLateUpdate() { }

        public override void OnFixedUpdate() { }

    }

}

#endif