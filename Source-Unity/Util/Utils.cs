#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
            if (string.IsNullOrWhiteSpace(name)) return name;

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
            if (string.IsNullOrWhiteSpace(name)) return name;

            string mirroredName = name;
            string mirroredNameLower = caseInsensitive ? mirroredName.ToLower() : mirroredName;
            string leftTagLower = caseInsensitive ? leftTag.ToLower() : leftTag;
            string rightTagLower = caseInsensitive ? rightTag.ToLower() : rightTag;

            if (includeRight && mirroredNameLower.StartsWith(leftTagLower))
            {
                mirroredName = MatchTagCaseAndReplace(mirroredName, 0, leftTag.Length, rightTag);
            }
            else if (includeLeft && mirroredNameLower.StartsWith(rightTagLower))
            {
                mirroredName = MatchTagCaseAndReplace(mirroredName, 0, rightTag.Length, leftTag);
            }
            else if (includeRight && mirroredNameLower.EndsWith(leftTagLower))
            {
                int start = mirroredName.Length - leftTag.Length;
                mirroredName = mirroredName.Substring(0, start) + MatchTagCase(mirroredName.Substring(start, leftTag.Length), rightTag);
            }
            else if (includeLeft && mirroredNameLower.EndsWith(rightTagLower))
            {
                int start = mirroredName.Length - rightTag.Length;
                mirroredName = mirroredName.Substring(0, start) + MatchTagCase(mirroredName.Substring(start, rightTag.Length), leftTag);
            }
            else if (includeRight && ContainsTagWithConditions(mirroredName, leftTag, caseInsensitive, out int idx))
            {
                mirroredName = MatchTagCaseAndReplace(mirroredName, idx, leftTag.Length, rightTag);
            }
            else if (includeLeft && ContainsTagWithConditions(mirroredName, rightTag, caseInsensitive, out idx))
            {
                mirroredName = MatchTagCaseAndReplace(mirroredName, idx, rightTag.Length, leftTag);
            }

            return mirroredName;
        }

        private static bool ContainsTagWithConditions(string name, string tag, bool caseInsensitive, out int index)
        {
            string nameCmp = caseInsensitive ? name.ToLower() : name;
            string tagCmp = caseInsensitive ? tag.ToLower() : tag;
            for (int i = 0; i <= name.Length - tag.Length; i++)
            {
                if (nameCmp.Substring(i, tag.Length) == tagCmp)
                {
                    // Check if the tag starts with an uppercase letter
                    if (char.IsUpper(name[i]))
                    {
                        bool isFollowedByUppercaseOrNonAlpha = i + tag.Length < name.Length && (char.IsUpper(name[i + tag.Length]) || !char.IsLetterOrDigit(name[i + tag.Length]));
                        if (isFollowedByUppercaseOrNonAlpha)
                        {
                            index = i;
                            return true;
                        }
                    } 
                    else
                    {
                        bool isPrecededByNonAlpha = i == 0 || !char.IsLetterOrDigit(name[i - 1]);
                        bool isFollowedByUppercaseOrNonAlpha = i + tag.Length < name.Length && (char.IsUpper(name[i + tag.Length]) || !char.IsLetterOrDigit(name[i + tag.Length]));
                        if (isPrecededByNonAlpha && isFollowedByUppercaseOrNonAlpha) 
                        {
                            index = i;
                            return true;
                        }
                    }
                }
            }
            index = -1;
            return false;
        }

        private static string MatchTagCaseAndReplace(string name, int start, int length, string newTag)
        {
            return name.Substring(0, start) + MatchTagCase(name.Substring(start, length), newTag) + name.Substring(start + length);
        }

        private static string MatchTagCase(string oldTag, string newTag)
        {
            char[] result = new char[newTag.Length];
            for (int i = 0; i < newTag.Length; i++)
            {
                if (i < oldTag.Length)
                {
                    result[i] = char.IsUpper(oldTag[i]) ? char.ToUpper(newTag[i]) : char.ToLower(newTag[i]);
                }
                else
                {
                    result[i] = newTag[i];
                }
            }
            return new string(result);
        }

        public static string GetMirroredName(string name, bool includeLeft = true, bool includeRight = true)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

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
        public static Texture2D TakeScreenshot(bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0, Camera camera = null, Color cameraBG_color = default) => TakeScreenshot(null, Vector2.zero, Vector2.one, hideUI, mipMaps, textureWidth, textureHeight, camera, cameraBG_color);         
        public static Texture2D TakeScreenshot(TextureCreationCallback callbackFunction, bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0, Camera camera = null, Color cameraBG_color = default) => TakeScreenshot(callbackFunction, Vector2.zero, Vector2.one, hideUI, mipMaps, textureWidth, textureHeight, camera, cameraBG_color);
        /// <summary>
        /// Texture will not be ready immediately so consider passing in a callback function.
        /// </summary>
        public static Texture2D TakeScreenshot(Vector2 startCoords, Vector2 endCoords, bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0, Camera camera = null, Color cameraBG_color = default) => TakeScreenshot(null, startCoords, endCoords, hideUI, mipMaps, textureWidth, textureHeight, camera, cameraBG_color);
        /// <summary>
        /// For transparent backgrounds make sure camera post processing is turned off.
        /// </summary>
        public static Texture2D TakeScreenshot(TextureCreationCallback callbackFunction, Vector2 startCoords, Vector2 endCoords, bool hideUI = false, bool mipMaps = false, int textureWidth = 0, int textureHeight = 0, Camera camera = null, Color cameraBG_color = default)
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
            Texture2D tex = new Texture2D(width, height, camera == null ? TextureFormat.RGB24 : TextureFormat.ARGB32, mipMaps, false); 
            Rect grabRect = new Rect(startX * Screen.width, startY * Screen.height, width, height);
             
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
                    if (camera != null)
                    {
                        var render_texture = RenderTexture.GetTemporary(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
                        var originalTarget = camera.targetTexture;
                        camera.targetTexture = render_texture;

                        var origClearFlags = camera.clearFlags;
                        var origBGColor = camera.backgroundColor;

                        camera.clearFlags = CameraClearFlags.SolidColor; 
                        camera.backgroundColor = cameraBG_color;
                        camera.Render();

                        RenderTexture.active = render_texture; 

                        tex.ReadPixels(grabRect, 0, 0);
                        camera.targetTexture = originalTarget; 
                        camera.clearFlags = origClearFlags;
                        camera.backgroundColor = origBGColor;
                    } 
                    else
                    {
                        tex.ReadPixels(grabRect, 0, 0);  
                    }

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

        public static bool IsInternalUnityObject(this UnityEngine.Object obj)
        {
            if (obj == null) return false;

            var type = obj.GetType();
            if (typeof(MonoBehaviour).IsAssignableFrom(type)) return false;
            if (typeof(ScriptableObject).IsAssignableFrom(type)) return false;  

            return true;
        }
        public static bool IsInternalUnityObject(object obj)
        {
            if (obj is UnityEngine.Object uObj) return IsInternalUnityObject(uObj);
            return false; 
        }

#if UNITY_EDITOR
        public static void CreateDirectoryFromAssetPath(string assetPath)
        {
            assetPath = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), assetPath);

            string directoryPath = Path.HasExtension(assetPath) ? Path.GetDirectoryName(assetPath) : assetPath; 
            if (Directory.Exists(directoryPath))
                return;

            Directory.CreateDirectory(directoryPath);
            AssetDatabase.Refresh();
        }

        //This method finds the first EditorWindow that's open, and is of the given type.
        //For example, this is how we can search for the "SceneHierarchyWindow" that's currently open (hopefully it *is* actually open).
        public static EditorWindow FindFirst(Type editorWindowType)
        {
            if (editorWindowType == null)
                throw new ArgumentNullException(nameof(editorWindowType));
            if (!typeof(EditorWindow).IsAssignableFrom(editorWindowType))
                throw new ArgumentException("The given type (" + editorWindowType.Name + ") does not inherit from " + nameof(EditorWindow) + ".");

            UnityEngine.Object[] openWindowsOfType = Resources.FindObjectsOfTypeAll(editorWindowType);
            if (openWindowsOfType.Length <= 0)
                return null;

            EditorWindow window = (EditorWindow)openWindowsOfType[0];
            return window;
        }

        /// <summary>
        /// source: https://discussions.unity.com/t/solved-duplicate-prefab-issue/765782/6
        /// </summary>
        public static GameObject DuplicatePrefabInstanceA(GameObject prefabInstance)
        {
            var previousSelection = Selection.objects;
            Selection.activeGameObject = prefabInstance;
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            var duplicate = Selection.activeGameObject;
            Selection.objects = previousSelection;

            return duplicate;
        }
        /// <summary>
        /// source: https://discussions.unity.com/t/solved-duplicate-prefab-issue/765782/6
        /// </summary>
        public static GameObject DuplicatePrefabInstanceB(GameObject prefabInstance)
        {
            UnityEngine.Object[] previousSelection = Selection.objects;
            Selection.objects = new UnityEngine.Object[] { prefabInstance };
            Selection.activeGameObject = prefabInstance;

            //For performance, you might want to cache this Reflection:
            Type hierarchyViewType = Type.GetType("UnityEditor.SceneHierarchyWindow, UnityEditor");
            EditorWindow hierarchyView = FindFirst(hierarchyViewType);

            //Using the Unity Hierarchy View window, we can duplicate our selected objects!
            hierarchyView.SendEvent(EditorGUIUtility.CommandEvent("Duplicate"));

            GameObject clone = Selection.activeGameObject;
            Selection.objects = previousSelection;
            return clone;
        }
#endif

    }

}

#endif
