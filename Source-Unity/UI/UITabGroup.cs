#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.UI
{

    public class UITabGroup : MonoBehaviour
    {

        [SerializeField, Tooltip("The tab that is activated by default")]
        private int defaultTabIndex;
        public int DefaultTabIndex
        {

            get
            {

                if (defaultTabIndex < 0 || defaultTabIndex >= Count || buttons[defaultTabIndex] == null) defaultTabIndex = 0;

                return defaultTabIndex;

            }

            set
            {

                defaultTabIndex = buttons == null || buttons.Count <= 0 ? 0 : Mathf.Clamp(value, 0, buttons.Count - 1);

            }

        }

        [Tooltip("Should the group allow no tabs to be active?")]
        public bool allowNullActive = false;

        public List<UITabButton> buttons = new List<UITabButton>();
        public int Count => buttons == null ? 0 : buttons.Count;

        protected virtual void ToggleButtons(UITabButton active, bool updateActive = true)
        {

            if (buttons == null) return;

            if (updateActive) 
            {

                if (active == null && !allowNullActive && buttons.Count > 0) active = buttons[DefaultTabIndex];
                
                active?.ToggleOn(); 
            
            }

            foreach (UITabButton button in buttons)
            {
                if (button == null || button == active) continue;
                button.ToggleOff();
            }

        }

        protected virtual void Awake()
        {

            if (buttons == null) buttons = new List<UITabButton>();

        } 

        protected virtual void Start()
        {

            buttons.RemoveAll(i => i == null);

            foreach (UITabButton button in buttons)
            {

                if (button.OnClick == null) button.OnClick = new UnityEvent();

                button.OnClick.AddListener(() =>
                {

                    ToggleButtons(button, false);

                });

            }

            if (allowNullActive)
            {

                ToggleButtons(null, false);

            }
            else
            {


                ToggleButtons(buttons[0]);

            }

        }

    }

}

#endif
