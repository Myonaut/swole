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

        private const int _loopLimit = 16384;

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

        [NonSerialized]
        public IAnimationController source;
        public IAnimationController Source => source;

        public bool IsFromSource(IAnimationController source) => ReferenceEquals(source, this.source);
        public bool DisposeIfIsFromSource(IAnimationController source)
        {
            bool dispose = IsFromSource(source);
            if (dispose) Dispose();

            return dispose;
        }

        public bool HasPrefix(string prefix) => name == null ? false : name.ToLower().Trim().StartsWith(prefix);
        public bool DisposeIfHasPrefix(string prefix)
        {
            bool dispose = HasPrefix(prefix); 
            if (dispose) Dispose();

            return dispose;
        }

        [SerializeField]
        protected CustomAvatarMaskAsset avatarMaskAsset;

        protected WeightedAvatarMask avatarMask;

        [SerializeField]
        protected bool invertAvatarMask;

        public WeightedAvatarMask AvatarMask 
        {
            get => avatarMask == null ? (avatarMaskAsset == null ? null : avatarMaskAsset.mask) : avatarMask;    
            set => SetAvatarMask(value, false); 
        }
        public bool InvertAvatarMask 
        { 
            get => invertAvatarMask;
            set => SetAvatarMask(avatarMask, value);
        }

        private static readonly List<AvatarMaskUsage> tempAvatarMasks = new List<AvatarMaskUsage>();
        public void SetAvatarMask(WeightedAvatarMask mask, bool invertMask = false)
        {
            this.avatarMask = mask;
            this.invertAvatarMask = invertMask;

            if (this.m_motionControllers != null)
            {
                tempAvatarMasks.Clear();
                if (avatarMask != null) tempAvatarMasks.Add(new AvatarMaskUsage() { mask = avatarMask.AsComposite(true), invertMask = invertAvatarMask });
                foreach (var controller in this.m_motionControllers)
                {
                    if (controller == null || controller.HasParent) continue; 

                    controller.Initialize(this, tempAvatarMasks);
                }
                tempAvatarMasks.Clear(); 
            }
        }

        public void ReinitializeController(int controllerIndex)
        {
            if (m_motionControllers == null || controllerIndex < 0 || controllerIndex >= m_motionControllers.Length) return;
            ReinitializeController(m_motionControllers[controllerIndex]);
        }
        public void ReinitializeController(IAnimationMotionController controller)
        {
            if (controller == null) return;

            tempAvatarMasks.Clear();
            if (avatarMask != null) tempAvatarMasks.Add(new AvatarMaskUsage() { mask = avatarMask.AsComposite(true), invertMask = invertAvatarMask });

            while (controller.HasParent) controller = controller.Parent; // Find top level parent. All children will get initialized by this parent.
            controller.Initialize(this, tempAvatarMasks, null);
            
            tempAvatarMasks.Clear();
        }

        public IAnimationLayer NewInstance(IAnimator animator, IAnimationController animationController = null)
        {
            if (animationController == null && animator != null) animationController = animator.DefaultController;

            CustomAnimationLayer layer = new CustomAnimationLayer(animator);

            layer.source = animationController;

            layer.name = name;
            layer.isAdditive = isAdditive;
            layer.mix = mix;

            layer.avatarMask = avatarMask;
            layer.invertAvatarMask = invertAvatarMask;
            
            Dictionary<int, int> layerStateIndexRemapper = new Dictionary<int, int>();
            layer.states = new CustomAnimationLayerState[states == null ? 0 : states.Length];
            if (states != null)
            {
                for (int a = 0; a < states.Length; a++)
                {
                    if (states[a] != null)
                    {
                        var newState = states[a].NewInstance(layer);
                        newState.Index = a;
                        layer.states[a] = newState;

                        layerStateIndexRemapper[a] = newState.Index;
                    }
                }
            }

            int initialMotionControllerCount = 0;
            List<CustomMotionController> newMotionControllers = new List<CustomMotionController>();
            if (m_motionControllers != null && m_motionControllers.Length > 0) // Instantiating from an already instantiated layer
            {

                for (int a = 0; a < m_motionControllers.Length; a++)
                {

                    if (m_motionControllers[a] != null) newMotionControllers.Add((CustomMotionController)m_motionControllers[a].Clone()); else newMotionControllers.Add(null);

                }
                initialMotionControllerCount = newMotionControllers.Count;

            }
            else if (motionControllerIdentifiers != null && animationController != null)  // Instantiating from a prototype layer
            {

                initialMotionControllerCount = motionControllerIdentifiers.Length;

                List<int> childIndices = new List<int>();
                List<MotionControllerIdentifier> identifiers = new List<MotionControllerIdentifier>(motionControllerIdentifiers); // Allows non nested motion controllers to keep the same index
                for (int a = 0; a < motionControllerIdentifiers.Length; a++) // Clones base motion controllers and sets up nested motion controllers to be cloned. This separates motion controllers that are referenced by layer states and motion controllers that are referenced by other motion controllers.
                {
                    var identifier = motionControllerIdentifiers[a];
                    var motionController = animationController.GetMotionController(identifier);
                    if (motionController != null)
                    {
                        motionController = (CustomMotionController)motionController.Clone();
                        newMotionControllers.Add((CustomMotionController)motionController);
                    }
                    else
                    {
                        newMotionControllers.Add(null);
                        continue;
                    }

                    if (layer.states != null)
                    {
                        childIndices.Clear();
                        motionController.GetChildIndices(childIndices, false);
                        for (int b = 0; b < childIndices.Count; b++)
                        {
                            bool clone = false;
                            int ind = childIndices[b];
                            for (int c = 0; c < layer.states.Length; c++)
                            {
                                var state = layer.states[c];
                                if (state == null) continue; ;

                                if (state.motionControllerIndex == ind)
                                {
                                    clone = true;
                                    break;
                                }
                            }

                            if (clone)
                            {
                                motionController.SetChildIndex(b, identifiers.Count);
                                identifiers.Add(identifiers[ind]);
                            }
                        }
                    }
                }

                for (int a = motionControllerIdentifiers.Length; a < identifiers.Count; a++) // Clone all new motion controllers using the collection of identifiers.
                {

                    var identifier = identifiers[a];
                    var motionController = animationController.GetMotionController(identifier);

                    identifier.index = newMotionControllers.Count; // Keep track of the index of this newly cloned motion controller
                    identifiers[a] = identifier;

                    if (motionController != null) newMotionControllers.Add((CustomMotionController)motionController.Clone()); else newMotionControllers.Add(null); // Clone the nested motion controller reference
                }
            }

            for (int a = 0; a < layer.states.Length; a++) // Duplicate motion controllers if they're referenced by multiple states. Duplicate states if they appear in the state array multiple times. Avoids conflicts when transitioning between states.
            {

                var state = layer.states[a];

                int motionControllerIndex = state.MotionControllerIndex;
                if (motionControllerIndex < 0 || motionControllerIndex >= newMotionControllers.Count) continue; 

                bool cloneState = false;
                bool cloneController = false;
                for (int b = 0; b < a; b++)
                {
                    var state2 = layer.states[b];
                    if (state2.MotionControllerIndex == motionControllerIndex)
                    {
                        cloneController = true;
                    }
                    if (ReferenceEquals(state, state2))
                    {
                        cloneState = true;
                        cloneController = true; // if the state is cloned, always clone the top level motion controller as well.
                    }
                }

                if (cloneState) // Only clone states if necessary 
                {
                    var newState = state.NewInstance(state.Layer);
                    newState.Index = a;
                    layer.states[a] = newState;
                    layerStateIndexRemapper[a] = newState.Index;
                }

                if (cloneController)
                {
                    layer.states[a].motionControllerIndex = newMotionControllers.Count;
                    newMotionControllers.Add((CustomMotionController)newMotionControllers[motionControllerIndex].Clone()); 
                }
            }

            // Checks for deep references to the indices provided in the list
            bool IsCyclic(List<int> indices, CustomMotionController controller, string inPath, out string outPath, out string culprit)
            {

                culprit = string.Empty;

                outPath = inPath;
                List<int> children = new List<int>();
                controller.GetChildIndices(children);

                foreach (int child in children)
                {

                    if (child < 0 || child >= newMotionControllers.Count) continue;
                    var childController = newMotionControllers[child];
                    if (childController == null || !childController.HasChildControllers) continue; 
                    if (indices.Contains(child)) 
                    { 
                        culprit = childController.name;
                        return true;
                    }

                }

                //List<int> combined = new List<int>(children);
                //combined.AddRange(indices);
                for (int a = 0; a < children.Count; a++)
                {

                    int childIndex = children[a];
                    if (childIndex < 0 || childIndex >= newMotionControllers.Count) continue;
                    var childController = newMotionControllers[childIndex];
                    if (childController == null || !controller.HasChildControllers) continue; 

                    List<int> combined = new List<int>(indices); 
                    combined.Add(childIndex);
                    if (IsCyclic(combined, childController, inPath + "/" + childController.name, out outPath, out culprit)) return true;  

                }

                return false;

            }

            for (int a = 0; a < newMotionControllers.Count; a++) // Check for circular references and nullify perpetrators. Could cause infinite invocation if not accounted for.
            {

                var controller = newMotionControllers[a];
                if (controller == null || !controller.HasChildControllers) continue;

                if (IsCyclic(new List<int>() { a }, controller, controller.name, out string outPath, out string culprit))
                {

                    swole.LogError($"[{nameof(CustomAnimationLayer)}] Found circular reference '{culprit}' in layer '{layer.name}' for '{outPath}'");
                    newMotionControllers[a] = null;

                }

            }
            
            List<CustomMotionController> backupControllers = new List<CustomMotionController>(newMotionControllers);
            HashSet<int> reservedIndices = new HashSet<int>();
            List<int> referencedIndices = new List<int>();
            Dictionary<int, CustomMotionController> firstIndexReferencers = new Dictionary<int, CustomMotionController>();
            Dictionary<int, int> cloneCounts = new Dictionary<int, int>();
            int count = newMotionControllers.Count;
            int i = 0;
            while (i < count) // Duplicate motion controllers if they're referenced by multiple parents or the same parent multiple times. Avoids conflicts when blending or transitioning between states.
            {

                var controller = newMotionControllers[i];
                if (controller != null)
                {

                    referencedIndices.Clear();
                    controller.GetChildIndices(referencedIndices, false);
                    for (int b = 0; b < referencedIndices.Count; b++)
                    {

                        int index = referencedIndices[b];
                        if (index < 0 || index >= newMotionControllers.Count) continue;

                        int currentIndex = controller.GetChildIndex(b);
                        if (reservedIndices.Contains(currentIndex))
                        {

                            var originalReferencedController = newMotionControllers[index];
                            var referencedController = newMotionControllers[currentIndex];
                            if (originalReferencedController != null && referencedController != null)
                            {
                                int newIndex = newMotionControllers.Count;
                                controller.SetChildIndex(b, newIndex);
                                cloneCounts.TryGetValue(index, out int cloneCount); 
                                cloneCount += 1;
                                cloneCounts[index] = cloneCount;
                                reservedIndices.Add(newIndex);
                                firstIndexReferencers[newIndex] = controller;

                                var clone = (CustomMotionController)referencedController.Clone(); 
                                clone.name = originalReferencedController.name + $"_{(cloneCount + 1)}";  
                                newMotionControllers.Add(clone);
                                swole.Log($"Duplicated multi-referenced controller '{referencedController.name}'. (Clone Count: {cloneCount}) (Referenced by '{controller.name}' (index:{i})) (First referenced by '{firstIndexReferencers[index].name}')");
                            } 
                            else
                            {
                                swole.LogError($"Controller duplication mismatch for '{controller.name}' at child index {b}. (Original child controller: {(originalReferencedController == null ? "null" : originalReferencedController.Name)}) (Reference child controller: {(referencedController == null ? "null" : referencedController.Name)})"); 
                            }

                        }
                        else 
                        { 
                            reservedIndices.Add(currentIndex);
                            firstIndexReferencers[currentIndex] = controller;
                            //swole.Log($"Registering first reference of '{newMotionControllers[index].name}' (index:{index}) by '{controller.name}' (index:{i})"); 
                        }

                    }

                }
                
                count = newMotionControllers.Count;
                i++;
                if (i >= _loopLimit)
                {

                    swole.LogError($"[{nameof(CustomAnimationLayer)}] Controller duplication loop limit reached for layer '{layer.name}' - aborting...'");
                    newMotionControllers = backupControllers;
                    break;

                }
            }

            layer.m_motionControllers = newMotionControllers.ToArray(); 

            tempAvatarMasks.Clear();
            if (avatarMask != null) tempAvatarMasks.Add(new AvatarMaskUsage() { mask = avatarMask.AsComposite(true), invertMask = invertAvatarMask });
            for (int a = 0; a < layer.m_motionControllers.Length; a++)
            {
                var controller = layer.m_motionControllers[a];
                if (controller == null) continue;
                 
                if (!controller.HasParent) controller.Initialize(layer, tempAvatarMasks); // Essentially tells all animation reference motion controllers to create their animation players
                controller.ForceSetLoopMode(layer, controller.GetLoopMode(layer));  
            }
            tempAvatarMasks.Clear();
             
            int entryIndex = EntryStateIndex;
            if (layerStateIndexRemapper.TryGetValue(entryIndex, out int newEntryIndex))
            {
                entryIndex = newEntryIndex;
            }

            layer.m_activeState = layer.entryStateIndex = entryIndex; 

            for(int a = 0; a < layer.states.Length; a++)
            {
                var state = layer.states[a];

                if (state.transitions != null) 
                {
                    var transitions = new Transition[state.transitions.Length]; // instantiate transitions
                    for (int b = 0; b < transitions.Length; b++) transitions[b] = state.transitions[b].Duplicate();
                    state.transitions = transitions;

                    foreach(var transition in state.transitions)
                    {
                        if (layerStateIndexRemapper.TryGetValue(transition.targetStateIndex, out int newIndex))
                        {
                            transition.targetStateIndex = newIndex;    
                        }
                    }
                }
            }
             
            for(int a = 0; a < layer.m_motionControllers.Length; a++)
            {
                var mc = layer.m_motionControllers[a];
                //string path = $"/{mc.name}";
                var parent = mc.Parent;
                if (parent == null) 
                {
                    bool isStateReferenced = false;
                    for (int b = 0; b < layer.states.Length; b++)
                    {
                        var state = layer.states[b];
                        if (state.MotionControllerIndex == a)
                        {
                            isStateReferenced = true;
                        }
                    }
                    if (!isStateReferenced)
                    {
                        if (a < initialMotionControllerCount)
                        {
                            swole.LogWarning($"Found isolated motion controller '{mc.Name}'! (Not referenced by a state or another motion controller) ");  
                        } 
                        else
                        {
                            swole.LogError($"Found isolated cloned motion controller '{mc.Name}'! (Not referenced by a parent controller or layer state. This should not be possible.) ");
                        }
                    }
                }

                var topLevelParent = parent;
                while(parent != null)
                {
                    //path = $"/{parent.Name}{path}";
                    parent = parent.Parent;
                    if (parent != null) topLevelParent = parent;
                }
                if (topLevelParent == null) topLevelParent = mc;

                CustomAnimationLayerState stateRef = null;
                for (int b = 0; b < layer.states.Length; b++)
                {
                    var state = layer.states[b];
                    if (state.MotionControllerIndex < 0) 
                    {
                        swole.LogWarning($"Layer state '{state.Name}' has no associated motion controller!"); 
                        continue;
                    }

                    if (ReferenceEquals(layer.m_motionControllers[state.MotionControllerIndex], topLevelParent))
                    {
                        stateRef = state;
                        break;
                    }
                }
                //Debug.Log($"STATE:{(stateRef == null ? "NULL" : stateRef.Name)}::: {a}:: {path}");   
            }
             
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
        /// <summary>
        /// Set the position of this layer in the animator's layer list.
        /// </summary>
        public Dictionary<int, int> Move(int newIndex, bool recalculateIndices = true)
        {
            if (Animator == null) return null;
            return Animator.MoveLayer(indexInAnimator, newIndex, recalculateIndices);
        }
        /// <summary>
        /// Set the position of this layer in the animator's layer list.
        /// </summary>
        public void MoveNoRemap(int newIndex, bool recalculateIndices = true)
        {
            if (Animator == null) return;
            Animator.MoveLayerNoRemap(indexInAnimator, newIndex, recalculateIndices);  
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
        public bool deactivateAtZeroMix;
        public bool IsActive => !deactivate && (mix != 0 || !deactivateAtZeroMix); 
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

        public IAnimationParameter GetParameter(int index)
        {
            if (Animator == null) return null;     
            return Animator.GetParameter(index);
        }

        public int FindParameterIndex(string name)
        {
            if (Animator == null) return -1;
            return Animator.FindParameterIndex(name);
        }
        public IAnimationParameter FindParameter(string name, out int parameterIndex)
        {
            parameterIndex = -1;
            if (Animator == null) return null;

            return Animator.FindParameter(name, out parameterIndex);
        }
        public IAnimationParameter FindParameter(string name)
        {
            if (Animator == null) return null;
            return Animator.FindParameter(name);
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

        public int FindMotionControllerIndex(string id)
        {
            if (m_motionControllers == null) return -1;

            for (int a = 0; a < m_motionControllers.Length; a++)
            {
                if (m_motionControllers[a] != null && m_motionControllers[a].Name == id)
                {
                    return a;
                }
            }

            return -1;
        }
        public IAnimationMotionController FindMotionController(string id, out int controllerIndex)
        {
            controllerIndex = FindMotionControllerIndex(id);
            if (controllerIndex < 0) return null;

            return m_motionControllers[controllerIndex];
        }
        public IAnimationMotionController FindMotionController(string id) => FindMotionController(id, out _);

        public int FindStateIndex(string id)
        {
            if (states == null) return -1;

            for (int a = 0; a < states.Length; a++)
            {
                if (states[a] != null && states[a].Name == id)
                {
                    return a;
                }
            }

            return -1;
        }

        [SerializeField]
        public int entryStateIndex;
        public int EntryStateIndex
        {
            get => entryStateIndex;
            set => entryStateIndex = value;
        }

        [SerializeField]
        public CustomAnimationLayerState[] states;
        public IAnimationLayerState[] States
        {
            get
            {
                if (states == null) return null;
                var array = new IAnimationLayerState[states.Length];
                for (int a = 0; a < states.Length; a++) array[a] = states[a];
                return array;
            }
            set
            {
                SetStates(value);
            }
        }
        public IAnimationLayerState FindState(string id, out int stateIndex)
        {
            stateIndex = FindStateIndex(id);
            if (stateIndex < 0) return null;

            return states[stateIndex];
        }
        public IAnimationLayerState FindState(string id) => FindState(id, out _);

        public void SetStates(IAnimationLayerState[] states)
        {
            if (states == null)
            {
                this.states = null; 
                return;
            }
            this.states = new CustomAnimationLayerState[states.Length];
            for (int a = 0; a < states.Length; a++)
            {
                var state = states[a];
                if (states[a] is not CustomAnimationLayerState als)
                {
                    als = new CustomAnimationLayerState();
                    if (state != null)
                    {
                        als.name = state.Name;
                        als.index = state.Index;
                        als.motionControllerIndex = state.MotionControllerIndex;
                        als.transitions = state.Transitions;
                    } 
                    else
                    {
                        als.name = "null";
                        als.index = a;
                        als.motionControllerIndex = -1;
                    }
                }
                this.states[a] = als;
            }
        }

        public int StateCount => states == null ? 0 : states.Length;
        public IAnimationLayerState GetState(int index)
        {

            if (states == null || index < 0 || index >= states.Length) return null;

            return GetStateUnsafe(index);

        }
        public IAnimationLayerState GetStateUnsafe(int index) => states[index];
        public void SetState(int index, IAnimationLayerState state)
        {
            if (state is not CustomAnimationLayerState als) return;
            SetState(index, als);
        }
        public void SetState(int index, CustomAnimationLayerState state)
        {
            if (states == null || index < 0 || index >= states.Length) return;
            states[index] = state;
        }

        [NonSerialized]
        protected JobHandle m_jobHandle;
        public JobHandle OutputDependency => m_jobHandle;

        [NonSerialized]
        protected int m_activeState;
        public void SetActiveState(int index)
        {
            m_activeState = -1;
            if (index >= 0 && states != null && index < states.Length) m_activeState = index;
        }
        public void SetActiveStateUnsafe(int index) => m_activeState = index;
        public int ActiveStateIndex
        {
            get => m_activeState;
            set => SetActiveState(value);  
        }
        public IAnimationLayerState ActiveState => GetState(ActiveStateIndex);
        public CustomAnimationLayerState ActiveStateTyped 
        { 
            get
            {
                var state = ActiveState;
                if (state is CustomAnimationLayerState als) return als;
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

        public delegate void CreateAnimationPlayerDelegate(CustomAnimation.Player player);
        public event CreateAnimationPlayerDelegate OnCreateAnimationPlayer; 
        public void ClearAnimationPlayerCreationListeners() => OnCreateAnimationPlayer = null;
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

            var player = new CustomAnimation.Player(this, ca, animation, AvatarMask == null ? default : AvatarMask.AsComposite(true), InvertAvatarMask, isAdditive, true); 
            player.index = players.Count;
            
            players.Add(player);

            OnCreateAnimationPlayer?.Invoke(player);

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

            var player = new CustomAnimation.Player(this, ca, asset, AvatarMask == null ? default : AvatarMask.AsComposite(true), InvertAvatarMask, isAdditive, true);
            player.index = players.Count;

            players.Add(player);

            OnCreateAnimationPlayer?.Invoke(player);

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
            if (activeState is CustomAnimationLayerState als)
            {

                m_activeState = als.Progress(nextHierarchy, nextIsAdditiveOrBlended, false, deltaTime, ref m_jobHandle, !disableMultithreading, isFinal, true, false, true, localHierarchy);

            }// else Debug.Log(name + $" has no state! {EntryStateIndex} {ActiveStateIndex}"); 

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

        protected readonly List<SwolePuppetMaster.MuscleConfigMix> muscleConfigMixes = new List<SwolePuppetMaster.MuscleConfigMix>();
        public List<SwolePuppetMaster.MuscleConfigMix> GetCurrentMuscleConfigMix()
        {
            muscleConfigMixes.Clear();

            var activeState = ActiveState;
            if (activeState is CustomAnimationLayerState als)
            {
                var activeConfig = als.PuppetConfiguration;

                bool flag = true;
                if (activeState.IsTransitioning)
                {
                    var transitionTarget = GetState(activeState.TransitionTarget);
                    if (transitionTarget is CustomAnimationLayerState targetAls)
                    {
                        var transition = activeState.ActiveTransition;

                        if (transition.overrideMuscleConfig)
                        {
                            flag = false;

                            var targetConfig = targetAls.PuppetConfiguration;
                            if (targetConfig != null && targetConfig.IsValid)
                            {
                                muscleConfigMixes.Add(new SwolePuppetMaster.MuscleConfigMix()
                                {
                                    mix = 1f,
                                    config = targetConfig
                                });
                            }
                        }
                        else
                        {
                            var targetConfig = targetAls.PuppetConfiguration;
                            if (targetConfig != null && targetConfig.IsValid)
                            {
                                flag = false;

                                float progress = activeState.TransitionProgress; 
                                muscleConfigMixes.Add(new SwolePuppetMaster.MuscleConfigMix() 
                                {
                                    mix = progress,
                                    config = targetConfig
                                });
                                muscleConfigMixes.Add(new SwolePuppetMaster.MuscleConfigMix()
                                {
                                    mix = 1f - progress,
                                    config = activeConfig == null ? default : activeConfig // An invalid config will get converted to the default config automatically. Inserting an invalid config is desireable to maintain the mixing behaviour.
                                });
                            }
                        }
                    }
                }

                if (flag)
                {
                    if (activeConfig != null && activeConfig.IsValid)
                    { 
                        muscleConfigMixes.Add(new SwolePuppetMaster.MuscleConfigMix() 
                        {
                            mix = 1f,
                            config = activeConfig
                        });
                    }
                }
            }

            return muscleConfigMixes;
        }

    }
}

#endif