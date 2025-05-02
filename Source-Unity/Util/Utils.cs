#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace Swole
{

    public static class Utils
    {

        public static Vector3 MousePositionWorld(Camera camera = null)
        {

            return MousePositionWorld(UnityEngineHook.AsUnityVector(InputProxy.CursorScreenPosition), camera);

        }

        public static Vector3 MousePositionWorld(Vector3 mousePos, Camera camera = null)
        {

            if (camera == null)
            {
                camera = Camera.main;
                if (camera == null) return mousePos;
            }

            mousePos.z = camera.nearClipPlane + 0.001f;

            return camera.ScreenToWorldPoint(mousePos);

        }

        private const string _underscoreStr = "_";
        private const string _periodStr = ".";
        private const string _spaceStr = " ";

        public const string _leftTagInnerUnderscore = "_L_";
        public const string _leftTagSuffixUnderscore = "_L";
        public const string _rightTagInnerUnderscore = "_R_";
        public const string _rightTagSuffixUnderscore = "_R";

        public const string _leftWord = "Left";
        public const string _rightWord = "Right";


        public static string GetMirroredName(string name, string delimiter, bool includeLeft = true, bool includeRight = true) => GetMirroredName(name, _leftTagInnerUnderscore.Replace(_underscoreStr, delimiter), _leftTagSuffixUnderscore.Replace(_underscoreStr, delimiter), _rightTagInnerUnderscore.Replace(_underscoreStr, delimiter), _rightTagSuffixUnderscore.Replace(_underscoreStr, delimiter), includeLeft, includeRight);     
        public static string GetMirroredName(string name, string leftTagInner, string leftTagSuffix, string rightTagInner, string rightTagSuffix, bool includeLeft = true, bool includeRight = true)
        {
            string mirroredName = name;

            if (mirroredName.EndsWith(leftTagSuffix) && includeRight) mirroredName = mirroredName.Substring(0, mirroredName.Length - leftTagSuffix.Length) + rightTagSuffix;
            else
            if (mirroredName.EndsWith(rightTagSuffix) && includeLeft) mirroredName = mirroredName.Substring(0, mirroredName.Length - rightTagSuffix.Length) + leftTagSuffix;
            else
            if (mirroredName.Contains(leftTagInner) && includeRight) mirroredName = mirroredName.Replace(leftTagInner, rightTagInner);
            else
            if (mirroredName.Contains(rightTagInner) && includeLeft) mirroredName = mirroredName.Replace(rightTagInner, leftTagInner);

            return mirroredName;
        }

        public static string GetMirroredNameAdvanced(string name, string leftTag, string rightTag, bool includeLeft = true, bool includeRight = true, bool caseInsensitive = true)
        {
            string mirroredName = name;
            string mirroredNameLower = caseInsensitive ? mirroredName.ToLower() : mirroredName;
            string leftTagLower = caseInsensitive ? leftTag.ToLower() : leftTag;
            string rightTagLower = caseInsensitive ? rightTag.ToLower() : rightTag;

            if (includeRight && mirroredNameLower.StartsWith(leftTagLower))
            {
                // Replace if the tag is at the start
                mirroredName = rightTag + mirroredName.Substring(leftTag.Length);
            }
            else if (includeLeft && mirroredNameLower.StartsWith(rightTagLower))
            {
                // Replace if the tag is at the start
                mirroredName = leftTag + mirroredName.Substring(rightTag.Length);
            }
            else if (includeRight && mirroredNameLower.EndsWith(leftTagLower))
            {
                // Replace if the tag is at the end
                mirroredName = mirroredName.Substring(0, mirroredName.Length - leftTag.Length) + rightTag;
            }
            else if (includeLeft && mirroredNameLower.EndsWith(rightTagLower))
            {
                // Replace if the tag is at the end
                mirroredName = mirroredName.Substring(0, mirroredName.Length - rightTag.Length) + leftTag;
            }
            else if (includeRight && ContainsTagWithConditions(mirroredName, leftTag))
            {
                // Replace if the tag meets the conditions
                mirroredName = ReplaceTagWithConditions(mirroredName, leftTag, rightTag);
            }
            else if (includeLeft && ContainsTagWithConditions(mirroredName, rightTag))
            {
                // Replace if the tag meets the conditions
                mirroredName = ReplaceTagWithConditions(mirroredName, rightTag, leftTag);
            }

            return mirroredName;
        }

        private static bool ContainsTagWithConditions(string name, string tag)
        {
            for (int i = 0; i <= name.Length - tag.Length; i++)
            {
                if (name.Substring(i, tag.Length) == tag)
                {
                    // Check if the tag starts with an uppercase letter
                    if (char.IsUpper(tag[0]))
                    {
                        // Check if the tag is preceded by a non-alphanumeric character or is at the start
                        bool isPrecededByNonAlpha = i == 0 || !char.IsLetterOrDigit(name[i - 1]);

                        // Check if the tag is followed by an uppercase letter
                        bool isFollowedByUppercase = i + tag.Length < name.Length && char.IsUpper(name[i + tag.Length]);

                        if (isPrecededByNonAlpha && isFollowedByUppercase)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static string ReplaceTagWithConditions(string name, string oldTag, string newTag)
        {
            for (int i = 0; i <= name.Length - oldTag.Length; i++)
            {
                if (name.Substring(i, oldTag.Length) == oldTag)
                {
                    // Check if the tag starts with an uppercase letter
                    if (char.IsUpper(oldTag[0]))
                    {
                        // Check if the tag is preceded by a non-alphanumeric character or is at the start
                        bool isPrecededByNonAlpha = i == 0 || !char.IsLetterOrDigit(name[i - 1]);

                        // Check if the tag is followed by an uppercase letter
                        bool isFollowedByUppercase = i + oldTag.Length < name.Length && char.IsUpper(name[i + oldTag.Length]);

                        if (isPrecededByNonAlpha && isFollowedByUppercase)
                        {
                            // Replace the tag
                            return name.Substring(0, i) + newTag + name.Substring(i + oldTag.Length);
                        }
                    }
                }
            }
            return name;
        }

        public static string GetMirroredName(string name, bool includeLeft = true, bool includeRight = true)
        {
            string mirroredName = GetMirroredName(name, _underscoreStr, includeLeft, includeRight); 
            if (mirroredName == name) mirroredName = GetMirroredName(name, _periodStr, includeLeft, includeRight);
            if (mirroredName == name) mirroredName = GetMirroredName(name, _spaceStr, includeLeft, includeRight); 
            if (mirroredName == name) mirroredName = GetMirroredNameAdvanced(mirroredName, _leftWord, _rightWord, includeLeft, includeRight, true);
            
            return mirroredName;
        }

        public delegate void TextureCreationCallback(Texture2D output);  
        /// <summary>
        /// Texture will not be ready immediately so consider passing in a callback function.
        /// </summary>
        public static Texture2D TakeScreenshot(bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0) => TakeScreenshot(null, Vector2.zero, Vector2.one, hideUI, mipMaps, textureWidth, textureHeight);         
        public static Texture2D TakeScreenshot(TextureCreationCallback callbackFunction, bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0) => TakeScreenshot(callbackFunction, Vector2.zero, Vector2.one, hideUI, mipMaps, textureWidth, textureHeight);
        /// <summary>
        /// Texture will not be ready immediately so consider passing in a callback function.
        /// </summary>
        public static Texture2D TakeScreenshot(Vector2 startCoords, Vector2 endCoords, bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0) => TakeScreenshot(null, startCoords, endCoords, hideUI, mipMaps, textureWidth, textureHeight);
        public static Texture2D TakeScreenshot(TextureCreationCallback callbackFunction, Vector2 startCoords, Vector2 endCoords, bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0)
        {

            float startX = Mathf.Clamp01(Mathf.Min(startCoords.x, endCoords.x));
            float startY = Mathf.Clamp01(Mathf.Min(startCoords.y, endCoords.y));

            float endX = Mathf.Clamp01(Mathf.Max(startCoords.x, endCoords.x));
            float endY = Mathf.Clamp01(Mathf.Max(startCoords.y, endCoords.y));   

            var width = Mathf.CeilToInt(Screen.width * (endX - startX));
            var height = Mathf.CeilToInt(Screen.height * (endY - startY));

            width = Mathf.Min(width, Screen.width - Mathf.CeilToInt(startX * Screen.width));
            height = Mathf.Min(height, Screen.height - Mathf.CeilToInt(startY * Screen.height));

            if (width <= 0 || height <= 0) return null; 

            if (textureWidth <= 0) textureWidth = width;
            if (textureHeight <= 0) textureHeight = height;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, mipMaps, false); 
            Rect rect = new Rect(startX * Screen.width, startY * Screen.height, width, height);
             
            Canvas[] canvases = null;
#if BULKOUT_ENV
            RLD.RLDApp rldApp = null;
#endif
            if (hideUI)
            {
                canvases = GameObject.FindObjectsOfType<Canvas>(); 
                for (int a = 0; a < canvases.Length; a++)
                {
                    var canvas = canvases[a];
                    if (canvas == null || !canvas.enabled || !canvas.gameObject.activeInHierarchy)
                    {
                        canvases[a] = null;
                        continue;
                    }

                    canvas.enabled = false;
                }

#if BULKOUT_ENV
                rldApp = RLD.RLDApp.Get;
                if (rldApp != null) rldApp.enabled = false; 
#endif
            }

            IEnumerator WaitForEndOfFrame()
            {
                yield return new WaitForEndOfFrame();

                try
                {
                    tex.ReadPixels(rect, 0, 0); 
                    if (width != textureWidth || height != textureHeight) tex.ResizeAndApply(textureWidth, textureHeight); else tex.Apply(); 

                    callbackFunction?.Invoke(tex);
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex);
                }
                 
                if (hideUI)
                {
                    for (int a = 0; a < canvases.Length; a++) if (canvases[a] != null) canvases[a].enabled = true;

#if BULKOUT_ENV
                    if (rldApp != null) rldApp.enabled = true;
#endif
                }
            }

            CoroutineProxy.Start(WaitForEndOfFrame());

            return tex;
        }

    }

}

#endif
