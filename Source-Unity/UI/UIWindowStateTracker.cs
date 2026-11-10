#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Swole.UI
{

    /// <summary>
    /// Keeps a ResizableWindowStateCollection entry up to date with a window's visibility, position and size. 
    /// Works for any window regardless of how it is moved, resized, opened or closed, so it does not depend on UIPopup/UIResizer events.
    /// </summary>
    public class UIWindowStateTracker : MonoBehaviour
    {

        protected string id;
        protected ResizableWindowStateCollection collection;
        protected RectTransform canvasRT;
        protected Action onChanged;
        protected Func<bool> isValid;

        protected RectTransform rt;
        protected Vector3 lastPosition;
        protected Vector2 lastSize;
        protected bool quitting;
        protected int lastSiblingIndex;

        protected static readonly List<UIWindowStateTracker> activeTrackers = new List<UIWindowStateTracker>();

        protected static int restoreDepth;
        /// <summary>
        /// While a restore is in progress, trackers do not record. Applying a state toggles visibility, which would otherwise overwrite saved data (including saved order) with the live, not yet restored, values.
        /// </summary>
        public static void BeginRestore() => restoreDepth++;
        public static void EndRestore() => restoreDepth = Mathf.Max(0, restoreDepth - 1);

        public readonly struct RestoreScope : IDisposable
        {
            public void Dispose() => EndRestore();
        }
        /// <summary>
        /// Begins a restore that ends when the returned scope is disposed, e.g. "using var scope = UIWindowStateTracker.BeginRestoreScope();".
        /// </summary>
        public static RestoreScope BeginRestoreScope()
        {
            BeginRestore();
            return default;
        }

        protected static List<(string id, RectTransform window)> GetPeers(ResizableWindowStateCollection collection)
        {
            var list = new List<(string, RectTransform)>();
            foreach (var t in activeTrackers) if (t != null && t.collection == collection && t.rt != null) list.Add((t.id, t.rt));
            return list;
        }

        /// <summary>
        /// Stores each window's rank among the windows that share its parent. Entries must already exist in the collection. Returns true if anything changed.
        /// </summary>
        public static bool RecordOrder(ResizableWindowStateCollection collection, IEnumerable<(string id, RectTransform window)> windows)
        {
            bool changed = false;
            foreach (var group in windows.Where(w => w.window != null).GroupBy(w => w.window.parent))
            {
                int rank = 0;
                foreach (var (id, window) in group.OrderBy(w => w.window.GetSiblingIndex()))
                {
                    rank++;
                    if (!collection.TryGet(id, out var state) || state.relativeOrder == rank) continue;
                    state.relativeOrder = rank;
                    collection.Set(id, state);
                    changed = true;
                }
            }
            return changed;
        }

        /// <summary>
        /// Reorders windows that share a parent to match their saved relative order. They are placed into the sibling slots the windows already occupy, so other siblings keep their positions.
        /// </summary>
        public static void ApplyOrder(ResizableWindowStateCollection collection, IEnumerable<(string id, RectTransform window)> windows)
        {
            foreach (var group in windows.Where(w => w.window != null).GroupBy(w => w.window.parent))
            {
                var parent = group.Key;
                if (parent == null) continue;

                var ordered = new List<(RectTransform window, int order)>();
                foreach (var (id, window) in group)
                {
                    if (collection.TryGet(id, out var state) && state.relativeOrder > 0) ordered.Add((window, state.relativeOrder));
                }
                if (ordered.Count < 2) continue;

                var slots = ordered.Select(o => o.window.GetSiblingIndex()).OrderBy(i => i).ToList();
                var sorted = ordered.OrderBy(o => o.order).Select(o => o.window).ToList();

                var children = new List<Transform>();
                for (int i = 0; i < parent.childCount; i++) children.Add(parent.GetChild(i));
                for (int i = 0; i < slots.Count; i++) children[slots[i]] = sorted[i];
                for (int i = 0; i < children.Count; i++) children[i].SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// Applies saved relative order to every window currently tracked with the collection.
        /// </summary>
        public static void ApplyOrder(ResizableWindowStateCollection collection) => ApplyOrder(collection, GetPeers(collection));

        /// <summary>
        /// Begins tracking a window. If the collection already holds a state for the id it is applied to the window first.
        /// </summary>
        /// <param name="openOnRestore">If true, the window is forced visible when a saved state is applied (use when the window is being opened).</param>
        /// <param name="isValid">Optional. When it returns false the tracker detaches itself.</param>
        public static UIWindowStateTracker Track(RectTransform window, string id, ResizableWindowStateCollection collection, RectTransform canvasRT = null, bool openOnRestore = false, Action onChanged = null, Func<bool> isValid = null)
        {
            if (window == null || collection == null || string.IsNullOrWhiteSpace(id)) return null;

            var tracker = window.GetComponent<UIWindowStateTracker>();
            if (tracker == null) tracker = window.gameObject.AddComponent<UIWindowStateTracker>();

            tracker.rt = window;
            tracker.id = id;
            tracker.collection = collection;
            tracker.canvasRT = canvasRT;
            tracker.onChanged = onChanged;
            tracker.isValid = isValid;
            if (!activeTrackers.Contains(tracker)) activeTrackers.Add(tracker);

            if (collection.TryGet(id, out var state))
            {
                if (openOnRestore) state.visible = true;
                BeginRestore();
                try
                {
                    state.ApplyWindowState(window, canvasRT);
                    collection.Set(id, state);
                    ApplyOrder(collection);
                }
                finally { EndRestore(); }

                var tag = window.GetComponent<UITrackedWindow>();
                if (tag != null) tag.NotifyStateLoaded(state.visible);
            }
            else
            {
                tracker.Record(window.gameObject.activeSelf, false);
            }

            tracker.Snapshot();
            return tracker;
        }

        /// <summary>
        /// Stops tracking without altering the collection.
        /// </summary>
        public void Untrack()
        {
            activeTrackers.Remove(this);
            collection = null;
            onChanged = null;
            isValid = null;
        }

        protected void Snapshot()
        {
            if (rt == null) return;
            lastPosition = rt.position;
            lastSize = rt.rect.size;
            lastSiblingIndex = rt.GetSiblingIndex();
        }

        protected bool Validate()
        {
            if (collection == null) return false;
            if (isValid != null && !isValid())
            {
                Untrack();
                return false;
            }
            return true;
        }

        protected void Record(bool visible, bool notify = true)
        {
            if (restoreDepth > 0 || !Validate() || rt == null) return;

            var state = rt.AsResizableWindowState(visible, canvasRT);
            if (collection.TryGet(id, out var existing))
            {
                state.dataIntA = existing.dataIntA;
                state.dataStringA = existing.dataStringA;
                state.relativeOrder = existing.relativeOrder;
            }
            collection.Set(id, state);
            RecordOrder(collection, GetPeers(collection));

            if (notify) onChanged?.Invoke();
        }

        protected void OnEnable()
        {
            if (collection == null) return;
            Record(true);
            Snapshot();
        }

        protected void OnDisable()
        {
            if (collection == null || quitting || !gameObject.scene.isLoaded) return;
            if (!gameObject.activeSelf) Record(false); // Only a real close counts; a deactivated parent or destruction does not mark the window as closed
        }

        /// <summary>
        /// Records the window as closed and stops tracking. Use for windows that close by being destroyed.
        /// </summary>
        public void MarkClosed()
        {
            Record(false);
            Untrack();
        }

        protected void OnDestroy() => activeTrackers.Remove(this);

        protected void OnApplicationQuit() => quitting = true;

        protected void LateUpdate()
        {
            if (collection == null || rt == null || restoreDepth > 0) return;
            var position = rt.position;
            var size = rt.rect.size;
            if (position == lastPosition && size == lastSize)
            {
                if (rt.GetSiblingIndex() == lastSiblingIndex) return;
                lastSiblingIndex = rt.GetSiblingIndex();
                if (Validate() && RecordOrder(collection, GetPeers(collection))) onChanged?.Invoke();
                return;
            }

            Snapshot();
            Record(true);
        }

    }

}

#endif
