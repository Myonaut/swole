#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    public class InteractableTrigger : MonoBehaviour
    {
        protected IInteractable owner;
        public void SetOwner(IInteractable owner) { this.owner = owner; }

        private class Presence
        {
            public bool valid;

            public int colliderId;
            public int managerId;

            public IInteractionManager manager;

            public int framesLeft;
        }
        private Dictionary<int, Presence> inTrigger = new Dictionary<int, Presence>();

        private void OnDestroy()
        {
            if (inTrigger != null)
            {
                foreach (var presence in inTrigger.Values)
                {
                    if (presence != null && presence.valid)
                    {
                        presence.valid = false;
                        presence.manager.RevokeInteractability(GetInstanceID(), owner);
                    }
                }

                inTrigger.Clear();
            }
        }

        private IEnumerator TickPresence(Presence presence)
        {
            while(presence.framesLeft > 0 && presence.manager != null)
            {
                presence.framesLeft--;
                yield return null;
            }

            inTrigger.Remove(presence.colliderId);
            inTrigger.Remove(presence.managerId);

            if (presence.manager != null)
            {
                presence.valid = false;
                presence.manager.RevokeInteractability(GetInstanceID(), owner);
            }

            Debug.Log($"REMOVED PRESENCE");
        }
        protected virtual void OnTriggerEnter(Collider collider)
        {
            int colliderId = collider.GetInstanceID();
            Debug.Log($"TRIGGERED {collider.name}");

            IInteractionManager manager;
            Presence presence;

            if (inTrigger.TryGetValue(colliderId, out presence))
            {
                manager = presence.manager;
            } 
            else
            {
                manager = collider.GetComponentInParent<IInteractionManager>();
                if (manager == null) manager = collider.GetComponentInChildren<IInteractionManager>();
            }

            if (manager == null) return;

            if (!inTrigger.TryGetValue(manager.InteractionManagerID, out presence))
            {
                presence = new Presence();
                presence.valid = true;
                presence.manager = manager;
                presence.colliderId = collider.GetInstanceID();
                presence.managerId = manager.InteractionManagerID;

                inTrigger[presence.managerId] = presence;
                inTrigger[presence.colliderId] = presence; 

                presence.manager.AllowInteractability(GetInstanceID(), owner);

                presence.framesLeft = 3;

                StartCoroutine(TickPresence(presence));
            }

            presence.framesLeft = 3;
        }
        protected virtual void OnTriggerStay(Collider collider) 
        {
            if (!inTrigger.TryGetValue(collider.GetInstanceID(), out var presence)) return;
            presence.framesLeft = 3;
        }

    }

}

#endif