#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole
{

    /// <summary>
    /// A component that streamlines the activation/deactivation of multiple groups of GameObjects or MonoBehaviours.
    /// </summary>
    public class ActivationController : MonoBehaviour
    {

        public static ActivationController New(string name, ActivationGroup[] activationGroups) => AddToGameObject(new GameObject(name), activationGroups);

        public static ActivationController AddToGameObject(GameObject gameObject, ActivationGroup[] activationGroups)
        {
            if (gameObject == null) return null;
            var controller = gameObject.AddComponent<ActivationController>();
            controller.activationGroups = activationGroups;
            return controller;
        }

        protected virtual void Awake()
        {

            SetActiveGroup(startingActiveGroupIndex);

        }

        [Serializable]
        public struct ActivationGroup
        {

            public string name;

            [Header("Objects")]
            public GameObject[] toActivate;
            public GameObject[] toDeactivate;

            public MonoBehaviour[] toEnable;
            public MonoBehaviour[] toDisable;

            [Header("Events")]
            public UnityEvent OnActivate;
            public UnityEvent OnDeactivate;

        }

        [Header("Settings")]
        public int startingActiveGroupIndex;

        [SerializeField]
        protected ActivationGroup[] activationGroups;

        public int GroupCount => activationGroups == null ? 0 : activationGroups.Length;

        public void SetActiveState(int groupIndex, bool activeState)
        {

            if (groupIndex < 0 || groupIndex >= GroupCount) return;
            var group = activationGroups[groupIndex];

            if (group.toActivate != null) for (int a = 0; a < group.toActivate.Length; a++) if (group.toActivate[a] != null) group.toActivate[a].SetActive(activeState);
            if (group.toDeactivate != null) for (int a = 0; a < group.toDeactivate.Length; a++) if (group.toDeactivate[a] != null) group.toDeactivate[a].SetActive(!activeState);

            if (group.toEnable != null) for (int a = 0; a < group.toEnable.Length; a++) if (group.toEnable[a] != null) group.toEnable[a].enabled = activeState;
            if (group.toDisable != null) for (int a = 0; a < group.toDisable.Length; a++) if (group.toDisable[a] != null) group.toDisable[a].enabled = !activeState;

            if (activeState && group.OnActivate != null) group.OnActivate.Invoke();
            if (!activeState && group.OnDeactivate != null) group.OnDeactivate.Invoke();
             
            OnSetActiveState?.Invoke(groupIndex, activeState);

        }

        protected int activeGroup;
        public int ActiveGroupIndex => activeGroup;
        public ActivationGroup ActiveGroup => GetGroup(activeGroup);

        public ActivationGroup GetGroup(int index) => (activeGroup < 0 || activeGroup >= GroupCount) ? default : activationGroups[index];
        public ActivationGroup GetGroup(string name) => GetGroup(name, out _);
        public ActivationGroup GetGroup(string name, out int index) 
        {

            index = -1;
            if (activationGroups == null || string.IsNullOrEmpty(name)) return default;

            for (int a = 0; a < activationGroups.Length; a++) if (activationGroups[a].name == name) { index = a;  return activationGroups[a]; }
            name = name.AsID();
            for (int a = 0; a < activationGroups.Length; a++) if (!string.IsNullOrEmpty(activationGroups[a].name) && activationGroups[a].name.AsID() == name) { index = a; return activationGroups[a]; }

            return default;

        }
        public string GetGroupName(int index) => GetGroup(index).name;
        public int GetGroupIndex(string name) 
        {
            GetGroup(name, out int index);
            return index;
        }

        /// <summary>
        /// Set the active group (which activates said group), and deactivate all other groups.
        /// </summary>
        public void SetActiveGroup(int groupIndex)
        {

            if (groupIndex < 0 || groupIndex >= GroupCount) return;
            for (int a = 0; a < groupIndex; a++) SetActiveState(a, false);
            for (int a = groupIndex + 1; a < activationGroups.Length; a++) SetActiveState(a, false);

            SetActiveState(groupIndex, true); // Activate active group last in case there are shared objects that got disabled.
            activeGroup = groupIndex;

            OnSetActiveGroup?.Invoke(activeGroup); 

        }

        [Serializable]
        public class OnSetActiveGroupEvent : UnityEvent<int> { }

        [Serializable]
        public class OnSetActiveStateEvent : UnityEvent<int, bool> { }

        [Header("Events")]
        public OnSetActiveGroupEvent OnSetActiveGroup = new OnSetActiveGroupEvent();
        public OnSetActiveStateEvent OnSetActiveState = new OnSetActiveStateEvent();

    }

}

#endif