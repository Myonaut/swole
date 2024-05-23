#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Jobs;

using Swole.Animation;

namespace Swole.API.Unity.Animation
{
    [Serializable]
    public class CustomAnimationLayer : IAnimationLayer
    {

        protected bool m_disposed;
        public bool Valid => !m_disposed;

        public void Dispose()
        {

            m_disposed = true;
            if (m_animationPlayers != null)
            {

                foreach (var set in m_animationPlayers)
                {

                    var players = set.Value;
                    if (players == null) continue;

                    foreach (var player in players) player?.Dispose();

                }

            }
            m_animationPlayers = null;

        }

        public bool DisposeIfHasPrefix(string prefix)
        {

            bool dispose = name == null ? false : name.ToLower().Trim().StartsWith(prefix);

            if (dispose) Dispose();

            return dispose;

        }

        private const int loopLimit = 8192;

        public IAnimationLayer NewInstance(IAnimator animator, IAnimationController animationController = null)
        {
            if (animationController == null && animator != null) animationController = animator.DefaultController;

            CustomAnimationLayer layer = new CustomAnimationLayer(animator);

            layer.name = name;
            layer.isAdditive = isAdditive;
            layer.mix = mix;

            layer.stateMachines = new CustomStateMachine[stateMachines == null ? 0 : stateMachines.Length];
            if (stateMachines != null) for (int a = 0; a < stateMachines.Length; a++) if (stateMachines[a] != null) layer.stateMachines[a] = stateMachines[a].NewInstance(layer);

            List<CustomMotionController> newMotionControllers = new List<CustomMotionController>();
            if (m_motionControllers != null && m_motionControllers.Length > 0) // Instantiating from an already instantiated layer
            {

                for (int a = 0; a < m_motionControllers.Length; a++)
                {

                    if (m_motionControllers[a] != null) newMotionControllers.Add((CustomMotionController)m_motionControllers[a].Clone()); else newMotionControllers.Add(null);

                }

            }
            else if (motionControllerIdentifiers != null && animationController != null)  // Instantiating from a prototype layer
            {

                List<MotionControllerIdentifier> identifiers = new List<MotionControllerIdentifier>(motionControllerIdentifiers); // Allows non nested motion controllers to keep the same index
                for (int a = 0; a < motionControllerIdentifiers.Length; a++) // Add identifiers of nested motion controllers first
                {

                    var identifier = motionControllerIdentifiers[a];
                    var motionController = animationController.GetMotionController(identifier);

                    if (motionController == null) continue;

                    motionController.GetChildIndexIdentifiers(identifiers);

                }

                Dictionary<MotionControllerIdentifier, int> childIndexRemapper = new Dictionary<MotionControllerIdentifier, int>();
                for (int a = 0; a < identifiers.Count; a++) // Find and clone all referenced motion controllers using the collection of identifiers
                {

                    var identifier = identifiers[a];
                    var motionController = animationController.GetMotionController(identifier);

                    if (motionController != null) newMotionControllers.Add((CustomMotionController)motionController.Clone()); else newMotionControllers.Add(null); // Clone the motion controller reference. Non nested motion controlls will have the exact same index as their identifier

                    childIndexRemapper[identifier] = newMotionControllers.Count - 1; // Keep track of the index of this newly cloned motion controller

                }

                foreach (var controller in newMotionControllers) if (controller != null) controller.RemapChildIndices(childIndexRemapper, true); // Update nested motion controller indices to their cloned equivalents. Non nested motion controllers will already have the same index as their identifier, as they are added to the list first.

            }

            // Checks for deep references to the indices provided in the list
            bool IsCyclic(List<int> indices, CustomMotionController controller, string inPath, out string outPath)
            {

                outPath = inPath;
                List<int> children = new List<int>();
                controller.GetChildIndices(children);

                foreach (int child in children)
                {

                    if (child < 0 || child >= newMotionControllers.Count) continue;
                    var childController = newMotionControllers[child];
                    if (childController == null || !childController.HasChildControllers) continue;
                    if (indices.Contains(child)) return true;

                }

                List<int> combined = new List<int>(children);
                combined.AddRange(indices);

                for (int a = 0; a < children.Count; a++)
                {

                    int childIndex = children[a];
                    if (childIndex < 0 || childIndex >= newMotionControllers.Count) continue;
                    var childController = newMotionControllers[childIndex];
                    if (childController == null || !controller.HasChildControllers) continue;

                    if (IsCyclic(combined, childController, inPath + "/" + childController.name, out outPath)) return true;

                }

                return false;

            }

            for (int a = 0; a < newMotionControllers.Count; a++) // Check for circular references and nullify perpetrators. Could cause infinite invocation if not accounted for.
            {

                var controller = newMotionControllers[a];
                if (controller == null || !controller.HasChildControllers) continue;

                if (IsCyclic(new List<int>() { a }, controller, controller.name, out string outPath))
                {

                    swole.LogError($"[{nameof(CustomAnimationLayer)}] Found circular reference in layer '{layer.name}' for '{outPath}'");
                    newMotionControllers[a] = null;

                }

            }

            List<CustomMotionController> backupControllers = new List<CustomMotionController>(newMotionControllers);
            HashSet<int> reservedIndices = new HashSet<int>();
            List<int> referencedIndices = new List<int>();
            Dictionary<int, int> remapper = new Dictionary<int, int>();
            int count = newMotionControllers.Count;
            int i = 0;
            while (i < count) // Duplicate motion controllers if they're referenced by multiple parents. Avoids conflicts when transitioning between states.
            {

                var controller = newMotionControllers[i];
                if (controller != null)
                {

                    referencedIndices.Clear();
                    remapper.Clear();

                    controller.GetChildIndices(referencedIndices);
                    for (int b = 0; b < referencedIndices.Count; b++)
                    {

                        int index = referencedIndices[b];
                        if (index < 0 || index >= count) continue;

                        if (reservedIndices.Contains(index))
                        {

                            var referencedController = newMotionControllers[index];
                            if (referencedController != null)
                            {

                                remapper[index] = newMotionControllers.Count;
                                newMotionControllers.Add((CustomMotionController)referencedController.Clone());

                            }

                        }
                        else reservedIndices.Add(index);

                    }

                    controller.RemapChildIndices(remapper);

                }

                count = newMotionControllers.Count;
                i++;
                if (i >= loopLimit)
                {

                    swole.LogError($"[{nameof(CustomAnimationLayer)}] Controller duplication loop limit reached for layer '{layer.name}' - aborting...'");
                    newMotionControllers = backupControllers;
                    break;

                }

            }

            reservedIndices.Clear();
            // State machine motion controller indices do not need to be remapped because they reference non nested motion controllers, which do not change index.
            for (int a = 0; a < layer.stateMachines.Length; a++) // Duplicate motion controllers if they're referenced by multiple state machines. Avoids conflicts when transitioning between states.
            {

                var state = layer.stateMachines[a];

                int motionControllerIndex = state.MotionControllerIndex;
                if (motionControllerIndex < 0 || motionControllerIndex >= newMotionControllers.Count) continue;

                if (reservedIndices.Contains(motionControllerIndex))
                {

                    layer.stateMachines[a] = state.NewInstance(state.Layer, newMotionControllers.Count); // Only clone state machines if necessary 
                    newMotionControllers.Add((CustomMotionController)newMotionControllers[motionControllerIndex].Clone()); // TODO: clone all child motion controllers as well?
                    // could add the new index to reserved indices, but nothing else should ever be referencing it; so it would only slow things down
                }
                else reservedIndices.Add(motionControllerIndex);

            }

            layer.m_motionControllers = newMotionControllers.ToArray();

            for (int a = 0; a < layer.m_motionControllers.Length; a++) if (layer.m_motionControllers[a] != null)
                {

                    var controller = layer.m_motionControllers[a];

                    controller.Initialize(layer); // Essentially tells all animation reference motion controllers create their animation players
                    controller.ForceSetLoopMode(layer, controller.GetLoopMode(layer));

                }

            layer.m_activeState = layer.entryStateIndex = EntryStateIndex;

            return layer;

        }

        public CustomAnimationLayer() { }

        public CustomAnimationLayer(IAnimator animator)
        {
            m_animator = animator;
        }

        [NonSerialized]
        protected IAnimator m_animator;
        public IAnimator Animator => m_animator;

        public string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        [NonSerialized]
        public int indexInAnimator;
        public int IndexInAnimator
        {
            get => indexInAnimator;
            set => indexInAnimator = value;
        }
        /// <summary>
        /// Rearrange the position of this layer in the animator's layer list.
        /// </summary>
        public Dictionary<int, int> Rearrange(int swapIndex, bool recalculateIndices = true)
        {
            if (Animator == null) return null;
            return Animator.RearrangeLayer(indexInAnimator, swapIndex, recalculateIndices);
        }
        /// <summary>
        /// Rearrange the position of this layer in the animator's layer list.
        /// </summary>
        public void RearrangeNoRemap(int swapIndex, bool recalculateIndices = true)
        {
            if (Animator == null) return;
            Animator.RearrangeLayerNoRemap(indexInAnimator, swapIndex, recalculateIndices);
        }

        [SerializeField]
        private bool isAdditive;
        public bool IsAdditive
        {
            get => isAdditive;
            set => SetAdditive(value);
        }
        public void SetAdditive(bool isAdditiveLayer)
        {
            bool prevValue = isAdditive;
            isAdditive = isAdditiveLayer;
            if (prevValue != isAdditive)
            {
                if (m_animationPlayers != null)
                {
                    foreach (var playerCollection in m_animationPlayers)
                    {
                        var list = playerCollection.Value;
                        if (list == null) continue;

                        foreach (var player in list)
                        {
                            if (player == null) continue;
                            player.isAdditive = isAdditive;
                        }
                    }
                }
            }
        }

        public float mix;
        public float Mix
        {
            get => mix;
            set => mix = value;
        }
        public bool deactivate;
        public bool Deactivate
        {
            get => deactivate;
            set => deactivate = value;
        }
        public bool IsActive => mix != 0 && !deactivate;
        public void SetActive(bool active)
        {
            deactivate = !active;
        }

        [SerializeField]
        public int blendParameterIndex = -1;
        public int BlendParameterIndex
        {
            get => blendParameterIndex;
            set => blendParameterIndex = value;
        }

        public void GetParameterIndices(List<int> indices)
        {

            if (indices == null) return;

            if (BlendParameterIndex >= 0) indices.Add(BlendParameterIndex);

            if (m_motionControllers != null)
            {

                foreach (var controller in m_motionControllers) if (controller != null) controller.GetParameterIndices(this, indices);

            }

        }

        public void RemapParameterIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false)
        {

            if (remapper == null) return;

            if (BlendParameterIndex >= 0)
            {

                if (!remapper.TryGetValue(BlendParameterIndex, out blendParameterIndex) && invalidateNonRemappedIndices) blendParameterIndex = -1;

            }

            if (m_motionControllers != null)
            {

                foreach (var controller in m_motionControllers) if (controller != null) controller.RemapParameterIndices(this, remapper, invalidateNonRemappedIndices);

            }

        }

        [SerializeField]
        public MotionControllerIdentifier[] motionControllerIdentifiers;
        public MotionControllerIdentifier[] MotionControllerIdentifiers
        {
            get => motionControllerIdentifiers;
            set => motionControllerIdentifiers = value;
        }

        /// <summary>
        /// A runtime collection of motion controllers brought in from an animation controller.
        /// </summary>
        [NonSerialized]
        protected CustomMotionController[] m_motionControllers;
        public int ControllerCount => m_motionControllers == null ? 0 : m_motionControllers.Length;
        public bool IsPrototype => m_motionControllers == null; 
        public IAnimationMotionController GetMotionController(int index)
        {

            if (m_motionControllers == null || index < 0 || index >= m_motionControllers.Length) return null;

            return GetMotionControllerUnsafe(index);

        }
        public IAnimationMotionController GetMotionControllerUnsafe(int index) => m_motionControllers[index];

        [SerializeField]
        public int entryStateIndex;
        public int EntryStateIndex
        {
            get => entryStateIndex;
            set => entryStateIndex = value;
        }

        [SerializeField]
        public CustomStateMachine[] stateMachines;
        public IAnimationStateMachine[] StateMachines
        {
            get
            {
                if (stateMachines == null) return null;
                var array = new IAnimationStateMachine[stateMachines.Length];
                for (int a = 0; a < stateMachines.Length; a++) array[a] = stateMachines[a];
                return array;
            }
            set
            {
                SetStateMachines(value);
            }
        }
        public void SetStateMachines(IAnimationStateMachine[] stateMachines)
        {
            if (stateMachines == null)
            {
                this.stateMachines = null; 
                return;
            }
            this.stateMachines = new CustomStateMachine[stateMachines.Length];
            for (int a = 0; a < stateMachines.Length; a++)
            {
                var stateMachine = stateMachines[a];
                if (stateMachines[a] is not CustomStateMachine csm)
                {
                    csm = new CustomStateMachine();
                    if (stateMachine != null)
                    {
                        csm.name = stateMachine.Name;
                        csm.index = stateMachine.Index;
                        csm.motionControllerIndex = stateMachine.MotionControllerIndex;
                        csm.transitions = stateMachine.Transitions;
                    } 
                    else
                    {
                        csm.name = "null";
                        csm.index = a;
                        csm.motionControllerIndex = -1;
                    }
                }
                this.stateMachines[a] = csm;
            }
        }

        public int StateCount => stateMachines == null ? 0 : stateMachines.Length;
        public IAnimationStateMachine GetStateMachine(int index)
        {

            if (stateMachines == null || index < 0 || index >= stateMachines.Length) return null;

            return GetStateMachineUnsafe(index);

        }
        public IAnimationStateMachine GetStateMachineUnsafe(int index) => stateMachines[index];
        public void SetStateMachine(int index, IAnimationStateMachine stateMachine)
        {
            if (stateMachine is not CustomStateMachine csm) return;
            SetStateMachine(index, csm);
        }
        public void SetStateMachine(int index, CustomStateMachine stateMachine)
        {
            if (stateMachines == null || index < 0 || index >= stateMachines.Length) return;
            stateMachines[index] = stateMachine;
        }

        [NonSerialized]
        protected JobHandle m_jobHandle;
        public JobHandle OutputDependency => m_jobHandle;

        [NonSerialized]
        protected int m_activeState;
        public int ActiveStateIndex => m_activeState;
        public IAnimationStateMachine ActiveState => GetStateMachine(ActiveStateIndex);
        public CustomStateMachine ActiveStateTyped 
        { 
            get
            {
                var state = ActiveState;
                if (state is CustomStateMachine csm) return csm;
                return null;
            }
        }
        public bool HasActiveState
        {

            get
            {

                var activeState = ActiveState;
                bool active = false;

                if (activeState != null) active = activeState.IsActive();

                return active;

            }

        }

        [NonSerialized]
        protected Dictionary<string, List<CustomAnimation.Player>> m_animationPlayers;
        public void IteratePlayers(IterateAnimationPlayerDelegate del)
        {
            if (del == null || m_animationPlayers == null) return;
            foreach (var pair in m_animationPlayers) if (pair.Value != null) foreach (var player in pair.Value) if (player != null) del(player);
        }

        protected void UpdateAnimationPlayerIndices(List<CustomAnimation.Player> players)
        {
            if (players == null) return;
            for (int a = 0; a < players.Count; a++) if (players[a] != null) players[a].Index = a;
        }
        protected void UpdateAnimationPlayerIndices(List<IAnimationPlayer> players)
        {
            if (players == null) return;
            for (int a = 0; a < players.Count; a++) if (players[a] != null) players[a].Index = a;
        }

        public IAnimationPlayer GetNewAnimationPlayer(IAnimationAsset animation)
        {
            if (animation == null) return null;

            if (animation is CustomAnimation anim)
            {
                return GetNewAnimationPlayer(anim);
            } 
            else if ( animation is CustomAnimationAsset asset)
            {
                return GetNewAnimationPlayer(asset);
            }

            swole.LogError($"Cannot create animation player for type {animation.GetType().Name}!");
            return null;
        }
        public IAnimationPlayer GetNewAnimationPlayer(CustomAnimation animation)
        {
            if (animation == null || !Valid) return null;
            if (Animator is not CustomAnimator ca) return null;

            if (m_animationPlayers == null) m_animationPlayers = new Dictionary<string, List<CustomAnimation.Player>>();

            string id = animation.ID;
            if (!m_animationPlayers.TryGetValue(id, out List<CustomAnimation.Player> players))
            {
                players = new List<CustomAnimation.Player>();
                m_animationPlayers[id] = players;
            }

            var player = new CustomAnimation.Player(ca, animation, isAdditive, true);
            player.index = players.Count;

            players.Add(player);
            return player;
        }
        public IAnimationPlayer GetNewAnimationPlayer(CustomAnimationAsset asset)
        {
            if (asset == null || !Valid) return null;
            if (Animator is not CustomAnimator ca) return null;

            if (m_animationPlayers == null) m_animationPlayers = new Dictionary<string, List<CustomAnimation.Player>>();

            string id = asset.ID;
            if (!m_animationPlayers.TryGetValue(id, out List<CustomAnimation.Player> players))
            {
                players = new List<CustomAnimation.Player>();
                m_animationPlayers[id] = players;
            }

            var player = new CustomAnimation.Player(ca, asset, isAdditive, true);
            player.index = players.Count;

            players.Add(player);
            return player;
        }
        public bool RemoveAnimationPlayer(IAnimationAsset animation, int playerIndex) => RemoveAnimationPlayer(animation == null ? null : animation.ID, playerIndex);
        public bool RemoveAnimationPlayer(CustomAnimation animation, int playerIndex) => RemoveAnimationPlayer(animation == null ? null : animation.ID, playerIndex);
        public bool RemoveAnimationPlayer(CustomAnimationAsset asset, int playerIndex) => RemoveAnimationPlayer(asset == null ? null : asset.ID, playerIndex);
        public bool RemoveAnimationPlayer(string id, int playerIndex)
        {
            if (m_animationPlayers == null || playerIndex < 0 || string.IsNullOrEmpty(id)) return false;
            if (!m_animationPlayers.TryGetValue(id, out List<CustomAnimation.Player> players) || players == null) return false;
            return RemoveAnimationPlayer(players, playerIndex);
        }
        public bool RemoveAnimationPlayer(List<CustomAnimation.Player> players, int playerIndex)
        {
            if (playerIndex >= players.Count) return false;

            var player = players[playerIndex];
            players.RemoveAt(playerIndex);
            player.Dispose();

            UpdateAnimationPlayerIndices(players);

            return true;
        }
        public bool RemoveAnimationPlayer(List<IAnimationPlayer> players, int playerIndex)
        {
            if (playerIndex >= players.Count) return false;

            var player = players[playerIndex];
            players.RemoveAt(playerIndex);
            player.Dispose();

            UpdateAnimationPlayerIndices(players);

            return true;
        }

        public bool RemoveAnimationPlayer(IAnimationPlayer player)
        {

            if (player == null) return false;
            bool removed = RemoveAnimationPlayer(player.Animation, player.Index);
            player.Dispose();

            return removed;

        }

        /// <summary>
        /// Progress the animation by deltaTime
        /// </summary>
        /// <param name="nextHierarchy"></param>
        /// <param name="nextIsAdditiveOrBlended">Used to determine if animation calculations should be skipped if they will be done before a non-additive and non-blended layer.</param>
        /// <param name="deltaTime">The amount of time to progress forward or backward in the animation.</param>
        /// <param name="disableMultithreading"></param>
        /// <param name="isFinal">Used to indicate the final iteration in the animation process. During the final iteration, all transforms are finally updated with the newly calculated data.</param>
        /// <param name="inputDeps"></param>
        /// <param name="localHierarchy"></param>
        /// <returns></returns>
        public JobHandle Progress(TransformHierarchy nextHierarchy, bool nextIsAdditiveOrBlended, float deltaTime, bool disableMultithreading, bool isFinal, JobHandle inputDeps = default, TransformHierarchy localHierarchy = null)
        {

            m_jobHandle = inputDeps;

            /*using (var enumerator = m_animationPlayers.GetEnumerator())
            {

                if (enumerator.MoveNext())
                {

                    var toStep = enumerator.Current;

                    bool isFinal = !enumerator.MoveNext();

                    var next = enumerator.Current;
                    while (true)
                    {
                        m_jobHandle = toStep.Value.Progress(deltaTime, m_jobHandle, !disableMultithreading, isFinal);
                        if (isFinal) break;

                        toStep = next;

                        isFinal = !enumerator.MoveNext();

                        next = enumerator.Current;
                    }
                }
            }*/

            var activeState = ActiveState;
            if (activeState is CustomStateMachine csm)
            {
                
                m_activeState = csm.Progress(nextHierarchy, nextIsAdditiveOrBlended, false, deltaTime, ref m_jobHandle, !disableMultithreading, isFinal, true, true, localHierarchy);

            }

            return OutputDependency;

        }
         
        public TransformHierarchy GetActiveTransformHierarchy()
        {

            var activeState = ActiveState;
            if (activeState != null)
            {

                var controller = GetMotionController(activeState.MotionControllerIndex);
                if (controller == null) return null;

                int hierarchyIndex = controller.GetLongestHierarchyIndex(this);
                if (hierarchyIndex < 0) return null;

                return Animator.GetTransformHierarchy(hierarchyIndex);

            }

            return null;

        }

    }
}

#endif