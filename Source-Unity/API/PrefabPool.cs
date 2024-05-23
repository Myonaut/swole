#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    public class PrefabPool : ObjectPool<GameObject>
    {

        public bool dontSetNewlyCreatedInstancesToInactiveState;

        [SerializeField]
        protected Transform containerTransform;
        public void SetContainerTransform(Transform container, bool forceParentPooled = true, bool forceParentClaimed = false, bool worldPositionStays = true)
        {
            containerTransform = container;

            if (!IsValid) return;

            if (forceParentPooled)
            {
                foreach (var obj in pooledObjects)
                {
                    if (obj == null) continue;
                    obj.transform.SetParent(containerTransform, worldPositionStays);
                }
            }
            if (forceParentClaimed)
            {
                foreach (var obj in claimedObjects)
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
            obj.SetActive(true);
        }
        protected override void OnRelease(GameObject obj)
        {
            obj.SetActive(false);
        }
    }
} 

#endif
