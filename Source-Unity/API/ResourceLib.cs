#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Swole.API.Unity
{

    public class ResourceLib : SingletonBehaviour<ResourceLib>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public InternalResources[] resourcesInternal;

        public override bool DestroyOnLoad => false;

        protected override void OnInit()
        {

            base.OnInit();

            SceneManager.activeSceneChanged += OnSceneChange;

            resourcesInternal = Resources.LoadAll<InternalResources>("");

        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            SceneManager.activeSceneChanged -= OnSceneChange;

        }

        protected void OnSceneChange(Scene oldScene, Scene newScene)
        {

            Resources.UnloadUnusedAssets();

        }

        public static TileCollection FindTileCollection(string id)
        {

            var instance = Instance;
            if (instance == null) return null;

            if (instance.resourcesInternal != null)
            {
                foreach (var ri in instance.resourcesInternal)
                {
                    var collection = ri.LoadTileCollection(id);
                    if (collection != null) return collection;
                }
            }

            //if (instance.resourcesExternal != null) // Add support for loading user generated tiles
            {
                /*
                foreach (var re in instance.resourcesExternal)
                {
                    var collection = re.LoadTileCollection(id);
                    if (collection != null) return collection;
                }
                */
            }

            return null;

        }

    }

}

#endif