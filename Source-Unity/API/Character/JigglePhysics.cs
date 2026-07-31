#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Unity.Mathematics;

using Swole;
using Swole.API.Unity;
using Swole.Script;
using Swole.Morphing;

public class JigglePhysics : MonoBehaviour, IExecutableBehaviour
{

    [Serializable]
    public enum CharacterGroupTarget
    {
        BreastL, BreastR, Breasts, MuscleGroupL, MuscleGroupR, MuscleGroup, FatGroup
    }

    [Serializable]
    public enum GroupCombineMode
    {
        Additive, Min, Max
    }

    [Serializable]
    public class CharacterGroupTargetSync
    {
        public CharacterGroupTarget target;
        public string groupName;
        [NonSerialized]
        private int groupIndex = -1;
        public CustomizableCharacterMeshBase characterMesh;
        public float muscleFlexStiffnessMultiplierAdd;
        public float muscleFlexMixMultiplierAdd;

        public Vector2 stiffnessMultiplierRange = new Vector2(1f, 1f);
        public Vector2 mixMultiplierRange = new Vector2(1f, 1f);
        public Vector2 groupValueRange = new Vector2(0f, 1f);

        public bool clamp;
        public float contributionWeight = 1f;

        private UnityAction<int> listener;

        public void StartListening(string debugName)
        {
            if (characterMesh == null) return; 

            if (groupIndex < 0)
            {
                switch (target)
                {
                    case CharacterGroupTarget.MuscleGroupL:
                    case CharacterGroupTarget.MuscleGroupR:
                    case CharacterGroupTarget.MuscleGroup:
                        groupIndex = characterMesh.IndexOfMuscleGroup(groupName);
                        if (groupIndex < 0)
                        {
                            Debug.LogWarning($"{debugName}: Could not find muscle group '{groupName}' on character mesh '{characterMesh.name}'");
                            return;
                        }
                        break;

                    case CharacterGroupTarget.FatGroup:
                        groupIndex = characterMesh.IndexOfFatGroup(groupName);
                        if (groupIndex < 0)
                        {
                            Debug.LogWarning($"{debugName}: Could not find fat group '{groupName}' on character mesh '{characterMesh.name}'"); 
                            return;
                        }
                        break;
                }
            } 

            if (listener == null) 
            {
                listener = (int groupIndex) =>
                {
                    if (characterMesh == null) return;

                    switch(target)
                    {
                        case CharacterGroupTarget.BreastL:
                            {
                                if (characterMesh is CustomizableCharacterMeshV2 meshV2)
                                {
                                    //OnValueChanged(meshV2.BustSizeSplit.x);
                                    OnValueChanged(characterMesh.BustSize);
                                }
                                else
                                {
                                    OnValueChanged(characterMesh.BustSize);
                                }
                            }
                            break;

                        case CharacterGroupTarget.BreastR:
                            {
                                if (characterMesh is CustomizableCharacterMeshV2 meshV2)
                                {
                                    //OnValueChanged(meshV2.BustSizeSplit.y);
                                    OnValueChanged(characterMesh.BustSize);
                                }
                                else
                                {
                                    OnValueChanged(characterMesh.BustSize);
                                }
                            }
                            break;
                        case CharacterGroupTarget.Breasts:
                            {
                                OnValueChanged(characterMesh.BustSize);
                            }
                            break;
                        case CharacterGroupTarget.MuscleGroupL:
                            if (groupIndex == this.groupIndex)
                            {
#if UNITY_EDITOR
                                if (groupIndex < 0)
                                {
                                    Debug.LogError($"{debugName}: Tried to update muscle L sync target with invalid group index {groupIndex} and name '{groupName}'");
                                    return;
                                }
#endif
                                OnValueChanged(characterMesh.GetMuscleDataUnsafe(groupIndex).valuesLeft.mass);
                            }
                            break;
                        case CharacterGroupTarget.MuscleGroupR:
                            if (groupIndex == this.groupIndex)
                            {
#if UNITY_EDITOR
                                if (groupIndex < 0)
                                {
                                    Debug.LogError($"{debugName}: Tried to update muscle R sync target with invalid group index {groupIndex} and name '{groupName}'");
                                    return;
                                }
#endif
                                OnValueChanged(characterMesh.GetMuscleDataUnsafe(groupIndex).valuesRight.mass);
                            }
                            break;
                        case CharacterGroupTarget.MuscleGroup:
                            if (groupIndex == this.groupIndex)
                            {
#if UNITY_EDITOR
                                if (groupIndex < 0)
                                {
                                    Debug.LogError($"{debugName}: Tried to update muscle sync target with invalid group index {groupIndex} and name '{groupName}'");
                                    return;
                                }
#endif

                                var data = characterMesh.GetMuscleDataUnsafe(groupIndex);
                                OnValueChanged((data.valuesLeft.mass + data.valuesRight.mass) * 0.5f);
                            }
                            break;
                        case CharacterGroupTarget.FatGroup:
                            if (groupIndex == this.groupIndex)
                            {
#if UNITY_EDITOR
                                if (groupIndex < 0)
                                {
                                    Debug.LogError($"{debugName}: Tried to update fat sync target with invalid group index {groupIndex} and name '{groupName}'");
                                    return;
                                }
#endif
                                OnValueChanged(characterMesh.GetFatLevelUnsafe(groupIndex)); 
                            }
                            break;
                    }
                };
            }

            switch (target)
            {
                case CharacterGroupTarget.MuscleGroupL:
                case CharacterGroupTarget.MuscleGroupR:
                case CharacterGroupTarget.MuscleGroup:
                    characterMesh.AddListener(Swole.API.Unity.ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, listener);
                    break;

                case CharacterGroupTarget.FatGroup:
                    characterMesh.AddListener(Swole.API.Unity.ICustomizableCharacter.ListenableEvent.OnFatDataChanged, listener);
                    break;

                case CharacterGroupTarget.BreastL:
                case CharacterGroupTarget.BreastR:
                case CharacterGroupTarget.Breasts:
                    characterMesh.AddListener(Swole.API.Unity.ICustomizableCharacter.ListenableEvent.OnBustDataChanged, listener); 
                    break;
            }

            listener(groupIndex);
        }
        public void StopListening()
        {
            if (listener != null && characterMesh != null)
            {
                switch (target)
                {
                    case CharacterGroupTarget.MuscleGroupL:
                    case CharacterGroupTarget.MuscleGroupR:
                    case CharacterGroupTarget.MuscleGroup:
                        characterMesh.RemoveListener(Swole.API.Unity.ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, listener);
                        break;

                    case CharacterGroupTarget.FatGroup:
                        characterMesh.RemoveListener(Swole.API.Unity.ICustomizableCharacter.ListenableEvent.OnFatDataChanged, listener);
                        break;

                    case CharacterGroupTarget.BreastL:
                    case CharacterGroupTarget.BreastR:
                    case CharacterGroupTarget.Breasts:
                        characterMesh.RemoveListener(Swole.API.Unity.ICustomizableCharacter.ListenableEvent.OnBustDataChanged, listener); 
                        break;
                }
            }
        }

        protected float stiffnessMultiplier = 1f;
        public float StiffnessMultiplier => stiffnessMultiplier;

        protected float mixMultiplier = 1f;
        public float MixMultiplier => mixMultiplier;

        public void OnValueChanged(float value)
        {
            value = math.remap(groupValueRange.x, groupValueRange.y, 0f, 1f, value);
            if (clamp) value = Mathf.Clamp01(value);

            stiffnessMultiplier = Mathf.Lerp(stiffnessMultiplierRange.x, stiffnessMultiplierRange.y, value);
            mixMultiplier = Mathf.Lerp(mixMultiplierRange.x, mixMultiplierRange.y, value); 
        }

        public float RealtimeStiffnessMultiplier
        {
            get
            {
                float mult = 1.0f;

                switch(target)
                {
                    case CharacterGroupTarget.MuscleGroupL:
                        if (!Mathf.Approximately(muscleFlexStiffnessMultiplierAdd, 0f) && characterMesh != null && groupIndex >= 0)
                        {
                            mult += characterMesh.GetMuscleDataUnsafe(groupIndex).valuesLeft.flex * muscleFlexStiffnessMultiplierAdd;
                        }
                        break;

                    case CharacterGroupTarget.MuscleGroupR:
                        if (!Mathf.Approximately(muscleFlexStiffnessMultiplierAdd, 0f) && characterMesh != null && groupIndex >= 0)
                        {
                            mult += characterMesh.GetMuscleDataUnsafe(groupIndex).valuesRight.flex * muscleFlexStiffnessMultiplierAdd;
                        }
                        break;

                    case CharacterGroupTarget.MuscleGroup:
                        if (!Mathf.Approximately(muscleFlexStiffnessMultiplierAdd, 0f) && characterMesh != null && groupIndex >= 0)
                        {
                            var muscleData = characterMesh.GetMuscleDataUnsafe(groupIndex);
                            mult += (((muscleData.valuesLeft.flex + muscleData.valuesRight.flex)) * 0.5f) * muscleFlexStiffnessMultiplierAdd;  
                        }
                        break;
                }

                return mult;
            }
        }

        public float RealtimeMixMultiplier
        {
            get
            {
                float mult = 1.0f;

                switch (target)
                {
                    case CharacterGroupTarget.MuscleGroupL: 
                        if (!Mathf.Approximately(muscleFlexMixMultiplierAdd, 0f) && characterMesh != null && groupIndex >= 0)
                        {
                            mult += characterMesh.GetMuscleDataUnsafe(groupIndex).valuesLeft.flex * muscleFlexMixMultiplierAdd;
                        }
                        break;

                    case CharacterGroupTarget.MuscleGroupR:
                        if (!Mathf.Approximately(muscleFlexMixMultiplierAdd, 0f) && characterMesh != null && groupIndex >= 0)
                        {
                            mult += characterMesh.GetMuscleDataUnsafe(groupIndex).valuesRight.flex * muscleFlexMixMultiplierAdd;
                        }
                        break;

                    case CharacterGroupTarget.MuscleGroup:
                        if (!Mathf.Approximately(muscleFlexMixMultiplierAdd, 0f) && characterMesh != null && groupIndex >= 0)
                        {
                            var muscleData = characterMesh.GetMuscleDataUnsafe(groupIndex);
                            mult += (((muscleData.valuesLeft.flex + muscleData.valuesRight.flex)) * 0.5f) * muscleFlexMixMultiplierAdd; 
                        }
                        break;
                }

                return mult;
            }
        }
    }

    public GroupCombineMode groupCombineMode;
    public CharacterGroupTargetSync[] characterGroupTargetSyncs;
    public bool ignoreTargetSyncContributionWeights;

    public float mix;
    public AnimationCurve mixCurve;

    [Range(1f, 100f), Tooltip("Higher = catches up faster on large translations, Lower = heavier drag")]
    public float catchUpSpeed = 10f;
    [Range(0f, 1f), Tooltip("Blend factor for start of frame local position resets")]
    public float localPositionMix = 0.3f;

    [AnimatableProperty, Range(0f, 1f)]
    public float physicsNerf;

    public Transform mainBone;
    public Transform tipBone;

    public Vector3 boneForwardAxis = Vector3.up;
    public Vector3 boneUpAxis = Vector3.forward;

    private Vector3 velocity;

    [Range(0f, 2f),  Tooltip("--- TUNABLE INERTIAL LAG FILTER ---\r\n Lower this value to reduce how deep a bone will compress into its parent at high speeds. 0.4f to 0.6f is usually the sweet spot to keep it looking heavy but prevent clipping.")]
    public float lagDampening = 0.3f;

    [Tooltip("--- MASTER BOUNCE AMPLIFIER --- Higher values (like 1.8f or 2.5f) multiply the incoming animation impulses uniformly.")]
    public float bounceScale = 7.0f; 

    public float stiffness = 150f;
    [Tooltip("Scale factor for stiffness along each local axis (X, Y, Z) relative to the main bone's parent space. Higher values make the bone less flexible along that axis.")]
    public Vector3 stiffnessAxisScale = new Vector3(1f, 2f, 1f);
    [Tooltip("Scale factor for velocity along each local axis (X, Y, Z) relative to the main bone's parent space. Lower values make the bone gather less velocity along that axis.")]
    public Vector3 velocityAxisScale = new Vector3(1f, 0.5f, 1f);
    public float velocityTransfer = 0.35f;
    public float maxVelocity = 8;
    public float damping = 2;
    public float maxDistance = 0.3f;
    [Range(0f, 1f)]
    public float angularFlexibility = 1.0f; // Controls how much the bone bends/rotates
    public float tipStiffness = 180f;
    public float tipDamping = 8f;
    public bool resetTransformsEachFrame;
    private Vector3 tipVelocity;
     
    private Vector3 prevPosition;
    private Vector3 origPosition;
    private Quaternion origRotation;
    private Vector3 localTipOrigPos;
    private Quaternion localTipOrigRot;

    private Vector3 savedBasePos;
    private Vector3 savedBaseLocalPos;
    private Quaternion savedBaseRot;
    private Quaternion savedBaseLocalRot;

    private Vector3 savedLocalTipPos;
    private Quaternion savedLocalTipRot;

    public static int ExecutionPriority => CharacterMuscleAutoFlexerUpdater.ExecutionPriority + 1;  

    public int Priority => ExecutionPriority;

    public int UpdatePriority => Priority;

    public int LateUpdatePriority => Priority;

    public int FixedUpdatePriority => Priority;

    protected void Awake()
    {
        if (mainBone == null) mainBone = transform;
        prevPosition = mainBone.position;
        origPosition = mainBone.localPosition;
        origRotation = mainBone.localRotation;
        if (tipBone != null) 
        {
            localTipOrigPos = tipBone.localPosition; 
            localTipOrigRot = tipBone.localRotation;
        }

        savedBasePos = mainBone.position;
        savedBaseRot = mainBone.rotation;
        savedBaseLocalPos = mainBone.localPosition;
        savedBaseLocalRot = mainBone.localRotation;

        if (tipBone != null)
        {
            savedLocalTipPos = tipBone.localPosition;
            savedLocalTipRot = tipBone.localRotation;
        }
    }

    protected void Start()
    {
        if (characterGroupTargetSyncs != null && characterGroupTargetSyncs.Length > 0)
        {
            foreach (var sync in characterGroupTargetSyncs)
            {
                sync.StartListening(name); 
            }
        }
    }

    void OnDestroy()
    {
        if (characterGroupTargetSyncs != null && characterGroupTargetSyncs.Length > 0)
        {
            foreach (var sync in characterGroupTargetSyncs)
            {
                sync.StopListening();
            }
        }
    }

    void OnEnable()
    {
        ((IExecutableBehaviour)this).AddToCallstack();
    }
    void OnDisable()
    {
        ((IExecutableBehaviour)this).RemoveFromCallstack(); 
    }

    //protected void Update()
    //{
    //    if (mix <= 0) return;
    //    Swole.API.Unity.Animation.CustomAnimatorUpdater.AddPostLateUpdateWork(OnLateUpdate);
    //}

    public void OnUpdate()
    {
    }

    public void OnFixedUpdate()
    {
    }

    public void OnPreFixedUpdate()
    {
    }

    public void OnPostFixedUpdate()
    { 
    }

    public int CompareTo(IExecutableBehaviour other) => Priority.CompareTo(other.Priority); 

    public void OnLateUpdate()
    {
        float mix = this.mix;

        float mixMul = 1f;
        float stiffnessMul = 1f;
        float tipStiffnessMul = 1f;
        if (characterGroupTargetSyncs != null && characterGroupTargetSyncs.Length > 0)
        {
            mixMul = 0f;
            stiffnessMul = 0f;
            tipStiffnessMul = 0f;
            float totalWeight = 0f;

            switch (groupCombineMode)
            {
                case GroupCombineMode.Min:
                    {
                        mixMul = float.MaxValue;
                        stiffnessMul = float.MaxValue;
                        tipStiffnessMul = float.MaxValue;
                    }
                    break;
                case GroupCombineMode.Max:
                    {
                        mixMul = float.MinValue;
                        stiffnessMul = float.MinValue;
                        tipStiffnessMul = float.MinValue;
                    }
                    break;
            }

            if (ignoreTargetSyncContributionWeights || groupCombineMode != GroupCombineMode.Additive)
            {
                switch(groupCombineMode)
                {
                    case GroupCombineMode.Additive:
                        {
                            foreach (var sync in characterGroupTargetSyncs)
                            {
                                mixMul += sync.MixMultiplier;
                                stiffnessMul += sync.StiffnessMultiplier;
                                tipStiffnessMul += sync.StiffnessMultiplier;
                            }
                        }
                        break;
                    case GroupCombineMode.Min:
                        {
                            foreach (var sync in characterGroupTargetSyncs)
                            {
                                mixMul = math.min(mixMul, sync.MixMultiplier);
                                stiffnessMul = math.min(stiffnessMul, sync.StiffnessMultiplier);
                                tipStiffnessMul = math.min(tipStiffnessMul, sync.StiffnessMultiplier);
                            }
                        }
                        break;
                    case GroupCombineMode.Max:
                        {
                            foreach (var sync in characterGroupTargetSyncs)
                            {
                                mixMul = math.max(mixMul, sync.MixMultiplier);
                                stiffnessMul = math.max(stiffnessMul, sync.StiffnessMultiplier);
                                tipStiffnessMul = math.max(tipStiffnessMul, sync.StiffnessMultiplier);
                            }
                        }
                        break;
                }

            }
            else
            {
                foreach (var sync in characterGroupTargetSyncs)
                {
                    if (Mathf.Approximately(sync.contributionWeight, 0f)) continue; 

                    mixMul += sync.MixMultiplier;
                    stiffnessMul += sync.StiffnessMultiplier;
                    tipStiffnessMul += sync.StiffnessMultiplier;
                    totalWeight += sync.contributionWeight;
                }

                if (totalWeight > 0f)
                {
                    mixMul /= totalWeight;
                    stiffnessMul /= totalWeight;
                    tipStiffnessMul /= totalWeight;
                }

                foreach (var sync in characterGroupTargetSyncs)
                {
                    if (!Mathf.Approximately(sync.contributionWeight, 0f)) continue;

                    mixMul += sync.MixMultiplier;
                    stiffnessMul += sync.StiffnessMultiplier;
                    tipStiffnessMul += sync.StiffnessMultiplier;
                }
            }

            foreach (var sync in characterGroupTargetSyncs)
            {
                stiffnessMul *= sync.RealtimeStiffnessMultiplier;  
                mixMul *= sync.RealtimeMixMultiplier;   
            }
        }

        mix = mix * mixMul; 
        mix = (mixCurve == null || mixCurve.length <= 0 ? mix : mixCurve.Evaluate(mix)) * (1f - physicsNerf);  
        if (mix <= 0f) return;

        float stiffness = this.stiffness * stiffnessMul;
        float tipStiffness = this.tipStiffness * tipStiffnessMul;

        Vector3 origPos = mainBone.parent.TransformPoint(origPosition);
        float velocityMix = mix;
        Vector3 initialMainBonePosition = Vector3.zero;
        Quaternion initialMainBoneRotation = Quaternion.identity;
        Vector3 initialTipBonePosition = Vector3.zero;
        Quaternion initialTipBoneRotation = Quaternion.identity;

        Vector3 rawTranslation = (origPos - prevPosition);

        if (resetTransformsEachFrame)
        {
            velocityMix = 1f;
            mainBone.GetLocalPositionAndRotation(out initialMainBonePosition, out initialMainBoneRotation);

            // Safe world tracking using relative offset (keeps bones riding along perfectly)
            mainBone.position = origPos + savedBasePos;

            if (tipBone != null)
            {
                mainBone.rotation = savedBaseRot;
                tipBone.GetLocalPositionAndRotation(out initialTipBonePosition, out initialTipBoneRotation);

                Vector3 targetTipWorldPos = mainBone.position + mainBone.parent.TransformDirection(localTipOrigPos);
                tipBone.position = targetTipWorldPos + savedLocalTipPos;
                tipBone.localRotation = savedLocalTipRot;
            }
        }

        // Displacement measures the pure local physical distortion
        Vector3 displacement = mainBone.position - origPos;
        Vector3 localDisplacement = mainBone.parent.InverseTransformDirection(displacement);

        Vector3 localSpringForce = -stiffness * Vector3.Scale(localDisplacement, stiffnessAxisScale);
        Vector3 springForce = mainBone.parent.TransformDirection(localSpringForce);

        float stableDamping = damping + (stiffness * 0.02f);
        Vector3 dampingForce = -stableDamping * velocity;

        Vector3 acceleration = springForce + dampingForce;

        // Transform the movement vector into the parent's local space
        Vector3 localTranslation = mainBone.parent.InverseTransformDirection(rawTranslation);
        localTranslation = Vector3.Scale(localTranslation * velocityTransfer, velocityAxisScale);

        // Convert back to world space
        Vector3 externalTranslation = mainBone.parent.TransformDirection(localTranslation);


        Vector3 lagForce = -externalTranslation * (1f / Mathf.Max(Time.deltaTime, 0.001f)) * lagDampening;

        Vector3 localLagForce = mainBone.parent.InverseTransformDirection(lagForce); 

        // We scale the X (lateral sway) and Y (vertical jiggle) forces symmetrically.
        // Because the vectors remain completely untouched structurally, your direction alignments,
        // landing/jumping reactions, and anti-stretching protections are completely preserved.
        localLagForce.x *= 0.5f * bounceScale;
        localLagForce.y *= 0.3f * bounceScale;
        // localLagForce.z retains the full or dampened compression to stop forward stretching

        // Convert the tuned lag force back to world space and apply
        lagForce = mainBone.parent.TransformDirection(localLagForce);
        acceleration += lagForce * velocityMix;

        // Standard, continuous physics integration
        velocity += acceleration * Time.deltaTime * velocityMix;
        velocity = Vector3.ClampMagnitude(velocity, maxVelocity);

        // Apply pure velocity bounce
        mainBone.position += velocity * Time.deltaTime;

        // Safe hard boundary relative to origPos so it never clips into the body mesh
        Vector3 offset = (mainBone.position - origPos);
        float dist = offset.magnitude;
        if (dist > 0 && dist >= maxDistance)
        {
            mainBone.position = origPos + (offset / dist) * maxDistance;
        }
        prevPosition = origPos;

        if (tipBone != null)
        {
            Vector3 targetTipWorldPos = mainBone.position + mainBone.parent.TransformDirection(localTipOrigPos);
            Vector3 tipDisplacement = tipBone.position - targetTipWorldPos;

            Vector3 localTipDisp = mainBone.parent.InverseTransformDirection(tipDisplacement);
            Vector3 localTipSpring = -tipStiffness * Vector3.Scale(localTipDisp, stiffnessAxisScale);
            Vector3 tipSpringForce = mainBone.parent.TransformDirection(localTipSpring);

            float stableTipDamping = tipDamping + (tipStiffness * 0.03f);
            Vector3 tipDampingForce = -stableTipDamping * tipVelocity;

            Vector3 tipAcceleration = tipSpringForce + tipDampingForce;

            tipVelocity += tipAcceleration * Time.deltaTime * velocityMix;
            tipBone.position += tipVelocity * Time.deltaTime;

            Quaternion alignmentCorrection = Quaternion.Inverse(Quaternion.LookRotation(boneForwardAxis, boneUpAxis));

            // Base Bone Rotation
            Vector3 baseLookDir = tipBone.position - mainBone.position;
            if (baseLookDir.sqrMagnitude > 0.001f)
            {
                Vector3 worldForwardTarget = baseLookDir.normalized;
                Vector3 worldUpTarget = mainBone.parent.TransformDirection(boneUpAxis);
                Quaternion standardRot = Quaternion.LookRotation(worldForwardTarget, worldUpTarget);
                mainBone.rotation = Quaternion.Slerp(mainBone.rotation, standardRot * alignmentCorrection, 20f * Time.deltaTime);
            }

            // Tip Bone Rotation
            Vector3 tipLookDir = tipBone.position - origPos;
            if (tipLookDir.sqrMagnitude > 0.001f)
            {
                Vector3 worldForwardTarget = tipLookDir.normalized;
                Vector3 worldUpTarget = mainBone.parent.TransformDirection(boneUpAxis);
                Quaternion standardRot = Quaternion.LookRotation(worldForwardTarget, worldUpTarget);
                tipBone.rotation = Quaternion.Slerp(tipBone.rotation, standardRot * alignmentCorrection, 20f * Time.deltaTime);
            }
        }

        if (resetTransformsEachFrame)
        {
            savedBasePos = mainBone.position - origPos;

            if (tipBone != null)
            {
                savedBaseRot = mainBone.rotation;
                Vector3 targetTipWorldPos = mainBone.position + mainBone.parent.TransformDirection(localTipOrigPos);
                savedLocalTipPos = tipBone.position - targetTipWorldPos;
                savedLocalTipRot = tipBone.localRotation;
            }

            Vector3 initialOffset = initialMainBonePosition - origPosition;
            Quaternion initialOffsetRot = initialMainBoneRotation * Quaternion.Inverse(origRotation);
            if (!Mathf.Approximately(mix, 1f))
            {
                mainBone.localPosition = Vector3.LerpUnclamped(initialMainBonePosition, mainBone.localPosition + initialOffset, mix);

                if (tipBone != null)
                {
                    mainBone.localRotation = Quaternion.SlerpUnclamped(initialMainBoneRotation, initialOffsetRot * mainBone.localRotation, mix);

                    Vector3 initialTipOffset = initialMainBonePosition - origPosition;
                    Quaternion initialTipOffsetRot = initialMainBoneRotation * Quaternion.Inverse(origRotation);
                    tipBone.localPosition = Vector3.LerpUnclamped(initialTipBonePosition, tipBone.localPosition + initialTipOffset, mix);
                    tipBone.localRotation = Quaternion.SlerpUnclamped(initialTipBoneRotation, initialTipOffsetRot * tipBone.localRotation, mix);
                }
            } 
            else
            {
                mainBone.localPosition = mainBone.localPosition + initialOffset;

                if (tipBone != null)
                {
                    mainBone.localRotation = initialOffsetRot * mainBone.localRotation;

                    Vector3 initialTipOffset = initialMainBonePosition - origPosition;
                    Quaternion initialTipOffsetRot = initialMainBoneRotation * Quaternion.Inverse(origRotation);
                    tipBone.localPosition = tipBone.localPosition + initialTipOffset;
                    tipBone.localRotation = initialTipOffsetRot * tipBone.localRotation;  
                }
            }
        }
    }

    /*public void OnLateUpdate()
    {
        if (resetTransformsEachFrame)
        {
            breastBone.localPosition = savedLocalBasePos;
            breastBone.localRotation = savedLocalBaseRot;

            if (breastTipBone != null)
            {
                breastTipBone.localPosition = savedLocalTipPos;
                breastTipBone.localRotation = savedLocalTipRot;
            }
        }

        var p1 = breastBone.parent.position;
        var p2 = breastBone.position;

        float mix = mixCurve == null || mixCurve.length <= 0 ? this.mix : mixCurve.Evaluate(this.mix); 

        Vector3 origPos = breastBone.parent.TransformPoint(origPosition);
        prevPosition = Vector3.Lerp(prevPosition, origPos, (1 - mix) * 30 * Time.deltaTime);
        Vector3 translation = origPos - prevPosition;
        float moveDist = translation.magnitude;
        translation = translation * velocityTransfer;
        if ((velocity + translation).magnitude * Time.deltaTime <= moveDist * Mathf.LerpUnclamped(1, 50, (-Vector3.Dot(velocity.normalized, translation.normalized)) + 1)) velocity = (velocity + translation); 
        breastBone.position = prevPosition + velocity * Time.deltaTime;
        velocity = velocity.normalized * Mathf.Min(maxVelocity, velocity.magnitude);
        velocity = velocity - (velocity.normalized * velocity.sqrMagnitude * Time.deltaTime);
        velocity *= Mathf.Pow(damping, Time.deltaTime);
        Vector3 offset = (breastBone.position - p1);
        float dist = offset.magnitude;
        if (dist > 0 && dist >= maxDistance)
        {
            offset = offset / dist;
            breastBone.position = p1 + offset * maxDistance; 
        }

        prevPosition = breastBone.position;

        if (resetTransformsEachFrame)
        {
            savedLocalBasePos = breastBone.localPosition;
            savedLocalBaseRot = breastBone.localRotation;

            if (breastTipBone != null)
            {
                savedLocalTipPos = breastTipBone.localPosition;
                savedLocalTipRot = breastTipBone.localRotation;
            }
        }
    }*/

}

#endif