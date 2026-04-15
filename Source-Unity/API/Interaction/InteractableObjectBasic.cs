#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using UnityEngine.Events;

namespace Swole
{
    public class InteractableObjectBasic : MonoBehaviour, IInteractable
    {

        public string prompt;
        public string Prompt => prompt;

        public Vector3 WorldPosition => transform.position;

        public int InteractableID => GetInstanceID();

        public bool allowAutoInteract;
        public bool AllowAutoInteract => allowAutoInteract;

        public bool requirePositionInBounds;
        public bool RequiresPositionInBounds => requirePositionInBounds;

        public Bounds[] requiredBounds;

        public bool useSeperateBoundsForAutoInteract;
        public Bounds[] autoInteractRequiredBounds;

        public bool IsPositionInBounds(Vector3 position)
        {
            if (!requirePositionInBounds || requiredBounds == null || requiredBounds.Length <= 0) return true;

            var localPosition = transform.InverseTransformPoint(position);
            foreach(var bounds in requiredBounds)
            {
                if (bounds.Contains(localPosition)) return true; 
            }

            return false;
        }

        public bool IsPositionInAutoInteractBounds(Vector3 position)
        {
            if (!useSeperateBoundsForAutoInteract) return IsPositionInBounds(position);

            if (!requirePositionInBounds || autoInteractRequiredBounds == null || autoInteractRequiredBounds.Length <= 0) return true; 

            var localPosition = transform.InverseTransformPoint(position);
            foreach (var bounds in autoInteractRequiredBounds)
            {
                if (bounds.Contains(localPosition)) return true;
            }

            return false;
        }

        public bool requireAlignment;
        public bool RequiresAlignment => requireAlignment;

        public Transform alignmentTransform;
        public Transform AlignmentTransform => alignmentTransform == null ? transform : alignmentTransform;

        public Vector3 localAlignmentVector = Vector3.forward;
        public Vector3 externalAlignmentVector = Vector3.forward;

        public Vector2 alignmentRange = new Vector2(0f, 1f);
        public Vector2 autoInteractAlignmentRange = new Vector2(0f, 1f);

        public Vector3 LocalAlignmentVector => AlignmentTransform.TransformDirection(localAlignmentVector);

        public bool IsAligned(Quaternion rotation)
        {
            if (!requireAlignment) return true;

            var externalAlignmentV = rotation * externalAlignmentVector;
            float alignment = Vector3.Dot(LocalAlignmentVector.normalized, externalAlignmentV.normalized);
            return alignment >= alignmentRange.x && alignment <= alignmentRange.y;
        }

        public bool IsAutoInteractAligned(Quaternion rotation)
        {
            if (!requireAlignment) return true;

            var externalAlignmentV = rotation * externalAlignmentVector;
            float alignment = Vector3.Dot(LocalAlignmentVector.normalized, externalAlignmentV.normalized);
            return alignment >= autoInteractAlignmentRange.x && alignment <= autoInteractAlignmentRange.y;
        }

#if UNITY_EDITOR

        protected void OnDrawGizmosSelected()
        {
            bool rflag = true;
            if (requirePositionInBounds && requiredBounds != null && requiredBounds.Length > 0)
            {
                UnityEngine.Random.InitState(GetInstanceID());
                rflag = false;
                Gizmos.matrix = transform.localToWorldMatrix;
                foreach(var bounds in requiredBounds)
                {
                    Gizmos.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.25f, 1f, 0.5f, 1f);
                    Gizmos.DrawCube(bounds.center, bounds.size);
                }

                if (allowAutoInteract && useSeperateBoundsForAutoInteract && autoInteractRequiredBounds != null)
                {
                    foreach (var bounds in autoInteractRequiredBounds)
                    {
                        Gizmos.color = Color.LerpUnclamped(UnityEngine.Random.ColorHSV(0f, 1f, 0.25f, 1f, 0.5f, 1f), Color.red, 0.5f);
                        Gizmos.DrawCube(bounds.center, bounds.size); 
                    }
                }

                Gizmos.matrix = Matrix4x4.identity;
            }

            if (requireAlignment)
            {
                if (alignmentRange.x < alignmentRange.y)
                {
                    Vector3 centerPos = transform.position;
                    Quaternion rot = transform.rotation;
                    Vector3 alignmentTangent = rot * new Vector3(localAlignmentVector.z, localAlignmentVector.y, localAlignmentVector.x);
                    Vector3 alignmentTangent2 = rot * new Vector3(localAlignmentVector.y, localAlignmentVector.z, localAlignmentVector.x);
                    for (int i = 0; i <= 10; i++)
                    {
                        float t = i / 10f;
                        float dot = 1f - ((Mathf.LerpUnclamped(alignmentRange.x, alignmentRange.y, t) + 1f) * 0.5f);

                        Gizmos.color = Color.cyan;
                        Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(180f * dot, alignmentTangent) * localAlignmentVector);
                        Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(180f * dot, alignmentTangent2) * localAlignmentVector);

                        Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(-180f * dot, alignmentTangent) * localAlignmentVector);
                        Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(-180f * dot, alignmentTangent2) * localAlignmentVector); 
                    }
                }

                if (allowAutoInteract)
                {
                    if (autoInteractAlignmentRange.x < autoInteractAlignmentRange.y)
                    {
                        Vector3 centerPos = transform.position + Vector3.right * 0.002f;
                        Quaternion rot = transform.rotation;
                        Vector3 alignmentTangent = rot * new Vector3(localAlignmentVector.z, localAlignmentVector.y, localAlignmentVector.x);
                        Vector3 alignmentTangent2 = rot * new Vector3(localAlignmentVector.y, localAlignmentVector.z, localAlignmentVector.x);
                        for (int i = 0; i <= 10; i++)
                        {
                            float t = i / 10f;
                            float dot = 1f - ((Mathf.LerpUnclamped(autoInteractAlignmentRange.x, autoInteractAlignmentRange.y, t) + 1f) * 0.5f);

                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(180f * dot, alignmentTangent) * localAlignmentVector);
                            Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(180f * dot, alignmentTangent2) * localAlignmentVector);

                            Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(-180f * dot, alignmentTangent) * localAlignmentVector);
                            Gizmos.DrawRay(centerPos, rot * Quaternion.AngleAxis(-180f * dot, alignmentTangent2) * localAlignmentVector);
                        }
                    }
                }
            }

            if (interactions != null)
            {
                if (rflag) UnityEngine.Random.InitState(GetInstanceID());
                foreach (var interaction in interactions)
                {
                    foreach (var ip in interaction.interactionPoints)
                    {
                        ip.Initialize();
                        ip.Refresh(out var wpos, out var wrot); 

                        Gizmos.color = UnityEngine.Random.ColorHSV(0f, 1f, 0.25f, 1f, 0.5f, 1f);
                        Gizmos.DrawSphere(wpos, 0.01f);

                        if (ip.ApplyRotation)
                        {
                            Gizmos.color = Color.blue;
                            Gizmos.DrawRay(wpos, wrot * Vector3.forward * 0.1f);

                            Gizmos.color = Color.red;
                            Gizmos.DrawRay(wpos, wrot * Vector3.right * 0.1f);

                            Gizmos.color = Color.green;
                            Gizmos.DrawRay(wpos, wrot * Vector3.up * 0.1f);
                        }
                    }
                }
            }
        }

#endif

        [Serializable]
        public class TransformInteraction : IInteraction
        {

            [NonSerialized]
            protected IInteractable owner;
            public IInteractable Owner => owner;
            public bool HasOwner => owner != null;

            public Transform target;

            public string ID => target == null ? string.Empty : target.name; 

            [NonSerialized]
            protected bool initialized;
            public bool IsInitialized => initialized;

            [NonSerialized]
            public Vector3 defaultLocalPosition;
            [NonSerialized]
            public Quaternion defaultLocalRotation;
            [NonSerialized]
            public Vector3 defaultLocalScale;

            public bool controlPosition;
            public bool controlRotation;
            public bool controlScale;

            public Vector3 targetLocalPosition;
            public bool relativeRotation;
            public Vector3 targetLocalRotationEuler;
            [NonSerialized]
            public Quaternion targetLocalRotation;
            public Vector3 targetLocalScale;

            public AnimationCurve timeCurve;
            public AnimationCurve positionCurve;
            public AnimationCurve rotationCurve;
            public AnimationCurve scaleCurve;

            public bool HasTimeCurve => timeCurve != null && timeCurve.length > 0;
            public bool HasPositionCurve => controlPosition && positionCurve != null && positionCurve.length > 0;
            public bool HasRotationCurve => controlRotation && rotationCurve != null && rotationCurve.length > 0;
            public bool HasScaleCurve => controlScale && scaleCurve != null && scaleCurve.length > 0;

            public bool Initialize(bool reinitialize = false, IInteractable owner = null)
            {
                if ((initialized && !reinitialize) || target == null) return false;

                this.owner = owner;

                defaultLocalPosition = target.localPosition;
                defaultLocalRotation = target.localRotation;
                defaultLocalScale = target.localScale;

                targetLocalRotation = Quaternion.Euler(targetLocalRotationEuler);

                initialized = true;
                return true;
            }

            public void SetTime(float normalizedTime)
            {
                if (!initialized)
                {
                    if (!Initialize(false, owner)) return;
                }

                normalizedTime = HasTimeCurve ? timeCurve.Evaluate(normalizedTime) : normalizedTime;

                if (controlPosition)
                {
                    float t = HasPositionCurve ? positionCurve.Evaluate(normalizedTime) : normalizedTime;
                    target.localPosition = Vector3.LerpUnclamped(defaultLocalPosition, targetLocalPosition, t);
                }
                if (controlRotation)
                {
                    float t = HasRotationCurve ? rotationCurve.Evaluate(normalizedTime) : normalizedTime;
                    if (relativeRotation)
                    {
                        target.localRotation = Quaternion.SlerpUnclamped(Quaternion.identity, targetLocalRotation, t) * defaultLocalRotation; 
                    } 
                    else
                    {
                        target.localRotation = Quaternion.SlerpUnclamped(defaultLocalRotation, targetLocalRotation, t);
                    }
                }
                if (controlScale)
                {
                    float t = HasScaleCurve ? scaleCurve.Evaluate(normalizedTime) : normalizedTime;
                    target.localScale = Vector3.LerpUnclamped(defaultLocalScale, targetLocalScale, t);
                }
            }

            public int InteractionPointCount => 0;

            public int GetInteractionPointIndex(string interactionPointId) => -1;

            public bool TryGetInteractionPointIndex(string interactionPointId, out int interactionPointIndex)
            {
                interactionPointIndex = -1;
                return false;
            }

            public InteractionPoint GetInteractionPoint(int interactionPointIndex) => null;

            public bool TryGetInteractionPoint(string interactionPointId, out InteractionPoint interactionPoint)
            {
                interactionPoint = null;
                return false;
            }

            public bool IsInitiationInput(string inputId) => false;

            public void Start(IInteractionManager initiator)
            {
            }

            public void End(IInteractionManager initiator)
            {
            }

        }

        [Serializable]
        public class TransformGroupInteraction : IInteraction
        {

            [NonSerialized]
            protected IInteractable owner;
            public IInteractable Owner => owner;
            public bool HasOwner => owner != null;

            public string name;
            public string ID => name;

            public TransformInteraction[] affectedTransforms;

            public InteractionPoint[] interactionPoints;

            public Behaviour[] componentsToDisable;
            public Collider[] collidersToDisable;
            public Behaviour[] componentsToEnable;
            public Collider[] collidersToEnable;

            public bool Initialize(bool reinitialize = false, IInteractable owner = null)
            {
                if (affectedTransforms != null)
                {
                    foreach(var t in affectedTransforms) if (t != null) t.Initialize(reinitialize, owner);
                }

                if (interactionPoints != null)
                {
                    foreach (var ip in interactionPoints) if (ip != null) ip.Initialize();
                }

                return true;
            }

            public void SetTime(float normalizedTime)
            {
                if (affectedTransforms != null)
                {
                    foreach (var t in affectedTransforms) if (t != null) t.SetTime(normalizedTime);
                }

                if (interactionPoints != null)
                {
                    foreach(var ip in interactionPoints) if (ip != null) ip.Refresh();
                }
            }

            public int InteractionPointCount => interactionPoints == null ? 0 : interactionPoints.Length;

            public int GetInteractionPointIndex(string interactionPointId)
            {
                for(int a = 0; a < interactionPoints.Length; a++) if (interactionPoints[a].ID == interactionPointId) return a;
                return -1;
            }

            public bool TryGetInteractionPointIndex(string interactionPointId, out int interactionPointIndex)
            {
                interactionPointIndex = GetInteractionPointIndex(interactionPointId);
                return interactionPointIndex >= 0;
            }

            public InteractionPoint GetInteractionPoint(int interactionPointIndex)
            {
                if (interactionPointIndex < 0 || interactionPointIndex >= InteractionPointCount) return null;
                return interactionPoints[interactionPointIndex];
            }

            public bool TryGetInteractionPoint(string interactionPointId, out InteractionPoint interactionPoint)
            {
                interactionPoint = null;
                if (!TryGetInteractionPointIndex(interactionPointId, out var index)) return false;

                interactionPoint = GetInteractionPoint(index);
                return true;
            }

            public List<string> validInitiationInputs = new List<string>();

            public bool IsInitiationInput(string inputId)
            {
                if (validInitiationInputs == null) return false;
                return validInitiationInputs.Contains(inputId);
            }

            public void Start(IInteractionManager initiator)
            {
                if (componentsToDisable != null)
                {
                    foreach (var comp in componentsToDisable) if (comp != null) comp.enabled = false;
                }
                if (componentsToEnable != null)
                {
                    foreach (var comp in componentsToEnable) if (comp != null) comp.enabled = true;
                }
                if (collidersToDisable != null)
                {
                    foreach (var comp in collidersToDisable) if (comp != null) comp.enabled = false;
                }
                if (collidersToEnable != null)
                {
                    foreach (var comp in collidersToEnable) if (comp != null) comp.enabled = true;
                }
            }

            public void End(IInteractionManager initiator)
            {
                if (componentsToDisable != null)
                {
                    foreach (var comp in componentsToDisable) if (comp != null) comp.enabled = true;
                }
                if (componentsToEnable != null)
                {
                    foreach (var comp in componentsToEnable) if (comp != null) comp.enabled = false;
                }
                if (collidersToDisable != null)
                {
                    foreach (var comp in collidersToDisable) if (comp != null) comp.enabled = true; 
                }
                if (collidersToEnable != null)
                {
                    foreach (var comp in collidersToEnable) if (comp != null) comp.enabled = false;
                }
            }

        }

        public TransformGroupInteraction[] interactions;

        public int InteractionCount => interactions == null ? 0 : interactions.Length;

        public int GetInteractionIndex(string interactionId)
        {
            for (int a = 0; a < interactions.Length; a++) if (interactions[a].ID == interactionId) return a;
            return -1;
        }

        public bool TryGetInteractionIndex(string interactionId, out int interactionIndex)
        {
            interactionIndex = GetInteractionIndex(interactionId);
            return interactionIndex >= 0;
        }

        public IInteraction GetInteraction(int interactionIndex) => GetInteractionTyped(interactionIndex);
        public TransformGroupInteraction GetInteractionTyped(int interactionIndex)
        {
            if (interactionIndex < 0 || interactionIndex >= InteractionCount) return null;
            return interactions[interactionIndex];
        }
        public IInteraction this[int interactionIndex] => GetInteraction(interactionIndex);

        public bool TryGetInteraction(string interactionId, out IInteraction interaction)
        {
            interaction = null;
            if (!TryGetInteractionTyped(interactionId, out var interactionTyped)) return false;

            interaction = interactionTyped;
            return true;
        }
        public bool TryGetInteractionTyped(string interactionId, out TransformGroupInteraction interaction)
        {
            interaction = null;
            if (!TryGetInteractionIndex(interactionId, out var index)) return false;

            interaction = GetInteractionTyped(index); 
            return true;
        }

        public InteractableTrigger[] triggers;

        protected virtual void Awake()
        {
            if (interactions != null) foreach(var i in interactions) i.Initialize(false, this);

            if (triggers != null) foreach (var t in triggers) if (t != null) t.SetOwner(this);
        }

        public UnityEvent<IInteractionManager> OnBeginInteraction;
        public UnityEvent<IInteractionManager> OnEndInteraction; 

        public void ListenForInteractions(UnityAction<IInteractionManager> listener)
        {
            if (OnBeginInteraction == null) OnBeginInteraction = new UnityEvent<IInteractionManager>();
            if (listener != null) OnBeginInteraction.AddListener(listener);
        }
        public void StopListeningForInteractions(UnityAction<IInteractionManager> listener)
        {
            if (OnBeginInteraction == null || listener == null) return;
            OnBeginInteraction.RemoveListener(listener);
        }

        public void ListenForInteractionEnding(UnityAction<IInteractionManager> listener)
        {
            if (OnEndInteraction == null) OnEndInteraction = new UnityEvent<IInteractionManager>();
            if (listener != null) OnEndInteraction.AddListener(listener);
        }
        public void StopListeningForInteractionEnding(UnityAction<IInteractionManager> listener)
        {
            if (OnEndInteraction == null || listener == null) return;
            OnEndInteraction.RemoveListener(listener);
        }

    }
}

#endif