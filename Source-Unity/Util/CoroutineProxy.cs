#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public class CoroutineProxy : SingletonBehaviour<CoroutineProxy>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        public override void OnQuit()
        {

            base.OnQuit();

            StopAllCoroutines();

        }

        public override bool DestroyOnLoad => false;

        public static Coroutine Start(IEnumerator routine)
        {

            if (IsQuitting()) return null;

            return Instance.StartCoroutine(routine);

        }

        public static void Stop(IEnumerator routine)
        {

            Instance.StopCoroutine(routine);

        }

        public static void Stop(Coroutine routine)
        {

            Instance.StopCoroutine(routine);

        }

        public static void StopAll()
        {

            Instance.StopAllCoroutines();

        }

    }

}

#endif
