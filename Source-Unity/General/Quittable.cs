#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;
using UnityEngine.Events;

namespace Swole 
{

    public abstract class Quittable : MonoBehaviour
    {

        public delegate bool QuitCriteria();

        public static QuitCriteria DefaultCriteria = new QuitCriteria(() => { return InputProxy.CloseOrQuitKeyDown; });

        public static int lastQuit;

        public bool allowQuit = true;

        public virtual void Quit()
        {

            lastQuit = Time.frameCount;

            onQuit?.Invoke();

        }

        public virtual bool CanQuit
        {

            get
            {

                return (lastQuit < Time.frameCount - 5) && allowQuit && CanQuitCriteria();

            }

        }

        public virtual bool ShouldQuit
        {

            get
            {

                return CanQuit && ShouldQuitCriteria();

            }

        }

        public UnityEvent onQuit;

        protected abstract bool CanQuitCriteria();

        protected abstract bool ShouldQuitCriteria();

        public virtual void Update()
        {

            if (ShouldQuit) Quit();

        }

    }

}

#endif
