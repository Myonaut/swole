#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole
{

    /// <summary>
    /// Trigger volume that grants and revokes interactability of an owning <see cref="IInteractable"/> with
    /// any <see cref="IInteractionManager"/> that enters or stays within it, using a frame-based timeout
    /// to detect when a collider has left.
    /// </summary>
    public class InteractableTrigger : MonoBehaviour
    {
        protected IInteractable owner;
        /// <summary>Assigns the interactable that this trigger grants/revokes interactability for.</summary>
        public void SetOwner(IInteractable owner) { this.owner = owner; }

        /// <summary>Tracks an interaction manager's presence within the trigger, including its remaining lifetime in frames.</summary>
        private class Presence
        {
            public bool valid;

            public int colliderId;
            public int managerId;

            public IInteractionManager manager;

            public int framesLeft;
        }
        private Dictionary<int, Presence> inTrigger = new Dictionary<int, Presence>();

        /// <summary>Revokes interactability for all currently tracked presences when this trigger is destroyed.</summary>
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

        /// <summary>
        /// Coroutine that counts down a presence's remaining frames each frame; once it expires, removes the
        /// presence and revokes interactability with its manager.
        /// </summary>
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
        /// <summary>
        /// Called when a collider enters the trigger; resolves its <see cref="IInteractionManager"/> and grants
        /// interactability, starting (or refreshing) a presence timeout coroutine.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (collider is TerrainCollider) return;

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
        /// <summary>Called while a collider remains within the trigger; refreshes its presence timeout.</summary>
        protected virtual void OnTriggerStay(Collider collider) 
        {
            if (!inTrigger.TryGetValue(collider.GetInstanceID(), out var presence)) return;
            presence.framesLeft = 3;
        }

    }

}

#endif