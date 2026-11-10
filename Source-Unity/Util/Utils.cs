#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Unity.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole
{

    public static class Utils
    {

        public static List<GameObject> GetAllRootObjects(List<GameObject> output = null)
        {
            if (output == null) output = new List<GameObject>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                {
                    scene.GetRootGameObjects(output);
                }
            }

            return output;
        }

        public static Vector3 MousePositionWorld(Camera camera = null, float distance = 0f)
        {

            return MousePositionWorld(UnityEngineHook.AsUnityVector(InputProxy.CursorScreenPosition), camera, distance);

        }

        public static Vector3 MousePositionWorld(Vector3 mousePos, Camera camera = null, float distance = 0f)
        {

            if (camera == null)
            {
                camera = Camera.main;
                if (camera == null) return mousePos;
            }

            mousePos.z = camera.nearClipPlane + 0.001f + Mathf.Max(0f, distance);

            return camera.ScreenToWorldPoint(mousePos);

        }

        #region Mirrored Name Matching

        private static readonly string[] _leftSideTokens = { "left", "l", "lt", "lft" };
        private static readonly string[] _rightSideTokens = { "right", "r", "rt", "rgt" };

        /// <summary>
        /// Returns 1 for a left token, -1 for a right token, 0 if not a side token. Also resolves tokens with a glued side word (e.g. "leftarm"); the remainder is returned in rest.
        /// </summary>
        public static int GetSideOfToken(string token, out string rest)
        {
            rest = string.Empty;
            if (string.IsNullOrEmpty(token)) return 0;

            token = token.ToLowerInvariant();
            if (Array.IndexOf(_leftSideTokens, token) >= 0) return 1;
            if (Array.IndexOf(_rightSideTokens, token) >= 0) return -1;
            foreach (var side in new[] { "left", "right" })
            {
                if (token.Length > side.Length)
                {
                    if (token.StartsWith(side)) { rest = token.Substring(side.Length); return side == "left" ? 1 : -1; }
                    if (token.EndsWith(side)) { rest = token.Substring(0, token.Length - side.Length); return side == "left" ? 1 : -1; }
                }
            }
            return 0;
        }

        /// <summary>
        /// True if the two tokens are opposite side tokens (case-insensitive) with otherwise identical content.
        /// </summary>
        public static bool IsSideTokenPair(string a, string b)
        {
            int sideA = GetSideOfToken(a, out string restA);
            int sideB = GetSideOfToken(b, out string restB);
            return sideA != 0 && sideB != 0 && sideA == -sideB && restA == restB;
        }

        /// <summary>
        /// Splits a name on non-alphanumerics, camelCase boundaries and letter/digit boundaries.
        /// </summary>
        public static List<string> TokenizeName(string name)
        {
            var spans = TokenizeNameSpans(name);
            var tokens = new List<string>(spans.Count);
            foreach (var span in spans) tokens.Add(name.Substring(span.start, span.length));
            return tokens;
        }

        /// <summary>
        /// Same as <see cref="TokenizeName"/> but returns the position of each token within the original name.
        /// </summary>
        public static List<(int start, int length)> TokenizeNameSpans(string name)
        {
            var spans = new List<(int start, int length)>();
            if (string.IsNullOrEmpty(name)) return spans;

            int start = -1;
            void Flush(int end) { if (start >= 0) { spans.Add((start, end - start)); start = -1; } }

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (!char.IsLetterOrDigit(c)) { Flush(i); continue; }
                if (start >= 0)
                {
                    char p = name[i - 1];
                    bool boundary = (char.IsLetter(p) != char.IsLetter(c))
                        || (char.IsLower(p) && char.IsUpper(c))
                        || (char.IsUpper(p) && char.IsUpper(c) && i + 1 < name.Length && char.IsLower(name[i + 1]) && i - start > 1);
                    if (boundary) Flush(i);
                }
                if (start < 0) start = i;
            }
            Flush(name.Length);
            return spans;
        }

        private static bool TryGetOppositeSideToken(string token, bool includeLeft, bool includeRight, out int side, out string mirrored)
        {
            side = 0;
            mirrored = null;

            string lower = token.ToLowerInvariant();
            string opposite = null;
            switch (lower)
            {
                case "l": side = 1; opposite = "r"; break;
                case "r": side = -1; opposite = "l"; break;
                case "left": side = 1; opposite = "right"; break;
                case "right": side = -1; opposite = "left"; break;
                default:
                    // Glued side word, e.g. "leftarm" / "armright"
                    if (lower.Length > 4 && lower.StartsWith("left", StringComparison.Ordinal)) { side = 1; opposite = "right" + lower.Substring(4); }
                    else if (lower.Length > 5 && lower.StartsWith("right", StringComparison.Ordinal)) { side = -1; opposite = "left" + lower.Substring(5); }
                    else if (lower.Length > 4 && lower.EndsWith("left", StringComparison.Ordinal)) { side = 1; opposite = lower.Substring(0, lower.Length - 4) + "right"; }
                    else if (lower.Length > 5 && lower.EndsWith("right", StringComparison.Ordinal)) { side = -1; opposite = lower.Substring(0, lower.Length - 5) + "left"; }
                    break;
            }

            if (side == 0 || (side == 1 && !includeRight) || (side == -1 && !includeLeft)) { side = 0; return false; }

            mirrored = MatchTagCase(token, opposite);
            return true;
        }

        /// <summary>
        /// Mirrors the side tokens (l/r/left/right, case-insensitive, any delimiter or camelCase) of a name. Every side token is mirrored independently (e.g. armL_side_right becomes armR_side_left).
        /// Returns the name unchanged if no mirrorable token exists.
        /// </summary>
        public static string GetMirroredNameTokenized(string name, bool includeLeft = true, bool includeRight = true)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

            var spans = TokenizeNameSpans(name);
            var replacements = new List<(int start, int length, string text)>();
            foreach (var span in spans)
            {
                if (!TryGetOppositeSideToken(name.Substring(span.start, span.length), includeLeft, includeRight, out _, out string mirrored)) continue;
                replacements.Add((span.start, span.length, mirrored));
            }

            if (replacements.Count == 0) return name;

            var sb = new System.Text.StringBuilder(name.Length + 4);
            int pos = 0;
            foreach (var r in replacements)
            {
                sb.Append(name, pos, r.start - pos).Append(r.text);
                pos = r.start + r.length;
            }
            sb.Append(name, pos, name.Length - pos);
            return sb.ToString();
        }

        /// <summary>
        /// Finds the mirrored counterpart of each name in the given set. Names are compared token by token; shared tokens are accumulated and the single differing token pair
        /// must be a left/right side pair (case-insensitive). Ambiguous matches are skipped. The returned map contains both directions.
        /// </summary>
        public static Dictionary<string, string> FindMirroredNames(IList<string> names)
        {
            var result = new Dictionary<string, string>();
            var tokens = new List<string>[names.Count];
            for (int a = 0; a < names.Count; a++) tokens[a] = TokenizeName(names[a]);

            for (int a = 0; a < names.Count; a++)
            {
                if (names[a] == null || result.ContainsKey(names[a])) continue;

                int match = -1;
                bool ambiguous = false;
                for (int b = 0; b < names.Count; b++)
                {
                    if (a == b || names[b] == null || names[a] == names[b] || tokens[a].Count != tokens[b].Count || tokens[a].Count == 0) continue;

                    int differing = -1;
                    bool valid = true;
                    for (int t = 0; t < tokens[a].Count; t++)
                    {
                        if (string.Equals(tokens[a][t], tokens[b][t], StringComparison.OrdinalIgnoreCase)) continue;
                        if (differing >= 0) { valid = false; break; }
                        differing = t;
                    }
                    if (!valid || differing < 0 || !IsSideTokenPair(tokens[a][differing], tokens[b][differing])) continue;

                    if (match >= 0) { ambiguous = true; break; }
                    match = b;
                }

                if (match >= 0 && !ambiguous && !result.ContainsKey(names[match]))
                {
                    result[names[a]] = names[match];
                    result[names[match]] = names[a];
                }
            }

            return result;
        }

        /// <summary>
        /// Builds a name-to-mirrored-name map for the given names. Names without a counterpart in the set fall back to GetMirroredName
        /// if the resulting name is not already in use.
        /// </summary>
        public static Dictionary<string, string> BuildMirroredNameMap(IList<string> names, bool useFallback = true)
        {
            var map = FindMirroredNames(names);
            if (!useFallback) return map;

            var used = new HashSet<string>(names);
            foreach (var n in names)
            {
                if (n == null || map.ContainsKey(n)) continue;
                string mirrored = GetMirroredName(n);
                if (mirrored != n && !used.Contains(mirrored)) map[n] = mirrored;
            }
            return map;
        }

        #endregion

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

            if (includeRight && mirroredName.EndsWith(leftTagSuffix, StringComparison.Ordinal)) mirroredName = mirroredName.Substring(0, mirroredName.Length - leftTagSuffix.Length) + rightTagSuffix;
            else
            if (includeLeft && mirroredName.EndsWith(rightTagSuffix, StringComparison.Ordinal)) mirroredName = mirroredName.Substring(0, mirroredName.Length - rightTagSuffix.Length) + leftTagSuffix;
            else
            if (includeRight && mirroredName.Contains(leftTagInner)) mirroredName = mirroredName.Replace(leftTagInner, rightTagInner);
            else
            if (includeLeft && mirroredName.Contains(rightTagInner)) mirroredName = mirroredName.Replace(rightTagInner, leftTagInner);

            return mirroredName;
        }

        public static string GetMirroredNameAdvanced(string name, string leftTag, string rightTag, bool includeLeft = true, bool includeRight = true, bool caseInsensitive = true)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

            string mirroredName = name;
            string mirroredNameLower = caseInsensitive ? mirroredName.ToLowerInvariant() : mirroredName;
            string leftTagLower = caseInsensitive ? leftTag.ToLowerInvariant() : leftTag;
            string rightTagLower = caseInsensitive ? rightTag.ToLowerInvariant() : rightTag;

            if (includeRight && mirroredNameLower.StartsWith(leftTagLower, StringComparison.Ordinal) && IsTagEndBoundary(mirroredName, leftTag.Length))
            {
                mirroredName = MatchTagCaseAndReplace(mirroredName, 0, leftTag.Length, rightTag);
            }
            else if (includeLeft && mirroredNameLower.StartsWith(rightTagLower, StringComparison.Ordinal) && IsTagEndBoundary(mirroredName, rightTag.Length))
            {
                mirroredName = MatchTagCaseAndReplace(mirroredName, 0, rightTag.Length, leftTag);
            }
            else if (includeRight && mirroredNameLower.EndsWith(leftTagLower, StringComparison.Ordinal) && IsTagStartBoundary(mirroredName, mirroredName.Length - leftTag.Length))
            {
                int start = mirroredName.Length - leftTag.Length;
                mirroredName = mirroredName.Substring(0, start) + MatchTagCase(mirroredName.Substring(start, leftTag.Length), rightTag);
            }
            else if (includeLeft && mirroredNameLower.EndsWith(rightTagLower, StringComparison.Ordinal) && IsTagStartBoundary(mirroredName, mirroredName.Length - rightTag.Length))
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

        // The character after the tag (at index 'end') must not continue a lowercase word.
        private static bool IsTagEndBoundary(string name, int end) => end >= name.Length || char.IsUpper(name[end]) || !char.IsLetterOrDigit(name[end]);
        // The tag starting at 'start' must be preceded by a non-alphanumeric or itself start a new camelCase word.
        private static bool IsTagStartBoundary(string name, int start) => start <= 0 || !char.IsLetterOrDigit(name[start - 1]) || char.IsUpper(name[start]);

        private static string MatchTagCase(string oldTag, string newTag)
        {
            if (oldTag.Length > 1 && oldTag.ToUpperInvariant() == oldTag && oldTag.ToLowerInvariant() != oldTag) return newTag.ToUpperInvariant();

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

            string mirroredName = GetMirroredNameTokenized(name, includeLeft, includeRight);
            if (mirroredName != name) return mirroredName;

            mirroredName = GetMirroredName(name, _underscoreStr, includeLeft, includeRight); 
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
#if UNITY_6000_0_OR_NEWER
                canvases = GameObject.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
                canvases = GameObject.FindObjectsOfType<Canvas>(); 
#endif
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

        public static long PrintNativeAllocationSizes(string name, object instance, List<object> refList = null, bool printTotal = true)
        {
            if (instance == null) return 0;

            if (printTotal) Debug.Log($"######### PRINTING NATIVE ALLOCATIONS FOR {name}");

            long total = 0;

            if (refList == null) refList = new List<object>();
            refList.Add(instance);

            var fields = instance.GetType().GetFields(BindingFlags.Instance |
                                                 BindingFlags.Public |
                                                 BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                var fieldVal = field.GetValue(instance);
                if (!typeof(IDisposable).IsAssignableFrom(fieldType) || !typeof(IEnumerable).IsAssignableFrom(fieldType))
                {
                    if (fieldType.IsClass)
                    {
                        bool flag = true;
                        foreach (var ref_ in refList)
                        {
                            if (ReferenceEquals(fieldVal, ref_))
                            {
                                flag = false;
                                break;
                            }
                        }

                        if (flag && !ReferenceEquals(fieldVal, null)) total += PrintNativeAllocationSizes($"{name}.{field.Name}", fieldVal, refList, false);
                    }

                    continue;
                }

                Type underlyingType = null;
                if (fieldType.IsGenericType)
                {
                    underlyingType = fieldType.GetGenericArguments()[0];
                }
                if (underlyingType == null) continue;

                int elementSize = Marshal.SizeOf(underlyingType);

                try
                {

                    var subProps = fieldType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in subProps)
                    {
                        if (prop.Name != "Length") continue;

                        int length = (int)prop.GetValue(fieldVal);

                        long byteSize = elementSize * length;
                        Debug.Log($"{name}.{field.Name}[length:{length}] -> ({((byteSize / 1024.0) / 1024.0).ToString("#.##")} mb) ({byteSize} bytes)");

                        total += byteSize;
                        break;
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error accessing native container: {field.Name} for {name}"); 
                    Debug.LogException(ex);
                }
            } 

            if (printTotal) Debug.Log($"{name} TOTAL: ({((total / 1024.0) / 1024.0).ToString("#.##")} mb) ({total} bytes)");
            if (printTotal) Debug.Log("##################");
            return total;
        }

        public static void TransferMaterialProperties(Material referenceMaterial, Material targetMaterial, IEnumerable<MaterialPropertyTransfer> materialPropertyTransfers)
        {
            foreach (var transfer in materialPropertyTransfers) TransferMaterialProperties(referenceMaterial, targetMaterial, transfer);
        }
        public static void TransferMaterialProperties(Material referenceMaterial, Material targetMaterial, MaterialPropertyTransfer materialPropertyTransfer)
        {
            if (referenceMaterial == null || targetMaterial == null) return;
            switch(materialPropertyTransfer.propertyType)
            {
                case MaterialPropertyType.Texture:
                    targetMaterial.SetTexture(materialPropertyTransfer.targetPropertyName, referenceMaterial.GetTexture(materialPropertyTransfer.referencePropertyName)); 
                    break;

                case MaterialPropertyType.Float:
                    targetMaterial.SetFloat(materialPropertyTransfer.targetPropertyName, referenceMaterial.GetFloat(materialPropertyTransfer.referencePropertyName));
                    break;

                case MaterialPropertyType.Int:
                    targetMaterial.SetInt(materialPropertyTransfer.targetPropertyName, referenceMaterial.GetInt(materialPropertyTransfer.referencePropertyName));
                    break;

                case MaterialPropertyType.Color:
                    targetMaterial.SetColor(materialPropertyTransfer.targetPropertyName, referenceMaterial.GetColor(materialPropertyTransfer.referencePropertyName));
                    break;

                case MaterialPropertyType.Vector4:
                    targetMaterial.SetVector(materialPropertyTransfer.targetPropertyName, referenceMaterial.GetVector(materialPropertyTransfer.referencePropertyName));
                    break;
            }
        }

    }

    [Serializable]
    public enum MaterialPropertyType
    {
        Texture, Float, Int, Color, Vector4
    }
    [Serializable]
    public struct MaterialPropertyTransfer
    {
        public MaterialPropertyType propertyType;
        public string referencePropertyName;
        public string targetPropertyName;
    }

}

#endif
