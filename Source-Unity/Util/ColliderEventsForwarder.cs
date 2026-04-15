using UnityEngine;

namespace Swole
{
    public class ColliderEventBroadcaster : MonoBehaviour
    {

        public GameObject target;

        void OnCollisionEnter(Collision collision)
        {
            if (target != null) target.SendMessage("OnColliderEnter", collision, SendMessageOptions.DontRequireReceiver);
        }

        void OnCollisionStay(Collision collision)
        {
            if (target != null) target.SendMessage("OnColliderStay", collision, SendMessageOptions.DontRequireReceiver); 
        }

        void OnCollisionExit(Collision collision)
        {
            if (target != null) target.SendMessage("OnColliderExit", collision, SendMessageOptions.DontRequireReceiver);
        }
    }
}
