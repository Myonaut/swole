#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole.API.Unity
{
    [ExecuteAlways]
    public class ObjectPlacementArray : MonoBehaviour
    {
        public bool generate;
        public bool autoGenerateOnEdit;
        public bool revertTargetObjectOnGenerate = true;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public GameObject targetObject;

        public bool startAtTargetObjectPosition = true;
        public bool parentToThis = false;
        public bool rotateToNextNode = false;

        public int cloneCount;
        public Vector3 offsetDirection = Vector3.forward;
        public float offsetDistance = 1f;
        public Vector3 offsetRotationEuler;

        public Vector3 offsetRotationPivot;
        public Vector3 lookRotationPivot;

        [Serializable]
        public struct MaskTarget
        {
            public string transformName;
            public bool maskChildren;
        }
        public static void EvaluateOffsetCurve(out Vector3 endOffset, out Vector3 endRotation, AnimationCurve curve, Vector3 offset, Vector3 rotationOffset, float t, float spacing = 0f, float tStartOffset = 0f)
        {
            if (spacing > 0f)
            {
                if (curve != null && curve.length > 1)
                {
                    t = t - tStartOffset;
                    float t1 = Mathf.Floor(t / spacing);
                    float t2 = ((t1 + 1) * spacing) + tStartOffset;
                    t1 = (t1 * spacing) + tStartOffset;
                    t = t + tStartOffset;
                    t = (t - t1) / spacing;

                    t1 = curve.Evaluate(t1);
                    t2 = curve.Evaluate(t2);
                    Vector3 v1 = t1 * offset;
                    Vector3 v2 = t2 * offset;
                    endOffset = Vector3.LerpUnclamped(v1, v2, t);
                    Vector3 r1 = t1 * rotationOffset;
                    Vector3 r2 = t2 * rotationOffset;
                    endRotation = Vector3.LerpUnclamped(r1, r2, t);
                    return;
                }
            }

            if (curve != null && curve.length > 1)
            {
                t = curve.Evaluate(t);
            }

            endOffset = t * offset;
            endRotation = t * rotationOffset;
        }
        [Serializable]
        public struct AdditionalOffset
        {
            public Vector3 finalOffset;
            public AnimationCurve offsetCurve;
            public float offsetCurveSpacing;
            public float offsetCurveStart;

            public bool affectsGizmos;
            public bool invertMask;
            public List<MaskTarget> transformMask;

            public bool IsMasked(Transform t)
            {
                if (transformMask == null) return false;

                foreach(var m in transformMask)
                {
                    if (m.transformName == t.name) return true;

                    if (m.maskChildren)
                    {
                        var parent = t.parent;
                        while(parent != null)
                        {
                            if (m.transformName == parent.name) return true; 

                            parent = parent.parent;
                        }
                    }
                }

                return false;
            }

            public Vector3 Evaluate(float t) 
            { 
                EvaluateOffsetCurve(out Vector3 endOffset, out Vector3 _, offsetCurve, finalOffset, Vector3.zero, t, offsetCurveSpacing, offsetCurveStart);
                return endOffset;
            }
        }

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public List<AdditionalOffset> additionalOffsets;

        [HideInInspector]
        public int cloneCountPrev;
        [HideInInspector]
        public Vector3 offsetDirectionPrev;
        [HideInInspector]
        public float offsetDistancePrev;

        public bool drawMarkers;
        public Vector3 upAxis = Vector3.up;
        public float markerWidth = 1f;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public AnimationCurve offsetCurve;
        public float offsetCurveSpacing = 0f;
        public float offsetCurveStart = 0f;

        public bool disconnectClones;
        public void DisconnectClones()
        {
            if (clones != null) clones.Clear();
        }

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public List<GameObject> clones = new List<GameObject>();

        [HideInInspector]
        public Vector3 originalPosition;
        [HideInInspector]
        public Quaternion originalRotation;
        [HideInInspector]
        public Vector3 originalScale;
        [HideInInspector]
        public bool hasOriginalTransformState;

        public bool setOriginalTransformState;
        public bool revertToOriginalTransformState;

        public void SetOriginalTransformState()
        {
            if (targetObject == null) return;

            hasOriginalTransformState = true;

            targetObject.transform.GetPositionAndRotation(out var pos, out var rot);

            originalPosition = pos;
            originalRotation = rot;
            originalScale = targetObject.transform.localScale;
        }
        public void RevertToOriginalTransformState()
        {
            if (targetObject == null || !hasOriginalTransformState) return;

            targetObject.transform.SetPositionAndRotation(originalPosition, originalRotation);
            targetObject.transform.localScale = originalScale;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (targetObject == null) return;

            var startPos = startAtTargetObjectPosition ? targetObject.transform.position : transform.position;
            var offsetDir = transform.TransformDirection(offsetDirection);
            float distance = offsetDistance * cloneCount;

            Vector3 markerAxis = Vector3.Cross(offsetDir, transform.TransformDirection(upAxis)).normalized;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(startPos, startPos + offsetDir * distance);
            for (int a = 0; a < cloneCount; a++)
            {
                float t = (a + 1f) / cloneCount;
                if (offsetCurve != null && offsetCurve.length > 1) t = offsetCurve.Evaluate(t);

                Vector3 pos = startPos;
                if (offsetCurve != null && offsetCurve.length > 1)
                {
                    EvaluateOffsetCurve(out Vector3 endOffset, out Vector3 _, offsetCurve, offsetDir * distance, Vector3.zero, t, offsetCurveSpacing, offsetCurveStart);
                }
                else
                {
                    pos = pos + offsetDir * distance * t;
                }
                if (additionalOffsets != null)
                {
                    foreach (var additionalOffset in additionalOffsets) if (additionalOffset.affectsGizmos) pos = pos + additionalOffset.Evaluate(t);
                }
                Gizmos.DrawWireSphere(pos, 0.1f);
                if (drawMarkers)
                {
                    Gizmos.DrawLine(pos - markerAxis * markerWidth * 0.5f, pos + markerAxis * markerWidth * 0.5f);
                }
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(targetObject.transform.TransformPoint(offsetRotationPivot), 0.3f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetObject.transform.TransformPoint(lookRotationPivot), 0.3f);
        }
        private void Update()
        {
            if (!hasOriginalTransformState)
            {
                SetOriginalTransformState();
            }
            if (setOriginalTransformState)
            {
                setOriginalTransformState = false;
                SetOriginalTransformState();
            }
            if (revertToOriginalTransformState)
            {
                revertToOriginalTransformState = false;
                RevertToOriginalTransformState();
            }

            if (disconnectClones)
            {
                disconnectClones = false;
                DisconnectClones();
            }

            if (autoGenerateOnEdit)
            {
                if (offsetDistance != offsetDistancePrev || cloneCount != cloneCountPrev || offsetDirectionPrev != offsetDirection)
                {
                    generate = true;
                }
            }

            if (generate)
            {
                cloneCountPrev = cloneCount;
                offsetDirectionPrev = offsetDirection;
                offsetDistancePrev = offsetDistance;

                generate = false;
                GenerateClones();
            }
        }
#endif

        public void GenerateClones()
        {
            if (revertTargetObjectOnGenerate) RevertToOriginalTransformState();

            if (clones == null) clones = new List<GameObject>();
            foreach(var clone in clones)
            {
                if (clone == null || ReferenceEquals(clone, targetObject)) continue;
                GameObject.DestroyImmediate(clone);
            }

            clones.Clear();

            if (targetObject == null || cloneCount <= 0) return;

            var startPos = startAtTargetObjectPosition ? targetObject.transform.position : transform.position;
            var startRot = startAtTargetObjectPosition ? targetObject.transform.rotation : transform.rotation;
            var offsetDirWorld = transform.TransformDirection(offsetDirection);
            var upAxisWorld = transform.TransformDirection(upAxis);
            var pitchAxisWorld = Vector3.Cross(offsetDirWorld, upAxisWorld);
            float distance = offsetDistance * cloneCount;

#if UNITY_EDITOR
            //GameObject prefab = null;
            //PropertyModification[] prefabMods = null;
            //bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(targetObject);
            /*if (isPrefab)
            {
                prefab = PrefabUtility.GetCorrespondingObjectFromSource(targetObject);
                prefabMods = PrefabUtility.GetPropertyModifications(targetObject);
            }*/
#endif

            Vector3[] finalPositions = new Vector3[cloneCount + 1];
            finalPositions[0] = startPos;
            for (int a = 0; a < cloneCount; a++)
            {
                float t = (a + 1f) / cloneCount;
                if (offsetCurve != null && offsetCurve.length > 1) t = offsetCurve.Evaluate(t);

#if UNITY_EDITOR
                GameObject clone = null;
                //if (isPrefab)
                //{
                    //clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, targetObject.transform.parent);
                    //if (prefabMods != null) PrefabUtility.SetPropertyModifications(clone, prefabMods); 
                    clone = Utils.DuplicatePrefabInstanceA(targetObject);
                    clone.transform.SetParent(targetObject.transform.parent, true);
                //} 
                //else
                //{
                //    clone = Instantiate(targetObject, targetObject.transform.parent);
                //}
#else
                GameObject clone = Instantiate(targetObject, targetObject.transform.parent);
#endif
                clone.name = targetObject.name;
                if (parentToThis)
                {
                    clone.transform.SetParent(transform, true);
                }

                Vector3 pos = startPos;
                Quaternion rot = Quaternion.identity;
                if (offsetCurve != null && offsetCurve.length > 1)
                {
                    EvaluateOffsetCurve(out Vector3 endOffset, out Vector3 endRotOffset, offsetCurve, offsetDirWorld * distance, offsetRotationEuler, t, offsetCurveSpacing, offsetCurveStart);
                    pos = pos + endOffset;
                    rot = Quaternion.Euler(endRotOffset);
                }
                else
                {
                    pos = pos + offsetDirWorld * distance * t;
                    rot = Quaternion.Euler(offsetRotationEuler * t); 
                }

                if (additionalOffsets != null)
                {
                    foreach (var additionalOffset in additionalOffsets)
                    {
                        if (additionalOffset.transformMask == null || additionalOffset.transformMask.Count < 0)
                        {
                            pos = pos + additionalOffset.Evaluate(t);
                        }
                    }
                }

                var offsetPivot = targetObject.transform.TransformPoint(lookRotationPivot);
                clone.transform.SetPositionAndRotation(pos, startRot);
                clone.transform.ApplyRotationAroundPivot(rot, offsetPivot); 

                if (additionalOffsets != null)
                {
                    var transforms = clone.GetComponentsInChildren<Transform>(true);

                    List<TransformState> tempTransformStates = new List<TransformState>();
                    Dictionary<Transform, TransformState> tempTransformStates2 = new Dictionary<Transform, TransformState>();
                    foreach (var additionalOffset in additionalOffsets)
                    {
                        if (additionalOffset.transformMask != null)
                        {
                            var offset = additionalOffset.Evaluate(t); 
                            if (additionalOffset.invertMask)
                            {
                                tempTransformStates2.Clear();

                                if (transforms != null)
                                {
                                    foreach (var transform_ in transforms) tempTransformStates2[transform_] = new TransformState(transform_, true);

                                    foreach (var transform_ in transforms)
                                    {
                                        if (!additionalOffset.IsMasked(transform_) && tempTransformStates2.TryGetValue(transform_, out var state))
                                        {
                                            transform_.position = state.position + offset; 
                                        }
                                    }

                                    foreach (var transform_ in transforms) if (additionalOffset.IsMasked(transform_) && tempTransformStates2.TryGetValue(transform_, out var state)) state.ApplyWorld(transform_);
                                }
                            }
                            else
                            {
                                tempTransformStates.Clear();

                                foreach (var transform_ in transforms)
                                {
                                    if (additionalOffset.IsMasked(transform_))
                                    {
                                        tempTransformStates.Clear();
                                        for (int b = 0; b < transform_.childCount; b++) tempTransformStates.Add(new TransformState(transform_.GetChild(a), true));

                                        transform_.position = transform_.position + offset;

                                        for (int b = 0; b < transform_.childCount; b++) tempTransformStates[b].ApplyWorld(transform_.GetChild(a));
                                    }
                                }
                            }
                        }
                    }
                }

                finalPositions[a + 1] = clone.transform.position;
                clones.Add(clone);
            }

            if (rotateToNextNode)
            {
                for (int a = 0; a < finalPositions.Length - 1; a++)
                {
                    var pos = finalPositions[a];
                    var nextPos = finalPositions[a + 1];

                    Vector3 dir = (nextPos - pos).normalized;
                    Quaternion lookRot = Quaternion.AngleAxis(Vector3.SignedAngle(offsetDirWorld, dir, pitchAxisWorld), pitchAxisWorld); 

                    if (a == 0)
                    {
                        var lookPivot = targetObject.transform.TransformPoint(lookRotationPivot); 
                        targetObject.transform.ApplyRotationAroundPivot(lookRot, lookPivot);
                    } 
                    else if (a == finalPositions.Length - 2)
                    {
                        var clone = clones[a - 1];
                        var lookPivot = clone.transform.TransformPoint(lookRotationPivot);
                        clone.transform.ApplyRotationAroundPivot(lookRot, lookPivot);

                        clone = clones[a];
                        lookPivot = clone.transform.TransformPoint(lookRotationPivot);
                        clone.transform.ApplyRotationAroundPivot(lookRot, lookPivot);
                    } 
                    else
                    {
                        var clone = clones[a - 1];
                        var lookPivot = clone.transform.TransformPoint(lookRotationPivot);
                        clone.transform.ApplyRotationAroundPivot(lookRot, lookPivot);
                    }
                }
            }
        }

    }
}

#endif