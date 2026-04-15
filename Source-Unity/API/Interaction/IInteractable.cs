#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    public interface IInteractable
    {

        public string Prompt { get; }

        public Vector3 WorldPosition { get; }

        public int InteractableID { get; }

        public int InteractionCount { get; }

        public int GetInteractionIndex(string interactionId);

        public bool TryGetInteractionIndex(string interactionId, out int interactionIndex);

        public IInteraction this[int index] { get; }

        public bool RequiresPositionInBounds { get; }

        public bool IsPositionInBounds(Vector3 position);

        public bool RequiresAlignment { get; }

        public bool IsAligned(Quaternion rotation);

        public bool AllowAutoInteract { get; }

        public bool IsPositionInAutoInteractBounds(Vector3 position);
        public bool IsAutoInteractAligned(Quaternion rotation); 

        public void ListenForInteractions(UnityAction<IInteractionManager> listener);
        public void StopListeningForInteractions(UnityAction<IInteractionManager> listener);

        public void ListenForInteractionEnding(UnityAction<IInteractionManager> listener);
        public void StopListeningForInteractionEnding(UnityAction<IInteractionManager> listener); 

    }

    public interface IInteraction
    {

        public IInteractable Owner { get; }
        public bool HasOwner { get; }

        public string ID { get; }

        public bool Initialize(bool reinitialize, IInteractable owner);

        public void SetTime(float normalizedTime);

        public int InteractionPointCount { get; }

        public int GetInteractionPointIndex(string interactionPointId);

        public bool TryGetInteractionPointIndex(string interactionPointId, out int interactionPointIndex); 

        public InteractionPoint GetInteractionPoint(int interactionPointIndex);

        public bool TryGetInteractionPoint(string interactionPointId, out InteractionPoint interactionPoint);

        public bool IsInitiationInput(string inputId);

        public void Start(IInteractionManager initiator);
        public void End(IInteractionManager initiator);

    }

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

        public Quaternion GetRotationOffsetWorld()  
        {
            var upperParent = parentTransform == null ? null : parentTransform.parent;
            Quaternion upperRot = upperParent == null ? Quaternion.identity : upperParent.rotation;
            Quaternion startRot = upperRot * startRotation;
            Quaternion currentRot = (parentTransform == null ? Quaternion.identity : parentTransform.rotation) * localRotation;

            return currentRot * Quaternion.Inverse(startRot); 
        }

        public InteractionPoint(string name, Transform parentTransform, Vector3 localPosition, bool applyRotation, Vector3 localRotationEuler)
        {
            this.name = name;
            this.parentTransform = parentTransform;
            this.localPosition = localPosition;
            this.applyRotation = applyRotation;
            this.localRotationEuler = localRotationEuler;
        }

        public float transitionTime;

        [NonSerialized]
        private Vector3 currentWorldPosition;
        public Vector3 CurrentWorldPosition => currentWorldPosition;
        [NonSerialized]
        private Quaternion currentWorldRotation;
        public Quaternion CurrentWorldRotation => currentWorldRotation;

        private Quaternion parentStartRotation;

        public void Initialize()
        {
            parentStartRotation = Quaternion.identity;
            if (parentTransform != null)
            {
                parentStartRotation = Quaternion.Inverse(parentTransform.rotation);
            }

            localRotation = Quaternion.Euler(localRotationEuler);

            startRotation = parentTransform.localRotation * localRotation;
        }

        public void Refresh() => Refresh(out _, out _);
        public void Refresh(out Vector3 outputWorldPosition) => Refresh(out outputWorldPosition, out _);
        public void Refresh(out Quaternion outputWorldRotation) => Refresh(out _, out outputWorldRotation);
        public void Refresh(out Vector3 outputWorldPosition, out Quaternion outputWorldRotation)
        {
            if (parentTransform != null)
            {
                currentWorldPosition = parentTransform.TransformPoint(localPosition);
                currentWorldRotation = (parentStartRotation * parentTransform.rotation) * localRotation;
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

    public interface IInteractionManager
    {

        public int InteractionManagerID { get; }

        public void AllowInteractability(int triggerId, IInteractable interactable);
        public void RevokeInteractability(int triggerId, IInteractable interactable);

        public bool IsInteracting { get; }
        public IInteractable ActiveInteractable { get; }
        public IInteraction ActiveInteraction { get; }

    }
}

#endif