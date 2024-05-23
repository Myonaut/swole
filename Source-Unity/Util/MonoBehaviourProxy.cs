#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Swole
{
    public class MonoBehaviourProxy : MonoBehaviour
    {
         
        [SerializeField]
        protected UnityEvent onAwake;
        [SerializeField]
        protected UnityEvent onStart;

        [SerializeField]
        protected UnityEvent onUpdate;
        [SerializeField]
        protected UnityEvent onLateUpdate;
        [SerializeField]
        protected UnityEvent onFixedUpdate;

        [SerializeField]
        protected UnityEvent onGUI;

        [SerializeField]
        protected UnityEvent onEnable;
        [SerializeField]
        protected UnityEvent onDisable;

        [SerializeField]
        protected UnityEvent onDestroy;
        [SerializeField]
        protected UnityEvent onQuit;

        protected virtual void Awake()
        {
            onAwake?.Invoke();
        }
        protected virtual void Start()
        {
            onStart?.Invoke();
        }
        protected virtual void Update()
        {
            onUpdate?.Invoke();
        }
        protected virtual void LateUpdate()
        {
            onLateUpdate?.Invoke();
        }
        protected virtual void FixedUpdate()
        {
            onFixedUpdate?.Invoke();
        }
        protected virtual void OnGUI()
        {
            onGUI?.Invoke();
        }
        protected virtual void OnEnable()
        {
            onEnable?.Invoke();
        }
        protected virtual void OnDisable()
        {
            onDisable?.Invoke();
        }
        protected virtual void OnDestroy()
        {
            onDestroy?.Invoke();
            RemoveAllListeners();
        }
        protected virtual void OnApplicationQuit()
        {
            onQuit?.Invoke();
        }

        [Serializable]
        public enum BehaviourEvent
        {
            Awake, Start, Update, LateUpdate, FixedUpdate, GUI, Enable, Disable, Destroy, Quit
        }

        public void Subscribe(BehaviourEvent behaviourEvent, UnityAction action)
        {
            void AddActionToEvent(ref UnityEvent unityEvent)
            {
                if (unityEvent == null) unityEvent = new UnityEvent();
                unityEvent.AddListener(action);
            }

            switch(behaviourEvent)
            {
                case BehaviourEvent.Awake:
                    AddActionToEvent(ref onAwake);
                    break;
                case BehaviourEvent.Start:
                    AddActionToEvent(ref onStart);
                    break;
                case BehaviourEvent.Update:
                    AddActionToEvent(ref onUpdate);
                    break;
                case BehaviourEvent.LateUpdate:
                    AddActionToEvent(ref onLateUpdate);
                    break;
                case BehaviourEvent.FixedUpdate:
                    AddActionToEvent(ref onFixedUpdate);
                    break;
                case BehaviourEvent.GUI:
                    AddActionToEvent(ref onGUI);
                    break;
                case BehaviourEvent.Enable:
                    AddActionToEvent(ref onEnable);
                    break;
                case BehaviourEvent.Disable:
                    AddActionToEvent(ref onDisable);
                    break;
                case BehaviourEvent.Destroy:
                    AddActionToEvent(ref onDestroy);
                    break;
                case BehaviourEvent.Quit:
                    AddActionToEvent(ref onQuit);
                    break;
            }
        }
        public void Unsubscribe(BehaviourEvent behaviourEvent, UnityAction action)
        {
            void RemoveActionFromEvent(ref UnityEvent unityEvent)
            {
                if (unityEvent == null) return;
                unityEvent.RemoveListener(action);
            }

            switch (behaviourEvent)
            {
                case BehaviourEvent.Awake:
                    RemoveActionFromEvent(ref onAwake);
                    break;
                case BehaviourEvent.Start:
                    RemoveActionFromEvent(ref onStart);
                    break;
                case BehaviourEvent.Update:
                    RemoveActionFromEvent(ref onUpdate);
                    break;
                case BehaviourEvent.LateUpdate:
                    RemoveActionFromEvent(ref onLateUpdate);
                    break;
                case BehaviourEvent.FixedUpdate:
                    RemoveActionFromEvent(ref onFixedUpdate);
                    break;
                case BehaviourEvent.GUI:
                    RemoveActionFromEvent(ref onGUI);
                    break;
                case BehaviourEvent.Enable:
                    RemoveActionFromEvent(ref onEnable);
                    break;
                case BehaviourEvent.Disable:
                    RemoveActionFromEvent(ref onDisable);
                    break;
                case BehaviourEvent.Destroy:
                    RemoveActionFromEvent(ref onDestroy);
                    break;
                case BehaviourEvent.Quit:
                    RemoveActionFromEvent(ref onQuit);
                    break;
            }
        }

        public void RemoveAllListeners()
        {
            onAwake?.RemoveAllListeners();
            onAwake = null;

            onStart?.RemoveAllListeners();
            onStart = null;

            onUpdate?.RemoveAllListeners();
            onUpdate = null;

            onLateUpdate?.RemoveAllListeners();
            onLateUpdate = null;

            onFixedUpdate?.RemoveAllListeners();
            onFixedUpdate = null;

            onGUI?.RemoveAllListeners();
            onGUI = null;

            onEnable?.RemoveAllListeners();
            onEnable = null;

            onDisable?.RemoveAllListeners();
            onDisable = null;

            onDisable?.RemoveAllListeners();
            onDisable = null;

            onDestroy?.RemoveAllListeners();
            onDestroy = null;

            onQuit?.RemoveAllListeners();
            onQuit = null;
        }

    }
}

#endif