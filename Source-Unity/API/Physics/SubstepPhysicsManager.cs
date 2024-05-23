#if (UNITY_EDITOR || UNITY_STANDALONE)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Swole.API.Unity
{

    public class SubstepPhysicsManager : SingletonBehaviour<SubstepPhysicsManager>
    {

        public override int Priority => 50;

        protected List<SubstepPhysicsBehaviour> behaviours = new List<SubstepPhysicsBehaviour>();

        protected void RegisterLocal(SubstepPhysicsBehaviour behaviour)
        {

            behaviours.Add(behaviour);

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

            Time.fixedDeltaTime = fixedDeltaTime;

        }

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
            if (Physics.autoSimulation || !AutoSimulate) return;
            SimulateLocal();
        }

        public void SimulateLocal()
        {

            Time.fixedDeltaTime = fixedDeltaTime;

            //Physics.SyncTransforms(); Physics.Simulate almost certainly does this for us?

            int nCalls = Mathf.FloorToInt(Time.deltaTime / fixedDeltaTime);
            for (int i = 0; i < nCalls; i++)
            {

                Physics.Simulate(fixedDeltaTime);
                UpdateBehaviours();

            }

            float remainingTime = Time.deltaTime - (nCalls * fixedDeltaTime);

            Time.fixedDeltaTime = remainingTime;

            Physics.Simulate(remainingTime);
            UpdateBehaviours();

        }
        public static void Simulate()
        {
            var instance = Instance;
            if (instance == null) return;;

            instance.SimulateLocal(); 
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

            if (!Physics.autoSimulation) return;

            UpdateBehaviours();

        }

        public override void OnLateUpdate() { }

    }

}

#endif