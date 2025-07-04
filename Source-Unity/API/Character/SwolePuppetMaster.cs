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
    public class SwolePuppetMaster : PuppetMaster, ISwolePuppetMaster
#else
    public class SwolePuppetMaster : Monobehaviour, ISwolePuppetMaster 
#endif
    {

        [SerializeField]
        protected SwolePuppetMuscleConfigurationAsset defaultConfigurationAsset;
        protected SwolePuppetMuscleConfiguration defaultConfiguration;

        public void SetDefaultConfiguration(SwolePuppetMuscleConfigurationAsset configuration)
        {
            defaultConfigurationAsset = configuration;
            SetDefaultConfiguration(configuration == null ? default : configuration.configuration);
        }
        public void SetDefaultConfiguration(SwolePuppetMuscleConfiguration configuration)
        {
            defaultConfiguration = configuration.CacheMuscleIndices(this);
        }

        public void ApplyDefaultConfiguration()
        {
            defaultConfiguration.ApplyUsingIndexCache(this);
        }

        [NonSerialized]
        public SwolePuppetMuscleConfiguration currentConfiguration;

        public void ValidateCurrentConfiguration()
        {
            if (currentConfiguration.muscleStates == null || currentConfiguration.muscleStates.Length != muscles.Length)
            {
                int prevSize = currentConfiguration.muscleStates == null ? 0 : currentConfiguration.muscleStates.Length;
                Array.Resize(ref currentConfiguration.muscleStates, muscles.Length);
                for (int i = prevSize; i < currentConfiguration.muscleStates.Length; i++)
                {
                    var muscle = muscles[i];
                    if (muscle == null) continue;

                    SwolePuppetMuscleState state = muscle.state;
                    state.name = muscle.name;
                    state.cachedIndex = i + 1;

                    currentConfiguration.muscleStates[i] = state;
                }
            }
        }

        public SwolePuppetMuscleState GetMuscleState(int muscleIndex)
        {
            ValidateCurrentConfiguration();
            return currentConfiguration.muscleStates[muscleIndex];
        }
        public void SetMuscleState(int muscleIndex, SwolePuppetMuscleState state)
        {
            ValidateCurrentConfiguration();

            currentConfiguration.muscleStates[muscleIndex] = state;

            var muscle = muscles[muscleIndex];
            if (muscle == null) return;

            state.Apply(pinWeight, muscle);
        }

        public struct MuscleConfigMix
        {
            public SwolePuppetMuscleConfiguration config;
            public float mix;
        }
        public void ApplyConfigurationMix(IEnumerable<MuscleConfigMix> configs)
        {
            if (muscles == null) return;

            ApplyDefaultConfiguration();

            if (configs == null) return;

            bool first = true;
            foreach(var config in configs)
            {
                ApplyConfigurationMixInternal(config.config, config.mix, !first);
                first = false;
            }

            if (behaviours != null)
            {
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is ISwolePuppetBehaviour spb)
                    {
                        spb.OnApplyConfigurationMix(this); 
                    }
                }
            }
        }
        public void ApplyConfigurationMix(ICollection<ICollection<MuscleConfigMix>> configCollections)
        {
            if (muscles == null) return;

            ApplyDefaultConfiguration();

            if (configCollections == null || configCollections.Count <= 0) return;

            float mix = 1f / configCollections.Count;

            bool first = true;
            foreach (var configCollection in configCollections)
            {
                foreach (var config in configCollection)
                {
                    ApplyConfigurationMixInternal(config.config, mix * config.mix, !first);
                    first = false;
                }
            }

            if (behaviours != null)
            {
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is ISwolePuppetBehaviour spb)
                    {
                        spb.OnApplyConfigurationMix(this);
                    }
                }
            }
        }
         
        public struct MuscleConfigCollectionMix
        {
            public ICollection<MuscleConfigMix> collection;
            public float mix;
        }
        public void ApplyConfigurationMix(ICollection<MuscleConfigCollectionMix> configCollections)
        {
            if (muscles == null) return;

            ApplyDefaultConfiguration();

            if (configCollections == null || configCollections.Count <= 0) return;

            bool first = true;
            foreach (var configCollection in configCollections)
            {
                if (configCollection.collection == null) continue;

                foreach (var config in configCollection.collection)
                {
                    ApplyConfigurationMixInternal(config.config, configCollection.mix * config.mix, !first); 
                    first = false;
                }
            }

            if (behaviours != null)
            {
                foreach(var behaviour in behaviours)
                {
                    if (behaviour is ISwolePuppetBehaviour spb)
                    {
                        spb.OnApplyConfigurationMix(this);
                    }
                }
            }
        }

        public void ApplyConfigurationMix(ICollection<SwolePuppetMuscleConfiguration> configs)
        {
            if (muscles == null) return;

            ApplyDefaultConfiguration();

            if (configs == null || configs.Count <= 0) return;

            float mix = 1f / configs.Count;

            bool first = true;
            foreach (var config in configs)
            {
                ApplyConfigurationMixInternal(config, mix, !first);
                first = false;
            }

            if (behaviours != null)
            {
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is ISwolePuppetBehaviour spb)
                    {
                        spb.OnApplyConfigurationMix(this);
                    }
                }
            }
        }

        public void ApplyConfigurationMix(SwolePuppetMuscleConfiguration configA, SwolePuppetMuscleConfiguration configB, float mix)
        {
            if (muscles == null) return;

            ApplyDefaultConfiguration();

            if (!configA.IsValid && !configB.IsValid) return;

            ApplyConfigurationMixInternal(configA, 1f - mix, false);
            ApplyConfigurationMixInternal(configB, mix, true);

            if (behaviours != null)
            {
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is ISwolePuppetBehaviour spb)
                    {
                        spb.OnApplyConfigurationMix(this);
                    }
                }
            }
        }
        public void ApplyConfigurationMix(SwolePuppetMuscleConfiguration config, float mix)
        {
            if (config.IsValid) ApplyConfigurationMix(defaultConfiguration, config, mix);
        }

        private void ApplyConfigurationMixInternal(SwolePuppetMuscleConfiguration config, float mix, bool additive)
        {
            ValidateCurrentConfiguration();

            if (!config.IsValid) config = defaultConfiguration;

            if (additive)
            {
                currentConfiguration.unbalancingSpeed = currentConfiguration.unbalancingSpeed + config.unbalancingSpeed * mix;
                currentConfiguration.rebalancingSpeed = currentConfiguration.rebalancingSpeed + config.rebalancingSpeed * mix;

                for (int a = 0; a < config.muscleStates.Length; a++)
                {
                    var state = config.muscleStates[a];

                    int muscleIndex = -1;
                    if (state.HasMuscleIndex)
                    {
                        muscleIndex = state.MuscleIndex;
                    }
                    else
                    {
                        muscleIndex = IndexOfMuscle(state.name);
                    }

                    if (muscleIndex < 0) continue;

                    var muscle = muscles[muscleIndex];
                    if (muscle == null) continue;

                    var currentState = currentConfiguration.muscleStates[muscleIndex];
                    if (!currentState.flag) continue;

                    state = currentState + state * mix;
                    currentConfiguration.muscleStates[muscleIndex] = state;  

                    state.Apply(pinWeight, muscle);
                }
            } 
            else
            {
                currentConfiguration.unbalancingSpeed = config.unbalancingSpeed * mix;
                currentConfiguration.rebalancingSpeed =  config.rebalancingSpeed * mix;

                for (int a = 0; a < config.muscleStates.Length; a++)
                {
                    var state = config.muscleStates[a];

                    int muscleIndex = -1;
                    if (state.HasMuscleIndex)
                    {
                        muscleIndex = state.MuscleIndex;
                    }
                    else
                    {
                        muscleIndex = IndexOfMuscle(state.name);
                    }

                    if (muscleIndex < 0) continue;

                    var muscle = muscles[muscleIndex];
                    if (muscle == null) continue;

                    state = state * mix;
                    state.flag = true;
                    currentConfiguration.muscleStates[muscleIndex] = state;

                    state.Apply(pinWeight, muscle);
                }
            }
        }

        public int IndexOfMuscle(string muscleName)
        {
            if (muscles == null) return -1;

            for(int a = 0; a < muscles.Length; a++) if (muscles[a].name == muscleName) return a;

            return -1;
        }
        public Muscle FindMuscle(string muscleName)
        {
            int ind = IndexOfMuscle(muscleName);
            if (ind < 0) return null;

            return muscles[ind];
        }
        public bool TryFindMuscle(string muscleName, out Muscle muscle)
        {
            muscle = FindMuscle(muscleName);
            return muscle != null;
        }

        [SerializeField]
        protected Transform muscleRoot;
        public Transform MuscleRoot
        {
            get
            {
                if (muscleRoot == null) muscleRoot = targetRoot;
                return muscleRoot;
            }
        }

        public Transform RootTransform
        {
            get => targetRoot == null ? transform : targetRoot;
        }

        public void BuildRagdoll(CharacterRagdoll ragdoll, string musclePrefix) => BuildRagdoll(ragdoll, null, musclePrefix);
        public void BuildRagdoll(CharacterRagdoll ragdoll, Transform muscleRoot, string musclePrefix)
        {
            if (muscleRoot == null)
            {
                muscleRoot = MuscleRoot;
            }

            this.muscleRoot = muscleRoot;
            if (muscleRoot != null)
            {
                var rb = muscleRoot.gameObject.AddOrGetComponent<Rigidbody>();
                rb.isKinematic = true; 
            }
             
            ragdoll.BuildLocalWithMuscleRoot(out List<CharacterRagdoll.MuscleJoint> muscleJoints, muscleRoot, null, musclePrefix);

#if BULKOUT_ENV
            List<Muscle> newMuscles = new List<Muscle>();
            List<PropMuscle> newPropMuscles = new List<PropMuscle>();

            foreach (var joint in muscleJoints)
            {
                newMuscles.Add(new Muscle()
                {
                    isPropMuscle = joint.isProp, 
                    name = joint.joint.name,
                    target = joint.reference,
                    joint = joint.joint,
                    props = new Muscle.Props()
                    {
                        group = joint.group.AsUnityType()
                    }
                });
            }

            muscles = newMuscles.ToArray();
            propMuscles = newPropMuscles.ToArray();
#endif
        }

        public void RemoveRagdoll(CharacterRagdoll ragdoll, string musclePrefix) => RemoveRagdoll(ragdoll, musclePrefix, false);
        public void RemoveRagdoll(CharacterRagdoll ragdoll, string musclePrefix, bool immediate)
        {
            ragdoll.RemoveComponentsWithMuscleRoot(muscleRoot, immediate, musclePrefix);

#if BULKOUT_ENV
            muscles = null;
            propMuscles = null;
#endif
        }

        public virtual void EnterTriggerMode()
        {
            for (int i = 0; i < muscles.Length; i++) 
            {
                EnterTriggerModeUnsafe(i); 
            }
        }
        public virtual void EnterTriggerMode(int muscleIndex)
        {
#if BULKOUT_ENV
            if (muscleIndex < 0 || muscleIndex >= muscles.Length) return;
            EnterTriggerModeUnsafe(muscleIndex);
#endif
        }
        public virtual void EnterTriggerModeUnsafe(int muscleIndex)
        {
#if BULKOUT_ENV
            var muscle = muscles[muscleIndex];
            if (muscle.colliders != null)
            {
                foreach (var collider in muscle.colliders)
                {
                    if (collider == null || collider.isTrigger) continue;

                    collider.isTrigger = true;
                }
            }
#endif
        }

        public virtual void ExitTriggerMode()
        {
            for (int i = 0; i < muscles.Length; i++)
            {
                ExitTriggerModeUnsafe(i);  
            }
        }
        public virtual void ExitTriggerMode(int muscleIndex)
        {
#if BULKOUT_ENV
            if (muscleIndex < 0 || muscleIndex >= muscles.Length) return;  
            ExitTriggerModeUnsafe(muscleIndex);
#endif
        }
        public virtual void ExitTriggerModeUnsafe(int muscleIndex)
        {
#if BULKOUT_ENV
            var muscle = muscles[muscleIndex];
            if (muscle.colliders != null)
            {
                foreach (var collider in muscle.colliders)
                {
                    if (collider == null || !collider.isTrigger) continue;

                    collider.isTrigger = false;
                }
            }
#endif
        }

#if BULKOUT_ENV
        protected override void Initiate()
        {
            base.Initiate();

            for (int i = 0; i < muscles.Length; i++)
            {
                var muscle = muscles[i];
                if (muscle.broadcaster != null)
                {
                    GameObject.DestroyImmediate(muscle.broadcaster);

                    muscle.broadcaster = muscle.joint.gameObject.AddOrGetComponent<SwoleMuscleCollisionBroadcaster>(); 
                    muscle.broadcaster.puppetMaster = this;
                    muscle.broadcaster.muscleIndex = i;
                }
            }
        }
#endif

#if BULKOUT_ENV
        protected override void Awake()
#else
        protected virtual void Awake()
#endif
        {
#if BULKOUT_ENV
            base.Awake();
#endif

            if (defaultConfigurationAsset == null)
            {
                if (muscles != null)
                {
                    defaultConfiguration = new SwolePuppetMuscleConfiguration();
                    defaultConfiguration.unbalancingSpeed = 1f;
                    defaultConfiguration.rebalancingSpeed = 1f;

                    defaultConfiguration.muscleStates = new SwolePuppetMuscleState[muscles.Length];
                    for (int i = 0; i < muscles.Length; i++)
                    {
                        var muscle = muscles[i];
                        if (muscle == null) continue;

                        SwolePuppetMuscleState state = muscle.state;
                        state.name = muscle.name;
                        state.cachedIndex = i + 1;

                        if (muscle.target != null)
                        {
                            muscle.target.GetPositionAndRotation(out state.lastAnimatedPosition_ROOTSPACE, out state.lastAnimatedRotation_ROOTSPACE);
                            state.lastAnimatedPosition_ROOTSPACE = RootTransform.InverseTransformPoint(state.lastAnimatedPosition_ROOTSPACE);
                            state.lastAnimatedRotation_ROOTSPACE = Quaternion.Inverse(RootTransform.rotation) * state.lastAnimatedRotation_ROOTSPACE;
                        } 
                        else if (muscle.transform != null)
                        {
                            muscle.transform.GetPositionAndRotation(out state.lastAnimatedPosition_ROOTSPACE, out state.lastAnimatedRotation_ROOTSPACE);
                            state.lastAnimatedPosition_ROOTSPACE = RootTransform.InverseTransformPoint(state.lastAnimatedPosition_ROOTSPACE);
                            state.lastAnimatedRotation_ROOTSPACE = Quaternion.Inverse(RootTransform.rotation) * state.lastAnimatedRotation_ROOTSPACE;
                        } 
                        else
                        {
                            state.lastAnimatedRotation_ROOTSPACE = Quaternion.identity;
                        }

                        state.unbalanceDistanceThreshold = 0.1f;
                        state.unbalanceDistanceRange = 0.5f;

                        state.unbalanceAngleThreshold = 10f;
                        state.unbalanceAngleRange = 25f;

                        defaultConfiguration.muscleStates[i] = state;
                    }
                }
            } 
            else
            {
                SetDefaultConfiguration(defaultConfigurationAsset); 
            }

            //SetOverrideUpdateCalls(OverrideUpdateCalls); // Force register to updater
            SwolePuppetMasterUpdater.Register(this);
        }

        public override void Start()
        {
            base.Start();

            if (defaultConfigurationAsset != null) ApplyDefaultConfiguration();
        }

#if BULKOUT_ENV
        protected override void OnDestroy()
#else
        protected virtual void OnDestroy()
#endif
        {
            /*if (!OverrideUpdateCalls) */SwolePuppetMasterUpdater.Unregister(this);

#if BULKOUT_ENV
            base.OnDestroy();
#endif
        } 


        /// <summary>
        /// Gets the Swole Animator on the target.
        /// </summary>
        public CustomAnimator swoleAnimator
        {
            get
            {
                // Protect from the Animator being replaced (UMA)
                if (_swoleAnimator == null) _swoleAnimator = targetRoot.GetComponentInChildren<CustomAnimator>();
                if (_swoleAnimator == null && targetRoot.parent != null) _swoleAnimator = targetRoot.parent.GetComponentInChildren<CustomAnimator>();
                return _swoleAnimator;
            }
            set
            {
                _swoleAnimator = value;
            }
        }
        protected CustomAnimator _swoleAnimator;

        public bool DisabledBySkip
        {
            get
            {
                if (swoleAnimator != null) return _swoleAnimator.disabledBySkip;
                return false;
            }
        }

#if BULKOUT_ENV
        protected override void SetAnimationEnabled(bool to)
        {
            animatorDisabled = false;

            if (swoleAnimator != null)
            {
                swoleAnimator.enabled = to;
            }
        }

        protected override void FrozenToAlive()
        {
            freezeFlag = false;

            foreach (Muscle m in muscles)
            {
                m.state.pinWeightMlp = 1f;
                m.state.muscleWeightMlp = 1f;
                m.state.muscleDamperAdd = 0f;
            }

            if (angularLimitsEnabledOnKill)
            {
                angularLimits = false;
                angularLimitsEnabledOnKill = false;
            }
            if (internalCollisionsEnabledOnKill)
            {
                internalCollisions = false;
                internalCollisionsEnabledOnKill = false;
            }

            ActivateRagdoll();

            foreach (BehaviourBase behaviour in behaviours)
            {
                behaviour.Unfreeze();
                behaviour.Resurrect();

                if (behaviour.deactivated) behaviour.gameObject.SetActive(true);
            }

            if (swoleAnimator != null) swoleAnimator.enabled = true;

            activeState = State.Alive;

            if (OnUnfreeze != null) OnUnfreeze();
            if (OnResurrection != null) OnResurrection();
        }

        // If reactivating a PuppetMaster that has been forcefully deactivated and state/mode switching interrupted
        protected override void OnEnable()
        {
            if (gameObject.activeInHierarchy && initiated && hasBeenDisabled && Application.isPlaying)
            {
                // Reset mode
                isSwitchingMode = false;
                activeMode = mode;
                lastMode = mode;
                mappingBlend = mode == Mode.Active ? 1f : 0f;

                // Reset state
                activeState = state;
                lastState = state;
                isKilling = false;
                freezeFlag = false;

                // Animation
                SetAnimationEnabled(state == State.Alive);
                if (state == State.Alive && swoleAnimator != null && swoleAnimator.gameObject.activeInHierarchy)
                {
                    swoleAnimator.UpdateStep(0.001f, true, true, false);
                    swoleAnimator.FinalizeAnimations(false);
                }

                // Muscle weights
                foreach (Muscle m in muscles)
                {
                    m.state.pinWeightMlp = state == State.Alive ? 1f : 0f;
                    m.state.muscleWeightMlp = state == State.Alive ? 1f : stateSettings.deadMuscleWeight;
                    m.state.muscleDamperAdd = 0f;
                    //m.state.immunity = 0f;
                }

                // Ragdoll and behaviours
                if (state != State.Frozen && mode != Mode.Disabled)
                {
                    ActivateRagdoll(mode == Mode.Kinematic);

                    foreach (BehaviourBase behaviour in behaviours)
                    {
                        behaviour.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Deactivate/Freeze
                    foreach (Muscle m in muscles)
                    {
                        m.joint.gameObject.SetActive(false);
                    }

                    // Freeze
                    if (state == State.Frozen)
                    {
                        foreach (BehaviourBase behaviour in behaviours)
                        {
                            if (behaviour.gameObject.activeSelf)
                            {
                                behaviour.deactivated = true;
                                behaviour.gameObject.SetActive(false);
                            }
                        }

                        if (stateSettings.freezePermanently)
                        {
                            if (behaviours.Length > 0 && behaviours[0] != null)
                            {
                                Destroy(behaviours[0].transform.parent.gameObject);
                            }
                            Destroy(gameObject);
                            return;
                        }
                    }
                }

                // Reactivate behaviours
                foreach (BehaviourBase behaviour in behaviours)
                {
                    behaviour.OnReactivate();
                }
            }
        }
        protected override void OnDisable()
        {

            base.OnDisable();
        }

        protected override bool autoSimulate => true;

        protected override void Update()
        {
        }
        public virtual void UpdateStep(float deltaTime)
        {
            if (state != PuppetMaster.State.Alive)
            {
                for (int a = 0; a < muscles.Length; a++)
                {
                    var muscle = muscles[a];
                    if (muscle == null) continue;

                    var state = muscle.state;
                    state.mappingWeightMlp = Mathf.MoveTowards(state.mappingWeightMlp, 1f, deltaTime * 2f);
                    muscle.state = state;
                }
            }

            foreach (BehaviourBase b in behaviours) b.UpdateB(deltaTime);

            if (!initiated) return;
            if (!autoSimulate) return;
            if (muscles.Length <= 0) return; 

            if (animatorDisabled)
            {
                swoleAnimator.enabled = true;
                animatorDisabled = false;
            }

            if (updateMode != UpdateMode.Normal) return;

            // Fix transforms to be sure of not having any drifting when the target bones are not animated
            FixTargetTransforms();
        }

        protected override void LateUpdate()
        {
        }
#endif

        public UnityEvent PostLateUpdate = new UnityEvent();

        public virtual void LateUpdateStep(float deltaTime)
        {
#if BULKOUT_ENV
            base.LateUpdate();
#endif

            PostLateUpdate?.Invoke(); 
        }

#if BULKOUT_ENV
        protected override void FixedUpdate()
        {
        }
#endif

        public UnityEvent PostFixedUpdate = new UnityEvent();

        public virtual void FixedUpdateStep(float deltaTime)
        {
#if BULKOUT_ENV
            base.FixedUpdate();
#endif

            PostFixedUpdate?.Invoke();
            foreach (BehaviourBase b in behaviours) 
            {
                if (b is ISwolePuppetBehaviour sb) sb.AfterFixedUpdate(Time.deltaTime); 
            }           
        }

#if BULKOUT_ENV
        protected override void Read()
        {
            base.Read();

            if (currentConfiguration.muscleStates != null)
            {
                var root = RootTransform;
                Quaternion toRootRot = Quaternion.Inverse(root.rotation);
                for (int a = 0; a < muscles.Length; a++)
                {
                    var muscle = muscles[a];
                    var state = currentConfiguration.muscleStates[a];

                    state.lastAnimatedPosition_ROOTSPACE = root.InverseTransformPoint(muscle.targetAnimatedPosition);
                    state.lastAnimatedRotation_ROOTSPACE = toRootRot * muscle.targetAnimatedWorldRotation;  

                    currentConfiguration.muscleStates[a] = state;
                }
            }
        }

        protected override void OnLateUpdate()
        {
            if (!initiated) return; 

            if (swoleAnimator != null && activeState == State.Alive && !isSwitchingState)
            {
                ApplyConfigurationMix(swoleAnimator.GetCurrentMuscleConfigMix());
            }

            if (animatorDisabled)
            {
                swoleAnimator.enabled = true; 
                animatorDisabled = false;
            }

            bool animationApplied = updateMode == UpdateMode.Normal || (!readInFixedUpdate && fixedFrame);
            readInFixedUpdate = false;
            bool muscleRead = animationApplied && isActive; // If disabled, reading will be done in PuppetMasterModes.cs

            if (animationApplied)
            {
                if (OnRead != null) OnRead(); // Update IK
                foreach (BehaviourBase behaviour in behaviours) behaviour.OnRead(Time.deltaTime);
            }
            if (muscleRead) Read();

            // Switching states
            SwitchStates();

            // Switching modes
            SwitchModes();

            switch (updateMode)
            {
                case UpdateMode.FixedUpdate:
                    if (!fixedFrame && !interpolated) return;
                    break;
                case UpdateMode.AnimatePhysics:
                    if (!fixedFrame && !interpolated) return;
                    break;
            }

            // Below is common code for all update modes! For AnimatePhysics modes the following code will run only in fixed frames
            fixedFrame = false;

            // Mapping
            if (!isFrozen)
            {
                mappingWeight = Mathf.Clamp(mappingWeight, 0f, 1f);
                float mW = mappingWeight * mappingBlend;

                if (mW > 0f)
                {
                    if (isActive)
                    {
                        //Debug.DrawLine(muscles[0].transform.position, Vector3.zero, Color.blue, 1f);
                        //if (muscles[0].transform.position.y > 2.5f) Debug.Break();

                        for (int i = 0; i < muscles.Length; i++) muscles[i].Map(mW);
                    }
                }
                else
                {
                    // Moving to Target when in Kinematic mode
                    if (activeMode == Mode.Kinematic) 
                    { 
                        MoveToTarget(); 
                    } 
                    else
                    {
                        for (int i = 0; i < muscles.Length; i++) muscles[i].lastMappingWeight = 0f;
                    }
                }

                foreach (BehaviourBase behaviour in behaviours) behaviour.OnWrite(Time.deltaTime);
                if (OnWrite != null) OnWrite();

                //StoreTargetMappedState(); //@todo no need to do this all the time

                foreach (Muscle m in muscles) m.CalculateMappedVelocity();
            }

            if (mapDisconnectedMuscles)
            {
                for (int i = 0; i < muscles.Length; i++) muscles[i].MapDisconnected(); 
            }

            // Freezing
            if (freezeFlag) OnFreezeFlag();
        }
#endif
    }

    public interface ISwolePuppetMaster
    {
        public void BuildRagdoll(CharacterRagdoll ragdoll, string musclePrefix);
        public void BuildRagdoll(CharacterRagdoll ragdoll, Transform muscleRoot, string musclePrefix);

        public void RemoveRagdoll(CharacterRagdoll ragdoll, string musclePrefix);
        public void RemoveRagdoll(CharacterRagdoll ragdoll, string musclePrefix, bool immediate);

        public void EnterTriggerMode();
        public void EnterTriggerMode(int muscleIndex);
        public void ExitTriggerMode();
        public void ExitTriggerMode(int muscleIndex);
    }

    public class SwolePuppetMasterUpdater : SingletonBehaviour<SwolePuppetMasterUpdater>
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority - 10; // fix transforms before animator update
        public static int LateExecutionPriority => CustomIKManagerUpdater.ExecutionPriority + 5;
        public override int Priority => LateExecutionPriority;
        public override int UpdatePriority => ExecutionPriority; 
        public override bool DestroyOnLoad => false;

        protected readonly List<SwolePuppetMaster> puppets = new List<SwolePuppetMaster>();
        public static bool Register(SwolePuppetMaster puppet)
        {
            var instance = Instance;
            if (puppet == null || instance == null) return false;

#if UNITY_EDITOR
            Debug.Log($"Registered Swole Puppet {puppet.name}");
#endif

            if (!instance.puppets.Contains(puppet)) instance.puppets.Add(puppet);
            return true;
        }
        public static bool Unregister(SwolePuppetMaster puppet)
        {
            var instance = InstanceOrNull;
            if (puppet == null || instance == null) return false;

#if UNITY_EDITOR
            Debug.Log($"Unregistered Swole Puppet {puppet.name}");
#endif

            return instance.puppets.Remove(puppet);
        }

        private readonly List<VoidParameterlessDelegate> postFixedUpdateWork = new List<VoidParameterlessDelegate>();
        public static void AddPostFixedUpdateWork(VoidParameterlessDelegate work)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.postFixedUpdateWork.Add(work);
        }

        public override void OnFixedUpdate() => OnFixedUpdate(Time.fixedDeltaTime);
        public void OnFixedUpdate(float deltaTime)
        {
            foreach (var puppet in puppets)
            {
                if (puppet != null)
                {
                    if (puppet.DisabledBySkip)
                    {
                    }
                    else if (puppet.isActiveAndEnabled) puppet.FixedUpdateStep(deltaTime);
                }
            }

            foreach (var work in postFixedUpdateWork) work?.Invoke();
            postUpdateWork.Clear();
        }

        private readonly List<VoidParameterlessDelegate> postUpdateWork = new List<VoidParameterlessDelegate>();
        public static void AddPostUpdateWork(VoidParameterlessDelegate work)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.postUpdateWork.Add(work);
        }
        public override void OnUpdate() => OnUpdate(Time.deltaTime);
        public void OnUpdate(float deltaTime)
        {
            foreach (var puppet in puppets)
            {
                if (puppet == null) continue;

                if (puppet.DisabledBySkip)
                {
                }
                else if (puppet.isActiveAndEnabled) puppet.UpdateStep(deltaTime);
            }

            foreach (var work in postUpdateWork) work?.Invoke();
            postUpdateWork.Clear();
        }

        private readonly List<VoidParameterlessDelegate> postLateUpdateWork = new List<VoidParameterlessDelegate>();
        public static void AddPostLateUpdateWork(VoidParameterlessDelegate work)
        {
            var instance = Instance;
            if (instance == null) return;

            instance.postLateUpdateWork.Add(work);
        }
        public override void OnLateUpdate() => OnLateUpdate(Time.deltaTime);
        public void OnLateUpdate(float deltaTime)
        {
            foreach (var puppet in puppets)
            {
                if (puppet == null) continue;

                if (puppet.DisabledBySkip)
                {
                }
                else if (puppet.isActiveAndEnabled) puppet.LateUpdateStep(deltaTime);
            }
            puppets.RemoveAll(i => i == null /*|| i.OverrideUpdateCalls*/);

            foreach (var work in postLateUpdateWork) work?.Invoke();
            postLateUpdateWork.Clear();

            //Physics.SyncTransforms(); // should never need to do this
        }
    }

}

#endif