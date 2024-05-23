using System;

namespace Swole.Script
{

    [Serializable]
    public enum ExecutionLayer
    {

        Load, Unload, Begin, End, Restart, SaveProgress, LoadProgress, Initialization, EarlyUpdate, Update, LateUpdate, FixedUpdate, Enable, Disable, Destroy, CollisionEnter, CollisionStay, CollisionExit, TriggerEnter, TriggerStay, TriggerExit, Interaction

    }

}
