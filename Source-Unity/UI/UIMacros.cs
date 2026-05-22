#if UNITY_2017_1_OR_NEWER

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Replaces text that has been encapsulated by the given tag with specified id. E.g <tag="id">text to replace</tag>
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="linkId">The tag's id to look for.</param>
        /// <param name="replacement">The string that will replace the source.</param>
        /// <param name="tag">Defaults to 'link' which is a text mesh pro tag that has an id and is not rendered.</param>
        /// <param name="replaceEntireSourceIfNotFound">Should the source string be replaced entirely if the tag isn't found? Defaults to false.</param>
        /// <returns></returns>
        public static string ReplaceTaggedContent(this string source, string id, string replacement, string tag = "link", bool replaceEntireSourceIfNotFound = false)
        {
            var output = source;
            if (!string.IsNullOrWhiteSpace(source))
            {
                string pattern = $"<{tag}=\"{id}\">.*?\\</{tag}\\>";
                string replacementBoundaries = $"<{tag}=\"{id}\">{replacement}</{tag}>";

                output = Regex.Replace(source, pattern, replacementBoundaries);
            }

            if (replaceEntireSourceIfNotFound && output == source) output = replacement;

            return output;
        }

    }

}

#endif