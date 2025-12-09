#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;

using Swole.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Swole
{

    public static class Extensions
    {

        #region Serialization

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootTransform">If there are references to game objects or components, this is used to find instantiated equivalents in a hierarchy.</param>
        public static void CopySerializedDataTo(this UnityEngine.Object objA, UnityEngine.Object objB, Transform originalRootTransform = null, Transform rootTransform = null)
        {
            void CopyFields(IEnumerable<FieldInfo> fields, object objA, object objB)
            {
                foreach (var field in fields)
                {
                    if (rootTransform != null)
                    {
                        if (typeof(Component).IsAssignableFrom(field.FieldType))
                        {
                            var val = field.GetValue(objA);
                            if (val is Component c && c != null)
                            {
                                var obj = ReferenceEquals(c.transform, originalRootTransform) ? rootTransform : rootTransform.FindDeepChild(c.name);
                                if (obj != null)
                                {
                                    c = obj.gameObject.GetComponent(field.FieldType);
                                    field.SetValue(objB, c);
                                }
                            }

                            continue;
                        }
                        else if (typeof(GameObject).IsAssignableFrom(field.FieldType))
                        {
                            var val = field.GetValue(objA);
                            if (val is GameObject go && go != null)
                            {
                                var obj = ReferenceEquals(go.transform, originalRootTransform) ? rootTransform : rootTransform.FindDeepChild(go.name);
                                if (obj != null)
                                {
                                    go = obj.gameObject;
                                    field.SetValue(objB, go);
                                }
                            }

                            continue;
                        }
                        else if (field.FieldType.IsClass && !field.FieldType.IsPrimitive && field.FieldType != typeof(string))
                        {
                            var nestedObjA = field.GetValue(objA);
                            var nestedObjB = field.GetValue(objB);
                            if (nestedObjA != null)
                            {
                                if (nestedObjB == null)
                                {
                                    if (field.FieldType.IsArray)
                                    {
                                        var elementType = field.FieldType.GetElementType();
                                        var arrayA = (Array)nestedObjA;
                                        var arrayB = Array.CreateInstance(elementType, arrayA.Length);

                                        if (!elementType.IsArray)
                                        {
                                            for (int i = 0; i < arrayA.Length; i++)
                                            {
                                                var elementA = arrayA.GetValue(i);
                                                object elementB;
                                                try
                                                {
                                                    elementB = elementA != null ? Activator.CreateInstance(elementA.GetType()) : null;
                                                }
                                                catch
                                                {
                                                    elementB = null;
                                                }

                                                if (elementA != null)
                                                {
                                                    var elementFields = elementA.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                        .Where(f => f.GetCustomAttribute<NonSerializedAttribute>() == null);
                                                    CopyFields(elementFields, elementA, elementB);
                                                }
                                                arrayB.SetValue(elementB, i);
                                            }
                                        }
                                        nestedObjB = arrayB;
                                    }
                                    else if (typeof(IList).IsAssignableFrom(field.FieldType))
                                    {
                                        var listA = (IList)nestedObjA;
                                        var listB = (IList)Activator.CreateInstance(field.FieldType);
                                        foreach (var elementA in listA)
                                        {
                                            if (elementA != null && !elementA.GetType().IsArray)
                                            {
                                                object elementB;
                                                try
                                                {
                                                    elementB = elementA != null ? Activator.CreateInstance(elementA.GetType()) : null;
                                                }
                                                catch
                                                {
                                                    elementB = null;
                                                }

                                                if (elementA != null)
                                                {
                                                    var elementFields = elementA.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                                        .Where(f => f.GetCustomAttribute<NonSerializedAttribute>() == null);
                                                    CopyFields(elementFields, elementA, elementB);
                                                }
                                                listB.Add(elementB);
                                            }
                                        }
                                        nestedObjB = listB;
                                    }
                                    else
                                    {
                                        try
                                        {
                                            nestedObjB = Activator.CreateInstance(field.FieldType);
                                        }
                                        catch { }
                                    }
                                }

                                var nestedFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                    .Where(f => f.GetCustomAttribute<NonSerializedAttribute>() == null);
                                CopyFields(nestedFields, nestedObjA, nestedObjB);

                                field.SetValue(objB, nestedObjB);

                                continue;
                            }
                        }
                        else if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && field.FieldType != typeof(string))
                        {
                            var nestedObjA = field.GetValue(objA);
                            var nestedObjB = field.GetValue(objB);
                            if (nestedObjA != null && nestedObjB != null)
                            {
                                var nestedFields = field.FieldType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                    .Where(f => f.GetCustomAttribute<NonSerializedAttribute>() == null);
                                CopyFields(nestedFields, nestedObjA, nestedObjB);

                                // Set the modified struct back to the field
                                field.SetValue(objB, nestedObjB);

                                continue;
                            }
                        }
                    }

                    field.SetValue(objB, field.GetValue(objA));
                }
            }

            var publicFields = objA.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public).Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null);
            var nonPublicFields = objA.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(field => field.GetCustomAttribute<NonSerializedAttribute>() == null && field.GetCustomAttribute<SerializeField>() != null);

            CopyFields(publicFields, objA, objB);
            CopyFields(nonPublicFields, objA, objB);
        }

#if UNITY_EDITOR
        public static T CreateOrReplaceAsset<T>(this T asset, string path) where T : UnityEngine.Object
        {
            Utils.CreateDirectoryFromAssetPath(path);

            T existingAsset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(asset, path);
            }
            else
            {
                if (typeof(Mesh).IsAssignableFrom(typeof(T))) { (existingAsset as Mesh)?.Clear(); }
                EditorUtility.CopySerialized(asset, existingAsset);
                asset = existingAsset;
            }

            return asset;
        }
#endif

        public static string CreateUnityAssetPathString(string savePath, string assetName, string assetExtension)
        {
            if (assetExtension.StartsWith(".")) assetExtension = assetExtension.Substring(1);
            return (savePath.StartsWith("Assets") ? "" : $"Assets{System.IO.Path.DirectorySeparatorChar}") + savePath + (savePath.EndsWith($"{System.IO.Path.DirectorySeparatorChar}") ? "" : $"{System.IO.Path.DirectorySeparatorChar}") + (string.IsNullOrWhiteSpace(assetExtension) ? assetName : $"{assetName}.{assetExtension}");
        }
        public static string CreateUnityAssetPathString(this UnityEngine.Object obj, string saveDir, string assetExtension = null)
        {
            if (obj == null) return null;
            if (string.IsNullOrWhiteSpace(assetExtension))
            {
                assetExtension = "asset";

                if (typeof(Material).IsAssignableFrom(obj.GetType()))
                {
                    assetExtension = "mat";
                }
                else if (typeof(Cubemap).IsAssignableFrom(obj.GetType()))
                {
                    assetExtension = "cubemap";
                }
                else if (typeof(GUISkin).IsAssignableFrom(obj.GetType()))
                {
                    assetExtension = "GUISkin";
                }
                else if (typeof(AnimationClip).IsAssignableFrom(obj.GetType()))
                {
                    assetExtension = "anim";
                }
            }

            return CreateUnityAssetPathString(saveDir, obj.name, assetExtension);
        }

        #endregion

        public static T AddOrGetComponent<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp == null) comp = gameObject.AddComponent<T>();

            return comp;

        }

        public static bool RemoveComponent<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp != null)
            {

                UnityEngine.Object.Destroy(comp);

                return true;

            }

            return false;

        }

        public static bool RemoveComponent<T>(this GameObject gameObject, Type type)
        {

            if (!type.IsSubclassOf(typeof(Component))) return false;

            Component comp = gameObject.GetComponent(type);

            if (comp != null)
            {

                UnityEngine.Object.Destroy(comp);

                return true;

            }

            return false;

        }

        public static bool RemoveComponentImmediate<T>(this GameObject gameObject) where T : Component
        {

            T comp = gameObject.GetComponent<T>();

            if (comp != null)
            {

                UnityEngine.Object.DestroyImmediate(comp);

                return true;

            }

            return false;

        }

        public static bool RemoveComponentImmediate<T>(this GameObject gameObject, Type type)
        {

            if (!type.IsSubclassOf(typeof(Component))) return false;

            Component comp = gameObject.GetComponent(type);

            if (comp != null)
            {

                UnityEngine.Object.DestroyImmediate(comp);

                return true;

            }

            return false;

        }

        /// <summary>
        /// Find the first available component in a child's transform hierarchy. Will check all hierarchical layers of the root and use the first child with childName, if found.
        /// </summary>
        public static T FindFirstComponentUnderChild<T>(this GameObject root, string childName, bool includeInactive = true)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return default;
            return root.transform.FindFirstComponentUnderChild<T>(childName, includeInactive);
        }
        /// <summary>
        /// Find the first available component in a child's transform hierarchy. Will check all hierarchical layers of the root and use the first child with childName, if found.
        /// </summary>
        public static T FindFirstComponentUnderChild<T>(this Transform root, string childName, bool includeInactive = true)
        {
            if (root == null || string.IsNullOrEmpty(childName)) return default;
            var child = root.FindDeepChildLiberal(childName);
            if (child == null) return default;
            return child.GetComponentInChildren<T>(includeInactive);
        }

        /// <summary>
        /// Find the first available component in childB's hierarchy. Looks for childA under root, then looks for childB under childA.
        /// </summary>
        public static T FindFirstComponentUnderChildsChild<T>(this GameObject root, string childNameA, string childNameB, bool includeInactive = true)
        {
            if (root == null || string.IsNullOrEmpty(childNameA) || string.IsNullOrEmpty(childNameB)) return default;
            return root.transform.FindFirstComponentUnderChildsChild<T>(childNameA, childNameB, includeInactive);
        }
        /// <summary>
        /// Find the first available component in childB's hierarchy. Looks for childA under root, then looks for childB under childA.
        /// </summary>
        public static T FindFirstComponentUnderChildsChild<T>(this Transform root, string childNameA, string childNameB, bool includeInactive = true)
        {
            if (root == null || string.IsNullOrEmpty(childNameA) || string.IsNullOrEmpty(childNameB)) return default;
            var childA = root.FindDeepChildLiberal(childNameA);
            if (childA == null) return default;
            var childB = childA.FindDeepChildLiberal(childNameB);
            if (childB == null) return default;
            return childB.GetComponentInChildren<T>(includeInactive);
        }

        /// <summary>
        /// Set the GameObject's layer and the layer of all children.
        /// </summary>
        public static void SetLayerAllChildren(this GameObject gameObject, int layer)
        {
            if (gameObject == null) return;

            gameObject.layer = layer;
            var children = gameObject.GetComponentsInChildren<Transform>(true);
            foreach (var child in children) child.gameObject.layer = layer;
        }

        public static Matrix4x4 CalculateViewProjectionMatrix(this Camera camera) => camera.projectionMatrix * camera.worldToCameraMatrix;

        public static Vector2 ScaleFactor(this Canvas canvas)
        {

            return canvas.transform.localScale;

        }

        public static int GetChildDepth(this Transform child, Transform parent = null)
        {

            int depth = 0;

            Transform parent_ = child.parent;

            while (parent_ != null && parent_ != parent)
            {

                depth++;

                parent_ = parent_.parent;

            }

            return depth;

        }

        public static bool IsInHierarchy(this Transform child, Transform topLevelTransform)
        {

            if (child == topLevelTransform) return true;

            while (true)
            {

                if (child.parent == null) break;

                if (child.parent == topLevelTransform) return true;

                child = child.parent;

            }

            return false;

        }

        public static bool IsInHierarchy(this List<Transform> children, Transform topLevelTransform)
        {

            if (children == null) return false;

            foreach (Transform child in children) if (child != null && child.IsInHierarchy(topLevelTransform)) return true;

            return false;

        }

        public static bool IsInHierarchy(this List<GameObject> children, Transform topLevelTransform)
        {

            if (children == null) return false;

            foreach (GameObject child in children) if (child != null && child.transform.IsInHierarchy(topLevelTransform)) return true;

            return false;

        }

        public static Transform FindDeepChild(this Transform aParent, string aName, bool includeSelf = true)
        {

            if (includeSelf) if (aParent.name == aName) return aParent;

            var result = aParent.Find(aName);
            if (result != null)
                return result;
            foreach (Transform child in aParent)
            {
                result = FindDeepChild(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }
        public static Transform FindDeepChildLiberal(this Transform aParent, string aName, bool includeSelf = true)
        {

            aName = aName.ToLower().Trim();
            if (includeSelf) if (aParent.name.ToLower().Trim() == aName) return aParent;

            for (int a = 0; a < aParent.childCount; a++)
            {
                var child = aParent.GetChild(a);
                if (child == null) continue;
                if (child.name.ToLower().Trim() == aName) return child;
            }
            foreach (Transform child in aParent)
            {
                Transform result = FindDeepChildLiberal(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }
        public static Transform FindTopLevelChild(this Transform parent, string name)
        {
            for (int a = 0; a < parent.childCount; a++)
            {
                var child = parent.GetChild(a);
                if (child.name == name) return child;
            }
            return null;
        }
        public static Transform FindActive(this Transform parent, string name)
        {
            if (!parent.gameObject.activeSelf) return null;
            for (int a = 0; a < parent.childCount; a++)
            {
                var child = parent.GetChild(a);
                if (child.gameObject.activeSelf && child.name == name) return child;
            }
            for (int a = 0; a < parent.childCount; a++)
            {
                var child = parent.GetChild(a);
                if (!child.gameObject.activeSelf) continue;
                var res = FindActive(child, name);
                if (res != null) return res;
            }
            return null;
        }
        public static Transform FindActiveDeepChild(this Transform aParent, string aName, bool includeSelf = true)
        {
            if (!aParent.gameObject.activeSelf) return null;
            if (includeSelf) if (aParent.name == aName) return aParent;

            var result = aParent.FindActive(aName);
            if (result != null)
                return result;
            foreach (Transform child in aParent)
            {
                result = FindActiveDeepChild(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }
        public static Transform FindActiveDeepChildLiberal(this Transform aParent, string aName, bool includeSelf = true)
        {

            if (!aParent.gameObject.activeSelf) return null;
            aName = aName.ToLower().Trim();
            if (includeSelf) if (aParent.name.ToLower().Trim() == aName) return aParent;

            for (int a = 0; a < aParent.childCount; a++)
            {
                var child = aParent.GetChild(a);
                if (child == null || !child.gameObject.activeSelf) continue;
                if (child.name.ToLower().Trim() == aName) return child;
            }
            foreach (Transform child in aParent)
            {
                Transform result = FindActiveDeepChildLiberal(child, aName, false);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static Transform FindAny(this Transform aParent, string aName, bool includeSelf = true, bool activeOnly = false)
        {

            return activeOnly ? FindActiveDeepChild(aParent, aName, includeSelf) : FindDeepChild(aParent, aName, includeSelf);

        }

        public static Transform FindAnyLiberal(this Transform aParent, string aName, bool includeSelf = true, bool activeOnly = false)
        {

            return activeOnly ? FindActiveDeepChild(aParent, aName, includeSelf) : FindDeepChildLiberal(aParent, aName, includeSelf);

        }

        public static bool IsChildOf(this Transform child, Transform parent)
        {

            if (child.parent == parent) return true;

            if (child.parent == null) return false;

            return child.parent.IsChildOf(parent);

        }

        public static string GetPathString(this Transform transform, bool ignoreTopTransform = false, string separator = "/", Transform root = null)
        {

            string path = transform.name;

            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + separator + path;

                if (root != null && ReferenceEquals(transform, root)) break;
            }

            if (ignoreTopTransform)
            {
                int firstSep = path.IndexOf(separator);
                if (firstSep < 0)
                {
                    path = string.Empty;
                }
                else
                {
                    path = path.Substring(firstSep + 1);
                }
            }

            return path;

        }

        public static Transform GetTransformByPath(string path, Transform root = null)
        {
            if (string.IsNullOrEmpty(path)) return root;

            if (root == null)
            {
                var obj = GameObject.Find(path);
                return obj == null ? null : obj.transform;
            }

            return root.Find(path);
        }

        public static Transform GetTransformByPath(this Transform root, string path) => GetTransformByPath(path, root);

        public static void ApplyRotationAroundPivot(this Transform transform, Quaternion rotation, Vector3 pivot)
        {
            Vector3 fromPivotToPos = transform.position - pivot;

            transform.rotation = rotation * transform.rotation;
            fromPivotToPos = rotation * fromPivotToPos;
            transform.position = pivot + fromPivotToPos;
        }

        public static void ForceUpdateLayouts(this GameObject gameObject)
        {

            if (gameObject == null) return;

            LayoutGroup[] layouts = gameObject.GetComponentsInParent<LayoutGroup>();

            foreach (LayoutGroup layout in layouts) if (layout != null)
                {

                    layout.CalculateLayoutInputHorizontal();
                    layout.CalculateLayoutInputVertical();
                    layout.SetLayoutHorizontal();
                    layout.SetLayoutVertical();

                }

        }

        public static void ForceUpdateLayouts(this Transform transform)
        {

            if (transform == null) return;

            transform.gameObject.ForceUpdateLayouts();

        }

        public static void ForceUpdateLayouts(this RectTransform transform)
        {

            if (transform == null) return;

            transform.gameObject.ForceUpdateLayouts();

        }

        public static Vector3[] ToVector3(this float3[] data)
        {

            if (data == null) return null;

            Vector3[] newData = new Vector3[data.Length];

            for (int a = 0; a < data.Length; a++) newData[a] = data[a];

            return newData;

        }

        public static float3[] ToFloat3(this Vector3[] data)
        {

            if (data == null) return null;

            float3[] newData = new float3[data.Length];

            for (int a = 0; a < data.Length; a++) newData[a] = data[a];

            return newData;

        }

        public static List<T> Shuffle<T>(this List<T> list)
        {
            var count = list.Count;
            var last = count - 1;
            for (var i = 0; i < last; ++i)
            {
                var r = UnityEngine.Random.Range(i, count);
                var tmp = list[i];
                list[i] = list[r];
                list[r] = tmp;
            }

            return list;
        }
        public static List<T> InsertRandomly<T>(this List<T> list, T element)
        {
            var count = list.Count;
            var r = UnityEngine.Random.Range(0, count + 1);

            if (r >= count)
            {
                list.Add(element);
            }
            else
            {
                list.Insert(r, element);
            }

            return list;
        }

        public static int GetCount<T>(this IEnumerable<T> obj)
        {
            if (obj is ICollection<T> collection) return collection.Count;

            int count = 0;
            foreach (var _ in obj) count++;

            return count;
        }
        public static T[] AsManagedArray<T>(this IEnumerable<T> obj)
        {
            if (obj is T[] array) return array;
            if (obj is ICollection<T> collection) return collection.ToArray();

            int count = 0;
            foreach (var _ in obj) count++;
            array = new T[count];
            count = 0;
            foreach (var item in obj)
            {
                array[count] = item;
                count++;
            }

            return array;
        }

        #region UI

        public static bool ContainsWorldPosition(this RectTransform rectTransform, Vector2 worldPosition) => rectTransform != null && rectTransform.rect.Contains(rectTransform.InverseTransformPoint(worldPosition));
        public static bool ContainsScreenPosition(this RectTransform rectTransform, Vector2 screenPosition)
        {
            if (rectTransform == null) return false;

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.transform.TransformPoint(ScreenToCanvasSpace(canvas, screenPosition));
            }

            return ContainsWorldPosition(rectTransform, screenPosition);
        }

        public static Vector3 ViewportToCanvasSpace(this Canvas canvas, Vector3 viewportPosition)
        {
            var centerBasedViewPortPosition = viewportPosition - new Vector3(0.5f, 0.5f, 0);
            var canvasRect = canvas.GetComponent<RectTransform>();
            var scale = canvasRect.sizeDelta;
            return Vector3.Scale(centerBasedViewPortPosition, scale);
        }
        public static Vector3 WorldToCanvasSpace(this Canvas canvas, Vector3 worldPosition, Camera camera = null)
        {
            if (camera == null)
            {
                camera = Camera.main;
            }
            var viewportPosition = camera.WorldToViewportPoint(worldPosition);
            return canvas.ViewportToCanvasSpace(viewportPosition);
        }
        public static Vector2 ScreenToCanvasSpace(this Canvas canvas, Vector2 screenPos) => ScreenToCanvasSpace((RectTransform)canvas.transform, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);

        public static Vector2 ScreenToCanvasSpace(this RectTransform rectTransform, Vector2 screenPos, Camera camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, camera, out Vector2 localPos);
            return localPos;
        }

        public static Vector2 GetNormalizedPointFromWorldPosition(this RectTransform t, Vector3 worldPos) => GetNormalizedPointFromLocalPosition(t, t.InverseTransformPoint(worldPos));
        public static Vector2 GetNormalizedPointFromLocalPosition(this RectTransform t, Vector3 localPos) => Rect.PointToNormalized(t.rect, localPos);

        public static Vector3 GetWorldPositionFromNormalizedPoint(this RectTransform t, Vector2 normalizedPoint) => t.TransformPoint(GetLocalPositionFromNormalizedPoint(t, normalizedPoint));
        public static Vector3 GetLocalPositionFromNormalizedPoint(this RectTransform t, Vector2 normalizedPoint) => Rect.NormalizedToPoint(t.rect, normalizedPoint);

        public static void ApplyWindowState(this ResizableWindowState state, RectTransform rt, RectTransform canvasRT = null)
        {
            if (canvasRT == null)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                canvasRT = canvas == null ? null : canvas.GetComponent<RectTransform>();
            }

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, state.width);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, state.height);
            rt.position = canvasRT == null ? UnityEngineHook.AsUnityVector(state.positionInCanvas) : canvasRT.GetWorldPositionFromNormalizedPoint(UnityEngineHook.AsUnityVector(state.positionInCanvas));
            rt.gameObject.SetActive(state.visible);
        }

        public static ResizableWindowState AsResizableWindowState(this RectTransform rt, RectTransform canvasRT = null) => AsResizableWindowState(rt, rt.gameObject.activeSelf, canvasRT);
        public static ResizableWindowState AsResizableWindowState(this RectTransform rt, bool visible, RectTransform canvasRT = null)
        {
            if (canvasRT == null)
            {
                var canvas = rt.GetComponentInParent<Canvas>();
                canvasRT = canvas == null ? null : canvas.GetComponent<RectTransform>();
            }

            var rect = rt.rect;
            return new ResizableWindowState() { positionInCanvas = canvasRT == null ? UnityEngineHook.AsSwoleVector(rt.position) : UnityEngineHook.AsSwoleVector(canvasRT.GetNormalizedPointFromWorldPosition(rt.position)), width = rect.width, height = rect.height, visible = visible };
        }

        public static void FitImageInParent(this Image imageRenderer)
        {
            var aspectRatio = imageRenderer.sprite == null || imageRenderer.sprite.texture == null ? 1 : (imageRenderer.sprite.texture.width / (float)imageRenderer.sprite.texture.height);

            var fitter = imageRenderer.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.aspectRatio = aspectRatio;
                return;
            }

            var rt = imageRenderer.GetComponent<RectTransform>();
            var parentRT = rt.parent.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition3D = Vector3.zero;
            rt.sizeDelta = Vector2.zero;
            var rect = rt.rect;
            var rectParent = parentRT.rect;
            if (aspectRatio > 1)
            {
                float h = rect.width / aspectRatio;
                float w = rect.width;
                float resize = Mathf.Max(0, h - rectParent.height);
                h = h - resize;
                w = w - resize;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }
            else
            {
                float h = rect.height;
                float w = rect.height * aspectRatio;
                float resize = Mathf.Max(0, w - rectParent.width);
                h = h - resize;
                w = w - resize;
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            }
        }

        #endregion

        public static Color Opaque(this Color color)
        {
            color.a = 1;
            return color;
        }
        public static Color Transparent(this Color color, float alpha)
        {
            color.a = alpha;
            return color; 
        }

    }
}

#endif