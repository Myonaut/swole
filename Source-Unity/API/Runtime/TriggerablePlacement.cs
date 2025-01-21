#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.API.Unity
{
    public class TriggerablePlacement : Triggerable
    {

        [Serializable]
        public struct Placement
        {
            public int[] targetPlaceables;
            public Transform parent;
            public Vector3 position;
            public Vector3 rotation;
            public Vector3 scale;
        }

        [Serializable]
        public struct DelayedEvent
        {
            public float delay;
            public UnityEvent PostDelay;
        }

        [Serializable]
        public class TriggerPoint
        {
            public string name;

            public Placement[] placements;

            public UnityEvent OnTriggered;

            public DelayedEvent[] delayedEvents;

            public void Trigger(Transform[] placeables)
            {
                if (placements != null && placeables != null)
                {
                    foreach (var placement in placements)
                    {
                        if (placement.targetPlaceables != null)
                        {
                            foreach (var placeableIndex in placement.targetPlaceables)
                            {
                                var placeable = placeables[placeableIndex];
                                if (placeable == null) continue;

                                placeable.SetParent(placement.parent, false);
                                if (placement.parent == null)
                                {
                                    placeable.SetPositionAndRotation(placement.position, Quaternion.Euler(placement.rotation));
                                }
                                else
                                {
                                    placeable.SetLocalPositionAndRotation(placement.position, Quaternion.Euler(placement.rotation));
                                }

                                placeable.localScale = placement.scale;
                            }
                        }
                    }
                }
                 
                OnTriggered?.Invoke();
                if (delayedEvents != null)
                {
                    foreach(var delayedEvent in delayedEvents)
                    {
                        if (delayedEvent.PostDelay != null) LeanTween.delayedCall(delayedEvent.delay, delayedEvent.PostDelay.Invoke);
                    }
                }
            }
        }

        public Transform[] placeables; 
        public TriggerPoint[] triggerPoints;

        public override void Trigger(int triggerIndex)
        {
            if (triggerIndex >= 0 && triggerPoints != null && triggerIndex < triggerPoints.Length)
            {
                triggerPoints[triggerIndex].Trigger(placeables);
            }

            base.Trigger(triggerIndex);
        }
    }
}

#endif