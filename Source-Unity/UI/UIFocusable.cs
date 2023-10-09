#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;

namespace Swole.UI
{

    public class UIFocusable : UIElement, IFocusable, IPointerEnterHandler, IPointerExitHandler
    {

        /// <summary>
        /// Optional array of proxy objects that activate this element when the cursor enters them.
        /// </summary>
        public PointerEventsProxy[] pointerProxies = new PointerEventsProxy[0];

        public override void AwakeLocal()
        {

            base.AwakeLocal();

            if (pointerProxies != null)
            {

                foreach (PointerEventsProxy proxy in pointerProxies)
                {

                    proxy.OnEnterEvent += OnPointerEnter;
                    proxy.OnExitEvent += OnPointerExit;

                }

            }

        }

        public override void OnDestroyLocal()
        {

            base.OnDestroyLocal();

            if (pointerProxies != null)
            {

                foreach (PointerEventsProxy proxy in pointerProxies)
                {

                    proxy.OnEnterEvent -= OnPointerEnter;
                    proxy.OnExitEvent -= OnPointerExit;

                }

                pointerProxies = null;

            }

        }

        protected int focusState;

        public bool IsInFocus => focusState > 0;

        public void AddFocus() { focusState++; }

        public void SubtractFocus() { focusState--; }

        protected bool localFocusState = false;

        public virtual void OnPointerEnter(PointerEventData eventData)
        {

            if (!localFocusState)
            {

                localFocusState = true;

                AddFocus();

            }

        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {

            if (localFocusState)
            {

                localFocusState = false;

                SubtractFocus();

            }

        }

    }

}

#endif
