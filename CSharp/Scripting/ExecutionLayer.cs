using System;

namespace Swolescript
{

    [Serializable]
    public enum ExecutionLayer
    {

        Initialization, EarlyUpdate, Update, LateUpdate, FixedUpdate, Enable, Disable, Destroy, CollisionEnter, CollisionStay, CollisionExit, TriggerEnter, TriggerStay, TriggerExit, Interaction

    }

}
