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

        [SerializeField]
        private List<UITabButton> buttons = new List<UITabButton>();
        public int Count => buttons == null ? 0 : buttons.Count;
        public UITabButton GetButton(int index)
        {
            if (index < 0 || index > Count) return null;
            return buttons[index];
        }

        public void Add(GameObject button)
        {
            if (button == null) return;
            Add(button.GetComponentInChildren<UITabButton>(true));
        }
        public void Add(UITabButton button)
        {
            if (button == null) return;
            if (!buttons.Contains(button)) buttons.Add(button);
            if (button.group != null) button.group.Remove(button);
            button.group = this;
        }
        public void Remove(GameObject button)
        {
            if (button == null) return;
            Remove(button.GetComponentInChildren<UITabButton>(true));
        }
        public void Remove(UITabButton button)
        {
            buttons.RemoveAll(i => i == button);
            if (button != null && button.group == this) button.group = null;
        }

        public virtual void ToggleButtons(int active, bool updateActive = true) => ToggleButtons(GetButton(active), updateActive);
        public virtual void ToggleButtons(UITabButton active, bool updateActive = true)
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
                if (button.group != null && button.group != this) button.group.Remove(button);
                button.group = this;
            }

            if (allowNullActive)
            {

                ToggleButtons(null, false);

            }
            else if (buttons.Count > 0)
            {


                ToggleButtons(buttons[0]);

            }

        }

    }

}

#endif
