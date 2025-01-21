#if (UNITY_STANDALONE || UNITY_EDITOR)

using UnityEngine;

namespace Swole.API.Unity
{
    public class ToonHeadController : MonoBehaviour
    {
        public SkinnedMeshRenderer dimplesRenderer; 
        [AnimatableProperty]
        public bool DimpleVisibility 
        { 
            get => dimplesRenderer == null ? false : dimplesRenderer.enabled;
            set
            {
                if (dimplesRenderer == null) return;
                dimplesRenderer.enabled = value;
            }
        }

        public SkinnedMeshRenderer teethRenderer;
        [AnimatableProperty(true, 1)]
        public bool TeethVisibility
        {
            get => teethRenderer == null ? false : teethRenderer.enabled;
            set
            {
                if (teethRenderer == null) return;
                teethRenderer.enabled = value;
            }
        }

        public SkinnedMeshRenderer teethDetailedRenderer;
        [AnimatableProperty]
        public bool TeethDetailedVisibility
        {
            get => teethDetailedRenderer == null ? false : teethDetailedRenderer.enabled;
            set
            {
                if (teethDetailedRenderer == null) return;
                teethDetailedRenderer.enabled = value;
            }
        }

        public SkinnedMeshRenderer tongueRenderer;
        [AnimatableProperty(true, 1)]
        public bool TongueVisibility
        {
            get => tongueRenderer == null ? false : tongueRenderer.enabled;
            set
            {
                if (tongueRenderer == null) return;
                tongueRenderer.enabled = value;
            }
        }

        [AnimatableProperty(true, 1)]
        public float ExpressionFactor
        {
            get => GetExpressionFactor();
            set => SetExpressionFactor(value);
        }
        protected float expressionFactor;
        public virtual void SetExpressionFactor(float value)
        {
            expressionFactor = value; 
        }
        public virtual float GetExpressionFactor()
        {
            return expressionFactor;
        }

        protected void Awake()
        {
            DimpleVisibility = false;
            TeethVisibility = true;
            TeethDetailedVisibility = false;
            TongueVisibility = true;
        }
    }
}

#endif