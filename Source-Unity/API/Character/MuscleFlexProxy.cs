#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    /// <summary>
    /// Used by animations to control muscle flex values
    /// </summary>
    public class MuscleFlexProxy : ControlProxy
    {

        private static string[] _muscleGroupNames;
        public static string[] MuscleGroupNames
        {
            get
            {
                if (_muscleGroupNames == null) _muscleGroupNames = Enum.GetNames(typeof(MuscleGroupsDefault));
                return _muscleGroupNames;
            }
        }

        [NonSerialized]
        protected CustomAnimator animator;  
        protected override void OnAwake()
        {
            base.OnAwake();
             
            Initialize();

        }

        protected virtual void Initialize()
        {
            if (Character != null)
            {
                animator = character.GetComponentInChildren<CustomAnimator>();
                if (animator != null)
                {
                    if (animator.OnPostLateUpdate == null) animator.OnPostLateUpdate = new UnityEngine.Events.UnityEvent();
                    animator.OnPreUpdate.RemoveListener(PreAnimationStep);
                    animator.OnPreUpdate.AddListener(PreAnimationStep); 
                    animator.OnPostLateUpdate.RemoveListener(PostAnimationStep);
                    animator.OnPostLateUpdate.AddListener(PostAnimationStep);

                    headTrembleBone?.FindBone(animator);
                    neckTrembleBone?.FindBone(animator);
                    leftArmTrembleBone?.FindBone(animator);
                    rightArmTrembleBone?.FindBone(animator);
                    leftLegTrembleBone?.FindBone(animator);
                    rightLegTrembleBone?.FindBone(animator);

                    if (massOffsets != null)
                    {
                        foreach (var massOffset in massOffsets)
                        {
                            massOffset.boneReference?.FindBone(animator); 
                        }
                    }
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (animator != null && animator.OnPostLateUpdate != null) animator.OnPostLateUpdate.RemoveListener(PostAnimationStep);
        }


        protected override bool IsReadyToBind => Character != null && Character.MuscleGroupCount > 0;

        [SerializeField]
        protected MuscularRenderedCharacter character;
        public MuscularRenderedCharacter Character
        {
            get
            {
                if (character == null) character = gameObject.GetComponent<MuscularRenderedCharacter>();
                return character; 
            }
        }

        protected void SetMassByProxy(float value, List<int> indices)
        {
            if (indices == null || Character == null) return;
            foreach (var index in indices) character.SetMuscleGroupMassUnsafe(index, value);
        }
        protected float GetMassByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Character == null) return 0;
            float value = 0;
            foreach (var index in indices) value += character.GetMuscleGroupMassUnsafe(index);
            return value / indices.Count;
        }

        protected void SetFlexByProxy(float value, List<int> indices)
        {
            if (indices == null || Character == null) return;
            foreach (var index in indices) character.SetMuscleGroupFlexUnsafe(index, value);
        }
        protected float GetFlexByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Character == null) return 0;
            float value = 0;
            foreach (var index in indices) value += character.GetMuscleGroupFlexUnsafe(index);
            return value / indices.Count;
        }

        protected void SetPumpByProxy(float value, List<int> indices)
        {
            if (indices == null || Character == null) return;
            foreach (var index in indices) character.SetMuscleGroupPumpUnsafe(index, value);
        }
        protected float GetPumpByProxy(List<int> indices)
        {
            if (indices == null || indices.Count <= 0 || Character == null) return 0;
            float value = 0;
            foreach (var index in indices) value += character.GetMuscleGroupPumpUnsafe(index);
            return value / indices.Count;
        }

        [AnimatableProperty]
        public float Flex_BicepsLeft
        {
            set => SetFlexByProxy(value, boundIndices_bicepsLeft);
            get => GetFlexByProxy(boundIndices_bicepsLeft);
        }
        public string[] bindings_bicepsLeft;
        protected List<int> boundIndices_bicepsLeft;

        [AnimatableProperty]
        public float Flex_BicepsRight
        {
            set => SetFlexByProxy(value, boundIndices_bicepsRight);
            get => GetFlexByProxy(boundIndices_bicepsRight);
        }
        public string[] bindings_bicepsRight;
        protected List<int> boundIndices_bicepsRight;

        [AnimatableProperty]
        public float Flex_TricepsLeft { set => SetFlexByProxy(value, boundIndices_tricepsLeft); get => GetFlexByProxy(boundIndices_tricepsLeft); }
        public string[] bindings_tricepsLeft; protected List<int> boundIndices_tricepsLeft;

        [AnimatableProperty]
        public float Flex_TricepsRight { set => SetFlexByProxy(value, boundIndices_tricepsRight); get => GetFlexByProxy(boundIndices_tricepsRight); }
        public string[] bindings_tricepsRight; protected List<int> boundIndices_tricepsRight;

        [AnimatableProperty]
        public float Flex_OuterForearmLeft { set => SetFlexByProxy(value, boundIndices_outerForearmLeft); get => GetFlexByProxy(boundIndices_outerForearmLeft); }
        public string[] bindings_outerForearmLeft; protected List<int> boundIndices_outerForearmLeft;

        [AnimatableProperty]
        public float Flex_OuterForearmRight { set => SetFlexByProxy(value, boundIndices_outerForearmRight); get => GetFlexByProxy(boundIndices_outerForearmRight); }
        public string[] bindings_outerForearmRight; protected List<int> boundIndices_outerForearmRight;

        [AnimatableProperty]
        public float Flex_InnerForearmLeft { set => SetFlexByProxy(value, boundIndices_innerForearmLeft); get => GetFlexByProxy(boundIndices_innerForearmLeft); }
        public string[] bindings_innerForearmLeft; protected List<int> boundIndices_innerForearmLeft;

        [AnimatableProperty]
        public float Flex_InnerForearmRight { set => SetFlexByProxy(value, boundIndices_innerForearmRight); get => GetFlexByProxy(boundIndices_innerForearmRight); }
        public string[] bindings_innerForearmRight; protected List<int> boundIndices_innerForearmRight;

        [AnimatableProperty]
        public float Flex_PecsLeft { set => SetFlexByProxy(value, boundIndices_pecsLeft); get => GetFlexByProxy(boundIndices_pecsLeft); }
        public string[] bindings_pecsLeft; protected List<int> boundIndices_pecsLeft;

        [AnimatableProperty]
        public float Flex_PecsRight { set => SetFlexByProxy(value, boundIndices_pecsRight); get => GetFlexByProxy(boundIndices_pecsRight); }
        public string[] bindings_pecsRight; protected List<int> boundIndices_pecsRight;

        [AnimatableProperty]
        public float Flex_Neck { set => SetFlexByProxy(value, boundIndices_neck); get => GetFlexByProxy(boundIndices_neck); }
        public string[] bindings_neck; protected List<int> boundIndices_neck;

        [AnimatableProperty]
        public float Flex_TrapsLeft { set => SetFlexByProxy(value, boundIndices_trapsLeft); get => GetFlexByProxy(boundIndices_trapsLeft); }
        public string[] bindings_trapsLeft; protected List<int> boundIndices_trapsLeft;

        [AnimatableProperty]
        public float Flex_TrapsRight { set => SetFlexByProxy(value, boundIndices_trapsRight); get => GetFlexByProxy(boundIndices_trapsRight); }
        public string[] bindings_trapsRight; protected List<int> boundIndices_trapsRight;

        [AnimatableProperty]
        public float Flex_ShouldersLeft { set => SetFlexByProxy(value, boundIndices_shouldersLeft); get => GetFlexByProxy(boundIndices_shouldersLeft); }
        public string[] bindings_shouldersLeft; protected List<int> boundIndices_shouldersLeft;

        [AnimatableProperty]
        public float Flex_ShouldersRight { set => SetFlexByProxy(value, boundIndices_shouldersRight); get => GetFlexByProxy(boundIndices_shouldersRight); }
        public string[] bindings_shouldersRight; protected List<int> boundIndices_shouldersRight;

        [AnimatableProperty]
        public float Flex_ScapulaLeft { set => SetFlexByProxy(value, boundIndices_scapulaLeft); get => GetFlexByProxy(boundIndices_scapulaLeft); }
        public string[] bindings_scapulaLeft; protected List<int> boundIndices_scapulaLeft;

        [AnimatableProperty]
        public float Flex_ScapulaRight { set => SetFlexByProxy(value, boundIndices_scapulaRight); get => GetFlexByProxy(boundIndices_scapulaRight); }
        public string[] bindings_scapulaRight; protected List<int> boundIndices_scapulaRight;

        [AnimatableProperty]
        public float Flex_LowerTrapsLeft { set => SetFlexByProxy(value, boundIndices_lowerTrapsLeft); get => GetFlexByProxy(boundIndices_lowerTrapsLeft); }
        public string[] bindings_lowerTrapsLeft; protected List<int> boundIndices_lowerTrapsLeft;

        [AnimatableProperty]
        public float Flex_LowerTrapsRight { set => SetFlexByProxy(value, boundIndices_lowerTrapsRight); get => GetFlexByProxy(boundIndices_lowerTrapsRight); }
        public string[] bindings_lowerTrapsRight; protected List<int> boundIndices_lowerTrapsRight;

        [AnimatableProperty]
        public float Flex_LatsLeft { set => SetFlexByProxy(value, boundIndices_latsLeft); get => GetFlexByProxy(boundIndices_latsLeft); }
        public string[] bindings_latsLeft; protected List<int> boundIndices_latsLeft;

        [AnimatableProperty]
        public float Flex_LatsRight { set => SetFlexByProxy(value, boundIndices_latsRight); get => GetFlexByProxy(boundIndices_latsRight); }
        public string[] bindings_latsRight; protected List<int> boundIndices_latsRight;

        [AnimatableProperty]
        public float Flex_TLF_Left { set => SetFlexByProxy(value, boundIndices_TLF_Left); get => GetFlexByProxy(boundIndices_TLF_Left); }
        public string[] bindings_TLF_Left; protected List<int> boundIndices_TLF_Left;

        [AnimatableProperty]
        public float Flex_TLF_Right { set => SetFlexByProxy(value, boundIndices_TLF_Right); get => GetFlexByProxy(boundIndices_TLF_Right); }
        public string[] bindings_TLF_Right; protected List<int> boundIndices_TLF_Right;

        [AnimatableProperty]
        public float Flex_AbsALeft { set => SetFlexByProxy(value, boundIndices_absALeft); get => GetFlexByProxy(boundIndices_absALeft); }
        public string[] bindings_absALeft; protected List<int> boundIndices_absALeft;

        [AnimatableProperty]
        public float Flex_AbsARight { set => SetFlexByProxy(value, boundIndices_absARight); get => GetFlexByProxy(boundIndices_absARight); }
        public string[] bindings_absARight; protected List<int> boundIndices_absARight;

        [AnimatableProperty]
        public float Flex_AbsBLeft { set => SetFlexByProxy(value, boundIndices_absBLeft); get => GetFlexByProxy(boundIndices_absBLeft); }
        public string[] bindings_absBLeft; protected List<int> boundIndices_absBLeft;

        [AnimatableProperty]
        public float Flex_AbsBRight { set => SetFlexByProxy(value, boundIndices_absBRight); get => GetFlexByProxy(boundIndices_absBRight); }
        public string[] bindings_absBRight; protected List<int> boundIndices_absBRight;

        [AnimatableProperty]
        public float Flex_AbsCLeft { set => SetFlexByProxy(value, boundIndices_absCLeft); get => GetFlexByProxy(boundIndices_absCLeft); }
        public string[] bindings_absCLeft; protected List<int> boundIndices_absCLeft;

        [AnimatableProperty]
        public float Flex_AbsCRight { set => SetFlexByProxy(value, boundIndices_absCRight); get => GetFlexByProxy(boundIndices_absCRight); }
        public string[] bindings_absCRight; protected List<int> boundIndices_absCRight;

        [AnimatableProperty]
        public float Flex_AbsDLeft { set => SetFlexByProxy(value, boundIndices_absDLeft); get => GetFlexByProxy(boundIndices_absDLeft); }
        public string[] bindings_absDLeft; protected List<int> boundIndices_absDLeft;

        [AnimatableProperty]
        public float Flex_AbsDRight { set => SetFlexByProxy(value, boundIndices_absDRight); get => GetFlexByProxy(boundIndices_absDRight); }
        public string[] bindings_absDRight; protected List<int> boundIndices_absDRight;

        [AnimatableProperty]
        public float Flex_Pelvic { set => SetFlexByProxy(value, boundIndices_pelvic); get => GetFlexByProxy(boundIndices_pelvic); }
        public string[] bindings_pelvic; protected List<int> boundIndices_pelvic;

        [AnimatableProperty]
        public float Flex_LowerObliquesLeft { set => SetFlexByProxy(value, boundIndices_lowerObliquesLeft); get => GetFlexByProxy(boundIndices_lowerObliquesLeft); }
        public string[] bindings_lowerObliquesLeft; protected List<int> boundIndices_lowerObliquesLeft;

        [AnimatableProperty]
        public float Flex_LowerObliquesRight { set => SetFlexByProxy(value, boundIndices_lowerObliquesRight); get => GetFlexByProxy(boundIndices_lowerObliquesRight); }
        public string[] bindings_lowerObliquesRight; protected List<int> boundIndices_lowerObliquesRight;

        [AnimatableProperty]
        public float Flex_UpperObliquesLeft { set => SetFlexByProxy(value, boundIndices_upperObliquesLeft); get => GetFlexByProxy(boundIndices_upperObliquesLeft); }
        public string[] bindings_upperObliquesLeft; protected List<int> boundIndices_upperObliquesLeft;

        [AnimatableProperty]
        public float Flex_UpperObliquesRight { set => SetFlexByProxy(value, boundIndices_upperObliquesRight); get => GetFlexByProxy(boundIndices_upperObliquesRight); }
        public string[] bindings_upperObliquesRight; protected List<int> boundIndices_upperObliquesRight;

        [AnimatableProperty]
        public float Flex_SerratusLeft { set => SetFlexByProxy(value, boundIndices_serratusLeft); get => GetFlexByProxy(boundIndices_serratusLeft); }
        public string[] bindings_serratusLeft; protected List<int> boundIndices_serratusLeft;

        [AnimatableProperty]
        public float Flex_SerratusRight { set => SetFlexByProxy(value, boundIndices_serratusRight); get => GetFlexByProxy(boundIndices_serratusRight); }
        public string[] bindings_serratusRight; protected List<int> boundIndices_serratusRight;

        [AnimatableProperty]
        public float Flex_QuadsLeft { set => SetFlexByProxy(value, boundIndices_quadsLeft); get => GetFlexByProxy(boundIndices_quadsLeft); }
        public string[] bindings_quadsLeft; protected List<int> boundIndices_quadsLeft;

        [AnimatableProperty]
        public float Flex_QuadsRight { set => SetFlexByProxy(value, boundIndices_quadsRight); get => GetFlexByProxy(boundIndices_quadsRight); }
        public string[] bindings_quadsRight; protected List<int> boundIndices_quadsRight;

        [AnimatableProperty]
        public float Flex_OuterLegLeft { set => SetFlexByProxy(value, boundIndices_outerLegLeft); get => GetFlexByProxy(boundIndices_outerLegLeft); }
        public string[] bindings_outerLegLeft; protected List<int> boundIndices_outerLegLeft;

        [AnimatableProperty]
        public float Flex_OuterLegRight { set => SetFlexByProxy(value, boundIndices_outerLegRight); get => GetFlexByProxy(boundIndices_outerLegRight); }
        public string[] bindings_outerLegRight; protected List<int> boundIndices_outerLegRight;

        [AnimatableProperty]
        public float Flex_HamstringsLeft { set => SetFlexByProxy(value, boundIndices_hamstringsLeft); get => GetFlexByProxy(boundIndices_hamstringsLeft); }
        public string[] bindings_hamstringsLeft; protected List<int> boundIndices_hamstringsLeft;

        [AnimatableProperty]
        public float Flex_HamstringsRight { set => SetFlexByProxy(value, boundIndices_hamstringsRight); get => GetFlexByProxy(boundIndices_hamstringsRight); }
        public string[] bindings_hamstringsRight; protected List<int> boundIndices_hamstringsRight;

        [AnimatableProperty]
        public float Flex_InnerLegLeft { set => SetFlexByProxy(value, boundIndices_innerLegLeft); get => GetFlexByProxy(boundIndices_innerLegLeft); }
        public string[] bindings_innerLegLeft; protected List<int> boundIndices_innerLegLeft;

        [AnimatableProperty]
        public float Flex_InnerLegRight { set => SetFlexByProxy(value, boundIndices_innerLegRight); get => GetFlexByProxy(boundIndices_innerLegRight); }
        public string[] bindings_innerLegRight; protected List<int> boundIndices_innerLegRight;

        [AnimatableProperty]
        public float Flex_GlutesLeft { set => SetFlexByProxy(value, boundIndices_glutesLeft); get => GetFlexByProxy(boundIndices_glutesLeft); }
        public string[] bindings_glutesLeft; protected List<int> boundIndices_glutesLeft;

        [AnimatableProperty]
        public float Flex_GlutesRight { set => SetFlexByProxy(value, boundIndices_glutesRight); get => GetFlexByProxy(boundIndices_glutesRight); }
        public string[] bindings_glutesRight; protected List<int> boundIndices_glutesRight;

        [AnimatableProperty]
        public float Flex_CalvesLeft { set => SetFlexByProxy(value, boundIndices_calvesLeft); get => GetFlexByProxy(boundIndices_calvesLeft); }
        public string[] bindings_calvesLeft; protected List<int> boundIndices_calvesLeft;

        [AnimatableProperty]
        public float Flex_CalvesRight { set => SetFlexByProxy(value, boundIndices_calvesRight); get => GetFlexByProxy(boundIndices_calvesRight); }
        public string[] bindings_calvesRight; protected List<int> boundIndices_calvesRight;

        [AnimatableProperty]
        public float Flex_TFL_Left { set => SetFlexByProxy(value, boundIndices_TFL_Left); get => GetFlexByProxy(boundIndices_TFL_Left); }
        public string[] bindings_TFL_Left; protected List<int> boundIndices_TFL_Left;

        [AnimatableProperty]
        public float Flex_TFL_Right { set => SetFlexByProxy(value, boundIndices_TFL_Right); get => GetFlexByProxy(boundIndices_TFL_Right); }
        public string[] bindings_TFL_Right; protected List<int> boundIndices_TFL_Right;

        public override int FindBindingIndex(string binding)
        {
            if (Character == null) return -1;
            return character.FindMuscleGroup(binding); 
        }

        public override void Rebind()
        {
            if (Character == null) 
            {
                swole.LogError($"[{nameof(MuscleFlexProxy)}] Failed to bind proxy '{name}' - No {nameof(MuscularRenderedCharacter)} instance set or found.");
                return; 
            }

            Bind(MuscleGroupsDefault.Biceps_Left.ToString(), bindings_bicepsLeft, ref boundIndices_bicepsLeft); 
            Bind(MuscleGroupsDefault.Biceps_Right.ToString(), bindings_bicepsRight, ref boundIndices_bicepsRight); 
            Bind(MuscleGroupsDefault.Triceps_Left.ToString(), bindings_tricepsLeft, ref boundIndices_tricepsLeft); 
            Bind(MuscleGroupsDefault.Triceps_Right.ToString(), bindings_tricepsRight, ref boundIndices_tricepsRight); 
            Bind(MuscleGroupsDefault.Outer_Forearm_Left.ToString(), bindings_outerForearmLeft, ref boundIndices_outerForearmLeft); 
            Bind(MuscleGroupsDefault.Outer_Forearm_Right.ToString(), bindings_outerForearmRight, ref boundIndices_outerForearmRight); 
            Bind(MuscleGroupsDefault.Inner_Forearm_Left.ToString(), bindings_innerForearmLeft, ref boundIndices_innerForearmLeft); 
            Bind(MuscleGroupsDefault.Inner_Forearm_Right.ToString(), bindings_innerForearmRight, ref boundIndices_innerForearmRight); 
            Bind(MuscleGroupsDefault.Pecs_Left.ToString(), bindings_pecsLeft, ref boundIndices_pecsLeft); 
            Bind(MuscleGroupsDefault.Pecs_Right.ToString(), bindings_pecsRight, ref boundIndices_pecsRight); 
            Bind(MuscleGroupsDefault.Neck.ToString(), bindings_neck, ref boundIndices_neck); 
            Bind(MuscleGroupsDefault.Traps_Left.ToString(), bindings_trapsLeft, ref boundIndices_trapsLeft); 
            Bind(MuscleGroupsDefault.Traps_Right.ToString(), bindings_trapsRight, ref boundIndices_trapsRight); 
            Bind(MuscleGroupsDefault.Shoulders_Left.ToString(), bindings_shouldersLeft, ref boundIndices_shouldersLeft); 
            Bind(MuscleGroupsDefault.Shoulders_Right.ToString(), bindings_shouldersRight, ref boundIndices_shouldersRight);
            Bind(MuscleGroupsDefault.Scapula_Left.ToString(), bindings_scapulaLeft, ref boundIndices_scapulaLeft); 
            Bind(MuscleGroupsDefault.Scapula_Right.ToString(), bindings_scapulaRight, ref boundIndices_scapulaRight); 
            Bind(MuscleGroupsDefault.Lower_Traps_Left.ToString(), bindings_lowerTrapsLeft, ref boundIndices_lowerTrapsLeft); 
            Bind(MuscleGroupsDefault.Lower_Traps_Right.ToString(), bindings_lowerTrapsRight, ref boundIndices_lowerTrapsRight); 
            Bind(MuscleGroupsDefault.Lats_Left.ToString(), bindings_latsLeft, ref boundIndices_latsLeft); 
            Bind(MuscleGroupsDefault.Lats_Right.ToString(), bindings_latsRight, ref boundIndices_latsRight); 
            Bind(MuscleGroupsDefault.TLF_Left.ToString(), bindings_TLF_Left, ref boundIndices_TLF_Left); 
            Bind(MuscleGroupsDefault.TLF_Right.ToString(), bindings_TLF_Right, ref boundIndices_TLF_Right); 
            Bind(MuscleGroupsDefault.Abs_A_Left.ToString(), bindings_absALeft, ref boundIndices_absALeft); 
            Bind(MuscleGroupsDefault.Abs_A_Right.ToString(), bindings_absARight, ref boundIndices_absARight); 
            Bind(MuscleGroupsDefault.Abs_B_Left.ToString(), bindings_absBLeft, ref boundIndices_absBLeft); 
            Bind(MuscleGroupsDefault.Abs_B_Right.ToString(), bindings_absBRight, ref boundIndices_absBRight); 
            Bind(MuscleGroupsDefault.Abs_C_Left.ToString(), bindings_absCLeft, ref boundIndices_absCLeft); 
            Bind(MuscleGroupsDefault.Abs_C_Right.ToString(), bindings_absCRight, ref boundIndices_absCRight); 
            Bind(MuscleGroupsDefault.Abs_D_Left.ToString(), bindings_absDLeft, ref boundIndices_absDLeft); 
            Bind(MuscleGroupsDefault.Abs_D_Right.ToString(), bindings_absDRight, ref boundIndices_absDRight); 
            Bind(MuscleGroupsDefault.Pelvic.ToString(), bindings_pelvic, ref boundIndices_pelvic); 
            Bind(MuscleGroupsDefault.Lower_Obliques_Left.ToString(), bindings_lowerObliquesLeft, ref boundIndices_lowerObliquesLeft); 
            Bind(MuscleGroupsDefault.Lower_Obliques_Right.ToString(), bindings_lowerObliquesRight, ref boundIndices_lowerObliquesRight); 
            Bind(MuscleGroupsDefault.Upper_Obliques_Left.ToString(), bindings_upperObliquesLeft, ref boundIndices_upperObliquesLeft); 
            Bind(MuscleGroupsDefault.Upper_Obliques_Right.ToString(), bindings_upperObliquesRight, ref boundIndices_upperObliquesRight); 
            Bind(MuscleGroupsDefault.Serratus_Left.ToString(), bindings_serratusLeft, ref boundIndices_serratusLeft); 
            Bind(MuscleGroupsDefault.Serratus_Right.ToString(), bindings_serratusRight, ref boundIndices_serratusRight); 
            Bind(MuscleGroupsDefault.Quads_Left.ToString(), bindings_quadsLeft, ref boundIndices_quadsLeft); 
            Bind(MuscleGroupsDefault.Quads_Right.ToString(), bindings_quadsRight, ref boundIndices_quadsRight); 
            Bind(MuscleGroupsDefault.Outer_Leg_Left.ToString(), bindings_outerLegLeft, ref boundIndices_outerLegLeft); 
            Bind(MuscleGroupsDefault.Outer_Leg_Right.ToString(), bindings_outerLegRight, ref boundIndices_outerLegRight); 
            Bind(MuscleGroupsDefault.Hamstrings_Left.ToString(), bindings_hamstringsLeft, ref boundIndices_hamstringsLeft); 
            Bind(MuscleGroupsDefault.Hamstrings_Right.ToString(), bindings_hamstringsRight, ref boundIndices_hamstringsRight); 
            Bind(MuscleGroupsDefault.Inner_Leg_Left.ToString(), bindings_innerLegLeft, ref boundIndices_innerLegLeft); 
            Bind(MuscleGroupsDefault.Inner_Leg_Right.ToString(), bindings_innerLegRight, ref boundIndices_innerLegRight); 
            Bind(MuscleGroupsDefault.Glutes_Left.ToString(), bindings_glutesLeft, ref boundIndices_glutesLeft); 
            Bind(MuscleGroupsDefault.Glutes_Right.ToString(), bindings_glutesRight, ref boundIndices_glutesRight); 
            Bind(MuscleGroupsDefault.Calves_Left.ToString(), bindings_calvesLeft, ref boundIndices_calvesLeft); 
            Bind(MuscleGroupsDefault.Calves_Right.ToString(), bindings_calvesRight, ref boundIndices_calvesRight); 
            Bind(MuscleGroupsDefault.TFL_Left.ToString(), bindings_TFL_Left, ref boundIndices_TFL_Left); 
            Bind(MuscleGroupsDefault.TFL_Right.ToString(), bindings_TFL_Right, ref boundIndices_TFL_Right);
        }

        protected virtual void PreAnimationStep()
        {
            if (resetTrembleEachFrame) ResetTremble();
            if (resetMassOffsetSettingsEachFrame) ResetMassOffsetSettings(); 
        }
        protected virtual void PostAnimationStep()
        {
            OffsetBonesByMass();
            Tremble();
        }

        [Serializable]
        public class BoneReference
        {
            public string boneName;
            [NonSerialized]
            public Transform reference;

            public void FindBone(CustomAnimator animator)
            {
                if (animator == null || animator.Bones == null) return;

                var boneIndex = animator.GetBoneIndex(boneName);
                if (boneIndex < 0) return;

                reference = UnityEngineHook.AsUnityTransform(animator.GetBone(boneIndex));  
            }
        }

        #region Mass Offsets
        
        [AnimatableProperty(true, 0)]
        public bool disableMassOffsets;
        public bool disableMassOffsetsAsEditor; 

        [AnimatableProperty(true, 1)]
        public float massOffsetsMix = 1;
        [AnimatableProperty(true, 1)]
        public float massOffsetsPositionMix = 1;
        [AnimatableProperty(true, 1)]
        public float massOffsetsRotationMix = 1;

        [AnimatableProperty(true, 0)]
        public bool disableMassArmOffsets;

        [AnimatableProperty(true, 1)]
        public float massArmOffsetsMix = 1;
        [AnimatableProperty(true, 1)]
        public float massArmOffsetsPositionMix = 1;
        [AnimatableProperty(true, 1)]
        public float massArmOffsetsRotationMix = 1;

        [AnimatableProperty(true, 0)]
        public bool disableMassLegOffsets;

        [AnimatableProperty(true, 1)]
        public float massLegOffsetsMix = 1;
        [AnimatableProperty(true, 1)]
        public float massLegOffsetsPositionMix = 1;
        [AnimatableProperty(true, 1)]
        public float massLegOffsetsRotationMix = 1;

        public bool resetMassOffsetSettingsEachFrame = true;

        public virtual void ResetMassOffsetSettings()
        {
            disableMassOffsets = false;
            massOffsetsMix = 1;
            massOffsetsPositionMix = 1;
            massOffsetsRotationMix = 1;

            disableMassArmOffsets = false;
            massArmOffsetsMix = 1;
            massArmOffsetsPositionMix = 1;
            massArmOffsetsRotationMix = 1;

            disableMassLegOffsets = false;
            massLegOffsetsMix = 1;
            massLegOffsetsPositionMix = 1;
            massLegOffsetsRotationMix = 1;

        }

        [Serializable]
        public enum MassOffsetTarget
        {
            Varied, Arms, Legs
        }

        [Serializable]
        public enum MassConditionCombination
        {
            Average, WeightedAverage, Minimum, Maximum
        }

        public static float GetMassConditionFactor(MuscleFlexProxy proxy, MassConditionCombination conditionCombinationType, MassOffsetCondition[] conditions)
        {
            float factor = 1;
            if (conditions != null && conditions.Length > 0)
            {
                factor = 0; 
                switch (conditionCombinationType)
                {
                    case MassConditionCombination.Average:
                        foreach (var condition in conditions)
                        {
                            factor = factor + condition.GetFactor(proxy);  
                        }
                        factor = factor / conditions.Length;
                        break;
                    case MassConditionCombination.WeightedAverage:
                        float div = 0;
                        foreach (var condition in conditions) 
                        { 
                            factor = factor + condition.GetFactor(proxy) * condition.weight;
                            div = div + condition.weight; 
                        }
                        factor = factor / div;
                        break;

                    case MassConditionCombination.Minimum:
                        factor = float.MaxValue;
                        foreach (var condition in conditions) factor = Mathf.Min(factor, condition.GetFactor(proxy));
                        
                        break;

                    case MassConditionCombination.Maximum: 
                        factor = float.MinValue; 
                        foreach (var condition in conditions) factor = Mathf.Max(factor, condition.GetFactor(proxy));
                        break;
                }

            }

            return factor;
        }

        [Serializable]
        //public struct MassOffsetCondition
        public class MassOffsetCondition // make it a class so we can cache the muscleGroupIndex
        {
            public MuscleGroupsDefault muscleGroup;
            public float weight;
            public float mass;

            [NonSerialized] 
            protected int muscleGroupIndex;

            public float GetFactor(MuscleFlexProxy proxy)
            {
                if (mass == 0) return 1;

                /*
                switch(muscleGroup)
                {
                    case MuscleGroupsDefault.Biceps_Left:
                        return proxy.GetMassByProxy(proxy.boundIndices_bicepsLeft) / mass;
                }
                */

                // The lazy way
                if (muscleGroupIndex == 0) muscleGroupIndex = proxy.character.GetMuscleGroupIndex(muscleGroup.ToString()) + 1;
                return proxy.character.GetMuscleGroupMassUnsafe(muscleGroupIndex - 1) / mass;  
            }
        }

        [Serializable]
        public struct MassOffset
        {
            public BoneReference boneReference;
            public MassOffsetTarget massOffsetTarget;

            public MassOffsetCondition[] offsetCondition;
            public MassConditionCombination offsetConditionCombination;
            public bool combineWithParentOffsetCondition;
            public int parentOffsetConditionIndex;
            public Vector3 axis;
            public float distance;

            public MassOffsetCondition[] rotationCondition;
            public MassConditionCombination rotationConditionCombination;
            public bool combineWithOffsetCondition;
            public bool combineWithParentRotationCondition;
            public int parentRotationConditionIndex;
            public Vector3 rotationOffset;

            public void Apply(MuscleFlexProxy proxy)
            {
                if (boneReference == null || boneReference.reference == null) return;

                float offsetFactor = 1;
                float rotationFactor = 1; 
                switch(massOffsetTarget)
                {
                    case MassOffsetTarget.Arms:
                        if (proxy.disableMassArmOffsets) return;

                        offsetFactor = proxy.massArmOffsetsMix * proxy.massArmOffsetsPositionMix;
                        rotationFactor = proxy.massArmOffsetsMix * proxy.massArmOffsetsRotationMix;
                        break;

                    case MassOffsetTarget.Legs:
                        if (proxy.disableMassLegOffsets) return;

                        offsetFactor = proxy.massLegOffsetsMix * proxy.massLegOffsetsPositionMix;
                        rotationFactor = proxy.massLegOffsetsMix * proxy.massLegOffsetsRotationMix;
                        break;
                }

                offsetFactor = offsetFactor * GetMassConditionFactor(proxy, offsetConditionCombination, offsetCondition) * proxy.massOffsetsMix * proxy.massOffsetsPositionMix;
                rotationFactor = rotationFactor * GetMassConditionFactor(proxy, rotationConditionCombination, rotationCondition) * proxy.massOffsetsMix * proxy.massLegOffsetsRotationMix;
                 
                if (combineWithParentOffsetCondition && parentOffsetConditionIndex >= 0)
                {
                    var parent = proxy.massOffsets[parentOffsetConditionIndex];
                    offsetFactor = offsetFactor * GetMassConditionFactor(proxy, parent.offsetConditionCombination, parent.offsetCondition);
                }
                if (combineWithParentRotationCondition && parentRotationConditionIndex >= 0)
                {
                    var parent = proxy.massOffsets[parentRotationConditionIndex];
                    rotationFactor = rotationFactor * GetMassConditionFactor(proxy, parent.rotationConditionCombination, parent.rotationCondition); 
                }
                if (combineWithOffsetCondition) rotationFactor = offsetFactor * rotationFactor;

                boneReference.reference.localPosition = boneReference.reference.localPosition + axis * distance * offsetFactor;   
                if (rotationFactor != 0) boneReference.reference.localRotation = (Quaternion.SlerpUnclamped(Quaternion.identity, Quaternion.Euler(rotationOffset), rotationFactor)) * boneReference.reference.localRotation;
            }
        }

        public MassOffset[] massOffsets;

        protected virtual void OffsetBonesByMass()
        {
            if (disableMassOffsets || disableMassOffsetsAsEditor) return; 

            if (massOffsets != null)
            {
                foreach (var massOffset in massOffsets) massOffset.Apply(this); 
            }
        }

        #endregion

        #region Trembling

        public bool disableTrembling;
        public bool resetTrembleEachFrame = true;

        public float baseTrembleFactor = 0.005f;
        public float baseTrembleFactorRot = 3;

        public BoneReference headTrembleBone;
        public BoneReference neckTrembleBone;
        [AnimatableProperty, Range(0, 1)]
        public float trembleHead; 

        public BoneReference leftArmTrembleBone;
        [AnimatableProperty, Range(0, 1)]
        public float trembleLeftArm;
        public BoneReference rightArmTrembleBone;  
        [AnimatableProperty, Range(0, 1)]
        public float trembleRightArm;

        public BoneReference leftLegTrembleBone;
        [AnimatableProperty, Range(0, 1)]
        public float trembleLeftLeg;
        public BoneReference rightLegTrembleBone;
        [AnimatableProperty, Range(0, 1)]
        public float trembleRightLeg;

        protected Vector3 NextTrembleOffset => (new Vector3((UnityEngine.Random.value - 0.5f) * 2, (UnityEngine.Random.value - 0.5f) * 2, (UnityEngine.Random.value - 0.5f) * 2) * baseTrembleFactor);
        protected Quaternion NextTrembleOffsetRot => Quaternion.Euler(new Vector3((UnityEngine.Random.value - 0.5f) * 2, (UnityEngine.Random.value - 0.5f) * 2, (UnityEngine.Random.value - 0.5f) * 2) * baseTrembleFactorRot);
        public virtual void ResetTremble()
        {
            trembleHead = 0;
            trembleLeftArm = 0;
            trembleRightArm = 0;
            trembleLeftLeg = 0;
            trembleRightLeg = 0;
        }
        protected virtual void Tremble()
        {
            if (disableTrembling) return;

            if (trembleHead > 0 && headTrembleBone != null && headTrembleBone.reference != null)
            {
                headTrembleBone.reference.localRotation = Quaternion.SlerpUnclamped(Quaternion.identity, NextTrembleOffsetRot, trembleHead) * headTrembleBone.reference.localRotation; 
            }

            if (trembleHead > 0 && neckTrembleBone != null && neckTrembleBone.reference != null)
            {
                neckTrembleBone.reference.localRotation = Quaternion.SlerpUnclamped(Quaternion.identity, NextTrembleOffsetRot, trembleHead * 0.6f) * neckTrembleBone.reference.localRotation;   
            }

            if (trembleLeftArm > 0 && leftArmTrembleBone != null && leftArmTrembleBone.reference != null)
            {
                leftArmTrembleBone.reference.localPosition = leftArmTrembleBone.reference.localPosition + NextTrembleOffset * trembleLeftArm;  
            }
            if (trembleRightArm > 0 && rightArmTrembleBone != null && rightArmTrembleBone.reference != null)
            {
                rightArmTrembleBone.reference.localPosition = rightArmTrembleBone.reference.localPosition + NextTrembleOffset * trembleRightArm;
            }

            if (trembleLeftLeg > 0 && leftLegTrembleBone != null && leftLegTrembleBone.reference != null)
            {
                leftLegTrembleBone.reference.localPosition = leftLegTrembleBone.reference.localPosition + NextTrembleOffset * trembleLeftLeg;
            }
            if (trembleRightLeg > 0 && rightLegTrembleBone != null && rightLegTrembleBone.reference != null)
            {
                rightLegTrembleBone.reference.localPosition = rightLegTrembleBone.reference.localPosition + NextTrembleOffset * trembleRightLeg;
            }
        }

        #endregion

    }
}

#endif
