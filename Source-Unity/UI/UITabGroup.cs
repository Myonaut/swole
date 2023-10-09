#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

namespace Swole.UI
{

    public class UITabGroup : MonoBehaviour
    {

        public bool allowNullActive = false;

        public UITabButton[] buttons;

        private void ToggleButtons(UITabButton active, bool updateActive = true)
        {

            if (updateActive) active.ToggleOn();

            foreach (UITabButton button in buttons)
            {

                if (button == active) continue;

                button.ToggleOff();

            }

        }

        private void Start()
        {

            foreach (UITabButton button in buttons)
            {

                UITabButton instance = button;

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
