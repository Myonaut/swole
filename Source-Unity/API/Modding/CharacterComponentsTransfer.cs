#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity;


#if BULKOUT_ENV
using RootMotion.FinalIK;
#endif

namespace Swole.Modding
{

    [ExecuteAlways]
    public class CharacterComponentsTransfer : MonoBehaviour
    {
        public bool apply;

        public bool includeIk;
        public bool includeConstraints;

        public GameObject rootToCopy;
        public GameObject rootTarget;

        public void Update()
        {
            if (apply)
            {
                apply = false;

                TransferComponents(rootToCopy, rootTarget, includeIk, includeConstraints);
            }
        }

        public static void TransferComponents(GameObject rootToCopy, GameObject rootTarget, bool includeIk = true, bool includeConstraints = true)
        {

            Transform rootTransform = rootTarget.transform;

            Transform FindTransform(GameObject equivalent) => ReferenceEquals(rootToCopy, equivalent) ? rootTransform : rootTransform.FindDeepChild(equivalent.name);

            IdenticalTransformConstraintCreator[] identicalTransformConstraintCreators = rootToCopy.GetComponentsInChildren<IdenticalTransformConstraintCreator>(true);
            foreach (var itcc in identicalTransformConstraintCreators)
            {
                var target = FindTransform(itcc.gameObject);
                if (target == null) continue;

                var targetITCC = target.gameObject.GetComponent<IdenticalTransformConstraintCreator>();
                if (targetITCC != null) continue;

                targetITCC = target.gameObject.AddComponent<IdenticalTransformConstraintCreator>();

                targetITCC.rootObject = itcc.rootObject == null ? null : FindTransform(itcc.rootObject.gameObject);
                if (itcc.transformsToConstrain != null)
                {
                    targetITCC.transformsToConstrain = new List<Transform>();
                    foreach (var t in itcc.transformsToConstrain)
                    {
                        targetITCC.transformsToConstrain.Add(FindTransform(t.gameObject));
                    }
                }
            }

            SetParent[] setParents = rootToCopy.GetComponentsInChildren<SetParent>(true);
            foreach (var sp in setParents)
            {
                var target = FindTransform(sp.gameObject);
                if (target == null) continue;

                var targetSP = target.gameObject.GetComponent<SetParent>();
                if (targetSP != null) continue;

                targetSP = target.gameObject.AddComponent<SetParent>();

                targetSP.onAwake = sp.onAwake;
                targetSP.parent = sp.parent == null ? null : FindTransform(sp.parent.gameObject);
            }

            if (includeConstraints)
            {
                var posConstraints = rootToCopy.GetComponentsInChildren<UnityEngine.Animations.PositionConstraint>(true);
                foreach (var c in posConstraints)
                {
                    var target = rootTarget.transform.FindDeepChildLiberal(c.name);
                    if (target == null) continue;

                    var targetC = target.gameObject.GetComponent<UnityEngine.Animations.PositionConstraint>();
                    if (targetC != null) continue;

                    targetC = target.gameObject.AddComponent<UnityEngine.Animations.PositionConstraint>();

                    if (c.sourceCount > 0)
                    {
                        var sources = new List<UnityEngine.Animations.ConstraintSource>();
                        c.GetSources(sources);
                        for (int i = 0; i < sources.Count; i++)
                        {
                            var source = sources[i];
                            if (source.sourceTransform != null)
                            {
                                source.sourceTransform = rootTarget.transform.FindDeepChildLiberal(source.sourceTransform.name);
                            }
                            sources[i] = source;
                        }
                        targetC.SetSources(sources);
                    }

                    targetC.translationAxis = c.translationAxis;
                    targetC.translationAtRest = c.translationAtRest;
                    targetC.translationOffset = c.translationOffset;
                    targetC.weight = c.weight;
                    targetC.constraintActive = c.constraintActive;
                    targetC.locked = c.locked;
                    targetC.enabled = c.enabled;
                }

                var rotConstraints = rootToCopy.GetComponentsInChildren<UnityEngine.Animations.RotationConstraint>(true);
                foreach (var c in rotConstraints)
                {
                    var target = rootTarget.transform.FindDeepChildLiberal(c.name);
                    if (target == null) continue;

                    var targetC = target.gameObject.GetComponent<UnityEngine.Animations.RotationConstraint>();
                    if (targetC != null) continue;

                    targetC = target.gameObject.AddComponent<UnityEngine.Animations.RotationConstraint>();

                    if (c.sourceCount > 0)
                    {
                        var sources = new List<UnityEngine.Animations.ConstraintSource>();
                        c.GetSources(sources);
                        for (int i = 0; i < sources.Count; i++)
                        {
                            var source = sources[i];
                            if (source.sourceTransform != null)
                            {
                                source.sourceTransform = rootTarget.transform.FindDeepChildLiberal(source.sourceTransform.name);
                            }
                            sources[i] = source;
                        }
                        targetC.SetSources(sources);
                    }

                    targetC.rotationAxis = c.rotationAxis;
                    targetC.rotationAtRest = c.rotationAtRest;
                    targetC.rotationOffset = c.rotationOffset;
                    targetC.weight = c.weight;
                    targetC.constraintActive = c.constraintActive;
                    targetC.locked = c.locked;
                    targetC.enabled = c.enabled;
                }

                var scaleConstraints = rootToCopy.GetComponentsInChildren<UnityEngine.Animations.ScaleConstraint>(true);
                foreach (var c in scaleConstraints)
                {
                    var target = rootTarget.transform.FindDeepChildLiberal(c.name);
                    if (target == null) continue;

                    var targetC = target.gameObject.GetComponent<UnityEngine.Animations.ScaleConstraint>();
                    if (targetC != null) continue;

                    targetC = target.gameObject.AddComponent<UnityEngine.Animations.ScaleConstraint>();

                    if (c.sourceCount > 0)
                    {
                        var sources = new List<UnityEngine.Animations.ConstraintSource>();
                        c.GetSources(sources);
                        for (int i = 0; i < sources.Count; i++)
                        {
                            var source = sources[i];
                            if (source.sourceTransform != null)
                            {
                                source.sourceTransform = rootTarget.transform.FindDeepChildLiberal(source.sourceTransform.name);
                            }
                            sources[i] = source;
                        }
                        targetC.SetSources(sources);
                    }

                    targetC.scalingAxis = c.scalingAxis;
                    targetC.scaleAtRest = c.scaleAtRest;
                    targetC.scaleOffset = c.scaleOffset;
                    targetC.weight = c.weight;
                    targetC.constraintActive = c.constraintActive;
                    targetC.locked = c.locked;
                    targetC.enabled = c.enabled;
                }
            }

            ProxyBone[] proxyBones = rootToCopy.GetComponentsInChildren<ProxyBone>(true);
            foreach (var pb in proxyBones)
            {
                var target = FindTransform(pb.gameObject);
                if (target == null) continue;

                var targetPB = target.gameObject.GetComponent<ProxyBone>();
                if (targetPB != null) continue;

                targetPB = target.gameObject.AddComponent<ProxyBone>();

                targetPB.hasStartingPose = pb.hasStartingPose;
                targetPB.skipAutoRegister = pb.skipAutoRegister;

                if (pb.bindings != null)
                {
                    targetPB.bindings = new ProxyBone.BoneBinding[pb.bindings.Length];
                    for (int a = 0; a < pb.bindings.Length; a++)
                    {
                        var binding = pb.bindings[a];
                        var targetBinding = new ProxyBone.BoneBinding();

                        targetBinding.bone = binding.bone == null ? null : FindTransform(binding.bone.gameObject);
                        targetBinding.binding = binding.binding;

                        targetPB.bindings[a] = targetBinding;
                    }
                }
            }

            ProxyTransform[] proxyTransforms = rootToCopy.GetComponentsInChildren<ProxyTransform>(true);
            foreach (var pt in proxyTransforms)
            {
                var target = FindTransform(pt.gameObject);
                if (target == null) continue;

                var targetPT = target.gameObject.GetComponent<ProxyTransform>();
                if (targetPT != null) continue;

                targetPT = target.gameObject.AddComponent<ProxyTransform>();

                targetPT.priority = pt.priority;
                targetPT.transformToCopy = pt.transformToCopy == null ? null : FindTransform(pt.transformToCopy.gameObject);
                targetPT.applyInWorldSpace = pt.applyInWorldSpace;
                targetPT.preserveChildTransforms = pt.preserveChildTransforms;
                targetPT.ignorePosition = pt.ignorePosition;
                targetPT.ignoreRotation = pt.ignoreRotation;
                targetPT.ignoreScale = pt.ignoreScale;

                targetPT.SetOffsets(pt.OffsetPos, pt.OffsetRot, pt.MultiplyScale);
            }

#if BULKOUT_ENV
            TrigonometricIK[] trigIKs = rootToCopy.GetComponentsInChildren<TrigonometricIK>(true);
            foreach (var ik in trigIKs)
            {
                var target = FindTransform(ik.gameObject);
                if (target == null) continue;

                var targetIK = target.gameObject.GetComponent<TrigonometricIK>();
                if (targetIK != null) continue;

                targetIK = target.gameObject.AddComponent<TrigonometricIK>();

                var solver = new IKSolverTrigonometric();

                solver.target = ik.solver.target == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.target.name);
                solver.IKRotationWeight = ik.solver.IKRotationWeight;
                solver.IKRotation = ik.solver.IKRotation;
                solver.bendNormal = ik.solver.bendNormal;
                solver.bone1 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone1.transform == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bone1.transform.name), weight = ik.solver.bone1.weight };
                solver.bone2 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone2.transform == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bone2.transform.name), weight = ik.solver.bone2.weight };
                solver.bone3 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone3.transform == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bone3.transform.name), weight = ik.solver.bone3.weight };

                targetIK.solver = solver;
            }

            LimbIK[] limbIks = rootToCopy.GetComponentsInChildren<LimbIK>(true);
            foreach (var ik in limbIks)
            {
                var target = rootTarget.transform.FindDeepChildLiberal(ik.name);
                if (target == null) continue;

                var targetIK = target.gameObject.GetComponent<LimbIK>();
                if (targetIK != null) continue;

                targetIK = target.gameObject.AddComponent<LimbIK>();

                var solver = new IKSolverLimb();

                solver.target = ik.solver.target == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.target.name);
                solver.goal = ik.solver.goal;
                solver.bendModifier = ik.solver.bendModifier;
                solver.bendGoal = ik.solver.bendGoal == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bendGoal.name);
                solver.IKRotationWeight = ik.solver.IKRotationWeight;
                solver.IKRotation = ik.solver.IKRotation;
                solver.bendNormal = ik.solver.bendNormal;
                solver.bone1 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone1.transform == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bone1.transform.name), weight = ik.solver.bone1.weight };
                solver.bone2 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone2.transform == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bone2.transform.name), weight = ik.solver.bone2.weight };
                solver.bone3 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone3.transform == null ? null : rootTarget.transform.FindDeepChildLiberal(ik.solver.bone3.transform.name), weight = ik.solver.bone3.weight };

                targetIK.solver = solver;
            }
        }
#endif
    }

}

#endif