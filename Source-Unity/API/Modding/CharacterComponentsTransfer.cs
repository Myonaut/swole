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

                TransferComponents(rootToCopy, rootTarget);
            }
        }

        public static void TransferComponents(GameObject rootToCopy, GameObject rootTarget)
        {

            ProxyBone[] proxyBones = rootToCopy.GetComponentsInChildren<ProxyBone>(true);
            foreach (var pb in proxyBones)
            {
                var target = rootTarget.transform.FindDeepChildLiberal(pb.name);
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

                        targetBinding.bone = binding.bone == null ? null : rootTarget.transform.FindDeepChildLiberal(binding.bone.name);
                        targetBinding.binding = binding.binding;

                        targetPB.bindings[a] = targetBinding;
                    }
                }
            }

            ProxyTransform[] proxyTransforms = rootToCopy.GetComponentsInChildren<ProxyTransform>(true);
            foreach (var pt in proxyTransforms)
            {
                var target = rootTarget.transform.FindDeepChildLiberal(pt.name);
                if (target == null) continue;

                var targetPT = target.gameObject.GetComponent<ProxyTransform>();
                if (targetPT != null) continue;

                targetPT = target.gameObject.AddComponent<ProxyTransform>();

                targetPT.priority = pt.priority;
                targetPT.transformToCopy = pt.transformToCopy == null ? null : rootTarget.transform.FindDeepChildLiberal(pt.transformToCopy.name);
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
                var target = rootTarget.transform.FindDeepChildLiberal(ik.name);
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
#endif
        }

    }
}

#endif