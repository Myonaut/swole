#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole.UI
{

    public static class UIMacros
    {

        public static LTDescr Resize(this RectTransform transform, Vector2 sizeDelta, float time = 0.1f, LeanTweenType ease = LeanTweenType.easeInExpo)
        {

            return transform.LeanSize(sizeDelta, time).setEase(ease);

        }

        public static LTDescr Rescale(this RectTransform transform, Vector3 scale, float time = 0.1f, LeanTweenType ease = LeanTweenType.easeInExpo)
        {

            return transform.LeanScale(scale, time).setEase(ease);

        }

        public static LTDescr DelayedCall(this System.Action action, float delay = 0.7f)
        {

            return LeanTween.delayedCall(delay, action);

        }

    }

}

#endif