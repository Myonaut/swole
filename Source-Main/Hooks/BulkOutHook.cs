#if (UNITY_EDITOR || UNITY_STANDALONE) && BULKOUT_ENV
#define FOUND_BULKOUT
using UnityEngine;
#endif

using System;
using System.Collections;
using System.Collections.Generic;

namespace Swole
{

    public class BulkOutHook : BulkOutIntermediaryHook
    {

        public override string Name => "BulkOut+Unity";

#if FOUND_BULKOUT

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Initialize()
        {
            if (!(typeof(BulkOutHook).IsAssignableFrom(Swole.Engine.GetType()))) 
            {
                activeHook = new BulkOutHook();
                Swole.SetEngine(activeHook); 
            }
        }

        #region Conversions | Swole -> Bulk Out!

        #endregion

        #region Conversions | Bulk Out! -> Swole

        #endregion

#else
        public override bool HookWasSuccessful => false;
#endif

    }

}
