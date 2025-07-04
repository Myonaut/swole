#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    [DefaultExecutionOrder(-40)]
    public class SubstepPhysicsManager : SingletonBehaviour<SubstepPhysicsManager>
    {

        public static int ExecutionPriority => -9999;
        public override int Priority => ExecutionPriority;

        protected List<SubstepPhysicsBehaviour> behaviours = new List<SubstepPhysicsBehaviour>();

        protected void RegisterLocal(SubstepPhysicsBehaviour behaviour)
        {

            if (!behaviours.Contains(behaviour)) behaviours.Add(behaviour);

        }

        protected void UnregisterLocal(SubstepPhysicsBehaviour behaviour)
        {

            behaviours.Remove(behaviour);

        }

        public static void Register(SubstepPhysicsBehaviour behaviour)
        {

            Instance?.RegisterLocal(behaviour);

        }

        public static void Unregister(SubstepPhysicsBehaviour behaviour)
        {

            Instance?.UnregisterLocal(behaviour);

        }

        public float fixedDeltaTime;

        protected override void OnInit()
        {

            base.OnInit();

            if (fixedDeltaTime <= 0) fixedDeltaTime = Time.fixedDeltaTime;

        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            if (fixedDeltaTime > 0) Time.fixedDeltaTime = fixedDeltaTime;

        }

        public bool forceSetSimulationMode;
        protected override void OnAwake()
        {
            base.OnAwake();

            if (forceSetSimulationMode) Physics.simulationMode = SimulationMode.Script; 
        }

        public bool applyFinalCatchUpStep = true;
        public bool updateManually;
        public static bool AutoSimulate
        {
            get 
            {
                var instance = Instance;
                if (instance == null) return false;
                return !instance.updateManually;
            }

            set
            {
                var instance = Instance;
                if (instance == null) return;
                instance.updateManually = !value;
            }
        }

        public override void OnUpdate() 
        {
#if UNITY_2022_3_OR_NEWER
            if (Physics.simulationMode != SimulationMode.Script || !AutoSimulate) return;   
#else
            if (Physics.autoSimulation || !AutoSimulate) return;
#endif
            SimulateLocal();
        }

        private float fixedDeltaStep = 0;
        public void SimulateLocal() => SimulateLocal(Time.deltaTime);
        public void SimulateLocal(float deltaTime)
        {
            if (fixedDeltaTime > 0) Time.fixedDeltaTime = fixedDeltaTime;
            float mainFixedDeltaTime = Time.fixedDeltaTime;

            Physics.SyncTransforms();

            int nCalls = Mathf.FloorToInt(deltaTime / mainFixedDeltaTime);
            for (int i = 0; i < nCalls; i++)
            {

                Physics.Simulate(mainFixedDeltaTime);
                UpdateBehaviours(); 

            }
            
            if (applyFinalCatchUpStep)
            {
                float remainingTime = deltaTime - (nCalls * mainFixedDeltaTime);
                if (remainingTime > 0)
                {
                    Time.fixedDeltaTime = remainingTime;

                    // simulate pre upate bhaviours here
                    PreUpdateBehaviours();
                    Physics.Simulate(remainingTime);
                    UpdateBehaviours();
                }

                Time.fixedDeltaTime = mainFixedDeltaTime;
            } 
            else
            {
                float remainingTime = deltaTime - (nCalls * mainFixedDeltaTime);
                fixedDeltaStep += remainingTime;

                if (fixedDeltaStep >= mainFixedDeltaTime)
                {
                    fixedDeltaStep -= mainFixedDeltaTime;
                    PreUpdateBehaviours();
                    Physics.Simulate(mainFixedDeltaTime); 
                    UpdateBehaviours();
                }
            }
        }
        public static void Simulate() => Simulate(Time.deltaTime);
        public static void Simulate(float deltaTime)
        {
            var instance = Instance;
            if (instance == null) return;;

            instance.SimulateLocal(deltaTime);  
        }

        protected void PreUpdateBehaviours()
        {
            foreach (var behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour.isActiveAndEnabled)
                {
                    behaviour.SubstepEarlyUpdate();
                }

            }
        }
        protected void UpdateBehaviours()
        {
            bool purge = false;

            foreach (var behaviour in behaviours) 
            {
                if (behaviour == null)
                {
                    purge = true;
                    continue;
                }

                if (behaviour.isActiveAndEnabled)
                {
                    behaviour.SubstepUpdate(); 
                }

            }

            foreach (var behaviour in behaviours)
            {
                if (behaviour != null && behaviour.isActiveAndEnabled)
                {
                    behaviour.SubstepLateUpdate();
                }
            }

            if (purge) behaviours.RemoveAll(i => i == null);  
        }

        public override void OnFixedUpdate()
        {

#if UNITY_2022_3_OR_NEWER
            if (Physics.simulationMode == SimulationMode.Script || !AutoSimulate) return;
#elif UNITY_2018_3_OR_NEWER
            if (!Physics.autoSimulation || !AutoSimulate) return;
#else
            return;
#endif

            PreUpdateBehaviours();
            UpdateBehaviours(); 

        }

        public override void OnLateUpdate() { } 

    }

}

#endif