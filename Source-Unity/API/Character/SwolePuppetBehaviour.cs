#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;


#if BULKOUT_ENV
using RootMotion;
using RootMotion.Dynamics;
#endif

namespace Swole.API.Unity.Animation
{
#if BULKOUT_ENV
    public class SwolePuppetBehaviour : RootMotion.Dynamics.BehaviourBase, ISwolePuppetBehaviour
#else
    public class SwolePuppetBehaviour : MonoBehaviour, ISwolePuppetBehaviour
#endif
    {
#if !BULKOUT_ENV
        [NonSerialized]
        public bool deactivated;
#endif

        public LayerMask rootBodyIgnoreLayers;
        public LayerMask muscleBodyIgnoreLayers;

        public float muscleTriggerMaxTargetOffset = 0.09f;
        public float collisionBlendSpeed = 2f;

        [SerializeField]
        protected Rigidbody rootBody;
        public Rigidbody RootBody
        {
            get
            {
                if (rootBody == null)
                {
                    if (puppetMaster == null)
                    {
                        rootBody = GetComponent<Rigidbody>();
                    }
                    else
                    {
                        rootBody = puppetMaster.GetComponent<Rigidbody>();
                    }
                }

                return rootBody;
            }
        }

        public Transform RootTransform
        {
            get
            {
                if (RootBody != null) return rootBody.transform;
                if (puppetMaster is SwolePuppetMaster spm) return spm.RootTransform;

                return transform;
            }
        }

        [SerializeField]
        protected Transform mainRigBone;
        public Transform MainRigBone
        {
            get => mainRigBone;
            set
            {
                mainRigBone = value;

                if (puppetMaster != null && puppetMaster.muscles != null)
                {
                    foreach (var muscle in puppetMaster.muscles)
                    {
                        if (muscle == null) continue;

                        if (ReferenceEquals(muscle.target, mainRigBone))
                        {
                            mainRigBoneMuscleTransform = muscle.transform;
                            break;
                        }
                    }
                }
            }
        }
        protected Transform mainRigBoneMuscleTransform;
        public Transform MainRigBoneMuscleTransform
        {
            get
            {
                if (mainRigBoneMuscleTransform == null)
                {
                    if (puppetMaster != null && puppetMaster.muscles != null)
                    {
                        foreach (var muscle in puppetMaster.muscles)
                        {
                            if (muscle == null) continue;

                            if (ReferenceEquals(muscle.target, mainRigBone))
                            {
                                mainRigBoneMuscleTransform = muscle.transform;
                                break;
                            }
                        }
                    }
                }

                return mainRigBoneMuscleTransform;
            }
        }

        [SerializeField]
        protected int rootMuscleIndex = -1; 
        public Muscle RootMuscle
        {
            get
            {
                if (rootMuscleIndex < 0)
                {
                    if (puppetMaster != null && puppetMaster.muscles != null)
                    {
                        for (int a = 0; a < puppetMaster.muscles.Length; a++)
                        {
                            var muscle = puppetMaster.muscles[a];
                            if (muscle == null) continue;

                            if (ReferenceEquals(muscle.target, mainRigBone))
                            {
                                rootMuscleIndex = a;
                                return muscle;
                            }
                        }
                    }
                }
                else return puppetMaster.muscles[rootMuscleIndex];

                return null;
            }
        }

        [SerializeField]
        protected Rigidbody rootMuscleBody;
        public Rigidbody RootMuscleBody
        {
            get
            {
                if (rootMuscleBody == null)
                {
                    if (puppetMaster != null && puppetMaster.muscles != null)
                    {
                        foreach (var muscle in puppetMaster.muscles)
                        {
                            if (muscle == null) continue;

                            if (ReferenceEquals(muscle.target, mainRigBone))
                            {
                                rootMuscleBody = muscle.rigidbody;
                                return rootMuscleBody == null ? RootBody : rootMuscleBody;
                            }
                        }
                    }
                }
                else return rootMuscleBody;

                return RootBody;
            }
        }

        public SwolePuppetMaster Puppet
        {
            get
            {
                if (this.puppetMaster is SwolePuppetMaster spm) return spm;

                return null;
            }
        }

        [Serializable]
        public enum State
        {
            Normal,
            NormalNoCollider,
            OffBalance,
            Tumbling,
            GettingUp,
            Floating
        }

        public UnityEvent<State> onStateChange = new UnityEvent<State>();

        protected State currentState;
        public State CurrentState
        {
            get => currentState;
            set => SetState(value);
        }
        public void SetState(State state)
        {
            if (currentState != state)
            {
                currentState = state;
                onStateChange?.Invoke(currentState);
            }
        }

        public struct MuscleCollisionInfo
        {
            public int collisionCount;
            public float collisionBlend;
        }

        protected MuscleCollisionInfo[] muscleCollisions;
        protected void ValidateMuscleCollisionsArray()
        {
            if (muscleCollisions == null || muscleCollisions.Length != puppetMaster.muscles.Length)
            {
                Array.Resize(ref muscleCollisions, puppetMaster.muscles.Length);
            }
        }

        protected UnityEvent postAnimateListener;
        protected override void OnInitiate()
        {
            if (puppetMaster is SwolePuppetMaster spm)
            {
                var animator = spm.swoleAnimator;
                if (animator != null)
                {
                    var ikManager = animator.IkManager;
                    if (ikManager != null)
                    {
                        if (ikManager.OnPostLateUpdate == null) ikManager.OnPostLateUpdate = new UnityEvent();
                        postAnimateListener = ikManager.OnPostLateUpdate;
                    }
                    else
                    {
                        if (animator.OnPostLateUpdate == null) animator.OnPostLateUpdate = new UnityEvent();
                        postAnimateListener = animator.OnPostLateUpdate;
                    }

                    postAnimateListener.AddListener(PostAnimate);
                }
            }

            PostAnimate();
        }

        protected virtual void OnDestroy()
        {
            if (postAnimateListener != null)
            {
                postAnimateListener.RemoveListener(PostAnimate);
                postAnimateListener = null;
            }
        }

        public override void OnReactivate()
        {
        }

#if BULKOUT_ENV
        public override void OnMuscleCollision(MuscleCollision collision)
        {
            ValidateMuscleCollisionsArray();

            var info = muscleCollisions[collision.muscleIndex];
            info.collisionCount++;
            muscleCollisions[collision.muscleIndex] = info;
        }
        public override void OnMuscleCollisionExit(MuscleCollision collision)
        {
        }
#endif

        protected bool[] muscleTriggerStates;

        public virtual void OnMuscleTriggerEnter(MuscleTriggerEvent triggerEvent)
        {
            ExitTriggerMode(triggerEvent.muscleIndex);
        }
        public virtual void OnMuscleTrigger(MuscleTriggerEvent triggerEvent)
        {
            ExitTriggerMode(triggerEvent.muscleIndex); 
        }
        public virtual void OnMuscleTriggerExit(MuscleTriggerEvent triggerEvent)
        {
            ExitTriggerMode(triggerEvent.muscleIndex);
        }
        protected void EnterTriggerMode(int muscleIndex)
        {
            if (muscleTriggerStates != null && !muscleTriggerStates[muscleIndex])
            {
                var muscle = puppetMaster.muscles[muscleIndex];
                muscleTriggerStates[muscleIndex] = true;
                muscle.rigidbody.isKinematic = true;
                if (muscle.colliders != null)
                {
                    foreach (var collider in muscle.colliders) collider.isTrigger = true;
                }
            }
        }
        private readonly Collider[] overlapColliders = new Collider[8];
        protected void ExitTriggerMode(int muscleIndex)
        {
            var muscle = puppetMaster.muscles[muscleIndex];
            if (muscleTriggerStates != null && muscleTriggerStates[muscleIndex]) 
            {
                if (muscle.colliders != null)
                {
                    int overlaps = muscle.rigidbody.OverlapColliderNonAlloc(overlapColliders, muscle.rigidbody.position, muscle.rigidbody.rotation, -1, QueryTriggerInteraction.Ignore, muscle.colliders);
                    if (overlaps > 0)
                    {
                        var rm = RootMuscle;
                        Vector3 refPos = rm == null ? (RootBody == null ? (transform.position + Vector3.up) : rootBody.transform.TransformPoint(rootBody.centerOfMass)) : rm.transform.TransformPoint(rm.rigidbody.centerOfMass);

                        Vector3 startPoint = Vector3.zero;
                        foreach(var collider in muscle.colliders)
                        {
                            startPoint += collider.ClosestPoint(refPos);
                        }
                        startPoint = startPoint / muscle.colliders.Length;
                        float dist = (startPoint - refPos).sqrMagnitude;

                        float i = 0f;
                        Vector3 offset = Vector3.zero;
                        for (int a = 0; a < overlaps; a++)
                        {
                            var collider = overlapColliders[a];

                            bool flag = false;
                            for (int b = 0; b < muscle.colliders.Length; b++)
                            {
                                if (muscle.colliders[b] == collider)
                                {
                                    flag = true;
                                    break;
                                }
                            }
                            if (flag) continue;

                            Vector3 closestPoint = (collider is TerrainCollider terrainCollider ? terrainCollider.ClosestPointOnBounds(refPos) : (collider is MeshCollider meshCollider && !meshCollider.convex) ? meshCollider.ClosestPointOnBounds(refPos) : collider.ClosestPoint(refPos));   
                            if ((closestPoint - refPos).sqrMagnitude < dist)
                            {
                                i += 1f;
                                offset += (closestPoint - startPoint);
                            }
                        }
                        if (i > 0f) offset = offset / i;
                        
                        //Debug.Log($"{muscle.name}: {overlaps}: {offset}"); 
                        muscle.transform.position = muscle.transform.position + offset; 

                        if (muscleCollisions != null)
                        {
                            var collisionInfo = muscleCollisions[muscleIndex];
                            collisionInfo.collisionCount += 1;
                            collisionInfo.collisionBlend = Mathf.Max(collisionInfo.collisionBlend, 0.35f);  
                            muscleCollisions[muscleIndex] = collisionInfo;
                        }
                    }
                }

                muscleTriggerStates[muscleIndex] = false;
                muscle.rigidbody.isKinematic = false;
                if (muscle.colliders != null)
                {
                    foreach (var collider in muscle.colliders) collider.isTrigger = false;
                }
            }
            else if (muscle.rigidbody.isKinematic)
            {
                muscle.rigidbody.isKinematic = false;
                foreach (var collider in muscle.colliders) collider.isTrigger = false; 
            }
        }

        protected void TrySyncMusclesToTargets()
        {
            if (puppetMaster == null) return;

            if (this.puppetMaster is SwolePuppetMaster spm && spm.currentConfiguration.muscleStates != null && muscleCollisions != null && muscleCollisions.Length > 0)
            {
                var root = spm.RootTransform;
                var rootRot = root.rotation;
                for (int muscleIndex = 0; muscleIndex < puppetMaster.muscles.Length; muscleIndex++)
                {
                    var muscle = puppetMaster.muscles[muscleIndex];
                    if (muscle == null) continue;

                    var state = spm.currentConfiguration.muscleStates[muscleIndex];
                    var collisionInfo = muscleCollisions[muscleIndex];
                    if (spm.pinWeight > 0f && collisionInfo.collisionBlend <= 0f)
                    {
                        float mp = spm.mappingWeight * muscle.props.mappingWeight * muscle.state.mappingWeightMlp; 
                        float pw = muscle.props.pinWeight * state.pinWeightMlp;
                        if (mp <= 0.01f && pw > 0.3f)
                        {
                            //muscle.MoveToTarget();
                            muscle.ClearVelocities();
                            if (!muscle.rigidbody.isKinematic)
                            {
                                muscle.rigidbody.velocity = Vector3.zero;
                                muscle.rigidbody.angularVelocity = Vector3.zero;
                            }

                            //muscle.transform.SetPositionAndRotation(muscle.targetAnimatedPosition, muscle.targetAnimatedWorldRotation);
                            muscle.transform.SetPositionAndRotation(root.TransformPoint(state.lastAnimatedPosition_ROOTSPACE), rootRot * state.lastAnimatedRotation_ROOTSPACE);
                            //muscle.rigidbody.Move(root.TransformPoint(state.lastAnimatedPosition_ROOTSPACE), rootRot * state.lastAnimatedRotation_ROOTSPACE);


                            //Debug.Log(muscle.name + $": (mp:{mp})  (pw:{pw})"); 
                            EnterTriggerMode(muscleIndex);
                        }
                        else
                        {
                            ExitTriggerMode(muscleIndex);
                        }
                    }
                    else
                    {
                        ExitTriggerMode(muscleIndex);
                    }
                }
            }
        }

        protected override void OnFixedUpdate(float deltaTime)
        {
            if (puppetMaster == null) return;

            ValidateMuscleCollisionsArray();

            for (int a = 0; a < muscleCollisions.Length; a++)
            {
                var info = muscleCollisions[a];

                info.collisionCount = 0;

                muscleCollisions[a] = info;
            }

            TrySyncMusclesToTargets(); 
        }

        public BehaviourUpdateDelegate OnAfterFixedUpdate;
        public virtual void AfterFixedUpdate(float deltaTime) 
        {
            //TrySyncMusclesToTargets();

            if (OnAfterFixedUpdate != null && enabled) OnAfterFixedUpdate(deltaTime); 
        }

        /// <summary>
        /// 0 = use local mapping multiplier, 1 = full mapping (mapping set to 1 for all muscles)
        /// </summary>
        protected float fullMappingOverride;
        protected float unpinningOverride;

        [NonSerialized]
        public float mappingOverrideSyncSpeed = 2f;
        [NonSerialized]
        public float unpinningOverrideSyncSpeed = 6f;

        protected float balance = 1f;
        public float Balance
        {
            get => balance;
            set => balance = value;
        }

        protected void Awake()
        {
            mappingOverrideSyncSpeed = 2f;
            unpinningOverrideSyncSpeed = 6f;
        }
        protected void Start()
        {
            var rb = RootBody;
            if (rb != null)
            {
                rb.excludeLayers = rb.excludeLayers | rootBodyIgnoreLayers;
            }

            if (Puppet != null && puppetMaster.muscles != null)
            {
                foreach(var muscle in puppetMaster.muscles)
                {
                    if (muscle == null || muscle.rigidbody == null) continue;

                    muscle.rigidbody.excludeLayers = muscle.rigidbody.excludeLayers | muscleBodyIgnoreLayers; 
                }

                muscleTriggerStates = new bool[puppetMaster.muscles.Length];
            }
        }

        protected override void OnUpdate(float deltaTime)
        {
        }

        protected void BlendCollision(SwolePuppetMaster spm, int muscleIndex, int initiatorIndex, float amount)
        {
            if (amount <= 0.001f) return;

            var muscle = spm.muscles[muscleIndex];
            if (muscle == null) return;

            var info = muscleCollisions[muscleIndex];
            var state = spm.currentConfiguration.muscleStates[muscleIndex];

            if (info.collisionCount > 0f || initiatorIndex >= 0)
            {
                info.collisionBlend = Mathf.MoveTowards(info.collisionBlend, 1f, amount * collisionBlendSpeed * (state.collisionResistance <= 0 ? collisionBlendSpeed : (collisionBlendSpeed / state.collisionResistance)));

                if (state.unpinParents > 0f && muscle.parentIndexes != null)
                {
                    foreach (var parent in muscle.parentIndexes)
                    {
                        if (parent == initiatorIndex) continue;

                        BlendCollision(spm, parent, muscleIndex, state.unpinParents * amount);
                    }
                }

                if (state.unpinChildren > 0f && muscle.childIndexes != null)
                {
                    foreach (var child in muscle.childIndexes)
                    {
                        if (child == initiatorIndex) continue;

                        BlendCollision(spm, child, muscleIndex, state.unpinChildren * amount);
                    }
                }
            }
            else if (initiatorIndex < 0)
            {
                info.collisionBlend = Mathf.MoveTowards(info.collisionBlend, 0f, amount * state.collisionRecoverySpeed);
            }

            muscleCollisions[muscleIndex] = info;
        }
        public override void OnWrite(float deltaTime)
        {
            PostPuppet(deltaTime);

            if (this.puppetMaster is SwolePuppetMaster spm)
            {
                if (spm.muscles != null)
                {
                    for (int a = 0; a < spm.muscles.Length; a++)
                    {
                        var muscle = spm.muscles[a];
                        if (muscle == null) continue;

                        if (muscle.positionOffset.magnitude * muscle.lastMappingWeight > muscleTriggerMaxTargetOffset)
                        {
                            //spm.ExitTriggerModeUnsafe(a);
                        }
                        else
                        {
                            //spm.EnterTriggerModeUnsafe(a); 
                        }
                    }

                    spm.ValidateCurrentConfiguration();
                    ValidateMuscleCollisionsArray();

                    for (int a = 0; a < muscleCollisions.Length; a++)
                    {
                        BlendCollision(spm, a, -1, deltaTime);
                    }
                }
            }

            //TrySyncMusclesToTargets();
        }

        public void OnApplyConfigurationMix(SwolePuppetMaster spm)
        {
            if (spm.muscles != null)
            {
                ValidateMuscleCollisionsArray();

                for (int a = 0; a < spm.muscles.Length; a++)
                {
                    var muscle = spm.muscles[a];
                    if (muscle == null) continue;

                    var collisionInfo = muscleCollisions[a];

                    var configState = spm.currentConfiguration.muscleStates[a];
                    configState.Apply(puppetMaster.pinWeight, muscle, collisionInfo.collisionBlend);
                }

                //Debug.Log($"Applying muscle config mix to {name} : {this.CurrentState.ToString()} :: (fullMappingOverride:{fullMappingOverride}) (unpinningOverride:{unpinningOverride})"); 
            }

            if (fullMappingOverride > 0.0001f || unpinningOverride > 0.0001f) 
            {
                for (int a = 0; a < spm.muscles.Length; a++)
                {
                    var muscle = spm.muscles[a];
                    if (muscle == null) continue;
                    
                    var state = muscle.state;
                    state.mappingWeightMlp = Mathf.LerpUnclamped(state.mappingWeightMlp, 1f, fullMappingOverride); 
                    state.pinWeightMlp = Mathf.LerpUnclamped(state.pinWeightMlp, 0f, unpinningOverride);
                    muscle.state = state;
                }
            }
        }

        protected readonly List<TransformState> muscleTargetAnimationTransformStates = new List<TransformState>();
        protected Vector3 mainRigBoneAnimatedPosition;
        protected void PostAnimate()
        {
            if (puppetMaster != null)
            {
                if (puppetMaster is SwolePuppetMaster spm)
                {
                    var animator = spm.swoleAnimator;
                    if (!animator.enabled) return;
                }
                else
                {
                    var animator = puppetMaster.targetAnimator;
                    if (!animator.enabled) return;
                }

                muscleTargetAnimationTransformStates.Clear();
                for (int a = 0; a < puppetMaster.muscles.Length; a++)
                {
                    var muscle = puppetMaster.muscles[a];
                    if (muscle == null || muscle.target == null)
                    {
                        muscleTargetAnimationTransformStates.Add(default);
                        continue;
                    }

                    var state = new TransformState();
                    muscle.target.GetPositionAndRotation(out state.position, out state.rotation);

                    muscleTargetAnimationTransformStates.Add(state); // used to determine balance by comparing muscle positions to animation target positions
                }
            }

            var mainRigBone = MainRigBone;
            if (mainRigBone == null) return;

            var rootTransform = RootTransform;
            mainRigBoneAnimatedPosition = rootTransform.InverseTransformPoint(mainRigBone.position); // used as an offset when syncing root transform to puppet position
        }

        Vector3 animationToPuppetOffset = Vector3.zero;
        protected void PostPuppet(float deltaTime)
        {
            if (puppetMaster.muscles != null && puppetMaster is SwolePuppetMaster spm && spm.currentConfiguration.IsValid)
            {
                var mainRigBone = MainRigBone; // will have been syned to muscle at this point
                if (mainRigBone != null)
                {
                    var rootTransform = RootTransform;
                    animationToPuppetOffset = mainRigBone.position - rootTransform.TransformPoint(mainRigBoneAnimatedPosition);
                }

                bool hasAnimationTargetStates = muscleTargetAnimationTransformStates.Count > 0;

                float balanceContr = 0f;
                float unbalanceContr = 0f;
                for (int a = 0; a < puppetMaster.muscles.Length; a++)
                {
                    var muscle = puppetMaster.muscles[a];
                    if (muscle == null || muscle.transform == null || muscle.target == null) continue;

                    var state = muscle.state;
                    var configState = spm.currentConfiguration.muscleStates[a];

                    float distanceRange = configState.unbalanceDistanceRange > 0f ? configState.unbalanceDistanceRange : 1f;
                    float angleRange = configState.unbalanceAngleRange > 0f ? configState.unbalanceAngleRange : 1f;

                    float offsetDistance = Vector3.Distance(muscle.transform.position, hasAnimationTargetStates ? muscleTargetAnimationTransformStates[a].position : muscle.target.position); //muscle.positionOffset.magnitude * muscle.lastMappingWeight;
                    float offsetAngle = Quaternion.Angle(muscle.transform.rotation, (hasAnimationTargetStates ? muscleTargetAnimationTransformStates[a].rotation : muscle.target.rotation));

                    float balanceContr_ = 0f;
                    float unbalanceContr_ = 0f;
                    if (configState.unbalanceDistanceThreshold > 0f)
                    {
                        if (offsetDistance > configState.unbalanceDistanceThreshold)
                        {
                            unbalanceContr_ += configState.unbalancingContribution * Mathf.Min(1f, (offsetDistance - configState.unbalanceDistanceThreshold) / distanceRange);
                        }
                        else if (offsetDistance < configState.unbalanceDistanceThreshold)
                        {
                            balanceContr_ += configState.rebalancingContribution * (1f - (offsetDistance / configState.unbalanceDistanceThreshold));
                        }
                    }
                    else
                    {
                        unbalanceContr_ += configState.unbalancingContribution * Mathf.Min(1f, offsetDistance / distanceRange);
                    }

                    if (configState.unbalanceAngleThreshold > 0f)
                    {
                        if (offsetAngle > configState.unbalanceAngleThreshold)
                        {
                            unbalanceContr_ += configState.unbalancingAngleContribution * Mathf.Min(1f, (offsetAngle - configState.unbalanceAngleThreshold) / angleRange);
                        }
                        else if (offsetAngle < configState.unbalanceAngleThreshold)
                        {
                            balanceContr_ += configState.rebalancingAngleContribution * (1f - (offsetAngle / configState.unbalanceAngleThreshold));
                        }
                    }
                    else
                    {
                        unbalanceContr_ += configState.unbalancingAngleContribution * Mathf.Min(1f, offsetAngle / angleRange);
                    }

                    //Debug.Log(muscle.name + $" : {offsetDistance} {offsetAngle}  :::: {balanceContr_} {unbalanceContr_}");

                    if (unbalanceContr_ > 0f) balanceContr_ = 0f;

                    balanceContr += balanceContr_ * configState.balanceContribution;
                    unbalanceContr += unbalanceContr_ * configState.balanceContribution;
                }

                float prevBalance = balance;
                balance = Mathf.Clamp01(balance + (balanceContr * deltaTime * 3f * spm.currentConfiguration.rebalancingSpeed) - (unbalanceContr * deltaTime * 3f * spm.currentConfiguration.unbalancingSpeed));
                //if (unbalanceContr > 0) Debug.Log("delta " + balanceContr + $" * {spm.currentConfiguration.rebalancingSpeed} (bal) : " + unbalanceContr + $" * {spm.currentConfiguration.unbalancingSpeed} (unbal)"); 
                //if (balance < 0.99f) Debug.Log("bal " + balance);

                switch (currentState)
                {
                    case State.Normal:
                    case State.NormalNoCollider:
                        if (balance < 0.5f)
                        {
                            SetState(State.OffBalance);
                        }
                        break;
                    case State.OffBalance:
                        if (balance < 0.1f)
                        {
                            SetState(State.Tumbling);
                        }
                        break;
                    case State.Tumbling:
                        if (balance > 0.5f)
                        {
                            //SetState(State.OffBalance);
                        }
                        else if (balance > 0.8f)
                        {
                            //SetState(State.Normal);
                        }
                        break;
                    case State.Floating:
                        if (balance < 0.1f)
                        {
                            SetState(State.Tumbling);
                        }
                        break;
                }
            }

            mappingOverrideSyncSpeed = Mathf.Max(mappingOverrideSyncSpeed, 1f);
            unpinningOverrideSyncSpeed = Mathf.Max(unpinningOverrideSyncSpeed, 1f); 

            fullMappingOverride = Mathf.MoveTowards(fullMappingOverride, puppetMaster.state != PuppetMaster.State.Alive || currentState == State.Tumbling || currentState == State.Floating ? 1f : (1f - balance), deltaTime * mappingOverrideSyncSpeed); 
            unpinningOverride = Mathf.MoveTowards(unpinningOverride, currentState == State.Tumbling ? 1f : (1f - balance), deltaTime * unpinningOverrideSyncSpeed); 
        }

        protected readonly List<Vector3> tempPositions = new List<Vector3>();
        public void TeleportRootBodyToMainRigBone()
        {
            Vector3 animationToPuppetOffset = Vector3.zero;
            var mainRigBone = MainRigBone;
            if (mainRigBone != null)
            {
                var rootTransform = RootTransform;
                animationToPuppetOffset = mainRigBone.position - rootTransform.TransformPoint(mainRigBoneAnimatedPosition);
            }

            TeleportRootBodyToPositionOffset(animationToPuppetOffset);
        }
        public void TeleportRootBodyToPuppetOffset(float deltaTime)
        {           
            var rootMuscle = RootMuscle;
            if (rootMuscle == null || rootMuscle.rigidbody == null) return;

            var rootBody = RootBody;
            if (rootBody == null) return;


            float pinWeight = 0f;
            foreach(var muscle in puppetMaster.muscles)
            {
                pinWeight = Mathf.Max(pinWeight, muscle.props.pinWeight * muscle.state.pinWeightMlp); 
            }
            pinWeight = puppetMaster.pinWeight * pinWeight;
            float mul = rootBody.isKinematic ? 1f : (1f - balance) * deltaTime; 
            if (pinWeight > 0f)
            {
                float dot = Vector3.Dot(animationToPuppetOffset, rootMuscle.rigidbody.velocity); // if pinned, only teleport if the root muscle is moving away from the root body
                if (dot > 0f) 
                { 
                    TeleportRootBodyToPositionOffset(animationToPuppetOffset * mul); 
                }
            }
            else
            {
                TeleportRootBodyToPositionOffset(animationToPuppetOffset * mul);
            }

            animationToPuppetOffset = Vector3.zero; // reset offset after teleporting 
        }
        public void TeleportRootBodyToPositionOffset(Vector3 offset/*, float minDistanceForNonKinematicTeleport = 1f*/)
        {
            if (puppetMaster != null)
            {
                var rootBody = RootBody;
                if (rootBody == null) return;
                float offsetMag = offset.magnitude;
                if (offsetMag <= 0f/*(rootBody.isKinematic ? 0f : minDistanceForNonKinematicTeleport)*/) return; 

                var mainRigBone = MainRigBone;
                var mainRigBonePos = mainRigBone.position;
                tempPositions.Clear();
                for (int a = 0; a < puppetMaster.muscles.Length; a++)
                {
                    var muscle = puppetMaster.muscles[a];
                    tempPositions.Add(muscle == null || muscle.transform == null ? default : muscle.transform.position); 
                }

                //if (rootBody.isKinematic)
                //{
                    var rootTransform = RootTransform;
                    //Debug.DrawLine(rootTransform.position + Vector3.up, rootTransform.position + offset + Vector3.up, Color.blue, 5f); 
                    rootTransform.position = rootTransform.position + offset;
                /*}
                else
                {
                    rootBody.MovePosition(rootBody.position + offset); 
                }*/


                mainRigBone.position = mainRigBonePos;
                for (int a = 0; a < puppetMaster.muscles.Length; a++) // keep original puppet muscle positions
                {
                    var muscle = puppetMaster.muscles[a];
                    if (muscle == null) continue;
                    
                    muscle.transform.position = tempPositions[a];
                }

                //if (!rootBody.isKinematic) Physics.SyncTransforms();
            }
        }
    }

    public struct MuscleTriggerEvent
    {

        /// <summary>
        /// The index of the triggered muscle in the puppet muscles array.
        /// </summary>
        public int muscleIndex;

        /// <summary>
        /// The collider from OnTriggerEnter/Stay/Exit.
        /// </summary>
        public Collider collider;

        public bool isStay;

        public MuscleTriggerEvent(int muscleIndex, Collider collider, bool isStay = false)
        {
            this.muscleIndex = muscleIndex;
            this.collider = collider;
            this.isStay = isStay;
        }
    }

    public interface ISwolePuppetBehaviour
    {
        public void OnReactivate();

        public void Resurrect();
        public void Freeze();
        public void Unfreeze();
        public void KillStart();
        public void KillEnd();
        public void OnTeleport(Quaternion deltaRotation, Vector3 deltaPosition, Vector3 pivot, bool moveToTarget);

#if BULKOUT_ENV
        public void OnMuscleDisconnected(Muscle m);
        public void OnMuscleReconnected(Muscle m);

        public void OnMuscleAdded(Muscle m);
        public void OnMuscleRemoved(Muscle m);
#endif

        public void Initiate();

        public void OnFixTransforms();

        public void OnRead(float deltaTime);

        public void OnWrite(float deltaTime);

#if BULKOUT_ENV
        public void OnMuscleHit(MuscleHit hit);
        public void OnMuscleCollision(MuscleCollision collision);
        public void OnMuscleCollisionExit(MuscleCollision collision);
#endif

        public void OnMuscleTriggerEnter(MuscleTriggerEvent triggerEvent);
        public void OnMuscleTrigger(MuscleTriggerEvent triggerEvent);
        public void OnMuscleTriggerExit(MuscleTriggerEvent triggerEvent);

        public void OnApplyConfigurationMix(SwolePuppetMaster puppet);

        public void Activate();

        public void FixedUpdateB(float deltaTime);

        public void AfterFixedUpdate(float deltaTime);

        public void UpdateB(float deltaTime);

        public void LateUpdateB(float deltaTime);
    } 

}

#endif