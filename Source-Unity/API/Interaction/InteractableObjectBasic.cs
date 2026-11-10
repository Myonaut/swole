#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Mathematics;
using UnityEngine.Events;

namespace Swole
{
    /// <summary>
    /// Basic implementation of <see cref="IInteractable"/> that exposes bounds, alignment and
    /// transform-driven interaction behaviour configurable via the inspector.
    /// </summary>
    public class InteractableObjectBasic : MonoBehaviour, IInteractable
    {

        /// <summary>The prompt text shown to the player when this object can be interacted with.</summary>
        public string prompt;
        public string Prompt => prompt;

        /// <summary>World-space position of this interactable (its transform position).</summary>
        public Vector3 WorldPosition => transform.position;

        /// <summary>Unique identifier for this interactable, based on the Unity instance ID.</summary>
        public int InteractableID => GetInstanceID();

        /// <summary>If true, this interactable can be auto-interacted with without explicit player input.</summary>
        public bool allowAutoInteract;
        public bool AllowAutoInteract => allowAutoInteract;

        /// <summary>If true, interaction managers will not require their own performable interaction mapping for this interactable's interactions.</summary>
        public bool bypassPerformableInteractionCheck;
        public virtual bool BypassPerformableInteractionCheck => bypassPerformableInteractionCheck;

        /// <summary>If true, the interacting entity's position must fall within <see cref="requiredBounds"/>.</summary>
        public bool requirePositionInBounds;
        public bool RequiresPositionInBounds => requirePositionInBounds;

        /// <summary>Local-space bounds that a position must fall within to be considered valid for interaction.</summary>
        public Bounds[] requiredBounds;

        /// <summary>If true, auto-interact uses <see cref="autoInteractRequiredBounds"/> instead of <see cref="requiredBounds"/>.</summary>
        public bool useSeperateBoundsForAutoInteract;
        /// <summary>Local-space bounds used specifically for auto-interact position checks.</summary>
        public Bounds[] autoInteractRequiredBounds;

        /// <summary>Determines whether the given world-space position falls within the required interaction bounds.</summary>
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

        /// <summary>Determines whether the given world-space position falls within the auto-interact bounds.</summary>
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

        /// <summary>If true, interaction requires the external rotation to fall within the alignment range.</summary>
        public bool requireAlignment;
        public bool RequiresAlignment => requireAlignment;

        /// <summary>Transform used as the basis for alignment checks. Defaults to this object's transform if unset.</summary>
        public Transform alignmentTransform;
        public Transform AlignmentTransform => alignmentTransform == null ? transform : alignmentTransform;

        /// <summary>Direction vector, local to <see cref="AlignmentTransform"/>, used to test alignment.</summary>
        public Vector3 localAlignmentVector = Vector3.forward;
        /// <summary>Reference direction vector on the external (interacting) entity used to test alignment.</summary>
        public Vector3 externalAlignmentVector = Vector3.forward;

        /// <summary>Acceptable dot-product range between the local and external alignment vectors for a valid interaction.</summary>
        public Vector2 alignmentRange = new Vector2(0f, 1f);
        /// <summary>Acceptable dot-product range between the local and external alignment vectors for a valid auto-interaction.</summary>
        public Vector2 autoInteractAlignmentRange = new Vector2(0f, 1f);

        /// <summary>World-space direction of <see cref="localAlignmentVector"/> as transformed by <see cref="AlignmentTransform"/>.</summary>
        public Vector3 LocalAlignmentVector => AlignmentTransform.TransformDirection(localAlignmentVector);

        /// <summary>Determines whether the given rotation satisfies the required alignment range.</summary>
        public bool IsAligned(Quaternion rotation)
        {
            if (!requireAlignment) return true;

            var externalAlignmentV = rotation * externalAlignmentVector;
            float alignment = Vector3.Dot(LocalAlignmentVector.normalized, externalAlignmentV.normalized);
            return alignment >= alignmentRange.x && alignment <= alignmentRange.y;
        }

        /// <summary>Determines whether the given rotation satisfies the required auto-interact alignment range.</summary>
        public bool IsAutoInteractAligned(Quaternion rotation)
        {
            if (!requireAlignment) return true;

            var externalAlignmentV = rotation * externalAlignmentVector;
            float alignment = Vector3.Dot(LocalAlignmentVector.normalized, externalAlignmentV.normalized);
            return alignment >= autoInteractAlignmentRange.x && alignment <= autoInteractAlignmentRange.y;
        }

#if UNITY_EDITOR

        /// <summary>Editor-only gizmo drawing to visualize required bounds, alignment ranges and interaction points.</summary>
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
                    Vector3 alignmentTangent = new Vector3(localAlignmentVector.z, localAlignmentVector.x, localAlignmentVector.y);
                    Vector3 alignmentTangent2 = new Vector3(localAlignmentVector.y, localAlignmentVector.z, localAlignmentVector.x);
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
                        Vector3 alignmentTangent = new Vector3(localAlignmentVector.z, localAlignmentVector.x, localAlignmentVector.y);
                        Vector3 alignmentTangent2 = new Vector3(localAlignmentVector.y, localAlignmentVector.z, localAlignmentVector.x);
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

        /// <summary>
        /// Describes how a <see cref="TransformInteraction"/> target transform should animate its position, rotation and/or scale
        /// between default and target values based on a normalized time value, for either the entering or exiting phase of the interaction.
        /// </summary>
        [Serializable]
        public class TransformInteractionMode
        {

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

            public AnimationCurve positionOffsetCurveX;
            public AnimationCurve positionOffsetCurveY;
            public AnimationCurve positionOffsetCurveZ;

            public AnimationCurve rotationOffsetCurveX;
            public AnimationCurve rotationOffsetCurveY;
            public AnimationCurve rotationOffsetCurveZ;

            public AnimationCurve scaleOffsetCurveX;
            public AnimationCurve scaleOffsetCurveY;
            public AnimationCurve scaleOffsetCurveZ;

            public bool HasTimeCurve => timeCurve != null && timeCurve.length > 0;

            public bool HasPositionCurve => controlPosition && positionCurve != null && positionCurve.length > 0;
            public bool HasRotationCurve => controlRotation && rotationCurve != null && rotationCurve.length > 0;
            public bool HasScaleCurve => controlScale && scaleCurve != null && scaleCurve.length > 0;

            public bool HasPositionOffsetCurveX => controlPosition && positionOffsetCurveX != null && positionOffsetCurveX.length > 0;
            public bool HasPositionOffsetCurveY => controlPosition && positionOffsetCurveY != null && positionOffsetCurveY.length > 0;
            public bool HasPositionOffsetCurveZ => controlPosition && positionOffsetCurveZ != null && positionOffsetCurveZ.length > 0;

            public bool HasRotationOffsetCurveX => controlRotation && rotationOffsetCurveX != null && rotationOffsetCurveX.length > 0;
            public bool HasRotationOffsetCurveY => controlRotation && rotationOffsetCurveY != null && rotationOffsetCurveY.length > 0;
            public bool HasRotationOffsetCurveZ => controlRotation && rotationOffsetCurveZ != null && rotationOffsetCurveZ.length > 0;

            public bool HasScaleOffsetCurveX => controlScale && scaleOffsetCurveX != null && scaleOffsetCurveX.length > 0;
            public bool HasScaleOffsetCurveY => controlScale && scaleOffsetCurveY != null && scaleOffsetCurveY.length > 0;
            public bool HasScaleOffsetCurveZ => controlScale && scaleOffsetCurveZ != null && scaleOffsetCurveZ.length > 0;

            /// <summary>Caches the target local rotation as a quaternion. Should be called whenever the owning interaction is (re)initialized.</summary>
            public void Initialize()
            {
                targetLocalRotation = Quaternion.Euler(targetLocalRotationEuler);
            }

            /// <summary>Applies this mode's position/rotation/scale transformation to the target transform at the given normalized time (0-1),
            /// relative to the given default local position/rotation/scale.</summary>
            public void SetTime(Transform target, float normalizedTime, Vector3 defaultLocalPosition, Quaternion defaultLocalRotation, Vector3 defaultLocalScale)
            {
                if (target == null) return;

                normalizedTime = HasTimeCurve ? timeCurve.Evaluate(normalizedTime) : normalizedTime;

                if (controlPosition)
                {
                    Vector3 offset = Vector3.zero;
                    if (HasPositionOffsetCurveX) offset.x = positionOffsetCurveX.Evaluate(normalizedTime);
                    if (HasPositionOffsetCurveY) offset.y = positionOffsetCurveY.Evaluate(normalizedTime);
                    if (HasPositionOffsetCurveZ) offset.z = positionOffsetCurveZ.Evaluate(normalizedTime);

                    float t = HasPositionCurve ? positionCurve.Evaluate(normalizedTime) : normalizedTime;
                    target.localPosition = Vector3.LerpUnclamped(defaultLocalPosition, targetLocalPosition, t) + offset;
                }
                if (controlRotation)
                {
                    Vector3 rotOffset = Vector3.zero;
                    if (HasRotationOffsetCurveX) rotOffset.x = rotationOffsetCurveX.Evaluate(normalizedTime);
                    if (HasRotationOffsetCurveY) rotOffset.y = rotationOffsetCurveY.Evaluate(normalizedTime);
                    if (HasRotationOffsetCurveZ) rotOffset.z = rotationOffsetCurveZ.Evaluate(normalizedTime);

                    float t = HasRotationCurve ? rotationCurve.Evaluate(normalizedTime) : normalizedTime;
                    if (relativeRotation)
                    {
                        target.localRotation = Quaternion.SlerpUnclamped(Quaternion.identity, targetLocalRotation, t) * Quaternion.Euler(rotOffset) * defaultLocalRotation;
                    }
                    else
                    {
                        target.localRotation = Quaternion.SlerpUnclamped(defaultLocalRotation, targetLocalRotation, t) * Quaternion.Euler(rotOffset);
                    }
                }
                if (controlScale)
                {
                    Vector3 scaleOffset = Vector3.zero;
                    if (HasScaleOffsetCurveX) scaleOffset.x = scaleOffsetCurveX.Evaluate(normalizedTime);
                    if (HasScaleOffsetCurveY) scaleOffset.y = scaleOffsetCurveY.Evaluate(normalizedTime);
                    if (HasScaleOffsetCurveZ) scaleOffset.z = scaleOffsetCurveZ.Evaluate(normalizedTime);

                    float t = HasScaleCurve ? scaleCurve.Evaluate(normalizedTime) : normalizedTime;
                    target.localScale = Vector3.LerpUnclamped(defaultLocalScale, targetLocalScale, t) + scaleOffset;
                }
            }

        }

        /// <summary>
        /// An <see cref="IInteraction"/> that animates a single transform's position, rotation and/or scale
        /// between default and target values based on a normalized time value.
        /// </summary>
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

            /// <summary>Controls how the target transform animates while entering this interaction (normalized time 0-1).</summary>
            public TransformInteractionMode enterMode = new TransformInteractionMode();
            /// <summary>Controls how the target transform animates while exiting this interaction (normalized time 0-1).</summary>
            public TransformInteractionMode exitMode = new TransformInteractionMode();

            /// <summary>Caches the target transform's default local position, rotation and scale as the interaction's start state.</summary>
            public bool Initialize(bool reinitialize = false, IInteractable owner = null)
            {
                if ((initialized && !reinitialize) || target == null) return false;

                this.owner = owner;

                defaultLocalPosition = target.localPosition;
                defaultLocalRotation = target.localRotation;
                defaultLocalScale = target.localScale;

                enterMode?.Initialize();
                exitMode?.Initialize();

                initialized = true;
                return true;
            }

            /// <summary>Applies the entering interaction's position/rotation/scale transformation at the given normalized time (0-1).</summary>
            public void SetTime(float normalizedTime) => SetTime(normalizedTime, false);

            /// <summary>Applies the entering or exiting interaction's position/rotation/scale transformation at the given normalized time (0-1).</summary>
            public void SetTime(float normalizedTime, bool isExit)
            {
                if (!initialized)
                {
                    if (!Initialize(false, owner)) return;
                }
                
                var mode = isExit ? exitMode : enterMode;
                if (mode == null) return;

                mode.SetTime(target, normalizedTime, defaultLocalPosition, defaultLocalRotation, defaultLocalScale);
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

            /// <summary>No-op; single transform interactions do not react to start/end events.</summary>
            public void Start(IInteractionManager initiator)
            {
            }

            /// <summary>No-op; single transform interactions do not react to start/end events.</summary>
            public void End(IInteractionManager initiator)
            {
            }

        }

        /// <summary>
        /// An <see cref="IInteraction"/> composed of multiple <see cref="TransformInteraction"/>s, interaction points,
        /// and components/colliders that get enabled or disabled while the interaction is active.
        /// </summary>
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

            public UnityEvent OnStart;
            public UnityEvent<IInteractionManager> OnStartWithArg;
            public DelayedInteractionEvent[] delayedStartEvents;

            public UnityEvent OnEnd;
            public UnityEvent<IInteractionManager> OnEndWithArg;
            public DelayedInteractionEvent[] delayedEndEvents;



            /// <summary>Initializes all affected transform interactions and interaction points in this group.</summary>
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

            /// <summary>Propagates the normalized time to all affected transform interactions (entering phase) and refreshes interaction points.</summary>
            public void SetTime(float normalizedTime) => SetTime(normalizedTime, false);

            /// <summary>Propagates the normalized time to all affected transform interactions (entering or exiting phase) and refreshes interaction points.</summary>
            public void SetTime(float normalizedTime, bool isExit)
            {
                if (affectedTransforms != null)
                {
                    foreach (var t in affectedTransforms) if (t != null) t.SetTime(normalizedTime, isExit);
                }

                if (interactionPoints != null)
                {
                    foreach(var ip in interactionPoints) if (ip != null) ip.Refresh();
                }
            }

            public int InteractionPointCount => interactionPoints == null ? 0 : interactionPoints.Length;

            public int GetInteractionPointIndex(string interactionPointId)
            {
#if UNITY_EDITOR
                Debug.Log("Searching for interaction point with ID: " + interactionPointId);
#endif
                for (int a = 0; a < interactionPoints.Length; a++) if (interactionPoints[a].ID == interactionPointId) return a; 
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

            /// <summary>Input identifiers that are permitted to start this interaction.</summary>
            public List<string> validInitiationInputs = new List<string>();

            /// <summary>Checks whether the given input ID is a valid initiator for this interaction.</summary>
            public bool IsInitiationInput(string inputId)
            {
                if (validInitiationInputs == null) return false;
                return validInitiationInputs.Contains(inputId);
            }

            /// <summary>Called when the interaction begins; disables/enables the configured components and colliders.</summary>
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

                OnStart?.Invoke();
                OnStartWithArg?.Invoke(initiator);

                if (delayedStartEvents != null && delayedStartEvents.Length > 0)
                {
                    foreach (var e in delayedStartEvents) if (e != null) e.Call(initiator);
                }
            }

            /// <summary>Called when the interaction ends; reverts the enabled/disabled state of the configured components and colliders.</summary>
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

                OnEnd?.Invoke();
                OnEndWithArg?.Invoke(initiator);

                if (delayedEndEvents != null && delayedEndEvents.Length > 0)
                {
                    foreach (var e in delayedEndEvents) if (e != null) e.Call(initiator);
                }
            }

        }

        /// <summary>The set of interactions available on this object.</summary>
        public TransformGroupInteraction[] interactions;

        public int InteractionCount => interactions == null ? 0 : interactions.Length;

        /// <summary>Finds the index of the interaction with the given ID, or -1 if not found.</summary>
        public int GetInteractionIndex(string interactionId)
        {
#if UNITY_EDITOR
            Debug.Log("Searching for interaction with ID: " + interactionId);
#endif
            for (int a = 0; a < interactions.Length; a++) if (interactions[a].ID == interactionId) return a;
            return -1;
        }

        /// <summary>Attempts to find the index of the interaction with the given ID.</summary>
        public bool TryGetInteractionIndex(string interactionId, out int interactionIndex)
        {
            interactionIndex = GetInteractionIndex(interactionId);
            return interactionIndex >= 0;
        }

        /// <summary>Gets the interaction at the given index as an <see cref="IInteraction"/>.</summary>
        public IInteraction GetInteraction(int interactionIndex) => GetInteractionTyped(interactionIndex);
        /// <summary>Gets the interaction at the given index as its concrete <see cref="TransformGroupInteraction"/> type.</summary>
        public TransformGroupInteraction GetInteractionTyped(int interactionIndex)
        {
            if (interactionIndex < 0 || interactionIndex >= InteractionCount) return null;
            return interactions[interactionIndex];
        }
        public IInteraction this[int interactionIndex] => GetInteraction(interactionIndex);

        /// <summary>Attempts to find the interaction with the given ID.</summary>
        public bool TryGetInteraction(string interactionId, out IInteraction interaction)
        {
            interaction = null;
            if (!TryGetInteractionTyped(interactionId, out var interactionTyped)) return false;

            interaction = interactionTyped;
            return true;
        }
        /// <summary>Attempts to find the interaction with the given ID as its concrete <see cref="TransformGroupInteraction"/> type.</summary>
        public bool TryGetInteractionTyped(string interactionId, out TransformGroupInteraction interaction)
        {
            interaction = null;
            if (!TryGetInteractionIndex(interactionId, out var index)) return false;

            interaction = GetInteractionTyped(index); 
            return true;
        }

        /// <summary>Trigger volumes that grant interactability with this object when entered.</summary>
        public InteractableTrigger[] triggers;

        protected readonly List<Func<IInteractionManager, bool>> interactionConditions = new List<Func<IInteractionManager, bool>>();

        /// <summary>Adds a condition the interacting manager must satisfy before this object can be interacted with.</summary>
        public void AddInteractionCondition(Func<IInteractionManager, bool> condition)
        {
            if (condition != null && !interactionConditions.Contains(condition)) interactionConditions.Add(condition);
        }
        public void RemoveInteractionCondition(Func<IInteractionManager, bool> condition) => interactionConditions.Remove(condition);

        public virtual bool CanBeInteractedWithBy(IInteractionManager manager)
        {
            foreach (var condition in interactionConditions) if (!condition(manager)) return false;
            return true;
        }

        /// <summary>Initializes all interactions and assigns this object as the owner of each trigger.</summary>
        protected virtual void Awake()
        { 
            if (interactions != null) foreach(var i in interactions)
            {
                i.Initialize(false, this);
                if (i.OnStartWithArg == null) i.OnStartWithArg = new UnityEvent<IInteractionManager>();
                i.OnStartWithArg.AddListener(NotifyInteractionStart);
                if (i.OnEndWithArg == null) i.OnEndWithArg = new UnityEvent<IInteractionManager>();
                i.OnEndWithArg.AddListener(NotifyInteractionEnd);
            }

            if (triggers != null) foreach (var t in triggers) if (t != null) t.SetOwner(this); 
        }

        protected void NotifyInteractionStart(IInteractionManager initiator)
        {
            OnBeginInteraction?.Invoke();
            OnBeginInteractionWithArg?.Invoke(initiator);

            if (delayedBeginInteractionEvents != null && delayedBeginInteractionEvents.Length > 0)
            {
                foreach (var e in delayedBeginInteractionEvents) if (e != null) e.Call(initiator);
            }
        }
        protected void NotifyInteractionEnd(IInteractionManager initiator)
        {
            OnEndInteraction?.Invoke();
            OnEndInteractionWithArg?.Invoke(initiator);

            if (delayedEndInteractionEvents != null && delayedEndInteractionEvents.Length > 0)
            {
                foreach (var e in delayedEndInteractionEvents) if (e != null) e.Call(initiator); 
            }
        }

        public UnityEvent OnBeginInteraction;
        /// <summary>Raised when an interaction with this object begins.</summary>
        public UnityEvent<IInteractionManager> OnBeginInteractionWithArg;
        public DelayedInteractionEvent[] delayedBeginInteractionEvents;

        public UnityEvent OnEndInteraction;
        /// <summary>Raised when an interaction with this object ends.</summary>
        public UnityEvent<IInteractionManager> OnEndInteractionWithArg;
        public DelayedInteractionEvent[] delayedEndInteractionEvents;

        /// <summary>Registers a listener to be invoked when an interaction with this object begins.</summary>
        public void ListenForInteractions(UnityAction<IInteractionManager> listener)
        {
            if (OnBeginInteractionWithArg == null) OnBeginInteractionWithArg = new UnityEvent<IInteractionManager>();
            if (listener != null) OnBeginInteractionWithArg.AddListener(listener);
        }
        /// <summary>Unregisters a previously registered interaction-begin listener.</summary>
        public void StopListeningForInteractions(UnityAction<IInteractionManager> listener)
        {
            if (OnBeginInteractionWithArg == null || listener == null) return;
            OnBeginInteractionWithArg.RemoveListener(listener);
        }

        /// <summary>Registers a listener to be invoked when an interaction with this object ends.</summary>
        public void ListenForInteractionEnding(UnityAction<IInteractionManager> listener)
        {
            if (OnEndInteractionWithArg == null) OnEndInteractionWithArg = new UnityEvent<IInteractionManager>();
            if (listener != null) OnEndInteractionWithArg.AddListener(listener);
        }
        /// <summary>Unregisters a previously registered interaction-end listener.</summary>
        public void StopListeningForInteractionEnding(UnityAction<IInteractionManager> listener)
        {
            if (OnEndInteractionWithArg == null || listener == null) return; 
            OnEndInteractionWithArg.RemoveListener(listener);
        }

    }

    [Serializable]
    public class DelayedInteractionEvent
    {
        public float delay;

        public UnityEvent OnCall;
        public UnityEvent<IInteractionManager> OnCallWithArg;

        public void Call(IInteractionManager initiator)
        {
            CoroutineProxy.Start(DelayedCall(initiator)); 
        }

        protected IEnumerator DelayedCall(IInteractionManager initiator)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            OnCall?.Invoke();
            OnCallWithArg?.Invoke(initiator);
        }
    }
}

#endif