using System;
using System.Collections.Generic;

namespace Swole.Animation
{
    public delegate void IterateAnimationPlayerDelegate(IAnimationPlayer player);
    public interface IAnimationLayer : IDisposable
    {

        public bool Valid { get; }

        public bool HasPrefix(string prefix);
        public bool DisposeIfHasPrefix(string prefix);

        public IAnimationController Source { get; }
        public bool IsFromSource(IAnimationController source);
        public bool DisposeIfIsFromSource(IAnimationController source);

        public IAnimationLayer NewInstance(IAnimator animator, IAnimationController animationController = null);

        public IAnimator Animator { get; }

        public string Name { get; set; }

        public int IndexInAnimator { get; set; }
        /// <summary>
        /// Swap the position of this layer in the animator's layer list with another layer.
        /// </summary>
        public Dictionary<int, int> Rearrange(int swapIndex, bool recalculateIndices = true);
        /// <summary>
        /// Swap the position of this layer in the animator's layer list with another layer.
        /// </summary>
        public void RearrangeNoRemap(int swapIndex, bool recalculateIndices = true);

        /// <summary>
        /// Set the position of this layer in the animator's layer list.
        /// </summary>
        public Dictionary<int, int> Move(int newIndex, bool recalculateIndices = true);
        /// <summary>
        /// Set the position of this layer in the animator's layer list.
        /// </summary>
        public void MoveNoRemap(int newIndex, bool recalculateIndices = true);

        public void SetAdditive(bool isAdditiveLayer);
        public bool IsAdditive { get; set; }

        public float Mix { get; set; }
        public bool Deactivate { get; set; }
        public bool IsActive { get; }
        public void SetActive(bool active);

        public int BlendParameterIndex { get; set; }

        public void GetParameterIndices(List<int> indices);
        public HashSet<int> GetActiveParameters(HashSet<int> indices);

        public void RemapParameterIndices(Dictionary<int, int> remapper, bool invalidateNonRemappedIndices = false);

        public IAnimationParameter GetParameter(int index);

        public int FindParameterIndex(string name);
        public IAnimationParameter FindParameter(string name, out int parameterIndex);
        public IAnimationParameter FindParameter(string name);

        public MotionControllerIdentifier[] MotionControllerIdentifiers { get; set; }

        public int ControllerCount { get; }
        public bool IsPrototype { get; }
        public IAnimationMotionController GetMotionController(int index);
        public IAnimationMotionController GetMotionControllerUnsafe(int index);
        public int FindMotionControllerIndex(string id);
        public IAnimationMotionController FindMotionController(string id, out int controllerIndex);
        public IAnimationMotionController FindMotionController(string id);

        public int EntryStateIndex { get; set; }

        public IAnimationLayerState[] States { get; set; }
        public int StateCount { get; }
        public IAnimationLayerState GetState(int index);
        public IAnimationLayerState GetStateUnsafe(int index);
        public void SetState(int index, IAnimationLayerState state);
        public void SetStates(IAnimationLayerState[] states);
        public int FindStateIndex(string id);
        public IAnimationLayerState FindState(string id, out int stateIndex);
        public IAnimationLayerState FindState(string id);

        public void SetActiveState(int index);
        public void SetActiveStateUnsafe(int index);
        public int ActiveStateIndex { get; set; }
        public IAnimationLayerState ActiveState { get; }
        public bool HasActiveState { get; }

        public void IteratePlayers(IterateAnimationPlayerDelegate del);
        public IAnimationPlayer GetNewAnimationPlayer(IAnimationAsset animation);
        public bool RemoveAnimationPlayer(IAnimationAsset animation, int playerIndex);
        public bool RemoveAnimationPlayer(string id, int playerIndex);
        public bool RemoveAnimationPlayer(List<IAnimationPlayer> players, int playerIndex);

        public bool RemoveAnimationPlayer(IAnimationPlayer player);

        public TransformHierarchy GetActiveTransformHierarchy();

        public WeightedAvatarMask AvatarMask { get; set; }
        public bool InvertAvatarMask { get; set; }

        public void SetAvatarMask(WeightedAvatarMask mask, bool invertMask = false);

        public void ReinitializeController(int controllerIndex);
        public void ReinitializeController(IAnimationMotionController controller);

    }
}
