#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if BULKOUT_ENV
using RootMotion.FinalIK; // Paid Asset Integration https://assetstore.unity.com/packages/tools/animation/final-ik-14290
#endif

namespace Swole.API.Unity.Animation
{
    public class CustomIKManagerUpdater : SingletonBehaviour<CustomIKManagerUpdater>
    {
        public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority + 10; // Update after animators
        public override int Priority => ExecutionPriority;
        public override bool DestroyOnLoad => false;

        protected readonly List<CustomIKManager> members = new List<CustomIKManager>();
        public static bool Register(CustomIKManager member)
        {
            var instance = Instance;
            if (member == null || instance == null) return false;

            if (!instance.members.Contains(member)) instance.members.Add(member);
            return true;
        }
        public static bool Unregister(CustomIKManager member)
        {
            var instance = Instance;
            if (member == null || instance == null) return false;

            return instance.members.Remove(member); 
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            foreach (var member in members) if (member != null && member.enabled) member.LateUpdateStep();
            members.RemoveAll(i => i == null || i.OverrideUpdateCalls);
        }

        public override void OnUpdate()
        {
            foreach (var member in members) if (member != null && member.enabled) member.UpdateStep();
        }
    }
    public class CustomIKManager : MonoBehaviour
    {

        [SerializeField]
        protected bool overrideUpdateCalls;
        public bool OverrideUpdateCalls
        {
            get => overrideUpdateCalls;
            set => SetOverrideUpdateCalls(value);
        }

        public void SetOverrideUpdateCalls(bool value)
        {
            overrideUpdateCalls = value;
            if (value)
            {
                CustomIKManagerUpdater.Unregister(this);
            }
            else
            {
                CustomIKManagerUpdater.Register(this);
            }
        }

        public abstract class IKController
        {
            public string name;

            public abstract bool CanBeToggled { get; set; }

            public abstract bool HasComponent { get; }
            public abstract Component Component { get; }

            public abstract int ChainLength { get; }
            public Transform this[int boneChainIndex] => GetBone(boneChainIndex);
            public abstract Transform GetBone(int boneChainIndex);

            public abstract Transform Target { get; }
            public abstract Transform BendGoal { get; }

            public abstract bool IsDependentOn(Transform transform);

            public abstract void SetActive(bool enabled);
            public abstract bool IsActive { get; }

            public abstract void SetWeight(float weight);
            public abstract void SetPositionWeight(float weight);
            public abstract void SetRotationWeight(float weight);
            public abstract void SetBendGoalWeight(float weight);

            public abstract float GetWeight();
            public abstract float GetPositionWeight();
            public abstract float GetRotationWeight();
            public abstract float GetBendGoalWeight();
        }

#if BULKOUT_ENV
        [Serializable]
        public class FinalIKComponent : IKController
        {

            public float baseWeight = 1;
            public float positionWeight = 1;
            public float rotationWeight = 1;

            public bool disable;
            public IK component;

            public bool disableToggling;
            public override bool CanBeToggled 
            {
                get => !disableToggling;
                set => disableToggling = !value;   
            }

            public override bool HasComponent => component != null;
            public override Component Component => component;

            public override int ChainLength 
            {
                get
                {
                    if (component != null)
                    {
                        if (component.GetIKSolver() is IKSolverTrigonometric) return 3;
                    }

                    return 0;
                }
            }

            public override Transform GetBone(int boneChainIndex)
            {
                if (component is LimbIK limbIK)
                {
                    if (boneChainIndex == 0) return limbIK.solver.bone1.transform;
                    if (boneChainIndex == 1) return limbIK.solver.bone2.transform;
                    if (boneChainIndex == 2) return limbIK.solver.bone3.transform;

                    return null;
                }

                return null;
            }

            public override Transform Target 
            {
                get
                {
                    if (component != null)
                    {
                        if (component.GetIKSolver() is IKSolverTrigonometric trigIk) return trigIk.target; 
                    }
                    return null;
                }
            }
            public override Transform BendGoal
            {
                get
                {
                    if (component != null)
                    {
                        if (component.GetIKSolver() is IKSolverLimb limbIk) return limbIk.bendGoal; 
                    }
                    return null;
                }
            }

            public override bool IsDependentOn(Transform transform)
            {
                if (transform == null) return false;

                if (transform == Target) return true;
                if (transform == BendGoal) return true;

                for (int a = 0; a < ChainLength; a++) if (transform == GetBone(a)) return true;

                return false;
            }

            public override void SetActive(bool active)
            {
                disable = !active;
            }
            public override bool IsActive => !disable;

            public override void SetWeight(float weight)
            {
                baseWeight = weight;
                SetPositionWeight(positionWeight);
                SetRotationWeight(rotationWeight);
            }
            public override void SetPositionWeight(float weight)
            {
                positionWeight = weight;
                if (component == null) return;
                var solver = component.GetIKSolver();
                if (solver == null) return;

                solver.SetIKPositionWeight(positionWeight * baseWeight);
            }
            public override void SetRotationWeight(float weight)
            {
                rotationWeight = weight;
                if (component == null) return;
                var solver = component.GetIKSolver();
                if (solver is not IKSolverTrigonometric solverTrig) return;

                solverTrig.SetIKRotationWeight(rotationWeight * baseWeight);
            }
            public override void SetBendGoalWeight(float weight)
            {
                if (component == null) return;
                var solver = component.GetIKSolver();
                if (solver is not IKSolverLimb solverLimb) return;

                solverLimb.bendModifierWeight = weight;
            }

            public override float GetWeight() => baseWeight;
            public override float GetPositionWeight()
            {
                if (component == null) return 1;
                var solver = component.GetIKSolver();
                if (solver == null) return 1;

                return positionWeight;// solver.GetIKPositionWeight();
            }
            public override float GetRotationWeight()
            {
                if (component == null) return 1;
                var solver = component.GetIKSolver();
                if (solver is not IKSolverTrigonometric solverTrig) return 1;

                return rotationWeight;// solverTrig.GetIKRotationWeight();
            }
            public override float GetBendGoalWeight()
            {
                if (component == null) return 1;
                var solver = component.GetIKSolver();
                if (solver is not IKSolverLimb solverLimb) return 1;

                return solverLimb.bendModifierWeight;
            }
        }

        [SerializeField]
        protected FinalIKComponent[] finalIKComponents;
#endif

        public int FindIKControllerIndex(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            name = name.AsID();

#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                for(int a = 0; a <  finalIKComponents.Length; a++) if (finalIKComponents[a].name.AsID() == name) return a;
            }
#endif

            return FindIKControllerIndexByGameObject(name); // Fallback
        }
        public int FindIKControllerIndexByGameObject(string gameObjectName)
        {
            if (string.IsNullOrWhiteSpace(name)) return -1;
            name = name.AsID();

#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                for (int a = 0; a < finalIKComponents.Length; a++) if (finalIKComponents[a].component != null && finalIKComponents[a].component.name.AsID() == name) return a;
            }
#endif

            return -1;
        }

        public int FindAssociatedIKControllerIndex(UnityEngine.Object obj)
        {
            int index = -1;

            if (obj is Transform t)
            {
                for (int a = 0; a < ControllerCount; a++)
                {
                    var c = GetController(a);
                    if (c.IsDependentOn(t))
                    {
                        return a;
                    }
                }
            }
            else if (obj is GameObject go)
            {
                t = go.transform;
                for (int a = 0; a < ControllerCount; a++)
                {
                    var c = GetController(a);
                    if (c.HasComponent && c.Component.gameObject == go)
                    {
                        return a;
                    }
                    else if (c.IsDependentOn(t))
                    {
                        return a;
                    }
                }
            }

            return index;
        }
        public bool TryFindAssociatedIKController(UnityEngine.Object obj, out IKController controller)
        {
            controller = null;
            int index = FindAssociatedIKControllerIndex(obj);
            if (index >= 0)
            {
                controller = GetController(index);
                return true;
            }

            return false;
        }

        public int ControllerCount
        {
            get
            {
                int count = 0;
#if BULKOUT_ENV
                if (finalIKComponents != null) count += finalIKComponents.Length;
#endif
                return count;
            }
        }
        public IKController this[int index] => GetController(index);

        public IKController GetController(int index)
        {
            if (index < 0) return null;

#if BULKOUT_ENV
            if (finalIKComponents != null && index < finalIKComponents.Length) return finalIKComponents[index];
#endif

            return null;
        }

        public delegate void IKControllerDelegate(int index, IKController controller);
        public void IterateControllers(IKControllerDelegate process)
        {
            for (int a = 0; a < ControllerCount; a++) process(a, this[a]);
        }
        public void IterateTogglableControllers(IKControllerDelegate process)
        {
            for (int a = 0; a < ControllerCount; a++) 
            {
                var controller = this[a];
                if (controller != null && controller.CanBeToggled) process(a, controller); 
            }
        }

        protected void DisableController(int index, IKController controller) { if (controller != null) controller.SetActive(false); }
        protected void EnableController(int index, IKController controller) { if (controller != null) controller.SetActive(true); }
        public void DisableAllControllers() => IterateControllers(DisableController);
        public void EnableAllControllers() => IterateControllers(EnableController);
        public void DisableAllTogglableControllers() => IterateTogglableControllers(DisableController);
        public void EnableAllTogglableControllers() => IterateTogglableControllers(EnableController);

        protected void ResetControllerPositionWeight(int index, IKController controller) { if (controller != null) controller.SetPositionWeight(1); }
        public void ResetPositionWeightOfAllControllers() => IterateControllers(ResetControllerPositionWeight);
        public void ResetPositionWeightOfAllTogglableControllers() => IterateTogglableControllers(ResetControllerPositionWeight);
        protected void ResetControllerRotationWeight(int index, IKController controller) { if (controller != null) controller.SetRotationWeight(1); }
        public void ResetRotationWeightOfAllControllers() => IterateControllers(ResetControllerRotationWeight);
        public void ResetRotationWeightOfAllTogglableControllers() => IterateTogglableControllers(ResetControllerRotationWeight);
        protected void ResetControllerBendGoalWeight(int index, IKController controller) { if (controller != null) controller.SetBendGoalWeight(1); } 
        public void ResetBendGoalWeightOfAllControllers() => IterateControllers(ResetControllerBendGoalWeight); 
        public void ResetBendGoalWeightOfAllTogglableControllers() => IterateTogglableControllers(ResetControllerBendGoalWeight); 

        protected virtual void Awake()
        {
#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                foreach(var ik in finalIKComponents)
                {
                    if (ik == null || ik.component == null) continue;
                    ik.component.enabled = false;

                    ik.SetPositionWeight(ik.positionWeight);
                    ik.SetRotationWeight(ik.rotationWeight); 
                }
            }
#endif
            SetOverrideUpdateCalls(OverrideUpdateCalls); // Force register to updater
        }
        protected virtual void OnDestroy()
        {
            if (!OverrideUpdateCalls) CustomIKManagerUpdater.Unregister(this);

            ClearAllListeners();
        }

        public virtual void ForceInitializeSolvers() 
        {
#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                foreach (var ik in finalIKComponents)
                {
                    if (ik == null || ik.component == null) continue;
                    var solver = ik.component.GetIKSolver();
                    if (solver == null) continue;

                    solver.Update(); // just update for now, which will initialize it
                }
            }
#endif
        }

        protected virtual void FixTransforms()
        {
#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                foreach (var ik in finalIKComponents)
                {
                    if (ik == null || ik.disable || ik.component == null || !ik.component.fixTransforms) continue;
                    var solver = ik.component.GetIKSolver();
                    if (solver == null) continue;

                    solver.FixTransforms();
                }
            }
#endif
        }

        protected virtual void UpdateSolvers()
        {
#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                foreach(var ik in finalIKComponents)
                {
                    if (ik == null || ik.disable || ik.component == null) continue;
                    var solver = ik.component.GetIKSolver();
                    if (solver == null) continue;

                    solver.Update();
                }
            }
#endif
        }

        protected virtual void FixTransformsAndUpdateSolvers()
        {
#if BULKOUT_ENV
            if (finalIKComponents != null)
            {
                foreach (var ik in finalIKComponents)
                {
                    if (ik == null || ik.disable || ik.component == null) continue;
                    var solver = ik.component.GetIKSolver();
                    if (solver == null) continue;

                    if (ik.component.fixTransforms) solver.FixTransforms();
                    solver.Update();
                }
            }
#endif
        }

        public void ClearAllListeners()
        {
            PreLateUpdate = null;
            PostLateUpdate = null;
        }

        public virtual void UpdateStep()
        {
        }

        public event VoidParameterlessDelegate PreLateUpdate;
        public event VoidParameterlessDelegate PostLateUpdate;

        public virtual void LateUpdateStep()
        {
            PreLateUpdate?.Invoke();

            FixTransformsAndUpdateSolvers();

            PostLateUpdate?.Invoke();
        }

    }
}

#endif