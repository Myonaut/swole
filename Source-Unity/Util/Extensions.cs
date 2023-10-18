#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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

        public static bool Contains(this RectTransform rectTransform, Vector2 pixelPosition) => rectTransform != null && rectTransform.rect.Contains(rectTransform.InverseTransformPoint(pixelPosition));

        public static Vector2 ScreenToCanvasSpace(this Canvas canvas, Vector2 screenPos) => ScreenToCanvasSpace((RectTransform)canvas.transform, screenPos, canvas.worldCamera);

        public static Vector2 ScreenToCanvasSpace(this RectTransform rectTransform, Vector2 screenPos, Camera camera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPos, camera, out Vector2 localPos);
            return localPos;
        }

        #endregion

    }

}

#endif