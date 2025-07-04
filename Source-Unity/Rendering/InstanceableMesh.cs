#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Jobs;
using Swole.DataStructures;
using Swole.API.Unity.Animation;

namespace Swole
{

    public class InstanceableMesh : InstanceableMeshBase
    {

        public const string _instanceIdDefaultPropertyName = "_InstanceID"; 

        [SerializeField]
        protected InstanceableMeshDataBase meshData;
        public override InstanceableMeshDataBase MeshData => meshData;

        public override InstancedMeshGroup MeshGroup => meshData.meshGroups[meshGroupIndex];

    }

    public abstract class InstanceableMeshBase : MonoBehaviour, IDisposable
    {

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            var meshData = MeshData;
            if (meshData == null) return;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(BoundsRootTransform.TransformPoint(meshData.boundsCenter), meshData.boundsExtents);  
        }
#endif

        public virtual void ClearAllListeners()
        {
            OnCreateInstance = null;
            OnCreateInstanceID = null;
        }

        public virtual void Dispose()
        {
            if (instance != null)
            {
                instance.Dispose();
                instance = null;
            }

            if (cullLODs != null)
            {
                cullLODs.Dispose();
                cullLODs = null; 
            }
        }

        [NonSerialized]
        protected bool destroyed;
        public bool IsDestroyed => destroyed;

        private void OnDestroy()
        {
            destroyed = true;

            ClearAllListeners();
            OnDestroyed();
            Dispose();
        }

        protected virtual void OnDestroyed() { }

        public abstract InstanceableMeshDataBase MeshData { get; }
        public abstract InstancedMeshGroup MeshGroup { get; }

        [SerializeField]
        protected int meshGroupIndex;
        public int SetMeshGroupIndex(int meshGroupIndex) => this.meshGroupIndex = meshGroupIndex;
        public int MeshGroupIndex => meshGroupIndex;
        [SerializeField]
        protected int subMeshIndex;
        public int SetSubMeshindex(int subMeshIndex) => this.subMeshIndex = subMeshIndex;
        public int SubMeshIndex => subMeshIndex;

        [NonSerialized]
        protected InstancedMesh instance;
        public InstancedMesh Instance => instance;
        public int InstanceSlot => instance == null ? -1 : instance.Slot;
        public bool HasInstance => instance != null && instance.Slot >= 0;
        public virtual bool IsInitialized => HasInstance;

        protected bool visible;
        public bool Visible
        {
            get => visible;
            set => SetVisible(value);
        }

        protected bool inViewOfCamera;
        public bool InViewOfCamera => inViewOfCamera;

        public bool IsRendering => (instance != null && instance.IsVisible);
        public bool IsRendered => IsRendering && InViewOfCamera;

        public virtual void SetVisible(bool visible)
        {
            this.visible = visible;
            if (visibleOnEnable && !visible) visibleOnEnable = false; 

            if (instance == null) return;

            if (visible && InViewOfCamera && enabled)
            {
                instance.StartRendering();
            }
            else
            {
                instance.StopRendering();
            }
        }

        public void StartRenderingInstance()
        {
            if (instance != null) instance.StartRendering();
        }
        public void StopRenderingInstance()
        {
            if (instance != null) instance.StopRendering();
        }

        protected virtual void SetInViewOfCamera(bool isInView)
        {
            inViewOfCamera = isInView;

            SetVisible(Visible); // toggles rendering if in view of camera or not
        }

        public virtual Transform BoundsRootTransform => transform;

        protected void Awake()
        {
            var meshData = MeshData;
            if (meshData != null) meshData.Initialize();

            OnAwake();
        }

        public bool autoCreateInstance = true; 
        public bool startRenderingImmediately = true;
        protected void Start()
        {
            var meshData = MeshData;
            if (meshData != null) meshData.Initialize();

            if (autoCreateInstance && instance == null) CreateInstance();
            if (startRenderingImmediately) SetVisible(true); 

            OnStart();  
        }

        private bool visibleOnEnable; 
        protected void OnEnable()
        {
            if (visibleOnEnable) StartRenderingInstance();
            visibleOnEnable = false; 

            OnEnabled();
        }
        protected void OnDisable()
        {
            visibleOnEnable = Visible;
            StopRenderingInstance();

            OnDisabled();
        }

        protected virtual void OnAwake()
        {
        }
        protected virtual void OnStart()
        {
        }

        protected virtual void OnEnabled()
        {
        }
        protected virtual void OnDisabled()
        {
        }

        protected virtual int OnSetLOD(int levelOfDetail) => levelOfDetail;
        public void SetLOD(int levelOfDetail)
        {
            levelOfDetail = OnSetLOD(levelOfDetail);
            if (instance != null) instance.SetLOD2(instance.CurrentLOD, levelOfDetail); 
        }
        protected void SetLOD2(int prevLevelOfDetail, int levelOfDetail)
        {
            levelOfDetail = OnSetLOD(levelOfDetail);
            if (instance != null) instance.SetLOD2(prevLevelOfDetail, levelOfDetail); 
        }

        [NonSerialized]
        protected CullingLODs.RendererCullLOD cullLODs;

        public delegate void CreateInstanceDelegate(InstancedMesh instance);
        public event CreateInstanceDelegate OnCreateInstance;
        public delegate void CreateInstanceIDDelegate(int instanceIndex);
        public event CreateInstanceIDDelegate OnCreateInstanceID;
        protected virtual void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (instance != null) instance.Dispose();
            if (cullLODs != null) cullLODs.Dispose();

            instance = MeshGroup.NewInstance(subMeshIndex, transform, floatOverrides, colorOverrides, vectorOverrides);
            cullLODs = CullingLODs.GetCameraCullLOD(Camera.main).AddRenderer(BoundsRootTransform, MeshData.boundsCenter, MeshData.boundsExtents, MeshData.LODs); 
            cullLODs.OnLODChange += SetLOD2;
            cullLODs.OnVisibilityChange += SetInViewOfCamera; 

            OnCreateInstance?.Invoke(instance);
            OnCreateInstanceID?.Invoke(instance.slot);
        }
        protected virtual void CreateInstance()
        {
            CreateInstance(null, null, null);
        }
    }

    public abstract class InstanceableSkinnedMeshBase : InstanceableMeshBase
    {

        [SerializeField]
        protected CustomAnimator animator;
        public virtual CustomAnimator Animator
        {
            get => animator;
            set => animator = value;
        }

        protected struct BlendShapeSync
        {
            public int listenerIndex;
            public int listenerShapeIndex;
        }

        protected struct BlendShapeSyncLR
        {
            public int listenerIndex;
            public int listenerShapeIndexLeft;
            public int listenerShapeIndexRight;
        }

        [SerializeField]
        protected List<SkinnedMeshRenderer> syncedSkinnedMeshes = new List<SkinnedMeshRenderer>();

        protected abstract void SetupSkinnedMeshSyncs();

        public void SyncSkinnedMesh(SkinnedMeshRenderer skinnedMesh, bool reinitialize = true)
        {
            if (skinnedMesh == null) return;

            syncedSkinnedMeshes.Add(skinnedMesh);
            if (reinitialize) SetupSkinnedMeshSyncs();
        }

        public void DesyncSkinnedMesh(SkinnedMeshRenderer skinnedMesh)
        {
            if (skinnedMesh == null) return;

            syncedSkinnedMeshes.RemoveAll(i => i == null || i == skinnedMesh);
            SetupSkinnedMeshSyncs();
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            SetupSkinnedMeshSyncs();

            if (animator == null) animator = gameObject.GetComponentInParent<CustomAnimator>(true); 
        }

        internal readonly static Dictionary<string, InstanceBuffer<float4x4>> _skinningMatricesBuffers = new Dictionary<string, InstanceBuffer<float4x4>>();

        protected InstanceBuffer<float4x4> skinningMatricesBuffer;  
        public InstanceBuffer<float4x4> SkinningMatricesBuffer
        {
            get
            {
                if (skinningMatricesBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup; 
                    string bufferID = RigBufferID;  
                    string matricesProperty = SkinnedMeshData.SkinningMatricesPropertyName; 
                    if (!_skinningMatricesBuffers.TryGetValue(bufferID, out skinningMatricesBuffer) || skinningMatricesBuffer == null || !skinningMatricesBuffer.IsValid()) 
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float4x4>(matricesProperty, BoneCount, 3, out skinningMatricesBuffer);
                        _skinningMatricesBuffers[bufferID] = skinningMatricesBuffer; 

                        string boneCountPropertyName = MeshData.BoneCountPropertyName;
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            material.SetInteger(boneCountPropertyName, BoneCount);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);    
                    } 
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        meshGroup.BindInstanceMaterialBuffer(matricesProperty, skinningMatricesBuffer);

                        string boneCountPropertyName = MeshData.BoneCountPropertyName;
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            //skinningMatricesBuffer.BindMaterialProperty(material, matricesProperty); 
                            
                            material.SetInteger(boneCountPropertyName, BoneCount);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);  
                    }
                }

                return skinningMatricesBuffer;
            }
        }

        protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            base.CreateInstance(floatOverrides, colorOverrides, vectorOverrides);
            if (instance != null) instance.StartRenderingInFrontOfCamera(); // forces the mesh to never get culled by unity (will get culled by custom system)

            var matricesBuffer = SkinningMatricesBuffer;
            if (RigSampler != null) rigSampler.AddWritableInstanceBuffer(matricesBuffer, RigInstanceID * BoneCount);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (rigSampler != null) 
            { 
                rigSampler.RemoveWritableInstanceBuffer(skinningMatricesBuffer);
                rigSampler.UnregisterAsUser();
                rigSampler.TryDispose();
            }
        }
        
        public virtual InstanceableSkinnedMeshDataBase SkinnedMeshData => ((InstanceableSkinnedMeshDataBase)MeshData);

        protected Rigs.StandaloneSampler rigSampler;
        public virtual Rigs.StandaloneSampler RigSampler
        {
            get
            {
                if (rigSampler == null && !IsDestroyed)
                {
                    if (!Rigs.TryGetStandaloneSampler(RigID, out rigSampler))
                    {
                        Rigs.CreateStandaloneSampler(RigID, Bones, BindPose, out rigSampler);
                        rigSampler.RegisterAsUser();
                    }
                }

                return rigSampler;
            }
        }

        public abstract string RigID { get; }
        public abstract string RigBufferID { get; }
        public virtual int RigInstanceID => InstanceSlot;
        public abstract Transform[] Bones { get; }
        public abstract int BoneCount { get; }
        public abstract Matrix4x4[] BindPose { get; }

        public abstract string ShapeBufferID { get; }

    }

    [Serializable]
    public class InstancedMeshGroup : IDisposable
    {

        protected readonly Dictionary<string, object> runtimeData = new Dictionary<string, object>();
        public object GetRuntimeData(string key)
        {
            if (runtimeData.TryGetValue(key, out object val)) return val;

            return null;
        }
        public void SetRuntimeData(string key, object value)
        {
            runtimeData[key] = value;
        }
        public bool HasRuntimeData(string key) => runtimeData.ContainsKey(key);

        public void Dispose()
        {
            foreach (var inst in instancesMO) 
            {
                try
                {
                    if (inst != null) inst.Dispose();
                }
                catch(Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            instancesMO.Clear();
            foreach (var buffer in instanceBuffersMO)
            {
                try
                {
                    if (buffer != null) buffer.Dispose();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            instanceBuffersMO.Clear();

            foreach (var inst in instancesRL)
            {
                try
                {
                    if (inst != null) inst.Dispose();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            instancesRL.Clear();
            foreach (var buffer in instanceBuffersRL)
            {
                try
                {
                    if (buffer != null) buffer.Dispose();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            instanceBuffersRL.Clear();

            foreach (var inst in instances)
            {
                try
                {
                    if (inst != null) inst.Dispose();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            instances.Clear();
            foreach (var buffer in instanceBuffers)
            {
                try
                {
                    if (buffer != null) buffer.Dispose();
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            instanceBuffers.Clear();
        }

        protected readonly List<InstancedMeshMO> instancesMO = new List<InstancedMeshMO>();
        protected readonly List<IInstanceBuffer> instanceBuffersMO = new List<IInstanceBuffer>();
        protected void EnsureInstanceBufferSizesMO()
        {
            foreach (var buffer in instanceBuffersMO) while (buffer.InstanceCount < instancesMO.Count) buffer.Grow(2, 0);
        }
        public int BindInstanceMaterialBufferMO(string propertyName, IInstanceBuffer buffer) => BindInstanceMaterialBufferMO(propertyName, null, buffer);
        public int BindInstanceMaterialBufferMO(string propertyName, ICollection<int> materialSlots, IInstanceBuffer buffer)
        {
            if (materialSlots == null)
            {
                foreach (var material in materials)
                {
                    buffer.BindMaterialProperty(material, propertyName);
                }
            }
            else
            {
                foreach (var slot in materialSlots)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);
                }
            }

            int bufferIndex = instanceBuffersMO.IndexOf(buffer);
            if (bufferIndex < 0)
            {
                bufferIndex = instanceBuffersMO.Count;
                instanceBuffersMO.Add(buffer);
            }
            return bufferIndex;
        }
        public int InstanceBufferCountMO => instanceBuffersMO.Count;  
        public IInstanceBuffer GetInstanceBufferMO(int index) => instanceBuffersMO[index];
        public int CreateInstanceMaterialBufferMO<T>(string propertyName, int elementsPerInstance, int bufferPoolSize, out InstanceBuffer<T> buffer) where T : unmanaged => CreateInstanceMaterialBufferMO(propertyName, null, elementsPerInstance, bufferPoolSize, out buffer);
        public int CreateInstanceMaterialBufferMO<T>(string propertyName, ICollection<int> materialSlots, int elementsPerInstance, int bufferPoolSize, out InstanceBuffer<T> buffer) where T : unmanaged
        {
            buffer = new InstanceBuffer<T>(propertyName, instancesMO.Count, elementsPerInstance, bufferPoolSize, ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
            return BindInstanceMaterialBufferMO(propertyName, materialSlots, buffer);
        }

        public InstancedMeshMO NewInstanceMO(int subMesh,
            Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) => NewInstanceMO(subMesh, 0, transform, floatOverrides, colorOverrides, vectorOverrides);
        public InstancedMeshMO NewInstanceMO(int subMesh, int levelOfDetail,
            Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null)
        {
            int slot = -1;
            for (int a = 0; a < instancesMO.Count; a++) if (instancesMO[a] == null)
                {
                    slot = a;
                    break;
                }
            if (slot < 0)
            {
                slot = instancesMO.Count;
                instancesMO.Add(null);
            }

            var instance = new InstancedMeshMO(subMesh, this, slot, transform, floatOverrides, colorOverrides, vectorOverrides);
            instance.SetLOD(levelOfDetail);
            instancesMO[slot] = instance;

            EnsureInstanceBufferSizesMO();

            return instance;
        }

        [NonSerialized]
        protected readonly List<InstancedMeshRL> instancesRL = new List<InstancedMeshRL>();
        [NonSerialized]
        protected readonly List<IInstanceBuffer> instanceBuffersRL = new List<IInstanceBuffer>();
        protected void EnsureInstanceBufferSizesRL()
        {
            foreach (var buffer in instanceBuffersRL) while (buffer.InstanceCount < instancesRL.Count) buffer.Grow(2, 0);
        }
        public int BindInstanceMaterialBufferRL(string propertyName, IInstanceBuffer buffer) => BindInstanceMaterialBufferRL(propertyName, null, buffer);
        public int BindInstanceMaterialBufferRL(string propertyName, ICollection<int> materialSlots, IInstanceBuffer buffer)
        {
            if (materialSlots == null)
            {
                foreach (var material in materials)
                {
                    buffer.BindMaterialProperty(material, propertyName);
                }
            }
            else
            {
                foreach (var slot in materialSlots)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);
                }
            }

            int bufferIndex = instanceBuffersRL.IndexOf(buffer);
            if (bufferIndex < 0)
            {
                bufferIndex = instanceBuffersRL.Count;
                instanceBuffersRL.Add(buffer);
            }
            return bufferIndex;
        }
        public int InstanceBufferCountRL => instanceBuffersRL.Count;
        public IInstanceBuffer GetInstanceBufferRL(int index) => instanceBuffersRL[index];
        public int CreateInstanceMaterialBufferRL<T>(string propertyName, int elementsPerInstance, int bufferPoolSize, out InstanceBuffer<T> buffer) where T : unmanaged => CreateInstanceMaterialBufferRL(propertyName, null, elementsPerInstance, bufferPoolSize, out buffer);
        public int CreateInstanceMaterialBufferRL<T>(string propertyName, ICollection<int> materialSlots, int elementsPerInstance, int bufferPoolSize, out InstanceBuffer<T> buffer) where T : unmanaged
        {
            buffer = new InstanceBuffer<T>(propertyName, instancesRL.Count, elementsPerInstance, bufferPoolSize, ComputeBufferType.Structured, ComputeBufferMode.Dynamic);
            return BindInstanceMaterialBufferRL(propertyName, materialSlots, buffer);
        }

        public InstancedMeshRL NewInstanceRL(uint renderingLayerMask, int subMesh,
            Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) => NewInstanceRL(renderingLayerMask, subMesh, 0, transform, floatOverrides, colorOverrides, vectorOverrides);
        public InstancedMeshRL NewInstanceRL(uint renderingLayerMask, int subMesh, int levelOfDetail,
            Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null)
        {
            int slot = -1;
            for (int a = 0; a < instancesRL.Count; a++) if (instancesRL[a] == null)
                {
                    slot = a;
                    break;
                }
            if (slot < 0)
            {
                slot = instancesRL.Count;
                instancesRL.Add(null);
            }

            var instance = new InstancedMeshRL(renderingLayerMask, subMesh, this, slot, transform, floatOverrides, colorOverrides, vectorOverrides);
            instance.SetLOD(levelOfDetail);
            instancesRL[slot] = instance;

            EnsureInstanceBufferSizesRL();

            return instance;
        }

        [NonSerialized]
        protected readonly List<InstancedMesh> instances = new List<InstancedMesh>();
        [NonSerialized]
        protected readonly List<IInstanceBuffer> instanceBuffers = new List<IInstanceBuffer>();
        protected void EnsureInstanceBufferSizes()
        {
            foreach (var buffer in instanceBuffers) while (buffer.InstanceCount < instances.Count) buffer.Grow(2, 0); 
        }
        public int BindInstanceMaterialBuffer(string propertyName, IInstanceBuffer buffer) => BindInstanceMaterialBuffer(propertyName, null, buffer);
        public int BindInstanceMaterialBuffer(string propertyName, ICollection<int> materialSlots, IInstanceBuffer buffer)
        {
            if (materialSlots == null)
            {
                foreach (var material in materials)
                {
                    buffer.BindMaterialProperty(material, propertyName);
                }
            }
            else
            {
                foreach (var slot in materialSlots)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);
                }
            }

            int bufferIndex = instanceBuffers.IndexOf(buffer);
            if (bufferIndex < 0)
            {
                bufferIndex = instanceBuffers.Count;
                instanceBuffers.Add(buffer);
            }
            return bufferIndex;
        }
        public int InstanceBufferCount => instanceBuffers.Count;
        public IInstanceBuffer GetInstanceBuffer(int index) => instanceBuffers[index];
        public int CreateInstanceMaterialBuffer<T>(string propertyName, int elementsPerInstance, int bufferPoolSize, out InstanceBuffer<T> buffer) where T : unmanaged => CreateInstanceMaterialBuffer(propertyName, null, elementsPerInstance, bufferPoolSize, out buffer);
        public int CreateInstanceMaterialBuffer<T>(string propertyName, ICollection<int> materialSlots, int elementsPerInstance, int bufferPoolSize, out InstanceBuffer<T> buffer) where T : unmanaged
        {
            buffer = new InstanceBuffer<T>(propertyName, instances.Count, elementsPerInstance, bufferPoolSize, ComputeBufferType.Structured, ComputeBufferMode.SubUpdates);
            return BindInstanceMaterialBuffer(propertyName, materialSlots, buffer);
        }

        public InstancedMesh NewInstance(int subMesh,
            Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) => NewInstance(subMesh, 0, transform, floatOverrides, colorOverrides, vectorOverrides);
        public InstancedMesh NewInstance(int subMesh, int levelOfDetail, 
            Transform transform = null, 
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null)
        {
            int slot = -1;
            for(int a = 0; a < instances.Count; a++) if (instances[a] == null)
                {
                    slot = a;
                    break;
                }
            if (slot < 0)
            {
                slot = instances.Count;
                instances.Add(null);
            }

            var instance = new InstancedMesh(subMesh, this, slot, transform, floatOverrides, colorOverrides, vectorOverrides);
            instance.SetLOD(levelOfDetail);
            instances[slot] = instance;

            EnsureInstanceBufferSizes(); 

            return instance;
        }

        [NonSerialized]
        protected bool disableInstanceRemoval;
        public void DisableInstanceRemoval(bool flag) => disableInstanceRemoval = flag;
        public void Remove(InstancedMeshMO mesh)
        {
            if (disableInstanceRemoval) return;

            var slot = instancesMO.IndexOf(mesh); 
            if (slot < 0) return;

            RemoveAtSlotMO(slot);
        }
        public void Remove(InstancedMeshRL mesh)
        {
            if (disableInstanceRemoval) return;

            var slot = instancesRL.IndexOf(mesh);
            if (slot < 0) return;

            RemoveAtSlotRL(slot);
        }
        public void Remove(InstancedMesh mesh)
        {
            if (disableInstanceRemoval) return;

            var slot = instances.IndexOf(mesh);
            if (slot < 0) return;

            RemoveAtSlot(slot);
        }
        public void RemoveAtSlotMO(int slot)
        {
            if (disableInstanceRemoval || slot < 0 || slot >= instancesMO.Count) return;

            var inst = instancesMO[slot];
            if (inst != null)
            {
                inst.group = null;
                inst.Dispose(); 
            }
            instancesMO[slot] = null;
        }
        public void RemoveAtSlotRL(int slot)
        {
            if (disableInstanceRemoval || slot < 0 || slot >= instancesRL.Count) return;

            var inst = instancesRL[slot];
            if (inst != null)
            {
                inst.group = null;
                inst.Dispose();
            }
            instancesRL[slot] = null;
        }
        public void RemoveAtSlot(int slot)
        {
            if (disableInstanceRemoval || slot < 0 || slot >= instances.Count) return;

            var inst = instances[slot];
            if (inst != null)
            {
                inst.group = null;
                inst.Dispose();
            }
            instances[slot] = null;
        }

        [SerializeField]
        protected string name;
        [SerializeField]
        protected int maxInstances = 511;
        [SerializeField]
        protected string instanceIdPropertyNameOverride;
        public string InstanceIDPropertyName => string.IsNullOrWhiteSpace(instanceIdPropertyNameOverride) ? InstanceableMesh._instanceIdDefaultPropertyName : instanceIdPropertyNameOverride;  

        [SerializeField]
        protected Material[] materials;
        public int MaterialCount => materials.Length;
        public Material GetMaterial(int index) => materials[index];

        public InstancedMeshGroup() { }

        public InstancedMeshGroup(string name, Material[] materials, string instanceIdPropertyNameOverride = null, int maxInstances = 511)
        {
            this.name = name;
            this.materials = materials;
            this.instanceIdPropertyNameOverride = instanceIdPropertyNameOverride;
            this.maxInstances = maxInstances;
        }

        [NonSerialized]
        internal InstanceableMeshDataBase meshData; 

        internal InstancedRendering.RenderGroup<InstancedRendering.InstanceDataMatrixAndMotionVectors> GetRenderGroup(int subMesh, int LOD) => InstancedRendering.GetRenderGroup<InstancedRendering.InstanceDataMatrixAndMotionVectors>(meshData.GetMesh(LOD), subMesh);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors> CreateNewInstance(int subMesh, int LOD, InstancedRendering.InstanceDataMatrixAndMotionVectors instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateNewInstance(default, subMesh, LOD, instanceData, floatOverrides, colorOverrides, vectorOverrides);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors> CreateNewInstance(
            RenderParams renderParams, int subMesh, int LOD, InstancedRendering.InstanceDataMatrixAndMotionVectors instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, 
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (renderParams.material == null) renderParams = InstancedRendering.GetDefaultRenderParams();
            renderParams.material = materials[subMesh];

            return GetRenderGroup(subMesh, LOD).AddNewInstance(renderParams.material, instanceData, true, true, renderParams, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal InstancedRendering.RenderGroup<InstancedRendering.InstanceDataMatrixOnly> GetRenderGroupMO(int subMesh, int LOD) => InstancedRendering.GetRenderGroup<InstancedRendering.InstanceDataMatrixOnly>(meshData.GetMesh(LOD), subMesh);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly> CreateNewInstanceMO(int subMesh, int LOD, InstancedRendering.InstanceDataMatrixOnly instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateNewInstanceMO(default, subMesh, LOD, instanceData, floatOverrides, colorOverrides, vectorOverrides);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly> CreateNewInstanceMO(
            RenderParams renderParams, int subMesh, int LOD, InstancedRendering.InstanceDataMatrixOnly instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (renderParams.material == null) renderParams = InstancedRendering.GetDefaultRenderParams();
            renderParams.material = materials[subMesh];

            return GetRenderGroupMO(subMesh, LOD).AddNewInstance(renderParams.material, instanceData, true, true, renderParams, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal InstancedRendering.RenderGroup<InstancedRendering.InstanceDataFull> GetRenderGroupRL(int subMesh, int LOD) => InstancedRendering.GetRenderGroup<InstancedRendering.InstanceDataFull>(meshData.GetMesh(LOD), subMesh);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull> CreateNewInstanceRL(int subMesh, int LOD, InstancedRendering.InstanceDataFull instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateNewInstanceRL(default, subMesh, LOD, instanceData, floatOverrides, colorOverrides, vectorOverrides);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull> CreateNewInstanceRL(
            RenderParams renderParams, int subMesh, int LOD, InstancedRendering.InstanceDataFull instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (renderParams.material == null) renderParams = InstancedRendering.GetDefaultRenderParams();
            renderParams.material = materials[subMesh];

            return GetRenderGroupRL(subMesh, LOD).AddNewInstance(renderParams.material, instanceData, true, true, renderParams, floatOverrides, colorOverrides, vectorOverrides);
        }
    }

    public interface IInstanceBuffer : IDisposable
    {
        public void BindMaterialProperty(Material material, string propertyName);
        public void UnbindMaterialProperty(Material material, string propertyName);
        public void Grow(float multiplier = 2, int updateActiveBufferFrameDelay = 0);
        public bool IsValid();


        /// <summary>
        /// The write indices determine which portion of the internal data to upload to the buffers and GPU. The minimum and maximim indices remain persistent until the buffer pool has made a full rotation.
        /// </summary>
        public void TrySetWriteIndices(int startIndex, int count);

        public void RequestUpload();


        public int Size { get; }
        public int InstanceCount { get; }
        public int Stride { get; }
        public int ElementsPerInstance { get; }
    }

    public class InstanceBuffer<T> : IInstanceBuffer where T : unmanaged
    {
        public struct BoundMaterialProperty
        {
            public string propertyName; 
            public Material material;
        }

        protected readonly List<BoundMaterialProperty> boundMaterialProperties = new List<BoundMaterialProperty>(); 
        /// <summary>
        /// Binds the buffer to the material and will set the property value again if the buffer changes to a new internal buffer instance.
        /// </summary>
        public void BindMaterialProperty(Material material, string propertyName)
        {
            material.SetBuffer(propertyName, bufferPool.ActiveBuffer);
            boundMaterialProperties.Add(new BoundMaterialProperty() { material = material, propertyName = propertyName });
        }
        public void UnbindMaterialProperty(Material material, string propertyName)
        {
            boundMaterialProperties.RemoveAll(i => (i.material == material && i.propertyName == propertyName));
        }

        protected void OnBufferSwap(ComputeBuffer newBuffer)
        {
            foreach (var binding in boundMaterialProperties) binding.material.SetBuffer(binding.propertyName, newBuffer);
        }

        protected ComputeBufferType bufferType;
        public ComputeBufferType BufferType => bufferType;

        protected ComputeBufferMode bufferMode;
        public ComputeBufferMode BufferMode => bufferMode;

        public int Stride => bufferPool.Stride;

        protected int elementsPerInstance;
        public int ElementsPerInstance => elementsPerInstance;

        protected DynamicComputeBufferPool<T> bufferPool;
        public T this[int index]
        {
            get => bufferPool[index];
            set => bufferPool[index] = value;
        }
        public bool IsValid() => bufferPool != null && bufferPool.IsValid();


        public bool WriteToBuffer(int index, T data)
        {
            bufferPool.Write(index, data);
            return true;
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public bool WriteToBufferFast(int index, T data)
        {
            bufferPool.WriteFast(index, data);
            return true;
        }

        public bool WriteToBuffer(NativeArray<T> localArray, int localIndex, int writeStartIndex, int count)
        {
            bufferPool.Write(localArray, localIndex, writeStartIndex, count);
            return true;
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public bool WriteToBufferFast(NativeArray<T> localArray, int localIndex, int writeStartIndex, int count)
        {
            bufferPool.WriteFast(localArray, localIndex, writeStartIndex, count);
            return true;
        }

        public bool WriteToBufferCallback(int writeStartIndex, int count, WriteToBufferDataDelegate<T> callback)
        {
            bufferPool.Write(callback, writeStartIndex, count);
            return true;
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public bool WriteToBufferCallbackFast(int writeStartIndex, int count, WriteToBufferDataDelegate<T> callback)
        {
            bufferPool.WriteFast(callback, writeStartIndex, count);
            return true;
        }
        public bool WriteToBufferCallback(int writeStartIndex, int count, WriteToBufferDataFromStartIndexDelegate<T> callback)
        {
            bufferPool.Write(callback, writeStartIndex, count);
            return true;
        }
        /// <summary>
        /// WARNING: Does not update write indices or request upload to the gpu!
        /// </summary>
        public bool WriteToBufferCallbackFast(int writeStartIndex, int count, WriteToBufferDataFromStartIndexDelegate<T> callback)
        {
            bufferPool.WriteFast(callback, writeStartIndex, count);
            return true;
        }

        /// <summary>
        /// The write indices determine which portion of the internal data to upload to the buffers and GPU. The minimum and maximim indices remain persistent until the buffer pool has made a full rotation.
        /// </summary>
        public void TrySetWriteIndices(int startIndex, int count) => bufferPool.TrySetWriteIndices(startIndex, count);

        public void RequestUpload() => bufferPool.RequestUpload();

        public int Size => bufferPool == null ? 0 : bufferPool.Size;
        public int InstanceCount => Size / ElementsPerInstance;

        public string name;

        public InstanceBuffer(string name, int initialSize, int elementPerInstance, int bufferPoolSize, ComputeBufferType bufferType, ComputeBufferMode bufferMode)
        {
            this.name = name;
            this.elementsPerInstance = elementPerInstance; 
            this.bufferType = bufferType;
            this.bufferMode = bufferMode;
            bufferPool = new DynamicComputeBufferPool<T>(name, Mathf.Max(1, initialSize) * elementPerInstance, Mathf.Max(1, bufferPoolSize), bufferType, bufferMode);
            bufferPool.ListenForBufferSwap(OnBufferSwap);
        }
         
        public override string ToString() => $"InstanceBuffer<{typeof(T).Name}>::{name}";

        public void Dispose()
        {
            boundMaterialProperties.Clear();

            if (bufferPool != null && bufferPool.IsValid())
            {
                bufferPool.Dispose();
                bufferPool = null;
            }
        }

        unsafe public void Grow(float multiplier = 2, int updateActiveBufferFrameDelay = 0)
        {
            multiplier = Mathf.Max(1, multiplier);
            int size = Mathf.Max(1, Mathf.CeilToInt(((bufferPool.Size / (float)ElementsPerInstance) * multiplier))) * ElementsPerInstance;

#if UNITY_EDITOR
            Debug.Log($"RESIZING BUFFER {name} TO {size} .... element size: {Stride}");
#endif
            bufferPool.SetSize(size, updateActiveBufferFrameDelay);
        }
    }

    public abstract class InstancedMeshBase : IDisposable
    {
        internal InstancedMeshGroup group;
        protected int subMesh;
        protected int LOD;
        public int CurrentLOD => LOD;

        /// <summary>
        /// The instance slot id for this mesh in its group
        /// </summary>
        internal readonly int slot;
        public int Slot => slot;

        public virtual void SetLOD(int detailLevel)
        {
            bool isVisible = IsVisible;

            StopRendering();
            LOD = detailLevel;
            if (isVisible) StartRendering(); 
            
            Debug.Log("SET LOD " + detailLevel);  
        }
        public virtual void SetLOD2(int prevDetailLevel, int detailLevel) => SetLOD(detailLevel);

        public virtual void SetVisibility(bool visible)
        {
            if (visible) StartRendering(); else StopRendering();
            Debug.Log("SET VISIBLE " + visible);
        }

        public bool IsValid => group != null;

        public void Dispose()
        {
            StopRendering();

            if (group != null)
            {
                group.RemoveAtSlot(slot);
                group = null;
            }
        }
        public InstancedMeshBase(int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null)
        {
            this.group = group;
            this.subMesh = subMesh;
            this.slot = slot;
            this.boundTransform = transform;
            this.floatOverrides = floatOverrides;
            this.colorOverrides = colorOverrides;
            this.vectorOverrides = vectorOverrides;

            if (this.floatOverrides == null) this.floatOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>();
            this.floatOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<float>() { propertyName = group.InstanceIDPropertyName, value = slot }); 
        }

        internal InstancedRendering.IRenderingInstance renderInstance;

        internal List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides;
        public void SetFloatOverride(string propertyName, float value, bool updateMaterial = true)
        {
            if (floatOverrides == null)
            {
                floatOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>() { new InstancedRendering.MaterialPropertyInstanceOverride<float>() { propertyName = group.InstanceIDPropertyName, value = slot } };
            }

            bool flag = false;
            for (int a = 0; a < floatOverrides.Count; a++)
            {
                var or = floatOverrides[a];
                if (or.propertyName == propertyName)
                {
                    or.value = value;
                    floatOverrides[a] = or;
                    flag = true;
                    break;
                }
            }

            if (!flag && !string.IsNullOrWhiteSpace(propertyName)) floatOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<float>() { propertyName = propertyName, value = value });

            if (updateMaterial && renderInstance != null) renderInstance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.Index, floatOverrides);
        }
        public void AddOrSetFloatOverrides(ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> overrides, bool updateMaterial = true)
        {
            int i = 0;
            foreach (var ov in overrides)
            {
                i++;
                SetFloatOverride(ov.propertyName, ov.value, updateMaterial && i >= overrides.Count);
            }
        }
        public void RemoveFloatOverride(string propertyName, bool updateMaterial = true)
        {
            if (floatOverrides == null || string.IsNullOrWhiteSpace(propertyName)) return;

            floatOverrides.RemoveAll(i => i.propertyName == propertyName);

            if (updateMaterial && renderInstance != null) renderInstance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.Index, floatOverrides);
        }

        internal List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides;
        public void SetColorOverride(string propertyName, Color value, bool updateMaterial = true)
        {
            if (colorOverrides == null) colorOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<Color>>();

            bool flag = false;
            for (int a = 0; a < colorOverrides.Count; a++)
            {
                var or = colorOverrides[a];
                if (or.propertyName == propertyName)
                {
                    or.value = value;
                    colorOverrides[a] = or;
                    flag = true;
                    break;
                }
            }

            if (!flag && !string.IsNullOrWhiteSpace(propertyName)) colorOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<Color>() { propertyName = propertyName, value = value });

            if (updateMaterial && renderInstance != null) renderInstance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.Index, null, colorOverrides, null);
        }
        public void AddOrSetColorOverrides(ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> overrides, bool updateMaterial = true)
        {
            int i = 0;
            foreach (var ov in overrides)
            {
                i++;
                SetColorOverride(ov.propertyName, ov.value, updateMaterial && i >= overrides.Count);
            }
        }
        public void RemoveColorOverride(string propertyName, bool updateMaterial = true)
        {
            if (colorOverrides == null || string.IsNullOrWhiteSpace(propertyName)) return;

            colorOverrides.RemoveAll(i => i.propertyName == propertyName);

            if (updateMaterial && renderInstance != null) renderInstance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.Index, null, colorOverrides, null);
        }

        internal List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides;
        public void SetVectorOverride(string propertyName, Vector4 value, bool updateMaterial = true)
        {
            if (vectorOverrides == null) vectorOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>>();

            bool flag = false;
            for (int a = 0; a < vectorOverrides.Count; a++)
            {
                var or = vectorOverrides[a];
                if (or.propertyName == propertyName)
                {
                    or.value = value;
                    vectorOverrides[a] = or;
                    flag = true;
                    break;
                }
            }

            if (!flag && !string.IsNullOrWhiteSpace(propertyName)) vectorOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<Vector4>() { propertyName = propertyName, value = value });

            if (updateMaterial && renderInstance != null) renderInstance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.Index, null, null, vectorOverrides);
        }
        public void AddOrSetVectorOverrides(ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> overrides, bool updateMaterial = true)
        {
            int i = 0;
            foreach (var ov in overrides)
            {
                i++;
                SetVectorOverride(ov.propertyName, ov.value, updateMaterial && i >= overrides.Count); 
            }
        }
        public void RemoveVectorOverride(string propertyName, bool updateMaterial = true)
        {
            if (vectorOverrides == null || string.IsNullOrWhiteSpace(propertyName)) return;

            vectorOverrides.RemoveAll(i => i.propertyName == propertyName);

            if (updateMaterial && renderInstance != null) renderInstance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.Index, null, null, vectorOverrides);
        }

        protected bool renderInFrontOfCamera;
        protected bool RenderInFrontOfCamera
        {
            get => renderInFrontOfCamera;
            set
            {
                if (value) StartRenderingInFrontOfCamera(); else StopRenderingInFrontOfCamera();
            }
        }

        public void StartRenderingInFrontOfCamera()
        {
            renderInFrontOfCamera = true;
            if (renderInstance != null) renderInstance.RenderGroup.StartRenderingInFrontOfCamera();
        }
        public void StopRenderingInFrontOfCamera()
        {
            renderInFrontOfCamera = false;
            if (renderInstance != null) renderInstance.RenderGroup.StopRenderingInFrontOfCamera();
        }

        public virtual void StartRendering()
        {
            if (renderInstance == null)
            {
                renderInstance = CreateRenderingInstance();
                if (renderInFrontOfCamera) renderInstance.RenderGroup.StartRenderingInFrontOfCamera();
            }
        }
        internal abstract InstancedRendering.IRenderingInstance CreateRenderingInstance();
        public void StopRendering()
        {
            if (renderInstance != null)
            {
                renderInstance.Dispose(true, true);
                renderInstance = null;
            }
        }

        public bool IsVisible => renderInstance != null && renderInstance.Valid;

        protected Transform boundTransform;
        /// <summary>
        /// The transform that controls the position, rotation, and scale of the mesh.
        /// </summary>
        public Transform BoundTransform
        {
            get => boundTransform;
            set => SetBoundTransform(value);
        }
        /// <summary>
        /// Set the transform that controls the position, rotation, and scale of the mesh.
        /// </summary>
        public virtual void SetBoundTransform(Transform transform)
        {
            boundTransform = transform;
            SetInstanceDataFromBoundTransform(); 
        }
        internal abstract void SetInstanceDataFromBoundTransform();

        /// <summary>
        /// Syncs the rendered mesh with the bound transform, if set.
        /// </summary>
        public abstract void SyncWithTransform();
    }
    public class InstancedMesh : InstancedMeshBase
    {

        public InstancedMesh(int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) : base(subMesh, group, slot, transform, floatOverrides, colorOverrides, vectorOverrides)
        {}

        internal override InstancedRendering.IRenderingInstance CreateRenderingInstance()
        {
            var localToWorld = boundTransform == null ? Matrix4x4.identity : boundTransform.localToWorldMatrix;
            return group.CreateNewInstance(subMesh, LOD, new InstancedRendering.InstanceDataMatrixAndMotionVectors()
            {
                objectToWorld = localToWorld,
                prevObjectToWorld = localToWorld
            }, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal override void SetInstanceDataFromBoundTransform()
        {
            if (boundTransform == null || renderInstance == null) return;

            var localToWorld = boundTransform.localToWorldMatrix;
            ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors>)renderInstance).SetData((new InstancedRendering.InstanceDataMatrixAndMotionVectors()
            {
                objectToWorld = localToWorld,
                prevObjectToWorld = localToWorld
            }));
        }

        public override void SyncWithTransform()
        {
            if (renderInstance == null || boundTransform == null) return;

            var ri = ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors>)renderInstance);
            var data = ri.GetData();
            data.prevObjectToWorld = data.objectToWorld;
            data.objectToWorld = boundTransform.localToWorldMatrix;
            ri.SetData(data);
        }
    }
    public class InstancedMeshMO : InstancedMeshBase
    {

        public InstancedMeshMO(int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) : base(subMesh, group, slot, transform, floatOverrides, colorOverrides, vectorOverrides)
        { }

        internal override InstancedRendering.IRenderingInstance CreateRenderingInstance()
        {
            var localToWorld = boundTransform == null ? Matrix4x4.identity : boundTransform.localToWorldMatrix;
            return group.CreateNewInstanceMO(subMesh, LOD, new InstancedRendering.InstanceDataMatrixOnly()
            {
                objectToWorld = localToWorld
            }, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal override void SetInstanceDataFromBoundTransform()
        {
            if (boundTransform == null || renderInstance == null) return;

            var localToWorld = boundTransform.localToWorldMatrix;
            ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly>)renderInstance).SetData((new InstancedRendering.InstanceDataMatrixOnly()
            {
                objectToWorld = localToWorld,
            }));
        }

        public override void SyncWithTransform()
        {
            if (renderInstance == null || boundTransform == null) return;

            var ri = ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly>)renderInstance);
            var data = ri.GetData();
            data.objectToWorld = boundTransform.localToWorldMatrix;
            ri.SetData(data);
        }
    }
    public class InstancedMeshRL : InstancedMeshBase
    {

        protected uint renderingLayerMask;
        public uint RenderingLayerMask => renderingLayerMask;

        public InstancedMeshRL(uint renderingLayerMask, int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) : base(subMesh, group, slot, transform, floatOverrides, colorOverrides, vectorOverrides)
        {
            this.renderingLayerMask = renderingLayerMask;
        }

        internal override InstancedRendering.IRenderingInstance CreateRenderingInstance()
        {
            var localToWorld = boundTransform == null ? Matrix4x4.identity : boundTransform.localToWorldMatrix;
            return group.CreateNewInstanceRL(subMesh, LOD, new InstancedRendering.InstanceDataFull()
            {
                objectToWorld = localToWorld,
                prevObjectToWorld = localToWorld,
                renderingLayerMask = renderingLayerMask
            }, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal override void SetInstanceDataFromBoundTransform()
        {
            if (boundTransform == null || renderInstance == null) return;

            var localToWorld = boundTransform.localToWorldMatrix;
            ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull>)renderInstance).SetData((new InstancedRendering.InstanceDataFull()
            {
                objectToWorld = localToWorld,
                prevObjectToWorld = localToWorld,
                renderingLayerMask = renderingLayerMask
            }));
        }

        public override void SyncWithTransform()
        {
            if (renderInstance == null || boundTransform == null) return;

            var ri = ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull>)renderInstance);
            var data = ri.GetData();
            data.prevObjectToWorld = data.objectToWorld;
            data.objectToWorld = boundTransform.localToWorldMatrix; 
            ri.SetData(data);
        }
    }

    [Serializable]
    public struct MeshLOD
    {
        public Mesh mesh;
        public float screenRelativeTransitionHeight;
    }
    public abstract class InstanceableMeshDataBase : ScriptableObject, IDisposable
    {

        public InstancedMeshGroup[] meshGroups; 

        [NonSerialized]
        protected bool initialized;
        public virtual void Initialize()
        {
            if (initialized) return;

            if (meshGroups != null)
            {
                for (int a = 0; a < meshGroups.Length; a++) if (meshGroups[a] != null) 
                    {
                        var group = meshGroups[a];
                        group.meshData = this;

                        string vertexCountProp = VertexCountPropertyName;
                        for(int b = 0; b < group.MaterialCount; b++)
                        {
                            var material = group.GetMaterial(b);
                            material.SetFloat(vertexCountProp, VertexCount);
                        }
                    }
            }

            initialized = true;  
        }

        [NonSerialized]
        private bool trackingDisposables;
        public void TrackDisposables()
        {
            if (trackingDisposables) return;

            if (!PersistentJobDataTracker.Track(this))
            {
                Dispose();
                return;
            }

            trackingDisposables = true;
        }

        public void Dispose()
        {
            if (trackingDisposables)
            {
                try
                {
                    PersistentJobDataTracker.Untrack(this);
                }
                catch (Exception ex)
                {
#if UNITY_EDITOR
                    Debug.LogError(ex);
#endif
                }
            }
            trackingDisposables = false;

            try
            {
                if (vertices.IsCreated)
                {
                    vertices.Dispose();
                    vertices = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (normals.IsCreated)
                {
                    normals.Dispose();
                    normals = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (tangents.IsCreated)
                {
                    tangents.Dispose();
                    tangents = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (boneWeightsBuffer != null && boneWeightsBuffer.IsValid())
                {
                    boneWeightsBuffer.Dispose();
                    boneWeightsBuffer = null;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            DisposeLocal();

            if (meshGroups != null)
            {
                foreach(var group in meshGroups)
                {
                    if (group != null)
                    {
                        try
                        {
                            group.DisableInstanceRemoval(true);
                            group.Dispose();
                            group.DisableInstanceRemoval(false);
                        }
                        catch (Exception ex)
                        {
#if UNITY_EDITOR
                            Debug.LogException(ex);
#endif
                        }
                    }
                }
            }
        }

        protected virtual void DisposeLocal()
        {
        }

        [SerializeField]
        protected MeshLOD[] meshLODs;
        public void SetMeshLODs(MeshLOD[] meshLODs) => this.meshLODs = meshLODs;
        public Mesh Mesh => meshLODs == null || meshLODs.Length <= 0 ? null : meshLODs[0].mesh;
        public int LevelsOfDetail => meshLODs == null ? 0 : meshLODs.Length;
        public Mesh GetMesh(int lod) => meshLODs == null ? null : GetMeshUnsafe(Mathf.Clamp(lod, 0, meshLODs.Length - 1));
        public Mesh GetMeshUnsafe(int lod) => meshLODs[lod].mesh;
        public MeshLOD GetLOD(int lod) => meshLODs == null ? default : GetLODUnsafe(Mathf.Clamp(lod, 0, meshLODs.Length - 1));
        public MeshLOD GetLODUnsafe(int lod) => meshLODs[lod];

        private static int SortLODsDescending(CullingLODs.LOD a, CullingLODs.LOD b) => Math.Sign(b.minDistance - a.minDistance);
        [NonSerialized]
        protected CullingLODs.LOD[] lods;
        public CullingLODs.LOD[] LODs
        {
            get
            {
                if (lods == null)
                {
                    var lodsList = new List<CullingLODs.LOD>(LevelsOfDetail);
                    for (int a = 0; a < LevelsOfDetail; a++) lodsList.Add(new CullingLODs.LOD() { detailLevel = a, minDistance = meshLODs[a].screenRelativeTransitionHeight }); 
                    lodsList.Sort(SortLODsDescending); 

                    lods = lodsList.ToArray();
                }

                return lods;
            }
        }

        [SerializeField]
        public Vector3 boundsCenter;
        [SerializeField]
        public Vector3 boundsExtents; 

        public int VertexCount => LevelsOfDetail <= 0 ? 0 : meshLODs[0].mesh.vertexCount;

        [NonSerialized]
        protected NativeArray<float3> vertices;
        [NonSerialized]
        protected NativeArray<float3> normals;
        [NonSerialized]
        protected NativeArray<float4> tangents;

        private static readonly List<Vector3> tempV3 = new List<Vector3>();
        private static readonly List<Vector4> tempV4 = new List<Vector4>();

        public NativeArray<float3> Vertices
        {
            get
            {
                if (!vertices.IsCreated)
                {
                    var mesh = Mesh;
                    if (mesh != null)
                    {
                        Initialize();

                        tempV3.Clear();
                        mesh.GetVertices(tempV3);
                        vertices = new NativeArray<float3>(tempV3.Count, Allocator.Persistent);
                        for (int a = 0; a < vertices.Length; a++) vertices[a] = tempV3[a];
                        tempV3.Clear();

                        TrackDisposables();
                    }
                }

                return vertices;
            }
        }
        public NativeArray<float3> Normals
        {
            get
            {
                if (!normals.IsCreated)
                {
                    var mesh = Mesh;
                    if (mesh != null)
                    {
                        Initialize();

                        tempV3.Clear();
                        mesh.GetNormals(tempV3);
                        normals = new NativeArray<float3>(tempV3.Count, Allocator.Persistent);
                        for (int a = 0; a < normals.Length; a++) normals[a] = tempV3[a];
                        tempV3.Clear();

                        TrackDisposables();
                    }
                }

                return normals;
            }
        }
        public NativeArray<float4> Tangents
        {
            get
            {
                if (!tangents.IsCreated)
                {
                    var mesh = Mesh;
                    if (mesh != null)
                    {
                        Initialize();

                        tempV4.Clear();
                        mesh.GetTangents(tempV4);
                        tangents = new NativeArray<float4>(tempV4.Count, Allocator.Persistent);
                        for (int a = 0; a < tangents.Length; a++) tangents[a] = tempV4[a];
                        tempV4.Clear();

                        TrackDisposables();
                    }
                }

                return tangents;
            }
        }

        protected static readonly List<BoneWeight8Float> tempBoneWeights = new List<BoneWeight8Float>();
        [NonSerialized]
        protected ComputeBuffer boneWeightsBuffer;
        public ComputeBuffer BoneWeightsBuffer
        {
            get
            {
                if (boneWeightsBuffer == null)
                {
                    tempBoneWeights.Clear();

                    var mesh = Mesh;
                    if (mesh != null)
                    {
                        Initialize();

                        using (var boneWeights = mesh.GetAllBoneWeights())
                        {
                            using (var boneCounts = mesh.GetBonesPerVertex())
                            {
                                int boneWeightIndex = 0;
                                for (int a = 0; a < boneCounts.Length; a++)
                                {
                                    var data = new BoneWeight8Float();

                                    float totalWeight = 0; 
                                    var boneCount = boneCounts[a];
                                    for (int b = 0; b < Mathf.Min(8, boneCount); b++) 
                                    {
                                        var boneWeight = boneWeights[boneWeightIndex + b];
                                        totalWeight = totalWeight + boneWeight.weight;
                                        data = data.Modify(b, boneWeight.boneIndex, boneWeight.weight); 
                                    }
                                    if (totalWeight > 0)
                                    {
                                        data.weightsA = data.weightsA / totalWeight;
                                        data.weightsB = data.weightsB / totalWeight;
                                    }
                                     
                                    tempBoneWeights.Add(data); 
                                    boneWeightIndex += boneCount;
                                }
                            }
                        }

                        boneWeightsBuffer = new ComputeBuffer(tempBoneWeights.Count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                        boneWeightsBuffer.SetData(tempBoneWeights);
                    }
                    else
                    {
                        boneWeightsBuffer = new ComputeBuffer(0, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    }
                    tempBoneWeights.Clear();

                    TrackDisposables();
                }

                return boneWeightsBuffer;
            }
        }

        public const string _vertexCountDefaultPropertyName = "_VertexCount";
        public string vertexCountPropertyNameOverride;
        public string VertexCountPropertyName => string.IsNullOrWhiteSpace(vertexCountPropertyNameOverride) ? _vertexCountDefaultPropertyName : vertexCountPropertyNameOverride;

        public const string _skinningDataDefaultPropertyName = "_SkinBindings";
        public string skinningDataPropertyNameOverride;
        public string SkinningDataPropertyName => string.IsNullOrWhiteSpace(skinningDataPropertyNameOverride) ? _skinningDataDefaultPropertyName : skinningDataPropertyNameOverride;

        public const string _boneCountDefaultPropertyName = "_BoneCount";
        public string boneCountPropertyNameOverride;
        public string BoneCountPropertyName => string.IsNullOrWhiteSpace(boneCountPropertyNameOverride) ? _boneCountDefaultPropertyName : boneCountPropertyNameOverride;

        public void ApplyBoneWeightsBufferToMaterials(string propertyName, ICollection<int> materialSlots) => ApplyBufferToMaterials(propertyName, materialSlots, BoneWeightsBuffer);
        public void ApplyBoneWeightsBufferToMaterials(string propertyName) => ApplyBoneWeightsBufferToMaterials(propertyName, null);

        public void ApplyBufferToMaterials(string propertyName, ICollection<int> materialSlots, ComputeBuffer buffer)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        for (int index = 0; index < group.MaterialCount; index++)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetBuffer(propertyName, buffer);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetBuffer(propertyName, buffer);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyBufferToMaterials(string propertyName, ComputeBuffer buffer) => ApplyBufferToMaterials(propertyName, null, buffer);

        public void ApplyFloatToMaterials(string propertyName, ICollection<int> materialSlots, float value)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        for (int index = 0; index < group.MaterialCount; index++)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetFloat(propertyName, value);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetFloat(propertyName, value);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyFloatToMaterials(string propertyName, float value) => ApplyFloatToMaterials(propertyName, null, value);

        public void ApplyIntegerToMaterials(string propertyName, ICollection<int> materialSlots, int value)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        for (int index = 0; index < group.MaterialCount; index++)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetInteger(propertyName, value);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetInteger(propertyName, value);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyIntegerToMaterials(string propertyName, int value) => ApplyIntegerToMaterials(propertyName, null, value);

        public void ApplyVectorToMaterials(string propertyName, ICollection<int> materialSlots, Vector4 value)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        for (int index = 0; index < group.MaterialCount; index++)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetVector(propertyName, value);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetVector(propertyName, value);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyVectorToMaterials(string propertyName, Vector4 value) => ApplyVectorToMaterials(propertyName, null, value);

        public void ApplyColorToMaterials(string propertyName, ICollection<int> materialSlots, Color value)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        for (int index = 0; index < group.MaterialCount; index++)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetColor(propertyName, value);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            var mat = group.GetMaterial(index);
                            if (mat != null)
                            {
                                mat.SetColor(propertyName, value);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyColorToMaterials(string propertyName, Color value) => ApplyColorToMaterials(propertyName, null, value);

    }
    public abstract class InstanceableSkinnedMeshDataBase : InstanceableMeshDataBase
    {
        public const string _skinningMatricesDefaultPropertyName = "_SkinningMatrices";
        public string skinningMatricesPropertyNameOverride;
        public string SkinningMatricesPropertyName => string.IsNullOrWhiteSpace(skinningMatricesPropertyNameOverride) ? _skinningMatricesDefaultPropertyName : skinningMatricesPropertyNameOverride;

        [NonSerialized]
        protected Matrix4x4[] managedBindPose;
        public Matrix4x4[] ManagedBindPose
        {
            get
            {
                if (managedBindPose == null)
                {
                    var mesh = Mesh;
                    if (mesh != null)
                    {
                        managedBindPose = mesh.bindposes;
                    } 
                    else
                    {
                        managedBindPose = new Matrix4x4[0];
                    }
                }

                return managedBindPose;
            }
        }

        public override void Initialize()
        {
            if (initialized) return;

            base.Initialize(); 
             
            ApplyBoneWeightsBufferToMaterials(SkinningDataPropertyName, null);  
        }
    }
}

#endif