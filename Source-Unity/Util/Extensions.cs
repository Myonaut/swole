#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;

namespace Swole
{

    public static class Extensions
    {

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

        public static Vector2 ScaleFactor(this Canvas canvas)
        {

            return canvas.transform.localScale;

        }

        public static int GetChildDepth(this Transform child, Transform parent)
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

        public static Transform FindAny(this Transform aParent, string aName, bool includeSelf = true)
        {

            return FindDeepChild(aParent, aName, includeSelf);

        }

        public static Transform FindAnyLiberal(this Transform aParent, string aName, bool includeSelf = true)
        {

            return FindDeepChildLiberal(aParent, aName, includeSelf);

        }

        public static bool IsChildOf(this Transform child, Transform parent)
        {

            if (child.parent == parent) return true;

            if (child.parent == null) return false;

            return child.parent.IsChildOf(parent);

        }

        public static string GetPathString(this Transform transform)
        {

            string path = transform.gameObject.name;

            while (transform.parent != null)
            {

                transform = transform.parent;

                path = transform.gameObject.name + "/" + path;

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

        public static Vector2 ScreenToCanvasSpace(this Canvas canvas, Vector2 screenPos) => ScreenToCanvasSpace((RectTransform)canvas.transform, screenPos, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera);

        public static Vector2 ScreenToCanvasSpace(this RectTransform rectTransform, Vector2 screenPos, Camera camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, camera, out Vector2 localPos);
            return localPos;
        }

        #endregion

    }

}

#endif