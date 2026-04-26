#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Swole.Morphing;

namespace Swole.API.Unity
{
    public class TriggerablePlacement : Triggerable
    {

        [Serializable]
        public struct Placement
        {
            public float delay;
            public int[] targetPlaceables;
            public Transform parent;
            public Vector3 position;
            public Vector3 randomPositionOffsetMin;
            public Vector3 randomPositionOffsetMax;
            public Vector3 rotation;
            public Vector3 randomRotationOffsetMin;
            public Vector3 randomRotationOffsetMax;
            public Vector3 scale;
            public Vector3 randomScaleOffsetMin;
            public Vector3 randomScaleOffsetMax;

            public CustomizableCharacterMeshPointTracker pointTracker;
            public string trackerName;
            public string trackedPointName;
            [NonSerialized]
            public int trackerIndex;
            [NonSerialized]
            public int trackedPointIndex;

            public UnityEvent OnTriggered;

            public void Trigger(Transform[] placeables)
            {
                if (targetPlaceables != null)
                {
                    Vector3 pos = position;
                    if (randomPositionOffsetMin != randomPositionOffsetMax)
                    {
                        pos = pos + new Vector3(
                            Mathf.LerpUnclamped(randomPositionOffsetMin.x, randomPositionOffsetMax.x, UnityEngine.Random.value),
                            Mathf.LerpUnclamped(randomPositionOffsetMin.y, randomPositionOffsetMax.y, UnityEngine.Random.value),
                            Mathf.LerpUnclamped(randomPositionOffsetMin.z, randomPositionOffsetMax.z, UnityEngine.Random.value));
                    }

                    Quaternion rot = Quaternion.Euler(rotation);
                    if (randomRotationOffsetMin != randomRotationOffsetMax)
                    {
                        rot = rot * Quaternion.Euler(new Vector3(
                            Mathf.LerpUnclamped(randomRotationOffsetMin.x, randomRotationOffsetMax.x, UnityEngine.Random.value),
                            Mathf.LerpUnclamped(randomRotationOffsetMin.y, randomRotationOffsetMax.y, UnityEngine.Random.value),
                            Mathf.LerpUnclamped(randomRotationOffsetMin.z, randomRotationOffsetMax.z, UnityEngine.Random.value)));
                    }

                    Vector3 scale_ = scale;
                    if (randomScaleOffsetMin != randomScaleOffsetMax)
                    {
                        scale_ = scale_ + new Vector3(
                            Mathf.LerpUnclamped(randomScaleOffsetMin.x, randomScaleOffsetMax.x, UnityEngine.Random.value),
                            Mathf.LerpUnclamped(randomScaleOffsetMin.y, randomScaleOffsetMax.y, UnityEngine.Random.value),
                            Mathf.LerpUnclamped(randomScaleOffsetMin.z, randomScaleOffsetMax.z, UnityEngine.Random.value));
                    }

                    if (pointTracker != null && trackerIndex >= 0 && trackedPointIndex >= 0)
                    {
                        var tracker = pointTracker.GetTrackerUnsafe(trackerIndex);
                        tracker.UpdatePointIfNeededUnsafe(trackedPointIndex);

                        var baseRot = pointTracker.GetPointRotationOffsetUnsafe(trackerIndex, trackedPointIndex);
                        pos = pointTracker.GetPointPositionUnsafe(trackerIndex, trackedPointIndex) + (baseRot * pos);
                        rot = baseRot * rot;
                    }
                    else
                    {
                        if (parent != null)
                        {
                            pos = parent.TransformPoint(pos);
                            rot = parent.rotation * rot;
                        }
                    }

                    foreach (var placeableIndex in targetPlaceables)
                    {
                        var placeable = placeables[placeableIndex];
                        if (placeable == null) continue;

                        placeable.SetParent(parent, false);
                        placeable.SetPositionAndRotation(pos, rot);

                        placeable.localScale = scale_;
                    }
                }

                OnTriggered?.Invoke();
            }
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

            public bool randomPlacements;
            public int randomPlacementCount;
            public Placement[] placements;

            public UnityEvent OnTriggered;

            public DelayedEvent[] delayedEvents;

            public void Init()
            {
                if (placements != null)
                {
                    for(int a = 0; a < placements.Length; a++)
                    {
                        Placement placement = placements[a];

                        placement.trackerIndex = -1;
                        placement.trackedPointIndex = -1;
                        if (placement.pointTracker !=  null)
                        {
                            if (placement.pointTracker.TryGetTracker(placement.trackerName, out var tracker))
                            {
                                placement.trackerIndex = tracker.Index;
                                if (tracker.TryGetPoint(placement.trackedPointName, out var point))
                                {
                                    placement.trackedPointIndex = point.IndexInTracker;
                                }
                            }
                        }

                        placements[a] = placement;
                    }
                }
            }

            private static readonly HashSet<int> tempIndices = new HashSet<int>();
            private IEnumerator TriggerDelayedPlacement(Placement placement, Transform[] placeables)
            {
                yield return new WaitForSeconds(placement.delay);
                placement.Trigger(placeables);
            }
            public void Trigger(Transform[] placeables)
            {
                if (placements != null && placeables != null)
                {
                    if (randomPlacements)
                    {
                        tempIndices.Clear();
                        for (int i = 0; i < randomPlacementCount; i++)
                        {
                            if (tempIndices.Count >= placements.Length) break;

                            int j = UnityEngine.Random.Range(0, placements.Length);
                            while(tempIndices.Contains(j)) j = UnityEngine.Random.Range(0, placements.Length); 

                            tempIndices.Add(j);
                            var placement = placements[j];
                            if (placement.delay > 0f)
                            {
                                CoroutineProxy.Start(TriggerDelayedPlacement(placement, placeables));
                            }
                            else
                            {
                                placement.Trigger(placeables);
                            }
                        }
                    }
                    else
                    {
                        foreach (var placement in placements)
                        {
                            placement.Trigger(placeables); 
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

        protected virtual void Start()
        {
            if (triggerPoints != null)
            {
                foreach (var tp in triggerPoints) tp.Init();
            }
        }

        public int IndexOfTriggerPoint(string triggerPointName)
        {
            if (triggerPoints != null)
            {
                for (int i = 0; i < triggerPoints.Length; i++)
                {
                    if (triggerPoints[i].name == triggerPointName) return i;
                }
            }
            return -1;
        }

        public override void Trigger(int triggerIndex)
        {
            if (triggerIndex >= 0 && triggerPoints != null && triggerIndex < triggerPoints.Length)
            {
                triggerPoints[triggerIndex].Trigger(placeables);
            }

            base.Trigger(triggerIndex);
        }

        public void TriggerByName(string triggerPointName)
        {
            int index = IndexOfTriggerPoint(triggerPointName); 
            if (index >= 0) Trigger(index);
        }
    }
}

#endif