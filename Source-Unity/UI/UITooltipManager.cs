#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.UI
{

    public class UITooltipManager : SingletonBehaviour<UITooltipManager>
    {

        // Refrain from update calls
        public override bool ExecuteInStack => false;
        public override void OnUpdate() { }
        public override void OnLateUpdate() { }
        public override void OnFixedUpdate() { }
        //

        protected override void OnInit()
        {

            if (tooltipCanvas == null) tooltipCanvas = gameObject.GetComponent<Canvas>();

        }

        public Canvas tooltipCanvas;

        public static Canvas TooltipCanvas => Instance.tooltipCanvas;

        public bool hasTooltipCanvas => tooltipCanvas != null;

        public static bool HasTooltipCanvas => Instance.hasTooltipCanvas;

    }

}

#endif
