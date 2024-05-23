#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Jobs;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{
    public class ProxyBoneJobs : SingletonBehaviour<ProxyBoneJobs>, IDisposable
    {

        public static int ExecutionPriority => CustomAnimatorUpdater.ExecutionPriority + 1; // Update after animators
        public override int Priority => ExecutionPriority;

        public void Dispose()
        {

            if (boneBindings.IsCreated) boneBindings.Dispose();
            boneBindings = default;

            if (transforms.isCreated) transforms.Dispose();
            transforms = default;

            if (proxyBindingHeaders.IsCreated) proxyBindingHeaders.Dispose();
            proxyBindingHeaders = default;

            if (proxyTransforms.isCreated) proxyTransforms.Dispose();
            proxyTransforms = default;

            if (proxyParentWorldTransforms.IsCreated) proxyParentWorldTransforms.Dispose();
            proxyParentWorldTransforms = default;

        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            Dispose();

            PreUpdate?.RemoveAllListeners();
            PostUpdate?.RemoveAllListeners();

            PreLateUpdate?.RemoveAllListeners();
            PostLateUpdate?.RemoveAllListeners();

        }

        [Serializable]
        public enum BindingOrder
        {

            XYZ, ZXY, YZX, XZY, YXZ, ZYX

        }

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct Binding
        {

            public bool relative;

            [NonSerialized]
            public bool firstRun;

            public bool applyFullRotationThenRevert;

            public bool applyInWorldSpace;

            [NonSerialized]
            public int proxyParentIndex;

            public float baseWeight;

            public BindingOrder rotationBindingOrder;

            public float3 rotationWeights;

            public float3 positionWeights;

            public float3 startPosition;
            public float3 startProxyPosition;
            [NonSerialized]
            public float3 currentProxyPosition;
            [NonSerialized]
            public float3 positionOffset;

            public quaternion startRotation;
            public quaternion startProxyRotation;
            [NonSerialized]
            public quaternion currentProxyRotation;
            [NonSerialized]
            public quaternion rotationOffset;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProxyParentWorldTransform
        {

            public float3 currentProxyParentWorldPosition;

            public quaternion currentProxyParentWorldRotation;

        }

        protected Dictionary<Transform, int> boundTransforms;
        protected NativeList<Binding> boneBindings;
        protected NativeList<ProxyParentWorldTransform> proxyParentWorldTransforms;
        protected TransformAccessArray transforms;
        protected NativeList<int3> proxyBindingHeaders;
        protected TransformAccessArray proxyTransforms;

        protected int IndexOf(ProxyBone proxy)
        {

            if (proxyTransforms.isCreated)
            {

                Transform transform = proxy.transform;

                for (int a = 0; a < proxyTransforms.length; a++) if (proxyTransforms[a] == transform) return a;

            }

            return -1;

        }

        protected int IndexOf(Transform proxyTransform)
        {

            if (proxyTransforms.isCreated)
            {

                for (int a = 0; a < proxyTransforms.length; a++) if (proxyTransforms[a] == proxyTransform) return a;

            }

            return -1;

        }

        protected int AddOrGetProxyIndex(Transform proxyTransform, int3 header)
        {

            int proxyIndex = IndexOf(proxyTransform);

            if (proxyIndex < 0)
            {

                if (!proxyTransforms.isCreated) proxyTransforms = new TransformAccessArray(1);
                proxyIndex = proxyTransforms.length;
                proxyTransforms.Add(proxyTransform);
                proxyBindingHeaders.Add(header);

            }

            return proxyIndex;

        }
        protected void RemoveProxy(int proxyIndex, bool onlyBindings = false, int newCount = 0)
        {
            if (proxyIndex < 0 || !proxyBindingHeaders.IsCreated || proxyIndex >= proxyBindingHeaders.Length) return;

            lastJobHandle.Complete();

            var proxyHeader = proxyBindingHeaders[proxyIndex];
            int startIndex = proxyHeader.y;
            int count = proxyHeader.x;
            int worldTransformIndex = proxyHeader.z;

            boneBindings.RemoveRange(startIndex, count);
            tempTransforms.Clear();
            for (int a = 0; a < transforms.length; a++) tempTransforms.Add(transforms[a]);
            tempTransforms.RemoveRange(startIndex, count);
            transforms.SetTransforms(tempTransforms.ToArray());

            if (!onlyBindings)
            {
                for (int a = 0; a < boneBindings.Length; a++) // Check if proxy is referenced as a parent for a different proxy
                {
                    var binding = boneBindings[a];

                    if (binding.proxyParentIndex != proxyIndex) continue; // Different index or is in range of the proxy being removed

                    onlyBindings = true; // It is referenced, so don't delete it entirely
#if UNITY_EDITOR
                    swole.Log($"[{nameof(ProxyBoneJobs)}] Preserved proxy transform {proxyIndex}");
#endif
                    break;
                }

                if (!onlyBindings)
                {
                    int swapIndex = proxyBindingHeaders.Length - 1;
                    proxyBindingHeaders.RemoveAtSwapBack(proxyIndex);
                    proxyTransforms.RemoveAtSwapBack(proxyIndex);
                    if (worldTransformIndex >= 0) proxyParentWorldTransforms.RemoveAt(worldTransformIndex);  

                    for (int a = 0; a < boneBindings.Length; a++) // Update parent indices in bindings
                    {
                        var binding = boneBindings[a];

                        if (binding.proxyParentIndex != swapIndex) continue;
                        binding.proxyParentIndex = proxyIndex;
                        boneBindings[a] = binding;
                    }
                }
            }
            else
            {
                proxyBindingHeaders[proxyIndex] = new int3(startIndex, newCount, worldTransformIndex);
            }

            for (int a = 0; a < proxyBindingHeaders.Length; a++) // Update indices in headers
            {
                var header = proxyBindingHeaders[a];

                if (header.y > startIndex) header.y = (header.y - count) + newCount;
                if (!onlyBindings && header.z >= worldTransformIndex && worldTransformIndex >= 0) header.z = header.z - 1;    

                proxyBindingHeaders[a] = header;
            }
        }

        private static readonly List<Transform> tempTransforms = new List<Transform>();
        private static readonly List<Binding> tempBindings = new List<Binding>();
        public static int Register(ProxyBone proxy)
        {
            if (proxy == null || proxy.bindings == null) return -1;

            var instance = Instance;
            if (instance == null) return -1; 

            instance.lastJobHandle.Complete();

            if (!instance.boneBindings.IsCreated) instance.boneBindings = new NativeList<Binding>(proxy.bindings.Length, Allocator.Persistent);
            if (!instance.proxyParentWorldTransforms.IsCreated) instance.proxyParentWorldTransforms = new NativeList<ProxyParentWorldTransform>(0, Allocator.Persistent);
            if (!instance.transforms.isCreated) instance.transforms = new TransformAccessArray(proxy.bindings.Length);
            if (!instance.proxyBindingHeaders.IsCreated) instance.proxyBindingHeaders = new NativeList<int3>(1, Allocator.Persistent);

            var proxyTransform = proxy.transform;
            int proxyIndex = instance.IndexOf(proxyTransform);
            int3 header;
            bool isNew = true;
            if (proxyIndex >= 0) 
            { 
                isNew=false;
                instance.RemoveProxy(proxyIndex, true, proxy.bindings.Length);
                header = instance.proxyBindingHeaders[proxyIndex];
            } 
            else
            {
                proxyIndex = instance.AddOrGetProxyIndex(proxyTransform, -1);
                header = instance.proxyBindingHeaders[proxyIndex];
                header.x = proxy.bindings.Length; // binding count
                header.y = instance.boneBindings.Length; // binding index start
                // .z is used to indicate that the transform is a parent used in world space calculations
            }

            instance.proxyBindingHeaders[proxyIndex] = header;

            if (!isNew)
            {
                tempTransforms.Clear();
                for (int a = 0; a < instance.transforms.length; a++) tempTransforms.Add(instance.transforms[a]);
                tempBindings.Clear();
                for (int a = 0; a < instance.boneBindings.Length; a++) tempBindings.Add(instance.boneBindings[a]);
            }

            for (int a = 0; a < proxy.bindings.Length; a++)
            {

                var binding = proxy.bindings[a];

                var bindingJobData = binding.binding;

                if (!proxy.hasStartingPose)
                {

                    bindingJobData.startPosition = binding.bone.localPosition;
                    bindingJobData.startRotation = binding.bone.localRotation;
                    bindingJobData.startProxyPosition = proxyTransform.localPosition;
                    bindingJobData.startProxyRotation = proxyTransform.localRotation;

                }

                if (bindingJobData.applyInWorldSpace)
                {

                    Transform proxyParentTransform = proxyTransform.parent;
                    if (proxyParentTransform != null)
                    {
                        int proxyParentIndex = instance.AddOrGetProxyIndex(proxyParentTransform, -1);
                        var proxyParentHeader = instance.proxyBindingHeaders[proxyParentIndex];
                        proxyParentHeader.z = instance.proxyParentWorldTransforms.Length;
                        instance.proxyBindingHeaders[proxyParentIndex] = proxyParentHeader;
                        instance.proxyParentWorldTransforms.Add(new ProxyParentWorldTransform() { currentProxyParentWorldPosition = proxyParentTransform.position, currentProxyParentWorldRotation = proxyParentTransform.rotation });

                        bindingJobData.proxyParentIndex = proxyParentIndex; // parent's PROXY index, i.e the index of its header
                    }
                    else
                    {
                        bindingJobData.applyInWorldSpace = false;
                    }

                }

                bindingJobData.firstRun = true;

                binding.binding = bindingJobData;

                proxy.bindings[a] = binding;

                if (isNew)
                {
                    instance.boneBindings.Add(binding.binding);
                    instance.transforms.Add(binding.bone); 
                } 
                else
                {
                    int index = header.y + a;
                    tempBindings.Insert(index, binding.binding);
                    tempTransforms.Insert(index, binding.bone);
                }

            }
            if (!isNew)
            {
                instance.transforms.SetTransforms(tempTransforms.ToArray());
                instance.boneBindings.Clear();
                instance.boneBindings.SetCapacity(tempBindings.Count);
                for (int a = 0; a < tempBindings.Count; a++) instance.boneBindings.Add(tempBindings[a]);
            }

            return proxyIndex;
        }

        public static void Unregister(ProxyBone proxy)
        {
            if (proxy == null) return;

            var instance = Instance;
            if (instance == null) return;

            Unregister(instance.IndexOf(proxy));
        }
        public static void Unregister(int proxyIndex)
        {
            var instance = Instance;
            if (instance == null) return;

            if (proxyIndex < 0 || !instance.boneBindings.IsCreated || proxyIndex >= instance.proxyBindingHeaders.Length) return;
             
            instance.RemoveProxy(proxyIndex, false);
        }

        protected JobHandle lastJobHandle;
        public static JobHandle OutputDependency
        {
            get
            {
                var instance = Instance;
                if (instance == null) return default;

                return instance.lastJobHandle;
            }
        }

        protected JobHandle RunJobs(JobHandle deps = default)
        {

            lastJobHandle.Complete();

            deps = JobHandle.CombineDependencies(deps, Rigs.OutputDependency);

            if (boneBindings.IsCreated)
            {

                if (proxyTransforms.isCreated) deps = new UpdateBoneProxiesJob()
                {

                    boneBindings = boneBindings,
                    proxyParentWorldTransforms = proxyParentWorldTransforms,
                    proxyBindingHeaders = proxyBindingHeaders

                }.Schedule(proxyTransforms, deps);

                if (transforms.isCreated) deps = new ApplyBoneProxiesJob()
                {

                    boneBindings = boneBindings,
                    proxyParentWorldTransforms = proxyParentWorldTransforms,
                    proxyBindingHeaders = proxyBindingHeaders

                }.Schedule(transforms, deps);

            }
             
            lastJobHandle = deps;

            //Rigs.AddInputDependency(lastJobHandle);

            return deps;

        }

        public static void ExecuteBeforeUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance.PreUpdate == null) instance.PreUpdate = new UnityEvent();
            instance.PreUpdate.AddListener(action);
        }
        public static void ExecuteAfterUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance.PostUpdate == null) instance.PostUpdate = new UnityEvent();
            instance.PostUpdate.AddListener(action);
        }

        public UnityEvent PreUpdate;
        public UnityEvent PostUpdate;

        public override void OnUpdate()
        {
            PreUpdate?.Invoke();
            JobHandle inputDeps = Rigs.InputDependency;
            RunJobs(inputDeps);
            PostUpdate?.Invoke();
        }

        public static void ExecuteBeforeLateUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance.PreLateUpdate == null) instance.PreLateUpdate = new UnityEvent();
            instance.PreLateUpdate.AddListener(action);
        }
        public static void ExecuteAfterLateUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance.PostLateUpdate == null) instance.PostLateUpdate = new UnityEvent();
            instance.PostLateUpdate.AddListener(action);
        }

        public UnityEvent PreLateUpdate;
        public UnityEvent PostLateUpdate;

        public override void OnLateUpdate()
        {
            PreLateUpdate?.Invoke();
            lastJobHandle.Complete();
            PostLateUpdate?.Invoke();
        }

        public override void OnFixedUpdate() { }

        [BurstCompile]
        public struct UpdateBoneProxiesJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<Binding> boneBindings;
            [NativeDisableParallelForRestriction]
            public NativeList<ProxyParentWorldTransform> proxyParentWorldTransforms;

            [ReadOnly]
            public NativeList<int3> proxyBindingHeaders;

            public void Execute(int proxyIndex, TransformAccess transform)
            {

                int3 bindingHeader = proxyBindingHeaders[proxyIndex];
                int bindingCount = bindingHeader.x;
                int bindingIndexStart = bindingHeader.y;

                float3 proxyLocalPosition = transform.localPosition;
                quaternion proxyLocalRotation = transform.localRotation;

                for (int a = 0; a < bindingCount; a++)
                {

                    int bindingIndex = bindingIndexStart + a;

                    Binding binding = boneBindings[bindingIndex];

                    binding.currentProxyPosition = proxyLocalPosition;
                    binding.currentProxyRotation = proxyLocalRotation;


                    boneBindings[bindingIndex] = binding;

                }

                if (bindingHeader.z >= 0) // Is a parent used in world space calculations
                {

                    proxyParentWorldTransforms[bindingHeader.z] = new ProxyParentWorldTransform()
                    {

                        currentProxyParentWorldPosition = transform.position,
                        currentProxyParentWorldRotation = transform.rotation

                    };

                }

            }

        }

        protected static readonly float3 axisRight = new float3(1, 0, 0);
        protected static readonly float3 axisUp = new float3(0, 1, 0);
        protected static readonly float3 axisForward = new float3(0, 0, 1);

        [BurstCompile]
        public struct ApplyBoneProxiesJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<Binding> boneBindings;

            [ReadOnly]
            public NativeList<ProxyParentWorldTransform> proxyParentWorldTransforms;
            [ReadOnly]
            public NativeList<int3> proxyBindingHeaders;

            public void Execute(int index, TransformAccess transform)
            {

                Binding binding = boneBindings[index];

                float3 posWeights = binding.positionWeights * binding.baseWeight;
                float3 rotWeights = binding.rotationWeights * binding.baseWeight;

                float3 localPosition = transform.localPosition;
                quaternion localRotation = transform.localRotation;

                quaternion currentRotation = math.mul(math.inverse(binding.startProxyRotation), binding.currentProxyRotation);

                if (math.any(rotWeights != 1))
                {

                    float3 currentProxyRotationEuler = currentRotation.ToEuler();

                    if (binding.applyFullRotationThenRevert)
                    {

                        switch (binding.rotationBindingOrder)
                        {

                            case BindingOrder.XYZ:
                                currentRotation = math.mul(currentRotation, quaternion.EulerXYZ(-currentProxyRotationEuler.x * (1 - rotWeights.x), -currentProxyRotationEuler.y * (1 - rotWeights.y), -currentProxyRotationEuler.z * (1 - rotWeights.z)));
                                break;

                            case BindingOrder.XZY:
                                currentRotation = math.mul(currentRotation, quaternion.EulerXZY(-currentProxyRotationEuler.x * (1 - rotWeights.x), -currentProxyRotationEuler.y * (1 - rotWeights.y), -currentProxyRotationEuler.z * (1 - rotWeights.z)));
                                break;

                            case BindingOrder.YXZ:
                                currentRotation = math.mul(currentRotation, quaternion.EulerYXZ(-currentProxyRotationEuler.x * (1 - rotWeights.x), -currentProxyRotationEuler.y * (1 - rotWeights.y), -currentProxyRotationEuler.z * (1 - rotWeights.z)));
                                break;

                            case BindingOrder.YZX:
                                currentRotation = math.mul(currentRotation, quaternion.EulerYZX(-currentProxyRotationEuler.x * (1 - rotWeights.x), -currentProxyRotationEuler.y * (1 - rotWeights.y), -currentProxyRotationEuler.z * (1 - rotWeights.z)));
                                break;

                            case BindingOrder.ZXY:
                                currentRotation = math.mul(currentRotation, quaternion.EulerZXY(-currentProxyRotationEuler.x * (1 - rotWeights.x), -currentProxyRotationEuler.y * (1 - rotWeights.y), -currentProxyRotationEuler.z * (1 - rotWeights.z)));
                                break;

                            case BindingOrder.ZYX:
                                currentRotation = math.mul(currentRotation, quaternion.EulerZYX(-currentProxyRotationEuler.x * (1 - rotWeights.x), -currentProxyRotationEuler.y * (1 - rotWeights.y), -currentProxyRotationEuler.z * (1 - rotWeights.z)));
                                break;

                        }

                    }
                    else
                    {

                        switch (binding.rotationBindingOrder)
                        {

                            case BindingOrder.XYZ:
                                currentRotation = quaternion.EulerXYZ(currentProxyRotationEuler.x * rotWeights.x, currentProxyRotationEuler.y * rotWeights.y, currentProxyRotationEuler.z * rotWeights.z);
                                break;

                            case BindingOrder.XZY:
                                currentRotation = quaternion.EulerXZY(currentProxyRotationEuler.x * rotWeights.x, currentProxyRotationEuler.y * rotWeights.y, currentProxyRotationEuler.z * rotWeights.z);
                                break;

                            case BindingOrder.YXZ:
                                currentRotation = quaternion.EulerYXZ(currentProxyRotationEuler.x * rotWeights.x, currentProxyRotationEuler.y * rotWeights.y, currentProxyRotationEuler.z * rotWeights.z);
                                break;

                            case BindingOrder.YZX:
                                currentRotation = quaternion.EulerYZX(currentProxyRotationEuler.x * rotWeights.x, currentProxyRotationEuler.y * rotWeights.y, currentProxyRotationEuler.z * rotWeights.z);
                                break;

                            case BindingOrder.ZXY:
                                currentRotation = quaternion.EulerZXY(currentProxyRotationEuler.x * rotWeights.x, currentProxyRotationEuler.y * rotWeights.y, currentProxyRotationEuler.z * rotWeights.z);
                                break;

                            case BindingOrder.ZYX:
                                currentRotation = quaternion.EulerZYX(currentProxyRotationEuler.x * rotWeights.x, currentProxyRotationEuler.y * rotWeights.y, currentProxyRotationEuler.z * rotWeights.z);
                                break;

                        }

                    }

                }

                if (binding.relative && !binding.firstRun)
                {

                    localPosition = localPosition - binding.positionOffset;
                    localRotation = math.mul(localRotation, math.inverse(binding.rotationOffset));

                }
                else
                {

                    binding.firstRun = false;

                    localPosition = binding.startPosition;
                    localRotation = binding.startRotation;

                }

                binding.positionOffset = (binding.currentProxyPosition - binding.startProxyPosition) * posWeights;
                binding.rotationOffset = currentRotation;

                localPosition = localPosition + binding.positionOffset;
                localRotation = math.mul(localRotation, binding.rotationOffset);

                if (binding.applyInWorldSpace)
                {

                    ProxyParentWorldTransform parentTransform = proxyParentWorldTransforms[proxyBindingHeaders[binding.proxyParentIndex].z];

                    transform.SetPositionAndRotation(parentTransform.currentProxyParentWorldPosition + math.mul(parentTransform.currentProxyParentWorldRotation, localPosition + (binding.startProxyPosition - binding.startPosition)), math.mul(parentTransform.currentProxyParentWorldRotation, localRotation));

                }
                else
                {

                    transform.SetLocalPositionAndRotation(localPosition, localRotation);

                }

                boneBindings[index] = binding;

            }

        }

    }
}

#endif