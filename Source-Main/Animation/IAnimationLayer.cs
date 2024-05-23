using System;
using System.Collections.Generic;

namespace Swole.Animation
{
    public delegate void IterateAnimationPlayerDelegate(IAnimationPlayer player);
    public interface IAnimationLayer : IDisposable
    {

        public bool Valid { get; }

        public bool DisposeIfHasPrefix(string prefix);

        public IAnimationLayer NewInstance(IAnimator animator, IAnimationController animationController = null);

        public IAnimator Animator { get; }

        public string Name { get; set; }

        public int IndexInAnimator { get; set; }
        /// <summary>
        /// Rearrange the position of this layer in the animator's layer list.
        /// </summary>
        public Dictionary<int, int> Rearrange(int swapIndex, bool recalculateIndices = true);
        /// <summary>
        /// Rearrange the position of this layer in the animator's layer list.
        /// </summary>
        public void RearrangeNoRemap(int swapIndex, bool recalculateIndices = true);

        public void SetAdditive(bool isAdditiveLayer);
        public bool IsAdditive { get; set; }

        public float Mix { get; set; }
        public bool Deactivate { get; set; }
        public bool IsActive { get; }
        public void SetActive(bool active);

        public int BlendParameterIndex { get; set; }

        public void GetParameterIndices(List<int> indices);

        public void RemapParameterIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false);

        public MotionControllerIdentifier[] MotionControllerIdentifiers { get; set; }

        public int ControllerCount { get; }
        public bool IsPrototype { get; }
        public IAnimationMotionController GetMotionController(int index);
        public IAnimationMotionController GetMotionControllerUnsafe(int index);

        public int EntryStateIndex { get; set; }

        public IAnimationStateMachine[] StateMachines { get; set; }
        public int StateCount { get; }
        public IAnimationStateMachine GetStateMachine(int index);
        public IAnimationStateMachine GetStateMachineUnsafe(int index);
        public void SetStateMachine(int index, IAnimationStateMachine stateMachine);
        public void SetStateMachines(IAnimationStateMachine[] stateMachines);

        public int ActiveStateIndex { get; }
        public IAnimationStateMachine ActiveState { get; }
        public bool HasActiveState { get; }

        public void IteratePlayers(IterateAnimationPlayerDelegate del);
        public IAnimationPlayer GetNewAnimationPlayer(IAnimationAsset animation);
        public bool RemoveAnimationPlayer(IAnimationAsset animation, int playerIndex);
        public bool RemoveAnimationPlayer(string id, int playerIndex);
        public bool RemoveAnimationPlayer(List<IAnimationPlayer> players, int playerIndex);

        public bool RemoveAnimationPlayer(IAnimationPlayer player);

        public TransformHierarchy GetActiveTransformHierarchy();

    }
}
