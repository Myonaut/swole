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

        public GameObject rootToCopy;
        public GameObject rootTarget;

        public void Update()
        {
            if (apply)
            {
                apply = false;

                Transfer(rootToCopy, rootTarget); 
            }
        }

        public virtual void Transfer(GameObject rootToCopy, GameObject rootTarget)
        {
            if (rootTarget == null) rootTarget = gameObject;
            TransferComponents(rootToCopy, rootTarget);
        }

        public static void TransferComponents(GameObject rootToCopy, GameObject rootTarget)
        {

            //Transform rootToCopyTransform = rootToCopy.transform;
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
                 
                solver.target = ik.solver.target == null ? null : FindTransform(ik.solver.target.gameObject);
                solver.IKRotationWeight = ik.solver.IKRotationWeight;
                solver.IKRotation = ik.solver.IKRotation;
                solver.bendNormal = ik.solver.bendNormal;
                solver.bone1 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone1.transform == null ? null : FindTransform(ik.solver.bone1.transform.gameObject), weight = ik.solver.bone1.weight };
                solver.bone2 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone2.transform == null ? null : FindTransform(ik.solver.bone2.transform.gameObject), weight = ik.solver.bone2.weight };
                solver.bone3 = new IKSolverTrigonometric.TrigonometricBone() { transform = ik.solver.bone3.transform == null ? null : FindTransform(ik.solver.bone3.transform.gameObject), weight = ik.solver.bone3.weight };

                targetIK.solver = solver;
            }
#endif
        }

    }
}

#endif