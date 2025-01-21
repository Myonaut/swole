#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    public class DefaultHumanoidIK : IKControlProxy
    { 

        [AnimatableProperty]
        public bool Active_ArmLeft { set => SetActiveByProxy(value, boundIndices_armLeft); get => GetActiveByProxy(boundIndices_armLeft); }
        [AnimatableProperty]
        public float Weight_ArmLeft { set => SetWeightByProxy(value, boundIndices_armLeft); get => GetWeightByProxy(boundIndices_armLeft); } 
        [AnimatableProperty]
        public float PositionWeight_ArmLeft { set => SetPositionWeightByProxy(value, boundIndices_armLeft); get => GetPositionWeightByProxy(boundIndices_armLeft); }
        [AnimatableProperty]
        public float RotationWeight_ArmLeft { set => SetRotationWeightByProxy(value, boundIndices_armLeft); get => GetRotationWeightByProxy(boundIndices_armLeft); }
        [AnimatableProperty]
        public float BendGoalWeight_ArmLeft { set => SetBendGoalWeightByProxy(value, boundIndices_armLeft); get => GetBendGoalWeightByProxy(boundIndices_armLeft); }
        public string[] bindings_armLeft; protected List<int> boundIndices_armLeft;

        [AnimatableProperty]
        public bool Active_ArmRight { set => SetActiveByProxy(value, boundIndices_armRight); get => GetActiveByProxy(boundIndices_armRight); }
        [AnimatableProperty]
        public float Weight_ArmRight { set => SetWeightByProxy(value, boundIndices_armRight); get => GetWeightByProxy(boundIndices_armRight); }
        [AnimatableProperty]
        public float PositionWeight_ArmRight { set => SetPositionWeightByProxy(value, boundIndices_armRight); get => GetPositionWeightByProxy(boundIndices_armRight); }
        [AnimatableProperty]
        public float RotationWeight_ArmRight { set => SetRotationWeightByProxy(value, boundIndices_armRight); get => GetRotationWeightByProxy(boundIndices_armRight); }
        [AnimatableProperty]
        public float BendGoalWeight_ArmRight { set => SetBendGoalWeightByProxy(value, boundIndices_armRight); get => GetBendGoalWeightByProxy(boundIndices_armRight); }
        public string[] bindings_armRight; protected List<int> boundIndices_armRight;

        [AnimatableProperty]
        public bool Active_LegLeft { set => SetActiveByProxy(value, boundIndices_legLeft); get => GetActiveByProxy(boundIndices_legLeft); }
        [AnimatableProperty]
        public float Weight_LegLeft { set => SetWeightByProxy(value, boundIndices_legLeft); get => GetWeightByProxy(boundIndices_legLeft); }
        [AnimatableProperty]
        public float PositionWeight_LegLeft { set => SetPositionWeightByProxy(value, boundIndices_legLeft); get => GetPositionWeightByProxy(boundIndices_legLeft); }
        [AnimatableProperty]
        public float RotationWeight_LegLeft { set => SetRotationWeightByProxy(value, boundIndices_legLeft); get => GetRotationWeightByProxy(boundIndices_legLeft); }
        [AnimatableProperty]
        public float BendGoalWeight_LegLeft { set => SetBendGoalWeightByProxy(value, boundIndices_legLeft); get => GetBendGoalWeightByProxy(boundIndices_legLeft); }
        public string[] bindings_legLeft; protected List<int> boundIndices_legLeft;

        [AnimatableProperty]
        public bool Active_LegRight { set => SetActiveByProxy(value, boundIndices_legRight); get => GetActiveByProxy(boundIndices_legRight); }
        [AnimatableProperty]
        public float Weight_LegRight { set => SetWeightByProxy(value, boundIndices_legRight); get => GetWeightByProxy(boundIndices_legRight); }
        [AnimatableProperty]
        public float PositionWeight_LegRight { set => SetPositionWeightByProxy(value, boundIndices_legRight); get => GetPositionWeightByProxy(boundIndices_legRight); }
        [AnimatableProperty]
        public float RotationWeight_LegRight { set => SetRotationWeightByProxy(value, boundIndices_legRight); get => GetRotationWeightByProxy(boundIndices_legRight); }
        [AnimatableProperty]
        public float BendGoalWeight_LegRight { set => SetBendGoalWeightByProxy(value, boundIndices_legRight); get => GetBendGoalWeightByProxy(boundIndices_legRight); }
        public string[] bindings_legRight; protected List<int> boundIndices_legRight;

        public override void Rebind()
        {
            if (Manager == null)
            {
                swole.LogError($"[{nameof(DefaultHumanoidIK)}] Failed to bind proxy '{name}' - No {nameof(CustomIKManager)} instance set or found.");
                return;
            }

            Bind(IKControllersDefault.Arm_Left.ToString(), bindings_armLeft, ref boundIndices_armLeft);
            Bind(IKControllersDefault.Arm_Right.ToString(), bindings_armRight, ref boundIndices_armRight);  

            Bind(IKControllersDefault.Leg_Left.ToString(), bindings_legLeft, ref boundIndices_legLeft);
            Bind(IKControllersDefault.Leg_Right.ToString(), bindings_legRight, ref boundIndices_legRight);
        }
    }
}

#endif