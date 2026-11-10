#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{
    /// <summary>
    /// Supplies named, game-agnostic input channels (e.g. "exertion", "load", "load.L", "breathlessness", "heartRate") to systems like <see cref="CharacterMuscleAutoFlexer"/> and <see cref="CharacterBreathing"/>.
    /// Implement this in game-specific code to feed game state into generic character systems without them knowing about the game.
    /// </summary>
    public interface IAutoFlexInputProvider
    {
        /// <summary>Returns true if the channel is supplied by this provider.</summary>
        bool TryGetAutoFlexInput(string channel, out float value);
    }

    public static class AutoFlexInputUtility
    {
        /// <summary>Queries the providers in order and returns the first value found.</summary>
        public static bool TryGetInput(List<MonoBehaviour> providers, string channel, out float value)
        {
            value = 0f;
            if (providers == null || string.IsNullOrEmpty(channel)) return false;
            for (int i = 0; i < providers.Count; i++)
            {
                if (providers[i] is IAutoFlexInputProvider provider && providers[i] != null && providers[i].isActiveAndEnabled && provider.TryGetAutoFlexInput(channel, out value)) return true;
            }
            value = 0f;
            return false;
        }

        /// <summary>Fills the list with provider components found on the root and its children (excluding the given component).</summary>
        public static void FindProviders(Component root, List<MonoBehaviour> providers, Component exclude = null)
        {
            if (root == null || providers == null) return;
            var found = root.GetComponentsInChildren<IAutoFlexInputProvider>(true);
            foreach (var p in found)
            {
                if (p is MonoBehaviour mb && !ReferenceEquals(mb, exclude) && !providers.Contains(mb)) providers.Add(mb);
            }
        }
    }
}

#endif
