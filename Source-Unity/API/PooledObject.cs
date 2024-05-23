#if (UNITY_EDITOR || UNITY_STANDALONE)

using UnityEngine;

namespace Swole.API.Unity
{

    public class PooledObject : MonoBehaviour
    {

        public IObjectPool pool;

        public void Claim() => pool?.Claim(this);
        public void Release() => pool?.Release(this);

        private bool quitting;
        protected void OnApplicationQuit() => quitting = true;
        protected void OnDestroy() { if (!quitting) pool?.Invalidate(this); }
    }

}

#endif