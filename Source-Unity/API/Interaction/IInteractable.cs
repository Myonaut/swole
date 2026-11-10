#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    /// <summary>
    /// Represents an object in the world that can be interacted with by an <see cref="IInteractionManager"/>.
    /// Exposes prompt text, position/alignment requirements, and a collection of available interactions.
    /// </summary>
    public interface IInteractable
    {

        /// <summary>Prompt text shown to the player when this object can be interacted with.</summary>
        public string Prompt { get; }

        /// <summary>World-space position of this interactable.</summary>
        public Vector3 WorldPosition { get; }

        /// <summary>Unique identifier for this interactable.</summary>
        public int InteractableID { get; }

        /// <summary>Number of interactions available on this interactable.</summary>
        public int InteractionCount { get; }

        /// <summary>Finds the index of the interaction with the given ID, or -1 if not found.</summary>
        public int GetInteractionIndex(string interactionId);

        /// <summary>Attempts to find the index of the interaction with the given ID.</summary>
        public bool TryGetInteractionIndex(string interactionId, out int interactionIndex);

        /// <summary>Gets the interaction at the given index.</summary>
        public IInteraction this[int index] { get; }

        /// <summary>Whether interacting requires the interactor's position to fall within specific bounds.</summary>
        public bool RequiresPositionInBounds { get; }

        /// <summary>Determines whether the given world-space position satisfies the interaction bounds requirement.</summary>
        public bool IsPositionInBounds(Vector3 position);

        /// <summary>Whether interacting requires the interactor's rotation to satisfy an alignment requirement.</summary>
        public bool RequiresAlignment { get; }

        /// <summary>Determines whether the given rotation satisfies the alignment requirement.</summary>
        public bool IsAligned(Quaternion rotation);

        /// <summary>Whether this interactable supports automatic interaction (without explicit player input).</summary>
        public bool AllowAutoInteract { get; }

        /// <summary>If true, interaction managers should not require this interactable's interactions to be mapped to one of their own predefined performable interactions (e.g. when the interactable drives the interaction itself).</summary>
        public bool BypassPerformableInteractionCheck { get; }

        /// <summary>Determines whether the given world-space position satisfies the auto-interact bounds requirement.</summary>
        public bool IsPositionInAutoInteractBounds(Vector3 position);
        /// <summary>Determines whether the given rotation satisfies the auto-interact alignment requirement.</summary>
        public bool IsAutoInteractAligned(Quaternion rotation); 

        /// <summary>Determines whether the given manager currently satisfies this interactable's additional conditions (e.g. required carried items).</summary>
        public bool CanBeInteractedWithBy(IInteractionManager manager);

        /// <summary>Registers a listener to be invoked when an interaction with this object begins.</summary>
        public void ListenForInteractions(UnityAction<IInteractionManager> listener);
        /// <summary>Unregisters a previously registered interaction-begin listener.</summary>
        public void StopListeningForInteractions(UnityAction<IInteractionManager> listener);

        /// <summary>Registers a listener to be invoked when an interaction with this object ends.</summary>
        public void ListenForInteractionEnding(UnityAction<IInteractionManager> listener);
        /// <summary>Unregisters a previously registered interaction-end listener.</summary>
        public void StopListeningForInteractionEnding(UnityAction<IInteractionManager> listener); 

    }

    /// <summary>
    /// Represents a single interaction that can be performed on an <see cref="IInteractable"/>, exposing
    /// interaction points and a normalized-time-driven update mechanism.
    /// </summary>
    public interface IInteraction
    {

        /// <summary>The interactable that owns this interaction.</summary>
        public IInteractable Owner { get; }
        /// <summary>Whether this interaction currently has an owning interactable.</summary>
        public bool HasOwner { get; }

        /// <summary>Unique identifier for this interaction.</summary>
        public string ID { get; }

        /// <summary>Initializes (or reinitializes) this interaction's default state, associating it with the given owner.</summary>
        public bool Initialize(bool reinitialize, IInteractable owner);

        /// <summary>Updates the interaction's state to reflect the given normalized time (0-1), for the entering phase.</summary>
        public void SetTime(float normalizedTime);

        /// <summary>Updates the interaction's state to reflect the given normalized time (0-1), for either the entering or exiting phase.</summary>
        public void SetTime(float normalizedTime, bool isExit); 

        /// <summary>Number of interaction points defined on this interaction.</summary>
        public int InteractionPointCount { get; }

        /// <summary>Finds the index of the interaction point with the given ID, or -1 if not found.</summary>
        public int GetInteractionPointIndex(string interactionPointId);

        /// <summary>Attempts to find the index of the interaction point with the given ID.</summary>
        public bool TryGetInteractionPointIndex(string interactionPointId, out int interactionPointIndex); 

        /// <summary>Gets the interaction point at the given index.</summary>
        public InteractionPoint GetInteractionPoint(int interactionPointIndex);

        /// <summary>Attempts to find the interaction point with the given ID.</summary>
        public bool TryGetInteractionPoint(string interactionPointId, out InteractionPoint interactionPoint);

        /// <summary>Determines whether the given input ID is a valid initiator for this interaction.</summary>
        public bool IsInitiationInput(string inputId);

        /// <summary>Called when the interaction begins, initiated by the given manager.</summary>
        public void Start(IInteractionManager initiator);
        /// <summary>Called when the interaction ends, initiated by the given manager.</summary>
        public void End(IInteractionManager initiator);

    }

    /// <summary>
    /// Describes a positioned/oriented point associated with an interaction, used for placing or aligning
    /// the interacting entity (e.g. hands, tools) relative to a parent transform.
    /// </summary>
    [Serializable]
    public class InteractionPoint
    {
        [SerializeField]
        private string name;
        public string ID => name;

        [SerializeField]
        private Transform parentTransform;

        [SerializeField]
        private Vector3 localPosition;

        [SerializeField]
        private bool applyRotation;
        public bool ApplyRotation => applyRotation;
        [SerializeField]
        private Vector3 localRotationEuler;
        private Quaternion localRotation;

        private Quaternion startRotation;
        public Quaternion StartRotation => startRotation;

        /// <summary>Computes the world-space rotation offset applied since this interaction point's start rotation.</summary>
        public Quaternion GetRotationOffsetWorld()  
        {
            var upperParent = parentTransform == null ? null : parentTransform.parent;
            Quaternion upperRot = upperParent == null ? Quaternion.identity : upperParent.rotation;
            Quaternion startRot = upperRot * startRotation;
            Quaternion currentRot = (parentTransform == null ? Quaternion.identity : parentTransform.rotation) * localRotation;

            return currentRot * Quaternion.Inverse(startRot); 
        }

        /// <summary>Creates a new interaction point with the given identifier, parent, local position, and optional rotation.</summary>
        public InteractionPoint(string name, Transform parentTransform, Vector3 localPosition, bool applyRotation, Vector3 localRotationEuler)
        {
            this.name = name;
            this.parentTransform = parentTransform;
            this.localPosition = localPosition;
            this.applyRotation = applyRotation;
            this.localRotationEuler = localRotationEuler;
        }

        public float transitionTime;

        [Tooltip("How exact the teleport is. 0 = purely additive (moves by the offset measured at the start), 1 = exactly interpolates the character's pose to this point.")]
        [Range(0f, 1f)]
        public float exactBlend;

        [Tooltip("Seconds to wait after the teleport begins before the interaction (and its enter animation) starts.")]
        public float startDelay;

        [NonSerialized]
        private Vector3 currentWorldPosition;
        public Vector3 CurrentWorldPosition => currentWorldPosition;
        [NonSerialized]
        private Quaternion currentWorldRotation;
        public Quaternion CurrentWorldRotation => currentWorldRotation;

        //private Quaternion parentStartRotation;

        /// <summary>Caches the parent transform's start rotation and computes the initial local/start rotation state.</summary>
        public void Initialize()
        {
            //parentStartRotation = Quaternion.identity;
            //if (parentTransform != null)
            //{
            //    parentStartRotation = Quaternion.Inverse(parentTransform.rotation);
            //}

            localRotation = Quaternion.Euler(localRotationEuler);

            startRotation = (parentTransform == null ? Quaternion.identity : parentTransform.localRotation) * localRotation;  
        }

        /// <summary>Recomputes the current world position and rotation from the parent transform.</summary>
        public void Refresh() => Refresh(out _, out _);
        /// <summary>Recomputes the current world position and rotation, outputting the world position.</summary>
        public void Refresh(out Vector3 outputWorldPosition) => Refresh(out outputWorldPosition, out _);
        /// <summary>Recomputes the current world position and rotation, outputting the world rotation.</summary>
        public void Refresh(out Quaternion outputWorldRotation) => Refresh(out _, out outputWorldRotation);
        /// <summary>Recomputes the current world position and rotation, outputting both.</summary>
        public void Refresh(out Vector3 outputWorldPosition, out Quaternion outputWorldRotation)
        {
            if (parentTransform != null)
            {
                currentWorldPosition = parentTransform.TransformPoint(localPosition);
                currentWorldRotation = parentTransform.rotation * localRotation;//(parentStartRotation * parentTransform.rotation) * localRotation;
            } 
            else
            {
                currentWorldPosition = localPosition; 
                currentWorldRotation = Quaternion.Euler(localRotationEuler);  
            }

            outputWorldPosition = currentWorldPosition;
            outputWorldRotation = currentWorldRotation;
        }
    }

    /// <summary>
    /// Manages the active interaction state for an interacting entity (e.g. a player), granting or revoking
    /// interactability with nearby <see cref="IInteractable"/>s.
    /// </summary>
    public interface IInteractionManager
    {

        /// <summary>Unique identifier for this interaction manager.</summary>
        public int InteractionManagerID { get; }

        /// <summary>Grants interactability with the given interactable, as requested by the specified trigger.</summary>
        public void AllowInteractability(int triggerId, IInteractable interactable);
        /// <summary>Revokes interactability with the given interactable, as requested by the specified trigger.</summary>
        public void RevokeInteractability(int triggerId, IInteractable interactable);

        /// <summary>Whether this manager is currently engaged in an interaction.</summary>
        public bool IsInteracting { get; }
        /// <summary>The interactable currently being interacted with, if any.</summary>
        public IInteractable ActiveInteractable { get; }
        /// <summary>The interaction currently in progress, if any.</summary>
        public IInteraction ActiveInteraction { get; }

    }
}

#endif