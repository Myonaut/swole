using System;
using System.Collections.Generic;

namespace Swole.UI
{
    /// <summary>
    /// A serializable-friendly set of window states keyed by window id. Shared by anything that persists window layouts (projects, editor sessions, etc.).
    /// </summary>
    public class ResizableWindowStateCollection
    {

        protected readonly Dictionary<string, ResizableWindowState> states = new Dictionary<string, ResizableWindowState>();

        public int Count => states.Count;

        public bool TryGet(string id, out ResizableWindowState state) => states.TryGetValue(id, out state);

        public ResizableWindowState Get(string id) => states.TryGetValue(id, out var state) ? state : default;

        public void Set(string id, ResizableWindowState state)
        {
            state.id = id;
            states[id] = state;
        }

        public void Clear() => states.Clear();

        public ResizableWindowState[] ToArray()
        {
            var array = new ResizableWindowState[states.Count];
            states.Values.CopyTo(array, 0);
            return array;
        }

        public void SetAll(IEnumerable<ResizableWindowState> newStates)
        {
            states.Clear();
            if (newStates == null) return;
            foreach (var state in newStates) if (!string.IsNullOrWhiteSpace(state.id)) states[state.id] = state;
        }

    }
}
