#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    public class PersistentAudioPlayer : SingletonBehaviour<PersistentAudioPlayer>
    {

        public override bool DestroyOnLoad => false;
        public override bool ExecuteInStack => false;

        [SerializeField]
        protected AudioSource source;
        public AudioSource Source => source;

        protected override void OnAwake()
        {
            if (source == null) source = gameObject.AddOrGetComponent<AudioSource>();
        }

        protected override void OnInit()
        {
            OnAwake();
        }

        public override void OnUpdate()
        {
        }

        public override void OnLateUpdate()
        {
        }

        public override void OnFixedUpdate()
        {
        }
    }

}


#endif