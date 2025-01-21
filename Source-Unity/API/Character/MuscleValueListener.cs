#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;

using UnityEngine;

namespace Swole.API.Unity
{

    public delegate void MuscleValueListenerDelegate(MuscleGroupInfo muscleData);
    public delegate void MuscleValueChangeDelegate(string muscleGroupName, int muscleGroupIndex, MuscleGroupInfo muscleData);

    public class MuscleValueListener : IDisposable
    {

        private bool invalid;
        public bool Valid => !invalid;

        public EngineInternal.IEngineObject listeningObject;
        public MuscleValueListenerDelegate callback;

        public void Dispose()
        {
            invalid = true;
        }

        public bool DisposeIfNull()
        {

            bool dispose = listeningObject == null;

            if (dispose) Dispose();

            return dispose;

        }

    }

}

#endif
