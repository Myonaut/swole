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
            OnSetRenderingCameras = null;
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
                foreach(var cullLOD in cullLODs) cullLOD.Dispose();
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
        public virtual int InstanceSlot => instance == null ? -1 : instance.Slot;
        public virtual bool HasInstance => instance != null && instance.Slot >= 0;
        public virtual bool IsInitialized => HasInstance;

        protected bool visible;
        public bool Visible
        {
            get => visible;
            set => SetVisible(value);
        }

        [Serializable]
        public class RenderingCamera
        {
            public Camera camera;
            [NonSerialized]
            public bool canSee;
        }
        [SerializeField]
        protected RenderingCamera[] renderingCameras;
        protected Camera[] renderingCameras_;
        public int CameraCount => renderingCameras == null ? 0 : renderingCameras.Length;
        public Camera[] GetRenderingCameras()
        {
            if (renderingCameras_ == null || renderingCameras_.Length != renderingCameras.Length) 
            { 
                renderingCameras_ = new Camera[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++) renderingCameras_[a] = renderingCameras[a].camera;
            }

            return renderingCameras_;
        }

        public delegate void OnSetRenderingCamerasDelegate(RenderingCamera[] renderingCameras);
        public event OnSetRenderingCamerasDelegate OnSetRenderingCameras;
        public void SetRenderingCameras(IEnumerable<Camera> cameras, bool notifyListeners = true, bool initInstance = true)
        {
            var count = cameras.GetCount();
            renderingCameras = new RenderingCamera[count];
            renderingCameras_ = null;

            int i = 0;
            foreach (var cam in cameras)
            {
                renderingCameras[i] = new RenderingCamera()
                {
                    camera = cam
                };
                i++;
            }

            if (instance != null)
            {
                instance.Dispose();
                instance = null;
            }
            if (initInstance)
            {
                if (autoCreateInstance) CreateInstance();
                if (startRenderingImmediately) SetVisible(true); 
            }

            if (notifyListeners) OnSetRenderingCameras?.Invoke(renderingCameras);
        }
        public void SetRenderingCameras(IEnumerable<RenderingCamera> cameras, bool notifyListeners = true, bool initInstance = true)
        {
            var count = cameras.GetCount();
            renderingCameras = new RenderingCamera[count];
            renderingCameras_ = null;

            int i = 0;
            foreach (var cam in cameras)
            {
                renderingCameras[i] = new RenderingCamera()
                {
                    camera = cam.camera
                };
                i++;
            }

            if (instance != null)
            {
                instance.Dispose();
                instance = null;
            }
            if (initInstance)
            {
                if (autoCreateInstance) CreateInstance();
                if (startRenderingImmediately) SetVisible(true);
            }

            if (notifyListeners) OnSetRenderingCameras?.Invoke(renderingCameras);
        }
        protected void SetRenderingCamerasInternal(RenderingCamera[] renderingCameras) => SetRenderingCameras(renderingCameras, false);

        public bool IsInViewOfCamera(int cameraIndex) => renderingCameras[cameraIndex].canSee;       

        public virtual bool IsRendering(int cameraIndex)
        {
            if (instance == null) return false;
            return instance.IsRendering(cameraIndex);
        }

        public virtual void SetVisible(bool visible)
        {
            this.visible = visible;
            if (visibleOnEnable && !visible) visibleOnEnable = false; 

            if (instance == null) return;

            if (visible && enabled)
            {
                for(int a = 0; a < renderingCameras.Length; a++)
                {
                    if (IsInViewOfCamera(a)) instance.StartRendering(a); else instance.StopRendering(a);
                }
            }
            else
            {
                instance.StopRendering();
            }
        }

        public virtual void StartRenderingInstance()
        {
            if (instance != null)
            {
                for (int a = 0; a < renderingCameras.Length; a++) if (IsInViewOfCamera(a)) instance.StartRendering(a); 
            }
        }
        public virtual void StopRenderingInstance()
        {
            if (instance != null) instance.StopRendering();
        }

        protected virtual void SetInViewOfCamera(int cameraIndex, bool isInView)
        {
            renderingCameras[cameraIndex].canSee = isInView;
            if (instance != null)
            {
                if (instance.IsRendering(cameraIndex) != isInView) 
                {
                    if (isInView)
                    {
                        instance.StartRendering(cameraIndex);
                    } 
                    else
                    {
                        instance.StopRendering(cameraIndex);
                    }
                }
            }
        }

        public virtual Transform BoundsRootTransform => transform;

        protected void Awake()
        {
            if (renderingCameras == null || renderingCameras.Length < 1)
            {
                SetRenderingCameras(new Camera[] { Camera.main }, true, false);
            }

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
        protected virtual void OnEnable()
        {
            if (visibleOnEnable) StartRenderingInstance();
            visibleOnEnable = false; 

            OnEnabled();
        }
        protected virtual void OnDisable()
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

        public virtual int MinimumLOD => 0;

        protected virtual int OnSetLOD(int cameraIndex, int levelOfDetail) => levelOfDetail;
        public void SetLOD(int cameraIndex, int levelOfDetail)
        {
            levelOfDetail = Mathf.Max(MinimumLOD, OnSetLOD(cameraIndex, levelOfDetail));
            if (instance != null) instance.SetLOD2(cameraIndex, instance.GetCurrentLOD(cameraIndex), levelOfDetail); 
        }
        protected void SetLOD2(int cameraIndex, int prevLevelOfDetail, int levelOfDetail)
        {
            levelOfDetail = Mathf.Max(MinimumLOD, OnSetLOD(cameraIndex, levelOfDetail));
            if (instance != null) instance.SetLOD2(cameraIndex, prevLevelOfDetail, levelOfDetail); 
        }

        [NonSerialized]
        protected CullingLODs.RendererCullLOD[] cullLODs;

        [SerializeField]
        protected float screenRelativeHeightBias = 1f;
        public void SetScreenRelativeHeightBias(float bias)
        {
            this.screenRelativeHeightBias = bias;
            if (cullLODs != null) 
            { 
                foreach(var cullLOD in cullLODs) cullLOD.SetScreenRelativeHeightBias(bias); 
            }
        }

        public delegate void CreateInstanceDelegate(InstancedMesh instance);
        public event CreateInstanceDelegate OnCreateInstance;
        public delegate void CreateInstanceIDDelegate(int instanceIndex);
        public event CreateInstanceIDDelegate OnCreateInstanceID;
        protected virtual void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (instance != null) instance.Dispose();
            if (cullLODs != null)
            {
                foreach (var cullLOD in cullLODs) cullLOD.Dispose();
            }

            instance = MeshGroup.NewInstance(GetRenderingCameras(), subMeshIndex, transform, floatOverrides, colorOverrides, vectorOverrides);

            if (cullLODs == null || cullLODs.Length != renderingCameras.Length) cullLODs = new CullingLODs.RendererCullLOD[renderingCameras.Length];
            for (int a = 0; a < renderingCameras.Length; a++)
            {
                int cameraIndex = a;

                var cullLOD = CullingLODs.GetCameraCullLOD(renderingCameras[a].camera).AddRenderer(BoundsRootTransform, MeshData.boundsCenter, MeshData.boundsExtents, MeshData.LODs, screenRelativeHeightBias);
                cullLOD.OnLODChange += (int prevLOD, int newLOD) => SetLOD2(cameraIndex, prevLOD, newLOD);
                cullLOD.OnVisibilityChange += (bool isInView) => SetInViewOfCamera(cameraIndex, isInView);

                cullLODs[a] = cullLOD;
            }

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

        protected void SyncStandaloneShape(IList<BlendShapeSync>[] syncs, int index, float weight)
        {
            var list = syncs[index];
            if (list != null && list.Count > 0)
            {
                foreach (var sync in list)
                {
                    var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                    if (mesh != null) mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weight);
                }
            }
        }

        protected void SyncPartialShapeData(IList<BlendShapeSync>[] syncs, int groupIndex, float weight, int count, float[] frameWeights)
        {
            int indexY = groupIndex * count;

            if (count == 1)
            {
                float frameWeight = frameWeights == null ? 1 : frameWeights[0];

                var list = syncs[indexY];
                if (list != null && list.Count > 0)
                {
                    foreach (var sync in list)
                    {
                        var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                        if (mesh != null)
                        {
                            mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weight / frameWeight);
                        }
                    }
                }
            }
            else if (count > 1)
            {

                int countM1 = count - 1;
                float countM1f = countM1;
                for (int a = 0; a < countM1; a++)
                {
                    float frameWeightA = frameWeights == null ? (a / countM1f) : frameWeights[a];

                    int b = a + 1;
                    float frameWeightB = frameWeights == null ? (b / countM1f) : frameWeights[b];

                    float weightRange = frameWeightB - frameWeightA;

                    float weightA = 0;
                    float weightB = (weight - frameWeightA) / weightRange;
                    if (weightB < 0)
                    {
                        if (a == 0)
                        {
                            if (frameWeightA != 0)
                            {
                                weightA = weight / frameWeightA;
                                weightB = 0;
                            }
                            else
                            {
                                weightA = 1 + Mathf.Abs(weight / weightRange);
                                weightB = 0;
                            }
                        }
                        else
                        {
                            weightA = Mathf.Abs(weightB);
                            weightB = 0;
                        }
                    }
                    else
                    {
                        weightA = 1 - weightB;
                        if (weightA < 0 && b < countM1)
                        {
                            weightA = 0;
                            weightB = 0;
                        }
                        else
                        {
                            weightA = Mathf.Max(0, weightA);
                        }
                    }

                    var list = syncs[indexY + a];
                    if (list != null && list.Count > 0)
                    {
                        foreach (var sync in list)
                        {
                            var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                            if (mesh != null)
                            {
                                mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weightA);
                            }
                        }
                    }

                    if (a == countM1 - 1)
                    {
                        list = syncs[indexY + b];
                        if (list != null && list.Count > 0)
                        {
                            foreach (var sync in list)
                            {
                                var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                                if (mesh != null)
                                {
                                    mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weightB);
                                }
                            }
                        }
                    }
                }
            }
        }
        protected void SyncPartialShapeData(IList<BlendShapeSyncLR>[] syncs, int groupIndex, float weightL, float weightR, int count, float[] frameWeights)
        {
            if (syncs == null) return;

            int indexY = groupIndex * count;

            if (count == 1)
            {
                float frameWeight = frameWeights == null ? 1 : frameWeights[0];

                var list = syncs[indexY];
                if (list != null && list.Count > 0)
                {
                    foreach (var sync in list)
                    {
                        var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                        if (mesh != null)
                        {
                            if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightL / frameWeight);
                            if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightR / frameWeight);
                        }
                    }
                }
            }
            else if (count > 1)
            {

                int countM1 = count - 1;
                float countM1f = countM1;
                for (int a = 0; a < countM1; a++)
                {
                    float frameWeightA = frameWeights == null ? (a / countM1f) : frameWeights[a];

                    int b = a + 1;
                    float frameWeightB = frameWeights == null ? (b / countM1f) : frameWeights[b];

                    float weightRange = frameWeightB - frameWeightA;

                    float weightA_L = 0;
                    float weightB_L = (weightL - frameWeightA) / weightRange;
                    if (weightB_L < 0)
                    {
                        if (a == 0)
                        {
                            if (frameWeightA != 0)
                            {
                                weightA_L = weightL / frameWeightA;
                                weightB_L = 0;
                            }
                            else
                            {
                                weightA_L = 1 + Mathf.Abs(weightL / weightRange);
                                weightB_L = 0;
                            }
                        }
                        else
                        {
                            weightA_L = Mathf.Abs(weightB_L);
                            weightB_L = 0;
                        }
                    }
                    else
                    {
                        weightA_L = 1 - weightB_L;
                        if (weightA_L < 0 && b < countM1)
                        {
                            weightA_L = 0;
                            weightB_L = 0;
                        }
                        else
                        {
                            weightA_L = Mathf.Max(0, weightA_L);
                        }
                    }

                    float weightA_R = 0;
                    float weightB_R = (weightR - frameWeightA) / weightRange;
                    if (weightB_R < 0)
                    {
                        if (a == 0)
                        {
                            if (frameWeightA != 0)
                            {
                                weightA_R = weightR / frameWeightA;
                                weightB_R = 0;
                            }
                            else
                            {
                                weightA_R = 1 + Mathf.Abs(weightR / weightRange);
                                weightB_R = 0;
                            }
                        }
                        else
                        {
                            weightA_R = Mathf.Abs(weightB_R);
                            weightB_R = 0;
                        }
                    }
                    else
                    {
                        weightA_R = 1 - weightB_R;
                        if (weightA_R < 0 && b < countM1)
                        {
                            weightA_R = 0;
                            weightB_R = 0;
                        }
                        else
                        {
                            weightA_R = Mathf.Max(0, weightA_R);
                        }
                    }

                    var list = syncs[indexY + a];
                    if (list != null && list.Count > 0)
                    {
                        foreach (var sync in list)
                        {
                            var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                            if (mesh != null)
                            {
                                if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightA_L);
                                if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightA_R);
                            }
                        }
                    }

                    if (a == countM1 - 1)
                    {
                        list = syncs[indexY + b];
                        if (list != null && list.Count > 0)
                        {
                            foreach (var sync in list)
                            {
                                var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                                if (mesh != null)
                                {
                                    if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightB_L);
                                    if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightB_R);
                                }
                            }
                        }
                    }
                }
            }
        }

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

            if (animator == null) animator = gameObject.GetComponentInParent<CustomAnimator>(true); 
        }
        protected override void OnStart()
        {
            base.OnStart();

            SetupSkinnedMeshSyncs();
        }

        internal readonly static Dictionary<string, InstanceBuffer<float4x4>> _skinningMatricesBuffers = new Dictionary<string, InstanceBuffer<float4x4>>();

        protected InstanceBuffer<float4x4> skinningMatricesBuffer;  
        public virtual InstanceBuffer<float4x4> SkinningMatricesBuffer
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
                        meshGroup.CreateInstanceMaterialBuffer<float4x4>(matricesProperty, SkinningBoneCount, 3, out skinningMatricesBuffer); 
                        _skinningMatricesBuffers[bufferID] = skinningMatricesBuffer; 

                        string boneCountPropertyName = MeshData.BoneCountPropertyName;
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            meshGroup.SetMaterialInteger(boneCountPropertyName, SkinningBoneCount);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);    
                    } 
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        meshGroup.BindInstanceMaterialBuffer(matricesProperty, skinningMatricesBuffer);

                        string boneCountPropertyName = MeshData.BoneCountPropertyName;
                        for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            meshGroup.SetMaterialInteger(boneCountPropertyName, SkinningBoneCount);
                        }

                        meshGroup.SetRuntimeData(bufferID, true);  
                    }
                }

                return skinningMatricesBuffer;
            }
        }

        //protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateInstance(floatOverrides, colorOverrides, vectorOverrides/*, true*/);
        protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides/*, bool renderInFrontOfCamera*/)
        {
            base.CreateInstance(floatOverrides, colorOverrides, vectorOverrides);
            //if (instance != null && renderInFrontOfCamera) instance.StartRenderingInFrontOfCamera(); // forces the mesh to never get culled by unity (will get culled by custom system)
            if (instance != null) instance.StartUsingCameraRelativeWorldBounds(Vector3.one * 10000f); // forces the mesh to never get culled by unity (will get culled by custom system)
            
            var matricesBuffer = SkinningMatricesBuffer;
            if (RigSampler != null) rigSampler.AddWritableInstanceBuffer(matricesBuffer, RigInstanceID * SkinningBoneCount);
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
                        var bindPose = BindPose;
                        var bones = SkinnedBones;
                        if (bones.Length > bindPose.Length)
                        {
#if UNITY_EDITOR
                            int origLength = bones.Length;
#endif
                            var bones_ = new Transform[bindPose.Length];
                            Array.Copy(bones, 0, bones_, 0, bindPose.Length);
                            bones = bones_;

#if UNITY_EDITOR
                            Debug.LogWarning($"Resizing rig sampler bones array ({origLength} -> {bones.Length}) for {name}");  
#endif
                        } 
                        else if (bindPose.Length > bones.Length) 
                        {
#if UNITY_EDITOR
                            int origLength = bindPose.Length;
#endif
                            var bindPose_ = new Matrix4x4[bones.Length];
                            Array.Copy(bindPose, 0, bindPose_, 0, bones.Length);
                            bindPose = bindPose_;

#if UNITY_EDITOR
                            Debug.LogError($"Resizing rig sampler bindPose array ({origLength} -> {bindPose.Length}) for {name}");
#endif
                        }

                        Rigs.CreateStandaloneSampler(RigID, bones, bindPose, out rigSampler);
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
        public abstract Transform[] SkinnedBones { get; }
        public virtual int BoneCount => SkinningBoneCount;
        public abstract Matrix4x4[] BindPose { get; }
        public virtual int SkinningBoneCount => BindPose.Length; 

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
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);

                    if (perCameraMaterials != null)
                    {
                        foreach (var entry in perCameraMaterials)
                        {
                            var mats = entry.Value;
                            if (mats != null)
                            {
                                buffer.BindMaterialProperty(mats[slot], propertyName);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var slot in materialSlots)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);

                    if (perCameraMaterials != null)
                    {
                        foreach (var entry in perCameraMaterials)
                        {
                            var mats = entry.Value;
                            if (mats != null)
                            {
                                buffer.BindMaterialProperty(mats[slot], propertyName);
                            }
                        }
                    }
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

        public InstancedMeshMO NewInstanceMO(Camera[] renderingCameras, int subMesh, int levelOfDetail,
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

            var instance = new InstancedMeshMO(renderingCameras, subMesh, this, slot, transform, floatOverrides, colorOverrides, vectorOverrides);
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
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);

                    if (perCameraMaterials != null)
                    {
                        foreach (var entry in perCameraMaterials)
                        {
                            var mats = entry.Value;
                            if (mats != null)
                            {
                                buffer.BindMaterialProperty(mats[slot], propertyName);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var slot in materialSlots)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);

                    if (perCameraMaterials != null)
                    {
                        foreach (var entry in perCameraMaterials)
                        {
                            var mats = entry.Value;
                            if (mats != null)
                            {
                                buffer.BindMaterialProperty(mats[slot], propertyName);
                            }
                        }
                    }
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

        public InstancedMeshRL NewInstanceRL(Camera[] renderingCameras, uint renderingLayerMask, int subMesh,
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

            var instance = new InstancedMeshRL(renderingCameras, renderingLayerMask, subMesh, this, slot, transform, floatOverrides, colorOverrides, vectorOverrides);
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
                for(int slot = 0; slot < materials.Length; slot++)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);

                    if (perCameraMaterials != null)
                    {
                        foreach(var entry in perCameraMaterials)
                        {
                            var mats = entry.Value;
                            if (mats != null)
                            {
                                buffer.BindMaterialProperty(mats[slot], propertyName);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var slot in materialSlots)
                {
                    buffer.BindMaterialProperty(materials[slot], propertyName);

                    if (perCameraMaterials != null)
                    {
                        foreach (var entry in perCameraMaterials)
                        {
                            var mats = entry.Value;
                            if (mats != null)
                            {
                                buffer.BindMaterialProperty(mats[slot], propertyName);
                            }
                        }
                    }
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

        public InstancedMesh NewInstance(Camera[] renderingCameras, int subMesh, 
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

            var instance = new InstancedMesh(renderingCameras, subMesh, this, slot, transform, floatOverrides, colorOverrides, vectorOverrides);
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

        public void Initialize(InstanceableMeshDataBase meshData)
        {
            this.meshData = meshData;

            if (materialPropertyOverrides == null || materialPropertyOverrides.Length != materials.Length)
            {
                materialPropertyOverrides = new MaterialPropertyOverrides[materials.Length];
                for (int a = 0; a < materials.Length; a++) materialPropertyOverrides[a] = new MaterialPropertyOverrides();
            }
        }

        [SerializeField]
        protected Material[] materials;
        public int MaterialCount => materials.Length; 
        public Material GetMaterial(int index) => materials[index];
        [NonSerialized]
        protected Dictionary<Camera, Material[]> perCameraMaterials;
        public bool GetPerCameraMaterialIfExists(Camera camera, int index, out Material material)
        {
            material = null;
            if (perCameraMaterials == null || !perCameraMaterials.TryGetValue(camera, out var mats)) return false;

            material = mats[index];
            return true;
        }
        public Material GetPerCameraMaterial(Camera camera, int index)
        {
            if (perCameraMaterials == null) perCameraMaterials = new Dictionary<Camera, Material[]>();
            if (!perCameraMaterials.TryGetValue(camera, out var mats))
            {
                mats = new Material[materials.Length];

                for (int a = 0; a < mats.Length; a++)
                {
                    var originalMaterial = materials[a];
                    var mat = Material.Instantiate(originalMaterial); 

                    var overrides = materialPropertyOverrides[a];
                    overrides.ApplyTo(mat);

                    if (instanceBuffers != null)
                    {
                        foreach (var buffer in instanceBuffers)
                        {
                            if (buffer == null) continue;

                            var propertyName = buffer.GetBoundPropertyName(originalMaterial); 
                            if (propertyName == null) continue;

                            buffer.BindMaterialProperty(mat, propertyName);
                        }
                    }
                    if (instanceBuffersMO != null)
                    {
                        foreach (var buffer in instanceBuffersMO)
                        {
                            if (buffer == null) continue;

                            var propertyName = buffer.GetBoundPropertyName(originalMaterial);
                            if (propertyName == null) continue;

                            buffer.BindMaterialProperty(mat, propertyName);
                        }
                    }
                    if (instanceBuffersRL != null)
                    {
                        foreach (var buffer in instanceBuffersRL)
                        {
                            if (buffer == null) continue;

                            var propertyName = buffer.GetBoundPropertyName(originalMaterial); 
                            if (propertyName == null) continue;

                            buffer.BindMaterialProperty(mat, propertyName); 
                        }
                    }

                    mats[a] = mat;
                }

                perCameraMaterials[camera] = mats; 
            }

            return mats[index];
        }

        protected struct ConstantBufferOverride
        {
            public ComputeBuffer buffer;
            public int offset;
            public int size;
        }
        protected class MaterialPropertyOverrides
        {
            public readonly Dictionary<string, float> floatOverrides = new Dictionary<string, float>();
            public readonly Dictionary<string, int> intOverrides = new Dictionary<string, int>();
            public readonly Dictionary<string, Color> colorOverrides = new Dictionary<string, Color>();
            public readonly Dictionary<string, Vector4> vectorOverrides = new Dictionary<string, Vector4>();
            public readonly Dictionary<string, Texture> textureOverrides = new Dictionary<string, Texture>();
            public readonly Dictionary<string, ComputeBuffer> bufferOverrides = new Dictionary<string, ComputeBuffer>();
            public readonly Dictionary<string, ConstantBufferOverride> constantBufferOverrides = new Dictionary<string, ConstantBufferOverride>();

            public void ApplyTo(Material material)
            {
                foreach (var entry in floatOverrides) material.SetFloat(entry.Key, entry.Value);
                foreach (var entry in intOverrides) material.SetInteger(entry.Key, entry.Value);
                foreach (var entry in colorOverrides) material.SetColor(entry.Key, entry.Value);
                foreach (var entry in vectorOverrides) material.SetVector(entry.Key, entry.Value);
                foreach (var entry in textureOverrides) material.SetTexture(entry.Key, entry.Value);
                foreach (var entry in bufferOverrides) material.SetBuffer(entry.Key, entry.Value);
                foreach (var entry in constantBufferOverrides) material.SetConstantBuffer(entry.Key, entry.Value.buffer, entry.Value.offset, entry.Value.size);
            }
        }
        protected MaterialPropertyOverrides[] materialPropertyOverrides;

        public void SetMaterialFloat(string propertyName, float value)
        {
            for(int a = 0; a < MaterialCount; a++)
            {
                SetMaterialFloat(a, propertyName, value);
            }
        }
        public void SetMaterialFloat(int index, string propertyName, float value)
        {
            var mat = materials[index];
            mat.SetFloat(propertyName, value);
            materialPropertyOverrides[index].floatOverrides[propertyName] = value;

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetFloat(propertyName, value);
                }
            }
        }

        public void SetMaterialInteger(string propertyName, int value)
        {
            for (int a = 0; a < MaterialCount; a++)
            {
                SetMaterialInteger(a, propertyName, value);
            }
        }
        public void SetMaterialInteger(int index, string propertyName, int value)
        {
            var mat = materials[index];
            mat.SetInteger(propertyName, value);
            materialPropertyOverrides[index].intOverrides[propertyName] = value;

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetInteger(propertyName, value);
                }
            }
        }

        public void SetMaterialColor(string propertyName, Color value)
        {
            for (int a = 0; a < MaterialCount; a++)
            {
                SetMaterialColor(a, propertyName, value);
            }
        }
        public void SetMaterialColor(int index, string propertyName, Color value)
        {
            var mat = materials[index];
            mat.SetColor(propertyName, value);
            materialPropertyOverrides[index].colorOverrides[propertyName] = value;

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetColor(propertyName, value);
                }
            }
        }

        public void SetMaterialVector(string propertyName, Vector4 value)
        {
            for (int a = 0; a < MaterialCount; a++)
            {
                SetMaterialVector(a, propertyName, value);
            }
        }
        public void SetMaterialVector(int index, string propertyName, Vector4 value)
        {
            var mat = materials[index];
            mat.SetVector(propertyName, value);
            materialPropertyOverrides[index].vectorOverrides[propertyName] = value;

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetVector(propertyName, value);
                }
            }
        }

        public void SetMaterialTexture(string propertyName, Texture value)
        {
            for (int a = 0; a < MaterialCount; a++)
            {
                SetMaterialTexture(a, propertyName, value);
            }
        }
        public void SetMaterialTexture(int index, string propertyName, Texture value)
        {
            var mat = materials[index];
            mat.SetTexture(propertyName, value);
            materialPropertyOverrides[index].textureOverrides[propertyName] = value;

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetTexture(propertyName, value);
                }
            }
        }

        public void SetMaterialBuffer(string propertyName, ComputeBuffer value)
        {
            for (int a = 0; a < MaterialCount; a++)
            {
                SetMaterialBuffer(a, propertyName, value);
            }
        }
        public void SetMaterialBuffer(int index, string propertyName, ComputeBuffer value)
        {
            var mat = materials[index];
            mat.SetBuffer(propertyName, value);
            materialPropertyOverrides[index].bufferOverrides[propertyName] = value;

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetBuffer(propertyName, value);
                }
            }
        }

        public void SetMaterialConstantBuffer(string propertyName, ComputeBuffer value, int offset, int size)
        {
            for (int a = 0; a < MaterialCount; a++)
            {
                SetMaterialConstantBuffer(a, propertyName, value, offset, size);
            }
        }
        public void SetMaterialConstantBuffer(int index, string propertyName, ComputeBuffer value, int offset, int size)
        {
            var mat = materials[index];
            mat.SetConstantBuffer(propertyName, value, offset, size);
            materialPropertyOverrides[index].constantBufferOverrides[propertyName] = new ConstantBufferOverride() { buffer = value, offset = offset, size = size };

            if (perCameraMaterials != null)
            {
                foreach (var entry in perCameraMaterials)
                {
                    var mats = entry.Value;
                    if (mats == null) continue;

                    mat = mats[index];
                    mat.SetConstantBuffer(propertyName, value, offset, size);
                }
            }
        }

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
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors> CreateNewInstance(Camera camera, bool instantiateMaterial,
            int subMesh, int LOD, InstancedRendering.InstanceDataMatrixAndMotionVectors instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateNewInstance(camera, instantiateMaterial, default, subMesh, LOD, instanceData, floatOverrides, colorOverrides, vectorOverrides);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors> CreateNewInstance(Camera camera, bool instantiateMaterial,
            RenderParams renderParams, int subMesh, int LOD, InstancedRendering.InstanceDataMatrixAndMotionVectors instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides, 
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (renderParams.material == null) renderParams = InstancedRendering.GetDefaultRenderParams();
            renderParams.camera = camera;
            renderParams.material = instantiateMaterial ? GetPerCameraMaterial(camera, subMesh) : GetMaterial(subMesh);

            return GetRenderGroup(subMesh, LOD).AddNewInstance(renderParams.material, instanceData, true, true, renderParams, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal InstancedRendering.RenderGroup<InstancedRendering.InstanceDataMatrixOnly> GetRenderGroupMO(int subMesh, int LOD) => InstancedRendering.GetRenderGroup<InstancedRendering.InstanceDataMatrixOnly>(meshData.GetMesh(LOD), subMesh);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly> CreateNewInstanceMO(Camera camera, bool instantiateMaterial,
            int subMesh, int LOD, InstancedRendering.InstanceDataMatrixOnly instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateNewInstanceMO(camera, instantiateMaterial, default, subMesh, LOD, instanceData, floatOverrides, colorOverrides, vectorOverrides);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly> CreateNewInstanceMO(Camera camera, bool instantiateMaterial,
            RenderParams renderParams, int subMesh, int LOD, InstancedRendering.InstanceDataMatrixOnly instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (renderParams.material == null) renderParams = InstancedRendering.GetDefaultRenderParams();
            renderParams.camera = camera;
            renderParams.material = instantiateMaterial ? GetPerCameraMaterial(camera, subMesh) : GetMaterial(subMesh);

            return GetRenderGroupMO(subMesh, LOD).AddNewInstance(renderParams.material, instanceData, true, true, renderParams, floatOverrides, colorOverrides, vectorOverrides);
        }

        internal InstancedRendering.RenderGroup<InstancedRendering.InstanceDataFull> GetRenderGroupRL(int subMesh, int LOD) => InstancedRendering.GetRenderGroup<InstancedRendering.InstanceDataFull>(meshData.GetMesh(LOD), subMesh);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull> CreateNewInstanceRL(Camera camera, bool instantiateMaterial,
            int subMesh, int LOD, InstancedRendering.InstanceDataFull instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides) => CreateNewInstanceRL(camera, instantiateMaterial, default, subMesh, LOD, instanceData, floatOverrides, colorOverrides, vectorOverrides);
        internal InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull> CreateNewInstanceRL(Camera camera, bool instantiateMaterial,
            RenderParams renderParams, int subMesh, int LOD, InstancedRendering.InstanceDataFull instanceData,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            if (renderParams.material == null) renderParams = InstancedRendering.GetDefaultRenderParams();
            renderParams.camera = camera;
            renderParams.material = instantiateMaterial ? GetPerCameraMaterial(camera, subMesh) : GetMaterial(subMesh);

            return GetRenderGroupRL(subMesh, LOD).AddNewInstance(renderParams.material, instanceData, true, true, renderParams, floatOverrides, colorOverrides, vectorOverrides);
        }
    }

    [Serializable]
    public struct BoundMaterialProperty
    {
        public string propertyName;
        public Material material;
    }
    public interface IInstanceBuffer : IDisposable
    {
        public string Name { get; }

        public void BindMaterialProperty(Material material, string propertyName);
        public void UnbindMaterialProperty(Material material, string propertyName);
        public void Grow(float multiplier = 2, int updateActiveBufferFrameDelay = 0);
        public bool IsValid();
        public string GetBoundPropertyName(Material boundMaterial);


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

        protected readonly List<BoundMaterialProperty> boundMaterialProperties = new List<BoundMaterialProperty>();
        public string GetBoundPropertyName(Material boundMaterial)
        {
            foreach(var binding in boundMaterialProperties)
            {
                if (binding.material == boundMaterial) return binding.propertyName;
            }

            return null;
        }
        /// <summary>
        /// Binds the buffer to the material and will set the property value again if the buffer changes to a new internal buffer instance.
        /// </summary>
        public void BindMaterialProperty(Material material, string propertyName)
        {
            material.SetBuffer(propertyName, bufferPool.ActiveBuffer);
            foreach (var binding in boundMaterialProperties) if (binding.material == material && binding.propertyName == propertyName) return;

            boundMaterialProperties.Add(new BoundMaterialProperty() { material = material, propertyName = propertyName });
        }
        public void UnbindMaterialProperty(Material material, string propertyName)
        {
            boundMaterialProperties.RemoveAll(i => (i.material == material && i.propertyName == propertyName));
        }

        protected void OnBufferSwap(ComputeBuffer newBuffer)
        {
            foreach (var binding in boundMaterialProperties) 
            {
                //Debug.Log($"UPDATING BUFFER {name} ON {binding.propertyName} FOR MAT {binding.material.name}"); 
                binding.material.SetBuffer(binding.propertyName, newBuffer); 
            }
        }

        protected ComputeBufferType bufferType;
        public ComputeBufferType BufferType => bufferType;

        protected ComputeBufferMode bufferMode;
        public ComputeBufferMode BufferMode => bufferMode;

        public int Stride => bufferPool.Stride;

        protected int elementsPerInstance;
        public int ElementsPerInstance => elementsPerInstance;

        public string Name => bufferPool == null ? "null" : bufferPool.Name;

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
            Debug.Log($"DISPOSED INSTANCE BUFFER {name}");
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
        public int GetCurrentLOD(int cameraIndex) => renderInstances[cameraIndex].levelOfDetail;

        /// <summary>
        /// The instance slot id for this mesh in its group
        /// </summary>
        internal readonly int slot;
        public int Slot => slot;

        internal Camera[] renderingCameras;

        public virtual void SetLOD(int cameraIndex, int detailLevel)
        {
            bool isVisible = IsRendering(cameraIndex);

            StopRendering(cameraIndex);
            renderInstances[cameraIndex].levelOfDetail = detailLevel; 
            if (isVisible) StartRendering(cameraIndex);

#if UNITY_EDITOR
            Debug.Log($"({renderingCameras[cameraIndex].name}) SET LOD " + detailLevel);
#endif
        }
        public virtual void SetLOD2(int cameraIndex, int prevDetailLevel, int detailLevel) => SetLOD(cameraIndex, detailLevel);

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
        public InstancedMeshBase(Camera[] renderingCameras, int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null)
        {
            if (renderingCameras == null || renderingCameras.Length < 1)
            {
                renderingCameras = new Camera[] { Camera.main }; 
            }
            this.renderingCameras = renderingCameras;
            renderInstances = new RenderInstanceLOD[renderingCameras.Length];
            for (int a = 0; a < renderInstances.Length; a++) renderInstances[a] = new RenderInstanceLOD(null, 0); 

            this.group = group;
            this.subMesh = subMesh;
            this.slot = slot;
            this.boundTransform = transform;

            if (floatOverrides != null)
            {
                this.floatOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++) this.floatOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>(floatOverrides);
            }
            if (colorOverrides != null)
            {
                this.colorOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<Color>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++) this.colorOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<Color>>(colorOverrides);
            }
            if (vectorOverrides != null)
            {
                this.vectorOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++) this.vectorOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>>(vectorOverrides);
            }

            if (this.floatOverrides == null) 
            { 
                this.floatOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++) this.floatOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>();
            }
            for (int a = 0; a < renderingCameras.Length; a++) this.floatOverrides[a].Add(new InstancedRendering.MaterialPropertyInstanceOverride<float>() { propertyName = group.InstanceIDPropertyName, value = slot }); 
        }

        internal class RenderInstanceLOD
        {
            public int levelOfDetail;
            public InstancedRendering.IRenderingInstance instance;
            public int Index => instance == null ? -1 : instance.Index;
            public int InstanceDataIndex => instance == null ? -1 : instance.InstanceDataIndex;
            public int RenderSequenceIndex => instance == null ? -1 : instance.RenderSequenceIndex;
            public int SubSequenceIndex => instance == null ? -1 : instance.SubSequenceIndex;
            public int IndexInSubsequence => instance == null ? -1 : instance.IndexInSubsequence;

            public RenderInstanceLOD(InstancedRendering.IRenderingInstance renderInstance, int levelOfDetail = 0)
            {
                this.levelOfDetail = levelOfDetail;
                this.instance = renderInstance;
            }
        }
        internal RenderInstanceLOD[] renderInstances;
        public int RenderInstanceCount => renderInstances == null ? 0 : renderInstances.Length;

        internal List<InstancedRendering.MaterialPropertyInstanceOverride<float>>[] floatOverrides;
        public void SetFloatOverride(string propertyName, float value, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                SetFloatOverride(i, propertyName, value, updateMaterial);
            }
        }
        public void AddOrSetFloatOverrides(ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> overrides, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                AddOrSetFloatOverrides(i, overrides, updateMaterial);
            }
        }
        public void RemoveFloatOverride(string propertyName, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                RemoveFloatOverride(i, propertyName, updateMaterial);
            }
        }
        public void SetFloatOverride(int cameraIndex, string propertyName, float value, bool updateMaterial = true)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (this.floatOverrides == null || this.floatOverrides.Length != renderingCameras.Length)
            {
                this.floatOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++)
                {
                    this.floatOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<float>>() { new InstancedRendering.MaterialPropertyInstanceOverride<float>() { propertyName = group.InstanceIDPropertyName, value = slot } };
                }
            }

            var floatOverrides = this.floatOverrides[cameraIndex];

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

            if (!flag) floatOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<float>() { propertyName = propertyName, value = value });

            var renderInstance = renderInstances[cameraIndex];
            if (updateMaterial && renderInstance != null && renderInstance.instance != null)
            {
                renderInstance.instance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.IndexInSubsequence, floatOverrides);
            }
        }
        public void AddOrSetFloatOverrides(int cameraIndex, ICollection<InstancedRendering.MaterialPropertyInstanceOverride<float>> overrides, bool updateMaterial = true)
        {
            int i = 0;
            foreach (var ov in overrides)
            {
                i++;
                SetFloatOverride(cameraIndex, ov.propertyName, ov.value, updateMaterial && i >= overrides.Count);
            }
        }
        public void RemoveFloatOverride(int cameraIndex, string propertyName, bool updateMaterial = true)
        {
            if (this.floatOverrides == null || string.IsNullOrWhiteSpace(propertyName)) return;

            var floatOverrides = this.floatOverrides[cameraIndex];
            if (floatOverrides == null) return;

            floatOverrides.RemoveAll(i => i.propertyName == propertyName);

            var renderInstance = renderInstances[cameraIndex];
            if (updateMaterial && renderInstance != null && renderInstance.instance != null)
            {
                renderInstance.instance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.IndexInSubsequence, floatOverrides);
            }
        }

        internal List<InstancedRendering.MaterialPropertyInstanceOverride<Color>>[] colorOverrides;
        public void SetColorOverride(string propertyName, Color value, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                SetColorOverride(i, propertyName, value, updateMaterial);
            }
        }
        public void AddOrSetColorOverrides(ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> overrides, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                AddOrSetColorOverrides(i, overrides, updateMaterial);
            }
        }
        public void RemoveColorOverride(string propertyName, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                RemoveColorOverride(i, propertyName, updateMaterial);
            }
        }
        public void SetColorOverride(int cameraIndex, string propertyName, Color value, bool updateMaterial = true)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (this.colorOverrides == null || this.colorOverrides.Length != renderingCameras.Length)
            {
                this.colorOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<Color>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++)
                {
                    this.colorOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<Color>>();
                }
            }

            var colorOverrides = this.colorOverrides[cameraIndex];

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

            if (!flag) colorOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<Color>() { propertyName = propertyName, value = value });

            var renderInstance = renderInstances[cameraIndex];
            if (updateMaterial && renderInstance != null && renderInstance.instance != null)
            {
                renderInstance.instance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.IndexInSubsequence, null, colorOverrides, null);
            }
        }
        public void AddOrSetColorOverrides(int cameraIndex, ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Color>> overrides, bool updateMaterial = true)
        {
            int i = 0;
            foreach (var ov in overrides)
            {
                i++;
                SetColorOverride(cameraIndex, ov.propertyName, ov.value, updateMaterial && i >= overrides.Count);
            }
        }
        public void RemoveColorOverride(int cameraIndex, string propertyName, bool updateMaterial = true)
        {
            if (this.colorOverrides == null || string.IsNullOrWhiteSpace(propertyName)) return;

            var colorOverrides = this.colorOverrides[cameraIndex];
            if (colorOverrides == null) return;

            colorOverrides.RemoveAll(i => i.propertyName == propertyName);

            var renderInstance = renderInstances[cameraIndex];
            if (updateMaterial && renderInstance != null && renderInstance.instance != null)
            {
                renderInstance.instance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.IndexInSubsequence, null, colorOverrides, null);
            }
        }

        internal List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>>[] vectorOverrides;
        public void SetVectorOverride(string propertyName, Vector4 value, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                SetVectorOverride(i, propertyName, value, updateMaterial);
            }
        }
        public void AddOrSetVectorOverrides(ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> overrides, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                AddOrSetVectorOverrides(i, overrides, updateMaterial);
            }
        }
        public void RemoveVectorOverride(string propertyName, bool updateMaterial = true)
        {
            for (int i = 0; i < renderingCameras.Length; i++)
            {
                RemoveVectorOverride(i, propertyName, updateMaterial);
            }
        }
        public void SetVectorOverride(int cameraIndex, string propertyName, Vector4 value, bool updateMaterial = true)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) return;

            if (this.vectorOverrides == null || this.vectorOverrides.Length != renderingCameras.Length)
            {
                this.vectorOverrides = new List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>>[renderingCameras.Length];
                for (int a = 0; a < renderingCameras.Length; a++)
                {
                    this.vectorOverrides[a] = new List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>>();
                }
            }

            var vectorOverrides = this.vectorOverrides[cameraIndex];

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

            if (!flag) vectorOverrides.Add(new InstancedRendering.MaterialPropertyInstanceOverride<Vector4>() { propertyName = propertyName, value = value });

            var renderInstance = renderInstances[cameraIndex];
            if (updateMaterial && renderInstance != null && renderInstance.instance != null)
            {
                renderInstance.instance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.IndexInSubsequence, null, null, vectorOverrides);
            }
        }
        public void AddOrSetVectorOverrides(int cameraIndex, ICollection<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> overrides, bool updateMaterial = true)
        {
            int i = 0;
            foreach (var ov in overrides)
            {
                i++;
                SetVectorOverride(cameraIndex, ov.propertyName, ov.value, updateMaterial && i >= overrides.Count);
            }
        }
        public void RemoveVectorOverride(int cameraIndex, string propertyName, bool updateMaterial = true)
        {
            if (this.vectorOverrides == null || string.IsNullOrWhiteSpace(propertyName)) return;

            var vectorOverrides = this.vectorOverrides[cameraIndex];
            if (vectorOverrides == null) return;

            vectorOverrides.RemoveAll(i => i.propertyName == propertyName);

            var renderInstance = renderInstances[cameraIndex];
            if (updateMaterial && renderInstance != null && renderInstance.instance != null)
            {
                renderInstance.instance.SubRenderSequence.SetMaterialPropertyOverrides(renderInstance.IndexInSubsequence, null, null, vectorOverrides);
            }
        }

        /*protected bool renderInFrontOfCamera;
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
            if (renderInstances != null) 
            {
                foreach (var renderInstance in renderInstances) if (renderInstance != null && renderInstance.instance != null) renderInstance.instance.RenderGroup.StartRenderingInFrontOfCamera(); 
            }
        }
        public void StopRenderingInFrontOfCamera()
        {
            renderInFrontOfCamera = false;
            if (renderInstances != null) 
            {
                foreach (var renderInstance in renderInstances) if (renderInstance != null && renderInstance.instance != null) renderInstance.instance.RenderGroup.StopRenderingInFrontOfCamera(); 
            }
        }*/

        protected bool useCameraRelativeWorldBounds;
        public bool UseCameraRelativeWorldBounds 
        { 
            get => UseCameraRelativeWorldBounds;
            set
            {
                if (value) StartUsingCameraRelativeWorldBounds(CameraRelativeWorldBoundsSize); else StopUsingCameraRelativeWorldBounds();
            }
        }
        protected Vector3 cameraRelativeWorldBoundsSize;
        public Vector3 CameraRelativeWorldBoundsSize 
        {
            get => CameraRelativeWorldBoundsSize;
            set 
            {
                CameraRelativeWorldBoundsSize = value;
                if (useCameraRelativeWorldBounds) StartUsingCameraRelativeWorldBounds(CameraRelativeWorldBoundsSize);
            }
        }
        public void StartUsingCameraRelativeWorldBounds(Vector3 size)
        {
            useCameraRelativeWorldBounds = true;
            cameraRelativeWorldBoundsSize = size;
            if (renderInstances != null)
            {
                foreach (var renderInstance in renderInstances) if (renderInstance != null && renderInstance.instance != null) renderInstance.instance.RenderGroup.StartUsingCameraRelativeWorldBounds(cameraRelativeWorldBoundsSize);
            }
        }
        public void StopUsingCameraRelativeWorldBounds()
        {
            useCameraRelativeWorldBounds = false;
            if (renderInstances != null)
            {
                foreach (var renderInstance in renderInstances) if (renderInstance != null && renderInstance.instance != null) renderInstance.instance.RenderGroup.StopRenderingInFrontOfCamera();
            }
        }

        public virtual void StartRendering(int cameraIndex)
        {
            var renderInstance = renderInstances[cameraIndex];
            if (renderInstance == null)
            {
                renderInstance = new RenderInstanceLOD(null, 0);
                renderInstances[cameraIndex] = renderInstance; 
            }
            if (renderInstance.instance == null)
            {
#if UNITY_EDITOR
                Debug.Log($"Started rendering {cameraIndex}");
#endif
                renderInstance.instance = CreateRenderingInstance(cameraIndex, renderInstance.levelOfDetail);
                //if (renderInFrontOfCamera) renderInstance.instance.RenderGroup.StartRenderingInFrontOfCamera();
                if (useCameraRelativeWorldBounds) renderInstance.instance.RenderGroup.StartUsingCameraRelativeWorldBounds(cameraRelativeWorldBoundsSize); 
            }
        }
        internal abstract InstancedRendering.IRenderingInstance CreateRenderingInstance(int cameraIndex, int levelOfDetail);
        public void StopRendering(int cameraIndex)
        {
            var renderInstance = renderInstances[cameraIndex];
            if (renderInstance != null)
            {
                if (renderInstance.instance != null) 
                {
                    renderInstance.instance.Dispose(true, true);
#if UNITY_EDITOR
                    Debug.Log($"Stopped rendering {cameraIndex}");
#endif
                }
                renderInstance.instance = null;
            }
        }

        public void StartRendering()
        {
            if (renderInstances != null)
            {
                for (int a = 0; a < renderInstances.Length; a++) StartRendering(a);
            }
        }
        public void StopRendering()
        {
            if (renderInstances != null)
            {
                for (int a = 0; a < renderInstances.Length; a++) StopRendering(a);
            }
        }

        public virtual bool IsRendering(int cameraIndex)
        {
            var renderInstance = renderInstances[cameraIndex];
            return renderInstance != null && renderInstance.instance != null && renderInstance.instance.Valid;
        }

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

        public InstancedMesh(Camera[] renderingCameras, int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) : base(renderingCameras, subMesh, group, slot, transform, floatOverrides, colorOverrides, vectorOverrides)
        {}

        internal override InstancedRendering.IRenderingInstance CreateRenderingInstance(int cameraIndex, int levelOfDetail)
        {
            var localToWorld = boundTransform == null ? Matrix4x4.identity : boundTransform.localToWorldMatrix;
            return group.CreateNewInstance(renderingCameras[cameraIndex], cameraIndex > 0, subMesh, levelOfDetail, new InstancedRendering.InstanceDataMatrixAndMotionVectors()
            {
                objectToWorld = localToWorld,
                prevObjectToWorld = localToWorld
            }, floatOverrides == null ? null : floatOverrides[cameraIndex], colorOverrides == null ? null : colorOverrides[cameraIndex], vectorOverrides == null ? null : vectorOverrides[cameraIndex]);
        }

        /// <summary>
        /// Overrides objectToWorld AND prevObjectToWorld
        /// </summary>
        internal override void SetInstanceDataFromBoundTransform()
        {
            if (renderInstances == null || boundTransform == null) return;

            var localToWorld = boundTransform.localToWorldMatrix;
            foreach (var renderInstance in renderInstances)
            {
                if (renderInstance == null || renderInstance.instance == null) continue;

                ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors>)renderInstance.instance).SetData((new InstancedRendering.InstanceDataMatrixAndMotionVectors()
                {
                    objectToWorld = localToWorld,
                    prevObjectToWorld = localToWorld
                }));
            }
        }

        public override void SyncWithTransform()
        {
            if (renderInstances == null || boundTransform == null) return;

            Matrix4x4 l2w = boundTransform.localToWorldMatrix;
            foreach (var renderInstance in renderInstances)
            {
                if (renderInstance == null || renderInstance.instance == null) continue;

                var ri = ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixAndMotionVectors>)renderInstance.instance);
                var data = ri.GetData();
                data.prevObjectToWorld = data.objectToWorld;
                data.objectToWorld = l2w;
                ri.SetData(data);
            }
        }
    }
    public class InstancedMeshMO : InstancedMeshBase
    {

        public InstancedMeshMO(Camera[] renderingCameras, int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) : base(renderingCameras, subMesh, group, slot, transform, floatOverrides, colorOverrides, vectorOverrides)
        { }

        internal override InstancedRendering.IRenderingInstance CreateRenderingInstance(int cameraIndex, int levelOfDetail)
        {
            var localToWorld = boundTransform == null ? Matrix4x4.identity : boundTransform.localToWorldMatrix;
            return group.CreateNewInstanceMO(renderingCameras[cameraIndex], cameraIndex > 0, subMesh, levelOfDetail, new InstancedRendering.InstanceDataMatrixOnly()
            {
                objectToWorld = localToWorld
            }, floatOverrides == null ? null : floatOverrides[cameraIndex], colorOverrides == null ? null : colorOverrides[cameraIndex], vectorOverrides == null ? null : vectorOverrides[cameraIndex]);
        }

        internal override void SetInstanceDataFromBoundTransform()
        {
            if (boundTransform == null || renderInstances == null) return;

            var localToWorld = boundTransform.localToWorldMatrix;
            foreach (var renderInstance in renderInstances)
            {
                if (renderInstance == null || renderInstance.instance == null) continue;

                ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly>)renderInstance.instance).SetData((new InstancedRendering.InstanceDataMatrixOnly()
                {
                    objectToWorld = localToWorld,
                }));
            }
        }

        public override void SyncWithTransform()
        {
            if (renderInstances == null || boundTransform == null) return;

            Matrix4x4 l2w = boundTransform.localToWorldMatrix;
            foreach (var renderInstance in renderInstances)
            {
                if (renderInstance == null || renderInstance.instance == null) continue;

                var ri = ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataMatrixOnly>)renderInstance.instance);
                var data = ri.GetData();
                data.objectToWorld = l2w;
                ri.SetData(data);
            }
        }
    }
    public class InstancedMeshRL : InstancedMeshBase
    {

        protected uint renderingLayerMask;
        public uint RenderingLayerMask => renderingLayerMask;

        public InstancedMeshRL(Camera[] renderingCameras, uint renderingLayerMask, int subMesh, InstancedMeshGroup group, int slot, Transform transform = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides = null,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides = null) : base(renderingCameras, subMesh, group, slot, transform, floatOverrides, colorOverrides, vectorOverrides)
        {
            this.renderingLayerMask = renderingLayerMask;
        }

        internal override InstancedRendering.IRenderingInstance CreateRenderingInstance(int cameraIndex, int levelOfDetail)
        {
            var localToWorld = boundTransform == null ? Matrix4x4.identity : boundTransform.localToWorldMatrix;
            return group.CreateNewInstanceRL(renderingCameras[cameraIndex], cameraIndex > 0, subMesh, levelOfDetail, new InstancedRendering.InstanceDataFull() 
            {
                objectToWorld = localToWorld,
                prevObjectToWorld = localToWorld,
                renderingLayerMask = renderingLayerMask
            }, floatOverrides == null ? null : floatOverrides[cameraIndex], colorOverrides == null ? null : colorOverrides[cameraIndex], vectorOverrides == null ? null : vectorOverrides[cameraIndex]); 
        }

        /// <summary>
        /// Overrides objectToWorld AND prevObjectToWorld
        /// </summary>
        internal override void SetInstanceDataFromBoundTransform()
        {
            if (boundTransform == null || renderInstances == null) return;

            var localToWorld = boundTransform.localToWorldMatrix;
            foreach (var renderInstance in renderInstances)
            {
                if (renderInstance == null || renderInstance.instance == null) continue;

                ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull>)renderInstance.instance).SetData((new InstancedRendering.InstanceDataFull() 
                {
                    objectToWorld = localToWorld,
                    prevObjectToWorld = localToWorld,
                    renderingLayerMask = renderingLayerMask
                }));
            }
        }

        public override void SyncWithTransform()
        {
            if (renderInstances == null || boundTransform == null) return;

            Matrix4x4 l2w = boundTransform.localToWorldMatrix;
            foreach (var renderInstance in renderInstances)
            {
                if (renderInstance == null || renderInstance.instance == null) continue; 

                var ri = ((InstancedRendering.RenderingInstance<InstancedRendering.InstanceDataFull>)renderInstance.instance);
                var data = ri.GetData();
                data.prevObjectToWorld = data.objectToWorld;
                data.objectToWorld = l2w;
                ri.SetData(data);
            }
        }
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
                for (int a = 0; a < meshGroups.Length; a++)
                {
                    if (meshGroups[a] != null)
                    {
                        var group = meshGroups[a];
                        group.Initialize(this);
                        
                        group.SetMaterialFloat(VertexCountPropertyName, VertexCount);
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

        [NonSerialized]
        protected CullingLODs.LOD[] lods;
        public CullingLODs.LOD[] LODs
        {
            get
            {
                if (lods == null)
                {
                    var lodsList = new List<CullingLODs.LOD>(LevelsOfDetail);
                    for (int a = 0; a < LevelsOfDetail; a++) lodsList.Add(new CullingLODs.LOD() { detailLevel = a, screenRelativeTransitionHeight = meshLODs[a].screenRelativeTransitionHeight }); 
                    lodsList.Sort(CullingLODs.SortLODsDescending); 

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

        public virtual List<BoneWeight8Float> GetConvertedBoneWeightData(List<BoneWeight8Float> outputList = null)
        {
            if (outputList == null) outputList = new List<BoneWeight8Float>();

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

                            outputList.Add(data);
                            boneWeightIndex += boneCount;
                        }
                    }
                }
            }

            return outputList;
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

                    GetConvertedBoneWeightData(tempBoneWeights);

                    boneWeightsBuffer = new ComputeBuffer(tempBoneWeights.Count, UnsafeUtility.SizeOf(typeof(BoneWeight8Float)), ComputeBufferType.Structured, ComputeBufferMode.Immutable);
                    if (tempBoneWeights.Count > 0) boneWeightsBuffer.SetData(tempBoneWeights);

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

        public virtual void ApplyBufferToMaterials(string propertyName, ICollection<int> materialSlots, ComputeBuffer buffer)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        group.SetMaterialBuffer(propertyName, buffer);
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            group.SetMaterialBuffer(index, propertyName, buffer);
                        }
                    }
                }
            }
        }
        public void ApplyBufferToMaterials(string propertyName, ComputeBuffer buffer) => ApplyBufferToMaterials(propertyName, null, buffer);

        public virtual void ApplyFloatToMaterials(string propertyName, ICollection<int> materialSlots, float value)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        group.SetMaterialFloat(propertyName, value);
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            group.SetMaterialFloat(index, propertyName, value);
                        }
                    }
                }
            }
        }
        public void ApplyFloatToMaterials(string propertyName, float value) => ApplyFloatToMaterials(propertyName, null, value);

        public virtual void ApplyIntegerToMaterials(string propertyName, ICollection<int> materialSlots, int value)
        {
            if (meshGroups != null)
            {
                if (materialSlots == null)
                {
                    foreach (var group in meshGroups)
                    {
                        group.SetMaterialInteger(propertyName, value);
                    }
                }
                else
                {
                    foreach (var group in meshGroups)
                    {
                        foreach (var index in materialSlots)
                        {
                            group.SetMaterialInteger(index, propertyName, value);
                        }
                    }
                }
            }
        }
        public void ApplyIntegerToMaterials(string propertyName, int value) => ApplyIntegerToMaterials(propertyName, null, value);

        public virtual void ApplyVectorToMaterials(string propertyName, ICollection<int> materialSlots, Vector4 value)
        {
            if (meshGroups != null)
            {
                if (meshGroups != null)
                {
                    if (materialSlots == null)
                    {
                        foreach (var group in meshGroups)
                        {
                            group.SetMaterialVector(propertyName, value);
                        }
                    }
                    else
                    {
                        foreach (var group in meshGroups)
                        {
                            foreach (var index in materialSlots)
                            {
                                group.SetMaterialVector(index, propertyName, value);
                            }
                        }
                    }
                }
            }
        }
        public void ApplyVectorToMaterials(string propertyName, Vector4 value) => ApplyVectorToMaterials(propertyName, null, value);

        public virtual void ApplyColorToMaterials(string propertyName, ICollection<int> materialSlots, Color value)
        {
            if (meshGroups != null)
            {
                if (meshGroups != null)
                {
                    if (materialSlots == null)
                    {
                        foreach (var group in meshGroups)
                        {
                            group.SetMaterialColor(propertyName, value);
                        }
                    }
                    else
                    {
                        foreach (var group in meshGroups)
                        {
                            foreach (var index in materialSlots)
                            {
                                group.SetMaterialColor(index, propertyName, value);
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
        public virtual Matrix4x4[] ManagedBindPose
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