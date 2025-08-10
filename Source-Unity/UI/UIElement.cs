#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{

    public abstract class UIElement : MonoBehaviour
    {

        protected Canvas canvas;
        public Canvas Canvas
        {
            get
            {
                if (canvas == null) canvas = GetComponentInParent<Canvas>(true);
                return canvas;
            }
        }
        protected Transform canvasTransform;
        public Transform CanvasTransform
        {
            get
            {
                if (Canvas == null) return null;
                return canvas.transform;
            }
        }

        protected RectTransform rectTransform;

        public RectTransform RectTransform => rectTransform;

        [SerializeField, Tooltip("The parent element to transfer update calls to, if any.")]
        private UIElement parent;

        [SerializeField, Tooltip("Subscribe to parent element's update method.")]
        private bool useParentUpdate = false;
        [SerializeField, Tooltip("Update just before the parent element's update call (if subscribed).")]
        private bool parentPreUpdate = false;

        [SerializeField, Tooltip("Subscribe to parent element's late update method.")]
        private bool useParentLateUpdate = false;
        [SerializeField, Tooltip("Update just before the parent element's late update call (if subscribed).")]
        private bool parentPreLateUpdate = false;

        [SerializeField, Tooltip("Subscribe to parent element's OnGUI method.")]
        private bool useParentOnGUI = false;
        [SerializeField, Tooltip("Update just before the parent element's OnGUI call (if subscribed).")]
        private bool parentPreOnGUI = false;

        public delegate void UpdateDelegate();

        protected event UpdateDelegate PreUpdateEvent;
        protected event UpdateDelegate PostUpdateEvent;

        protected event UpdateDelegate PreLateUpdateEvent;
        protected event UpdateDelegate PostLateUpdateEvent;

        protected event UpdateDelegate PreOnGUIEvent;
        protected event UpdateDelegate PostOnGUIEvent;

        protected void Awake()
        {

            rectTransform = gameObject.AddOrGetComponent<RectTransform>();

            AwakeBody();

        }

        protected void AwakeBody()
        {

            AwakeLocal();

        }

        public virtual void AwakeLocal() { }

        protected void OnEnable()
        {

            OnEnableBody();

        }

        protected void OnEnableBody()
        {

            if (parent == this) parent = null;

            if (parent != null)
            {

                if (useParentUpdate)
                {

                    if (parentPreUpdate) parent.SubscribePreUpdate(UpdateBody); else parent.SubscribePostUpdate(UpdateBody);

                }

                if (useParentLateUpdate)
                {

                    if (parentPreLateUpdate) parent.SubscribePreLateUpdate(LateUpdateBody); else parent.SubscribePostLateUpdate(LateUpdateBody);

                }

                if (useParentOnGUI)
                {

                    if (parentPreOnGUI) parent.SubscribePreOnGUI(OnGUIBody); else parent.SubscribePostOnGUI(OnGUIBody);

                }

            }

            OnEnableLocal();

        }

        public virtual void OnEnableLocal() { }

        protected void OnDisable()
        {

            if (parent != null)
            {

                if (useParentUpdate)
                {

                    if (parentPreUpdate) parent.UnsubscribePreUpdate(UpdateBody); else parent.UnsubscribePostUpdate(UpdateBody);

                }

                if (useParentLateUpdate)
                {

                    if (parentPreLateUpdate) parent.UnsubscribePreLateUpdate(LateUpdateBody); else parent.UnsubscribePostLateUpdate(LateUpdateBody);

                }

                if (useParentOnGUI)
                {

                    if (parentPreOnGUI) parent.UnsubscribePreOnGUI(OnGUIBody); else parent.UnsubscribePostOnGUI(OnGUIBody);

                }

            }

            OnDisableBody();

        }

        protected void OnDisableBody()
        {

            OnDisableLocal();

        }

        public virtual void OnDisableLocal() { }

        protected void OnDestroy()
        {

            OnDestroyBody();

        }

        protected void OnDestroyBody()
        {

            OnDestroyLocal();

            PreUpdateEvent = null;
            PostLateUpdateEvent = null;

            PreLateUpdateEvent = null;
            PostLateUpdateEvent = null;

            PreOnGUIEvent = null;
            PostOnGUIEvent = null;

        }

        public virtual void OnDestroyLocal() { }

        public void SubscribePreUpdate(UpdateDelegate func)
        {

            PreUpdateEvent += func;

        }

        public void UnsubscribePreUpdate(UpdateDelegate func)
        {

            PreUpdateEvent -= func;

        }

        public void SubscribePostUpdate(UpdateDelegate func)
        {

            PostUpdateEvent += func;

        }

        public void UnsubscribePostUpdate(UpdateDelegate func)
        {

            PostUpdateEvent -= func;

        }

        public void SubscribePreLateUpdate(UpdateDelegate func)
        {

            PreLateUpdateEvent += func;

        }

        public void UnsubscribePreLateUpdate(UpdateDelegate func)
        {

            PreLateUpdateEvent -= func;

        }

        public void SubscribePostLateUpdate(UpdateDelegate func)
        {

            PostLateUpdateEvent += func;

        }

        public void UnsubscribePostLateUpdate(UpdateDelegate func)
        {

            PostLateUpdateEvent -= func;

        }

        public void SubscribePreOnGUI(UpdateDelegate func)
        {

            PreOnGUIEvent += func;

        }

        public void UnsubscribePreOnGUI(UpdateDelegate func)
        {

            PreOnGUIEvent -= func;

        }

        public void SubscribePostOnGUI(UpdateDelegate func)
        {

            PostOnGUIEvent += func;

        }

        public void UnsubscribePostOnGUI(UpdateDelegate func)
        {

            PostOnGUIEvent -= func;

        }

        protected void UpdateBody()
        {

            PreUpdateEvent?.Invoke();

            UpdateLocal();

            PostUpdateEvent?.Invoke();

        }

        protected void Update()
        {

            if (parent == null || !useParentUpdate) UpdateBody();

        }

        public virtual void UpdateLocal() { }

        protected void LateUpdateBody()
        {

            PreLateUpdateEvent?.Invoke();

            LateUpdateLocal();

            PostLateUpdateEvent?.Invoke();

        }

        protected void LateUpdate()
        {

            if (parent == null || !useParentLateUpdate) LateUpdateBody();

        }

        public virtual void LateUpdateLocal() { }

        protected void OnGUIBody()
        {

            PreOnGUIEvent?.Invoke();

            OnGUILocal();

            PostOnGUIEvent?.Invoke();

        }

        protected void OnGUI()
        {

            if (parent == null || !useParentOnGUI) OnGUIBody();

        }

        public virtual void OnGUILocal() { }

    }

}

#endif
