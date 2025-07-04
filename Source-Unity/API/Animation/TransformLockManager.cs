#if (UNITY_EDITOR || UNITY_STANDALONE)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole
{
    public class TransformLockManager : MonoBehaviour
    {

        [Serializable]
        public class Lockable
        {
            public string name;
            public Transform transformToLock;
            public Transform transformToMove;
            public bool toMoveChildrenOnly;
            public Transform transformToRotate;
            public bool toRotateChildrenOnly;

            public bool preserveChildTransforms;

            [AnimatableProperty(true, 0f)]
            public bool locked;
            private bool prevLocked;

            public bool lockPosition = true;
            [NonSerialized]
            public Vector3 lockedWorldPosition;

            public bool lockRotation;
            [NonSerialized]
            public Quaternion lockedWorldRotation;

            public void Update()
            {
                if (locked != prevLocked)
                {
                    prevLocked = locked;
                    if (locked && transformToLock != null) 
                    {
                        lockedWorldPosition = transformToLock.position;
                        lockedWorldRotation = transformToLock.rotation;
                        transformToLock.GetPositionAndRotation(out lockedWorldPosition, out lockedWorldRotation);
                    }
                }
                else if (locked)
                {
                    MoveBack();
                }
            }

            private static readonly List<TransformState> tempStates = new List<TransformState>();
            public void MoveBack()
            {
                if (!locked || transformToLock == null) return;

                var toMove = transformToMove == null ? transformToLock : transformToMove;
                var toRotate = transformToRotate == null ? toMove : transformToRotate;
                if (preserveChildTransforms)
                {
                    tempStates.Clear();
                    for(int a = 0; a < toMove.childCount; a++)
                    {
                        tempStates.Add(new TransformState(toMove.GetChild(a)));
                    }
                }

                if (lockPosition)
                {
                    Vector3 offset = lockedWorldPosition - transformToLock.position;
                    if (toMoveChildrenOnly)
                    { 
                        for (int a = 0; a < toMove.childCount; a++)
                        {
                            var child = toMove.GetChild(a);
                            child.position = child.position + offset;
                        }
                    } 
                    else
                    {
                        toMove.position = toMove.position + offset;
                    }
                }
                if (lockRotation)
                {
                    Quaternion rotOffset = lockedWorldRotation * Quaternion.Inverse(transformToLock.rotation); 
                    if (toRotateChildrenOnly)
                    {
                        for (int a = 0; a < toRotate.childCount; a++)
                        {
                            var child = toRotate.GetChild(a);
                            child.rotation = rotOffset * child.rotation;  
                        }
                    }
                    else
                    {
                        toRotate.rotation = rotOffset * toRotate.rotation;
                    }
                    
                }

                if (preserveChildTransforms)
                {
                    for (int a = 0; a < toMove.childCount; a++)
                    {
                        tempStates[a].ApplyWorld(toMove.GetChild(a));
                    }

                    if (transformToMove != null)
                    {

                    }
                }
            }
        }

        public List<Lockable> lockables = new List<Lockable>();

        public int IndexOf(string lockName)
        {
            if (lockables == null) return -1;

            for (int a = 0; a < lockables.Count; a++)
            {
                if (lockables[a].name == lockName)
                {
                    return a;
                }
            }

            return -1;
        }
        public Lockable GetLockable(int index)
        {
            if (lockables == null || index < 0 || index >= lockables.Count) return null;
            return lockables[index];
        }
        public Lockable GetLockableUnsafe(int index) => lockables[index];
        public int LockCount => lockables != null ? lockables.Count : 0;

        public void UpdateStep()
        {
        }

        public void FixedUpdateStep()
        {
        }

        public void LateUpdateStep()
        {
            if (lockables != null)
            {
                for (int a = 0; a < lockables.Count; a++)
                {
                    lockables[a].Update(); 
                }
            }
        }

        protected void OnEnable()
        {
            TransformLockManagerUpdater.Register(this);
        }
        protected void OnDisable()
        {
            TransformLockManagerUpdater.Unregister(this);
        }

    }

    public class TransformLockManagerUpdater : SingletonBehaviour<TransformLockManagerUpdater>
    {

        public override int Priority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 50;

        private readonly List<TransformLockManager> managers = new List<TransformLockManager>();

        public void RegisterLocal(TransformLockManager manager)
        {
            if (manager == null || managers.Contains(manager)) return;
            managers.Add(manager);
        }
        public static void Register(TransformLockManager manager)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.RegisterLocal(manager);
        }

        public void UnregisterLocal(TransformLockManager manager)
        {
            managers.Remove(manager);
        }
        public static void Unregister(TransformLockManager manager)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterLocal(manager);
        }

        public override void OnFixedUpdate()
        {
            /*bool purgeNullManagers = false;
            for(int a = 0; a < managers.Count; a++)
            {
                var manager = managers[a];
                if (manager == null)
                {
                    purgeNullManagers = true;
                    continue;
                }

                manager.FixedUpdateStep();
            }

            if (purgeNullManagers) managers.RemoveAll(m => m == null);*/
        }

        public override void OnLateUpdate()
        {
            bool purgeNullManagers = false;
            for (int a = 0; a < managers.Count; a++)
            {
                var manager = managers[a];
                if (manager == null)
                {
                    purgeNullManagers = true;
                    continue;
                }

                manager.LateUpdateStep();
            }

            if (purgeNullManagers) managers.RemoveAll(m => m == null);
        }

        public override void OnUpdate()
        {
            /*bool purgeNullManagers = false;
            for (int a = 0; a < managers.Count; a++)
            {
                var manager = managers[a];
                if (manager == null)
                {
                    purgeNullManagers = true;
                    continue;
                }

                manager.UpdateStep();
            }

            if (purgeNullManagers) managers.RemoveAll(m => m == null);*/
        }
    }
}

#endif