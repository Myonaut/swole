#if UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEngine.Events;

namespace Swole.UI
{

    /// <summary>
    /// Tag for a window whose layout (visibility, position, size) should be saved and restored by whichever system owns the window layout (e.g. the animation editor's sessions).
    /// Add to any window root that is not already referenced by a field on that system.
    /// </summary>
    [DisallowMultipleComponent]
    public class UITrackedWindow : MonoBehaviour
    {

        [Tooltip("Unique id used when saving the window's state. Defaults to the object's name.")]
        public string id;

        public string ID => string.IsNullOrWhiteSpace(id) ? gameObject.name : id;

        [Tooltip("Invoked after a saved state has been applied to this window, whether the state is open or closed.")]
        public UnityEvent OnStateLoaded = new UnityEvent();

        [Tooltip("Invoked after a saved state has been applied to this window and the state is open. Wire a public open/refresh method here.")]
        public UnityEvent OnOpenStateLoaded = new UnityEvent();

        /// <summary>
        /// Called by the system that applied a saved state to this window.
        /// </summary>
        public void NotifyStateLoaded(bool open)
        {
            OnStateLoaded?.Invoke();
            if (open) OnOpenStateLoaded?.Invoke();
        }

    }

}

#endif
