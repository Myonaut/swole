using System;
using System.Collections.Generic;

namespace Swole.Animation
{
    public interface IAnimator : IDisposable, EngineInternal.IComponent
    {

        public void DisableIKControllers();
        public void EnableIKControllers();
        public void ResetIKControllers();

        public IAnimationController DefaultController { get; set; }
        public void ReinitializeControllers();

        public void ApplyController(IAnimationController controller, bool usePrefix = true, bool incrementDuplicateParameters = false);

        public bool HasControllerData(IAnimationController controller);
        public bool HasControllerData(string prefix);

        public void RemoveControllerData(IAnimationController controller, bool disposeLayers = true);

        public void RemoveControllerData(string prefix, bool disposeLayers = true);

        public string AvatarName { get; }

        public int GetBoneIndex(string name);
        public int BoneCount { get; }
        public EngineInternal.ITransform GetBone(int index);
        public EngineInternal.ITransform RootBone { get; }

        public void ClearControllerData();

        public int AffectedTransformCount { get; }

        public const string _animatorTransformPropertyStringPrefix = "*";

        public bool UseDynamicBindPose { get; set; }
        public bool DisableMultithreading { get; set; }
        public bool OverrideUpdateCalls { get; set; }

        public void SetOverrideUpdateCalls(bool value);
        public bool ForceFinalTransformUpdate { get; set; }

        public IAnimationParameter GetParameter(int index);
        public void AddParameter(IAnimationParameter parameter, bool initialize = true, object initObject = null, List<IAnimationParameter> outList = null, bool onlyOutputNew = false);
        public void AddParameters(ICollection<IAnimationParameter> toAdd, bool initialize = true, object initObject = null, List<IAnimationParameter> outList = null, bool onlyOutputNew = false);
        public bool RemoveParameter(IAnimationParameter parameter);
        public bool RemoveParameter(int index);
        public int RemoveParametersStartingWith(string prefix);
        public int FindParameterIndex(string name);
        public IAnimationParameter FindParameter(string name, out int parameterIndex);
        public IAnimationParameter FindParameter(string name);
        public Dictionary<int, int> RecalculateParameterIndices();

        public void AddLayer(IAnimationLayer layer, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null);
        public void InsertLayer(int index, IAnimationLayer layer, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null);
        public void AddLayers(ICollection<IAnimationLayer> toAdd, bool instantiate = true, string prefix = "", List<IAnimationLayer> outList = null, bool onlyOutputNew = false, IAnimationController animationController = null);
        public int LayerCount { get; }
        public IAnimationLayer GetLayer(int layerIndex);
        public int FindLayerIndex(string layerName);
        public IAnimationLayer FindLayer(string layerName);
        public bool RemoveLayer(IAnimationLayer layer, bool dispose = true);
        public bool RemoveLayer(int layerIndex, bool dispose = true);
        public bool RemoveLayer(string layerName, bool dispose = true);
        public int RemoveLayersStartingWith(string prefix, bool dispose = true);

        public Dictionary<int, int> RearrangeLayer(int layerIndex, int swapIndex, bool recalculateIndices = true);
        public void RearrangeLayerNoRemap(int layerIndex, int swapIndex, bool recalculateIndices = true);
        public Dictionary<int, int> MoveLayer(int layerIndex, int newIndex, bool recalculateIndices = true);
        public void MoveLayerNoRemap(int layerIndex, int newIndex, bool recalculateIndices = true); 
        public Dictionary<int, int> RecalculateLayerIndices();
        public void RecalculateLayerIndicesNoRemap();

        public bool IsLayerActive(int index);

        public void UpdateStep(float deltaTime);
        public void LateUpdateStep(float deltaTime);


        #region Transform Hierarchies

        public int TransformHierarchyCount { get; }
        public TransformHierarchy GetTransformHierarchy(int index);
        public TransformHierarchy GetTransformHierarchyUnsafe(int index);
        public TransformHierarchy GetTransformHierarchy(int[] transformIndices);

        public int GetTransformIndex(EngineInternal.ITransform transform);

        #endregion

    }

    public class TransformHierarchy
    {

        protected IAnimator animator;
        protected int m_index;
        public int Index => m_index;
        protected int[] m_transformIndices;
        public int Count => m_transformIndices == null ? 0 : m_transformIndices.Length;

        public TransformHierarchy(IAnimator animator, int index, int[] transformIndices)
        {

            this.animator = animator;
            m_index = index;
            m_transformIndices = transformIndices;
            parent = -1;

        }

        public int parent = -1;

        public bool Contains(int transformIndex)
        {

            if (m_transformIndices == null) return false;

            for (int a = 0; a < m_transformIndices.Length; a++) if (m_transformIndices[a] == transformIndex) return true;

            return false;

        }

        public bool Contains(int[] exTransformIndices)
        {

            if (m_transformIndices == null) return false;

            if (exTransformIndices != null) for (int a = 0; a < exTransformIndices.Length; a++) if (!Contains(exTransformIndices[a])) return false;

            return true;

        }

        public bool Contains(TransformHierarchy other)
        {

            return Contains(other.m_transformIndices);

        }

        public bool IsParentOf(TransformHierarchy other)
        {
            if (other == null) return false;
            return other.IsChildOf(this);
        }

        public bool IsChildOf(TransformHierarchy other)
        {

            if (parent < 0 || other == null) return false;
            if (parent == other.Index) return true;

            return animator.GetTransformHierarchyUnsafe(parent).IsChildOf(other);

        }
        public bool IsDerivative(int index)
        {

            if (Index == index) return true;

            return parent >= 0 && animator.GetTransformHierarchyUnsafe(parent).IsDerivative(index);

        }

        public bool IsDerivative(TransformHierarchy other) => other != null && IsDerivative(other.Index);

        public bool IsIdentical(TransformHierarchy other)
        {

            if (other == null) return false;

            return other.Count == Count && Contains(other);

        }
        public bool IsIdentical(int[] hierarchy)
        {

            if (hierarchy == null) return false;

            return hierarchy.Length == Count && Contains(hierarchy);

        }

        public int RootIndex
        {
            get
            {

                if (parent < 0) return Index;

                int root = parent;
                while (true)
                {
                    int next = animator.GetTransformHierarchyUnsafe(root).parent;
                    if (next >= 0) root = next; else break;
                }

                return root;

            }

        }

    }

}
