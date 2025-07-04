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
using UnityEngine.SceneManagement;

namespace Swole.API.Unity
{
    public class ProxyBoneJobs : SingletonBehaviour<ProxyBoneJobs>, IDisposable
    {

        public static int ExecutionPriority => SwolePuppetMasterUpdater.LateExecutionPriority + 5; // Update after animators, ik, and puppets
        public override int Priority => ExecutionPriority;
        //public override bool DestroyOnLoad => false; 

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

        /*protected override void OnAwake()
        {
            base.OnAwake(); 
            SceneManager.activeSceneChanged += OnSceneChange; 
        }*/
        public void OnSceneChange(Scene sceneFrom, Scene sceneTo)
        {
            Dispose();

            PreUpdate?.RemoveAllListeners();
            PostUpdate?.RemoveAllListeners(); 

            PreLateUpdate?.RemoveAllListeners();
            PostLateUpdate?.RemoveAllListeners();
        } 
        public override void OnDestroyed()
        {

            base.OnDestroyed();

            OnSceneChange(default, default);

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
            public bool applyScale;

            [NonSerialized]
            public int proxyParentIndex;

            public float baseWeight;

            public BindingOrder rotationBindingOrder;

            public float3 rotationWeights;

            public float3 positionWeights;

            public float3 scaleWeights;

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

            public float3 startScale;
            public float3 startProxyScale;
            [NonSerialized]
            public float3 currentProxyScale;
            [NonSerialized]
            public float3 scaleOffset;

        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProxyParentWorldTransform
        {

            public float3 currentProxyParentWorldPosition;

            public quaternion currentProxyParentWorldRotation;

            public float3 currentProxyParentLocalScale;

        }

        public class ProxyBoneIndex
        {
            public ProxyBone owner;
            protected int proxyIndex;
            public int ProxyIndex => proxyIndex;

            public ProxyBoneIndex(ProxyBone owner, int proxyIndex)
            {
                this.owner = owner;
                this.proxyIndex = proxyIndex;
            }

            public void ChangeIndex(int newIndex)
            {
                if (!IsValid) return;
                proxyIndex = newIndex;
            }
            public void Invalidate() => proxyIndex = -1;

            public bool IsValid => proxyIndex >= 0;

            private static readonly ProxyBoneIndex _invalid = new ProxyBoneIndex(null, -1);
            public static ProxyBoneIndex Invalid => _invalid;
        }

        protected readonly Dictionary<int, ProxyBoneIndex> proxyBoneIndices = new Dictionary<int, ProxyBoneIndex>();
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
                if (proxyTransform == null) return -1;

                if (!proxyTransforms.isCreated) proxyTransforms = new TransformAccessArray(1);
                proxyIndex = proxyTransforms.length;
                proxyTransforms.Add(proxyTransform);
                proxyBindingHeaders.Add(header);
#if UNITY_EDITOR
                //Debug.Log($"Adding new proxy bone header for {proxyTransform.name}"); 
#endif
            }

            return proxyIndex;

        }

        private static readonly List<int> parentsToRemove = new List<int>();
        private bool RemoveNextParentIn(IList<int> parentsToRemove)
        {
            if (parentsToRemove.Count <= 0) return false;

            int nextIndex = parentsToRemove[0];
            parentsToRemove.RemoveAt(0);
#if UNITY_EDITOR
            //Debug.Log("Removing EMPTY PARENT " + nextIndex);
#endif
            if (nextIndex < 0) return true;

            var proxyHeader = proxyBindingHeaders[nextIndex];
            int startIndex = proxyHeader.y;
            int count = Mathf.Max(0, proxyHeader.x);
            int worldTransformIndex = proxyHeader.z;

            if (proxyBoneIndices.TryGetValue(nextIndex, out var indexReference) && indexReference != null)
            {
                indexReference.ChangeIndex(-1);
                proxyBoneIndices.Remove(nextIndex);
            }

            int swapIndex = proxyBindingHeaders.Length - 1; // Only the last element is moved. All other elements remain at the same index (Swapback)
            proxyBindingHeaders.RemoveAtSwapBack(nextIndex);
            proxyTransforms.RemoveAtSwapBack(nextIndex);
            if (worldTransformIndex >= 0) proxyParentWorldTransforms.RemoveAt(worldTransformIndex);

            if (proxyBoneIndices.TryGetValue(swapIndex, out indexReference) && indexReference != null) 
            {
                indexReference.ChangeIndex(nextIndex);
                proxyBoneIndices[nextIndex] = indexReference;
                proxyBoneIndices.Remove(swapIndex);
            }        

            for (int a = 0; a < boneBindings.Length; a++) // Update parent indices in bindings that reference swap back index
            {
                var binding = boneBindings[a];
                if (binding.proxyParentIndex != swapIndex) continue; // If they don't reference the last element, then no change is needed

                binding.proxyParentIndex = nextIndex;
                boneBindings[a] = binding;
            }

            if (worldTransformIndex >= 0)
            {
                for (int a = 0; a < proxyBindingHeaders.Length; a++) // Update world transform indices in headers
                {
                    var header = proxyBindingHeaders[a];
                    if (header.y >= startIndex && startIndex >= 0) header.y = (header.y - count);
                    if (header.z >= worldTransformIndex && worldTransformIndex >= 0) header.z = header.z - 1;
                    proxyBindingHeaders[a] = header;
                }
            } 

            for(int a = 0; a < parentsToRemove.Count; a++)
            {
                int ind = parentsToRemove[a];
                if (ind == swapIndex)
                {
                    ind = nextIndex;
                }
                else if (ind == nextIndex)
                {
                    ind = -1;
                }

                parentsToRemove[a] = ind;
            }

            return true;
        }
        protected void RemoveProxy(int proxyIndex, bool onlyBindings = false, int newCount = 0)
        {
            if (proxyIndex < 0 || !proxyBindingHeaders.IsCreated || proxyIndex >= proxyBindingHeaders.Length) return;

            lastJobHandle.Complete();

            parentsToRemove.Clear();

            var proxyHeader = proxyBindingHeaders[proxyIndex];
            int startIndex = proxyHeader.y;
            int count = Mathf.Max(0, proxyHeader.x);
            int worldTransformIndex = proxyHeader.z;
            newCount = Mathf.Max(0, newCount); 

            if (proxyBoneIndices.TryGetValue(proxyIndex, out var indexReference) && indexReference != null) 
            {
                indexReference.ChangeIndex(-1);
                proxyBoneIndices.Remove(proxyIndex); 
            }

            if (startIndex >= 0 && count > 0) 
            {
#if UNITY_EDITOR
                //Debug.Log("Removing proxyIndex " + proxyIndex + " : " + proxyTransforms[proxyIndex].name + " (bindingIndices:" + startIndex + "-" + (startIndex + (count - 1)) + ") (bindingCount:" + count + ")");
                //Debug.Log("Prev size (Headers: " + proxyBindingHeaders.Length + ") (Bindings: " + boneBindings.Length + ")"); 
#endif

                for(int a = startIndex; a < startIndex + count; a++) // prepare to remove any parent proxies that have no bindings
                {
                    var binding = boneBindings[a];
                    if (binding.proxyParentIndex >= 0)
                    {
                        var parentProxy = proxyBindingHeaders[binding.proxyParentIndex];
                        if (parentProxy.x <= 0) // binding count
                        {
                            bool flag = true; // make sure no other bindings reference this parent before flagging it to be removed
                            for(int b = 0; b < startIndex; b++)
                            {
                                var binding_ = boneBindings[b];
                                if (binding_.proxyParentIndex == binding.proxyParentIndex)
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag)
                            {
                                for (int b = startIndex + count; b < proxyBindingHeaders.Length; b++)
                                {
                                    var binding_ = boneBindings[b];
                                    if (binding_.proxyParentIndex == binding.proxyParentIndex)
                                    {
                                        flag = false;
                                        break;
                                    }
                                }
                            }

                            if (flag) parentsToRemove.Add(binding.proxyParentIndex);  
                        }
                    }
                }
                
                boneBindings.RemoveRange(startIndex, count); 
                tempTransforms.Clear();
                for (int a = 0; a < transforms.length; a++) tempTransforms.Add(transforms[a]);
                tempTransforms.RemoveRange(startIndex, count);
                transforms.SetTransforms(tempTransforms.ToArray());
                //Debug.Log("New size " + boneBindings.Length); 
            }

            if (!onlyBindings)
            {
                for (int a = 0; a < boneBindings.Length; a++) // Check if proxy is referenced as a parent for a different proxy
                {
                    var binding = boneBindings[a];

                    if (binding.proxyParentIndex != proxyIndex) continue; // Different index or is in range of the proxy being removed

                    onlyBindings = true; // It is referenced, so don't delete it entirely
#if UNITY_EDITOR
                    Debug.Log($"[{nameof(ProxyBoneJobs)}] Preserved proxy transform {proxyIndex}. Saved by binding at {((a >= startIndex) ? a + count : a)}");
#endif
                    break;
                }

                if (!onlyBindings)
                {
                    int swapIndex = proxyBindingHeaders.Length - 1; // Only the last element is moved. All other elements remain at the same index (Swapback)
                    proxyBindingHeaders.RemoveAtSwapBack(proxyIndex);
                    proxyTransforms.RemoveAtSwapBack(proxyIndex);
                    if (worldTransformIndex >= 0) proxyParentWorldTransforms.RemoveAt(worldTransformIndex);

                    if (proxyBoneIndices.TryGetValue(swapIndex, out indexReference) && indexReference != null) 
                    {
                        indexReference.ChangeIndex(proxyIndex);
                        proxyBoneIndices[proxyIndex] = indexReference;
                        proxyBoneIndices.Remove(swapIndex); 
                    }

                    for (int a = 0; a < boneBindings.Length; a++) // Update parent indices in bindings that reference swap back index
                    {
                        var binding = boneBindings[a];
                        if (binding.proxyParentIndex != swapIndex) continue; // If they don't reference the last element, then no change is needed

                        binding.proxyParentIndex = proxyIndex;
                        boneBindings[a] = binding;
                    }
                     
                    for (int a = 0; a < parentsToRemove.Count; a++)
                    {
                        if (parentsToRemove[a] == swapIndex) parentsToRemove[a] = proxyIndex;
                    }
                }
            }
             
            if (onlyBindings)
            {
                //Debug.Log(proxyTransforms[proxyIndex].name + " " + newCount); 
                proxyBindingHeaders[proxyIndex] = new int3(newCount, newCount <= 0 ? -1 : startIndex, worldTransformIndex);   
            }

            for (int a = 0; a < proxyBindingHeaders.Length; a++) // Update indices in headers
            {
                if (onlyBindings && a == proxyIndex) continue;  

                var header = proxyBindingHeaders[a];

                if (header.y >= startIndex && startIndex >= 0) header.y = (header.y - count) + newCount;
                if (!onlyBindings && header.z >= worldTransformIndex && worldTransformIndex >= 0) header.z = header.z - 1;     

                proxyBindingHeaders[a] = header;
            }

            while(RemoveNextParentIn(parentsToRemove));

#if UNITY_EDITOR
            //Debug.Log("New size (Headers: " + proxyBindingHeaders.Length + ") (Bindings: " + boneBindings.Length + ")");
#endif
        }

        private static readonly List<Transform> tempTransforms = new List<Transform>();
        private static readonly List<Binding> tempBindings = new List<Binding>();
        public static ProxyBoneIndex Register(ProxyBone proxy)
        {
            if (proxy == null || proxy.bindings == null) return ProxyBoneIndex.Invalid;

            var instance = Instance;
            if (instance == null) return ProxyBoneIndex.Invalid;

            tempTransforms.Clear();
            tempBindings.Clear();

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

#if UNITY_EDITOR
                //Debug.Log($"Found existing proxy for {proxyTransform.name} : {proxyIndex} :: header: {header.x} {header.y} {header.z}");
#endif
            } 
            else
            {
                proxyIndex = instance.AddOrGetProxyIndex(proxyTransform, -1);
                header = instance.proxyBindingHeaders[proxyIndex];
            }

            var newIndRef = new ProxyBoneIndex(proxy, proxyIndex);

            header.x = proxy.bindings.Length; // binding count
            if (header.y < 0) // if it's negative then give it a new startIndex in the boneBindings array
            {
                header.y = instance.boneBindings.Length; // binding index start
                isNew = true;

#if UNITY_EDITOR
                //Debug.Log($"Adding new proxy for {proxyTransform.name} : {proxyIndex} :: header: {header.x} {header.y} {header.z}");
#endif
            }

            instance.proxyBindingHeaders[proxyIndex] = header; 

            if (!isNew)
            {
                for (int a = 0; a < instance.transforms.length; a++) tempTransforms.Add(instance.transforms[a]);
                for (int a = 0; a < instance.boneBindings.Length; a++) tempBindings.Add(instance.boneBindings[a]);
            }

            Binding InitBinding(Binding binding)
            {
                binding.proxyParentIndex = -1;
                if (binding.applyInWorldSpace)
                {

                    Transform proxyParentTransform = proxyTransform.parent;
                    if (proxyParentTransform != null)
                    {
                        int proxyParentIndex = instance.AddOrGetProxyIndex(proxyParentTransform, -1);
                        var proxyParentHeader = instance.proxyBindingHeaders[proxyParentIndex];
                        proxyParentHeader.z = instance.proxyParentWorldTransforms.Length;
                        instance.proxyBindingHeaders[proxyParentIndex] = proxyParentHeader;
                        instance.proxyParentWorldTransforms.Add(new ProxyParentWorldTransform() { currentProxyParentWorldPosition = proxyParentTransform.position, currentProxyParentWorldRotation = proxyParentTransform.rotation });

                        binding.proxyParentIndex = proxyParentIndex; // the header index of this proxy's parent
#if UNITY_EDITOR
                        //Debug.Log($"Proxy {proxy.name}, binding {a} has parent index {proxyParentIndex}");
#endif
                    }
                    else
                    {
                        binding.applyInWorldSpace = false;
                    }
                }

                binding.firstRun = true;

                return binding;
            }

            for (int a = 0; a < proxy.bindings.Length; a++)
            {

                var binding = proxy.bindings[a];

                var bindingJobData = binding.binding; 

                if (!proxy.hasStartingPose)
                {

                    /*if (binding.binding.applyInWorldSpace)
                    {
                        bindingJobData.startPosition = proxyTransform.parent == null ? binding.bone.position : proxyTransform.parent.InverseTransformPoint(binding.bone.position);
                        bindingJobData.startRotation = proxyTransform.parent == null ? binding.bone.rotation : (Quaternion.Inverse(proxyTransform.parent.rotation) * binding.bone.rotation);
                    } 
                    else
                    {
                        bindingJobData.startPosition = binding.bone.localPosition;
                        bindingJobData.startRotation = binding.bone.localRotation;
                    }
                    bindingJobData.startScale = binding.bone.localScale;

                    bindingJobData.startProxyPosition = proxyTransform.localPosition;
                    bindingJobData.startProxyRotation = proxyTransform.localRotation;
                    bindingJobData.startProxyScale = proxyTransform.localScale;*/

                    binding.ReinitializeStartPosition(proxyTransform); // ^ replaced comment above
                    bindingJobData = binding.binding; 

                }

                /*bindingJobData.proxyParentIndex = -1; 
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

                        bindingJobData.proxyParentIndex = proxyParentIndex; // the header index of this proxy's parent
#if UNITY_EDITOR
                        //Debug.Log($"Proxy {proxy.name}, binding {a} has parent index {proxyParentIndex}");
#endif
                    }
                    else
                    {
                        bindingJobData.applyInWorldSpace = false;
                    }

                }

                bindingJobData.firstRun = true;*/

                bindingJobData = InitBinding(bindingJobData); // ^ replaced comment above

                binding.binding = bindingJobData;

                proxy.bindings[a] = binding; 

                if (isNew)
                {
                    int index = instance.transforms.length;

                    instance.boneBindings.Add(binding.binding); 
                    instance.transforms.Add(binding.bone);
                    //Debug.Log(proxy.name + " : " + binding.bone.name + " : add " + (instance.boneBindings.Length - 1)); 

                    binding.ClearAllListeners();
                    binding.OnReinitBinding += (Binding newBinding) =>
                    {
                        if (!newIndRef.IsValid) return;

                        instance.lastJobHandle.Complete();

                        newBinding = InitBinding(newBinding);

                        var jobBinding = instance.boneBindings[index];

                        newBinding.currentProxyPosition = jobBinding.currentProxyPosition;
                        newBinding.currentProxyRotation = jobBinding.currentProxyRotation;
                        newBinding.currentProxyScale = jobBinding.currentProxyScale;

                        instance.boneBindings[index] = newBinding;
                    };
                } 
                else
                {
                    int index = header.y + a;
                    //Debug.Log(proxy.name + " : " + binding.bone.name + " : ins " + index);
                    /*if (index >= tempBindings.Count) tempBindings.Add(binding.binding); else */tempBindings.Insert(index, binding.binding);
                    /*if (index >= tempTransforms.Count) tempTransforms.Add(binding.bone); else */tempTransforms.Insert(index, binding.bone);

                    binding.ClearAllListeners();
                    binding.OnReinitBinding += (Binding newBinding) =>
                    {
                        if (!newIndRef.IsValid) return;

                        instance.lastJobHandle.Complete();

                        newBinding = InitBinding(newBinding);

                        var jobBinding = instance.boneBindings[index];

                        newBinding.currentProxyPosition = jobBinding.currentProxyPosition;
                        newBinding.currentProxyRotation = jobBinding.currentProxyRotation; 
                        newBinding.currentProxyScale = jobBinding.currentProxyScale; 

                        instance.boneBindings[index] = newBinding;
                    };
                }

            }
            if (!isNew)
            {
                instance.transforms.SetTransforms(tempTransforms.ToArray());
                instance.boneBindings.Clear();
                instance.boneBindings.SetCapacity(tempBindings.Count);
                for (int a = 0; a < tempBindings.Count; a++) instance.boneBindings.Add(tempBindings[a]); 
            }

#if UNITY_EDITOR
            //Debug.Log($"Current sizes (headers:{instance.proxyBindingHeaders.Length}) (bindings:{instance.boneBindings.Length})");
#endif

            if (instance.proxyBoneIndices.TryGetValue(proxyIndex, out var ind) && ind != null) ind.Invalidate();
            instance.proxyBoneIndices[proxyIndex] = newIndRef;  
            return ind;
        }

        public static void Unregister(ProxyBone proxy)
        {
            if (proxy == null) return;
            
            var instance = InstanceOrNull; 
            if (instance == null) return;

            Unregister(instance.IndexOf(proxy));
        }
        public static void Unregister(ProxyBoneIndex proxy)
        {
            if (proxy == null || !proxy.IsValid) return;

            var instance = InstanceOrNull;
            if (instance == null) return;
             
            Unregister(proxy.ProxyIndex);
        }
        public static void Unregister(int proxyIndex)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            if (proxyIndex < 0 || !instance.boneBindings.IsCreated || proxyIndex >= instance.proxyBindingHeaders.Length) return;
             
            instance.RemoveProxy(proxyIndex, false);
        }

        protected JobHandle lastJobHandle;
        public static JobHandle OutputDependency
        {
            get
            {
                var instance = InstanceOrNull;
                if (instance == null) return default;

                return instance.lastJobHandle;
            }
        }

        protected JobHandle RunJobs(JobHandle deps = default)
        {
            lastJobHandle.Complete();

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

            return deps;
        }

        public static void ExecuteBeforeUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance == null) return;

            if (instance.PreUpdate == null) instance.PreUpdate = new UnityEvent();
            instance.PreUpdate.AddListener(action);
        }
        public static void ExecuteAfterUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance == null) return;

            if (instance.PostUpdate == null) instance.PostUpdate = new UnityEvent();
            instance.PostUpdate.AddListener(action);
        }

        public UnityEvent PreUpdate;
        public UnityEvent PostUpdate;

        public override void OnUpdate()
        {
            PreUpdate?.Invoke();

            //RunJobs(); // Moved to late update so that synchronous IK code happens first. TODO: revisit this if there's ever a jobified IK solution
            // should also remain in late update to wait for animation jobs (which do not force completion until late update)

            PostUpdate?.Invoke();
        }

        public static void ExecuteBeforeLateUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance == null) return;

            if (instance.PreLateUpdate == null) instance.PreLateUpdate = new UnityEvent();
            instance.PreLateUpdate.AddListener(action);
        }
        public static void ExecuteAfterLateUpdate(UnityAction action)
        {
            var instance = Instance;
            if (instance == null) return;

            if (instance.PostLateUpdate == null) instance.PostLateUpdate = new UnityEvent();
            instance.PostLateUpdate.AddListener(action);
        }

        public UnityEvent PreLateUpdate;
        public UnityEvent PostLateUpdate;

        public override void OnLateUpdate()
        {
            PreLateUpdate?.Invoke();

            //EndFrameJobWaiter.WaitFor(RunJobs());
            RunJobs().Complete();

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
                float3 proxyLocalScale = transform.localScale;

                for (int a = 0; a < bindingCount; a++)
                {

                    int bindingIndex = bindingIndexStart + a;

                    Binding binding = boneBindings[bindingIndex];

                    binding.currentProxyPosition = proxyLocalPosition;
                    binding.currentProxyRotation = proxyLocalRotation;
                    binding.currentProxyScale = proxyLocalScale;


                    boneBindings[bindingIndex] = binding;

                }

                if (bindingHeader.z >= 0) // Is a parent used in world space calculations
                {

                    proxyParentWorldTransforms[bindingHeader.z] = new ProxyParentWorldTransform()
                    {

                        currentProxyParentWorldPosition = transform.position,
                        currentProxyParentWorldRotation = transform.rotation,
                        currentProxyParentLocalScale = proxyLocalScale

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
                float3 scaWeights = binding.scaleWeights * binding.baseWeight;

                float3 localPosition = transform.localPosition;
                quaternion localRotation = transform.localRotation;
                float3 localScale = transform.localScale;

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
                    // binding is relative and this isn't the first execution, so undo previous changes before applying new ones
                    localPosition = localPosition - binding.positionOffset;
                    localRotation = math.mul(localRotation, math.inverse(binding.rotationOffset));
                    localScale = localScale - binding.scaleOffset;

                }
                else
                {

                    binding.firstRun = false;

                    localPosition = binding.startPosition;
                    localRotation = binding.startRotation;
                    localScale = binding.startScale;

                }

                binding.positionOffset = (binding.currentProxyPosition - binding.startProxyPosition) * posWeights;
                binding.rotationOffset = currentRotation;
                binding.scaleOffset = (binding.currentProxyScale - binding.startProxyScale) * scaWeights;

                localPosition = localPosition + binding.positionOffset;
                localRotation = math.mul(localRotation, binding.rotationOffset);
                localScale = localScale + binding.scaleOffset;

                if (binding.applyInWorldSpace)
                {

                    ProxyParentWorldTransform parentTransform = proxyParentWorldTransforms[proxyBindingHeaders[binding.proxyParentIndex].z];

                    transform.SetPositionAndRotation(parentTransform.currentProxyParentWorldPosition + math.mul(parentTransform.currentProxyParentWorldRotation, localPosition + (binding.startProxyPosition - binding.startPosition)), math.mul(parentTransform.currentProxyParentWorldRotation, localRotation));

                }
                else
                {

                    transform.SetLocalPositionAndRotation(localPosition, localRotation);

                }
                if (binding.applyScale) transform.localScale = localScale;

                boneBindings[index] = binding;

            }

        }

    }
}

#endif