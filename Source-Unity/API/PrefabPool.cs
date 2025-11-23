#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public class PrefabPool : ObjectPoolBehaviour<GameObject>
    {

        public bool dontSetNewlyCreatedInstancesToInactiveState;

        public bool forceParentOnClaim;
        public bool forceParentOnRelease;

        public bool worldPositionStaysWhenParented;

        [SerializeField]
        protected Transform containerTransform;
        public void SetContainerTransform(Transform container, bool forceParentPooled = true, bool forceParentClaimed = false, bool worldPositionStays = true)
        {
            containerTransform = container;
            worldPositionStaysWhenParented = worldPositionStays;

            if (!IsValid) return;

            if (pool.Prototype != null) pool.Prototype.transform.SetParent(containerTransform, worldPositionStays);

            if (forceParentPooled)
            {
                foreach (var obj in pool.pooledObjects)
                {
                    if (obj == null) continue;
                    obj.transform.SetParent(containerTransform, worldPositionStays);
                }
            }
            if (forceParentClaimed)
            {
                foreach (var obj in pool.claimedObjects)
                {
                    if (obj == null) continue;
                    obj.transform.SetParent(containerTransform, worldPositionStays);
                }
            }
        }
        public Transform ContainerTransform
        {
            get => containerTransform;
            set => SetContainerTransform(value);
        }
        public bool worldPositionStays;

        protected override void OnCreateNew(GameObject obj)
        {
            if (containerTransform != null) obj.transform.SetParent(containerTransform, worldPositionStays);
            if (!dontSetNewlyCreatedInstancesToInactiveState) obj.SetActive(false);
        }
        protected override void OnClaim(GameObject obj)
        {
            if (forceParentOnClaim) obj.transform.SetParent(containerTransform, worldPositionStaysWhenParented);
            obj.SetActive(true);
        }
        protected override void OnRelease(GameObject obj)
        {
            if (forceParentOnRelease) obj.transform.SetParent(containerTransform, worldPositionStaysWhenParented); 
            obj.SetActive(false);
        }
    }
} 

#endif
