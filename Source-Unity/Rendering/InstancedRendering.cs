#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Collections;
using Unity.Mathematics;

namespace Swole
{

    public class InstancedRendering : SingletonBehaviour<InstancedRendering>
    {

        public override int Priority => 90;

        public static RenderParams GetDefaultRenderParams()
        {

            return new RenderParams()
            {

                camera = null,
                layer = 0,
                renderingLayerMask = GraphicsSettings.defaultRenderingLayerMask,
                rendererPriority = 0,
                worldBounds = new Bounds(Vector3.zero, Vector3.zero),
                lightProbeProxyVolume = null,
                lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off,
                material = null,
                matProps = null,
                motionVectorMode = MotionVectorGenerationMode.Camera,
                receiveShadows = true,
                reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off,
                shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On

            };

        }

        public static bool CheckRenderParamsEquality(RenderParams paramsA, RenderParams paramsB)
        {

            if (paramsA.layer != paramsB.layer) return false;
            if (paramsA.renderingLayerMask != paramsB.renderingLayerMask) return false;
            if (paramsA.rendererPriority != paramsB.rendererPriority) return false;
            if (paramsA.camera != paramsB.camera) return false;
            if (paramsA.motionVectorMode != paramsB.motionVectorMode) return false;
            if (paramsA.reflectionProbeUsage != paramsB.reflectionProbeUsage) return false;
            if (paramsA.material != paramsB.material) return false;
            if (paramsA.shadowCastingMode != paramsB.shadowCastingMode) return false;
            if (paramsA.receiveShadows != paramsB.receiveShadows) return false;
            if (paramsA.lightProbeUsage != paramsB.lightProbeUsage) return false;

            return true;

        }

        public class MaterialPropertyOverride<T>
        {

            public string propertyName;

            public T defaultValue;

            protected IList values;
            public int Count => values.Count;

            public MaterialPropertyOverride(string propertyName, T defaultValue)
            {

                this.propertyName = propertyName;
                this.defaultValue = defaultValue;

                values = new List<T>();

            }

            public void Add(T value) => values.Add(value);
            public void Insert(T value, int index)
            {

                index = Mathf.Max(0, index);

                if (index >= Count) Add(value); else values.Insert(index, value);

            }
            public void RemoveAt(int index) { if (index >= 0 && index < values.Count) values.RemoveAt(index); }

            public void SetCount(int count)
            {

                count = Mathf.Max(0, count);
                while (Count < count) Add(defaultValue);
                while (Count > count) RemoveAt(Count - 1);

            }
            public void SetValue(int index, T value)
            {
                if (index < 0 || index >= Count) return;
                values[index] = value;
            }

            public bool Override(MaterialPropertyBlock block)
            {

                if (string.IsNullOrEmpty(propertyName) || values == null) return true;

                if (typeof(T) == typeof(float))
                {

                    var existingArray = block.GetFloatArray(propertyName);
                    if (existingArray != null && existingArray.Length < values.Count) return false;

                    block.SetFloatArray(propertyName, (List<float>)values);

                    return true;

                }
                else if (typeof(T) == typeof(Vector4))
                {

                    var existingArray = block.GetFloatArray(propertyName);

                    if (existingArray != null && existingArray.Length < values.Count) return false;

                    block.SetVectorArray(propertyName, (List<Vector4>)values);

                    return true;

                }

                return true;

            }

        }

        public struct MaterialPropertyInstanceOverride<T>
        {

            public string propertyName;
            public T value;

            public MaterialPropertyInstanceOverride(string propertyName, T value)
            {

                this.propertyName = propertyName;
                this.value = value;

            }

        }

        public const int _maxInstancesPerCall = 1023;

        public class RenderSequence
        {

            private List<SubRenderSequence> subSequences;
            public int SubSequenceCount => subSequences.Count;

            public SubRenderSequence this[int index] => index < 0 || index >= subSequences.Count ? null : subSequences[index];

            public void FindNextSequenceSlotIndex(out int subSequenceIndex, out int index)
            {

                subSequenceIndex = -1;
                index = 0;

                for (int a = 0; a < subSequences.Count; a++)
                {

                    var sequence = subSequences[a];
                    int count = sequence.Count;
                    if (count < _maxInstancesPerCall)
                    {

                        subSequenceIndex = a;
                        index = count;

                        return;

                    }

                }

            }

            public void FindNextSequenceSlot(out SubRenderSequence sequence, out int index)
            {

                FindNextSequenceSlotIndex(out int subIndex, out index);

                if (subIndex < 0)
                {

                    subIndex = subSequences.Count;
                    subSequences.Add(new SubRenderSequence(this, subIndex, renderParams));
                    index = 0;

                }

                sequence = subSequences[subIndex];

            }

            public int Count
            {

                get
                {

                    int count = 0;

                    for (int a = 0; a < subSequences.Count; a++) count += subSequences[a].Count;

                    return count;

                }

            }

            private RenderParams renderParams;
            public RenderParams RenderParams => renderParams;
            public void SetRenderParams(RenderParams renderParams)
            {

                this.renderParams = renderParams;
                foreach (var sequence in subSequences) sequence.SetRenderParams(renderParams);

            }

            public List<MaterialPropertyInstanceOverride<float>> globalFloatPropertyOverrides;
            public List<MaterialPropertyInstanceOverride<Vector4>> globalVectorPropertyOverrides;
            public List<MaterialPropertyInstanceOverride<Color>> globalColorPropertyOverrides;

            public RenderSequence(RenderParams renderParams)
            {

                subSequences = new List<SubRenderSequence>() { new SubRenderSequence(this, 0, renderParams) };

                this.renderParams = renderParams;

            }

            public bool RefreshIfDirty()
            {

                bool refreshed = false;

                foreach (var subSequence in subSequences) if (subSequence.RefreshIfDirty()) refreshed = true;

                return refreshed;

            }

            public void OverrideGlobalFloatProperty(string propertyName, float value)
            {

                if (string.IsNullOrEmpty(propertyName)) return;
                if (globalFloatPropertyOverrides == null) globalFloatPropertyOverrides = new List<MaterialPropertyInstanceOverride<float>>();

                globalFloatPropertyOverrides.Add(new MaterialPropertyInstanceOverride<float>() { propertyName = propertyName, value = value });

            }

            public void OverrideGlobalVectorProperty(string propertyName, Vector4 value)
            {

                if (string.IsNullOrEmpty(propertyName)) return;
                if (globalVectorPropertyOverrides == null) globalVectorPropertyOverrides = new List<MaterialPropertyInstanceOverride<Vector4>>();

                globalVectorPropertyOverrides.Add(new MaterialPropertyInstanceOverride<Vector4>() { propertyName = propertyName, value = value });

            }

            public void OverrideGlobalColorProperty(string propertyName, Color value)
            {

                if (string.IsNullOrEmpty(propertyName)) return;
                if (globalColorPropertyOverrides == null) globalColorPropertyOverrides = new List<MaterialPropertyInstanceOverride<Color>>();

                globalColorPropertyOverrides.Add(new MaterialPropertyInstanceOverride<Color>() { propertyName = propertyName, value = value });

            }

            public int2 GetSubIndex(int index)
            {

                if (index < 0 || index >= Count) return -1;

                int i = 0;
                for (int a = 0; a < SubSequenceCount; a++)
                {

                    var sequence = subSequences[a];

                    if (index < i + sequence.Count)
                    {

                        return new int2(a, index - i);

                    }

                    i += sequence.Count;

                }

                return -1;

            }

            public int2 AddMember(ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {

                FindNextSequenceSlot(out var sequence, out int index);

                index = sequence.AddMember(instanceFloatPropertyOverrides, instanceColorPropertyOverrides, instanceVectorPropertyOverrides);

                return new int2(sequence.Index, index);

            }

            public int2 InsertMember(int index, ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {

                int2 subIndex = GetSubIndex(index);
                if (subIndex.x < 0 || subSequences[subIndex.x].Count >= _maxInstancesPerCall) return AddMember(instanceFloatPropertyOverrides, instanceColorPropertyOverrides, instanceVectorPropertyOverrides);

                subSequences[subIndex.x].InsertMember(subIndex.y, instanceFloatPropertyOverrides, instanceColorPropertyOverrides, instanceVectorPropertyOverrides);
                return subIndex;

            }
            public void SetMaterialPropertyOverrides(int index, ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {
                SetMaterialPropertyOverrides(GetSubIndex(index), instanceFloatPropertyOverrides, instanceColorPropertyOverrides, instanceVectorPropertyOverrides);
            }
            public void SetMaterialPropertyOverrides(int2 subIndex, ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {
                if (subIndex.x < 0 || subIndex.x >= SubSequenceCount) return;
                subSequences[subIndex.x].SetMaterialPropertyOverrides(subIndex.y, instanceFloatPropertyOverrides, instanceColorPropertyOverrides, instanceVectorPropertyOverrides);
            }
            public void ResetMaterialPropertyOverrides(int index)
            {
                ResetMaterialPropertyOverrides(GetSubIndex(index));
            }
            public void ResetMaterialPropertyOverrides(int2 subIndex)
            {
                if (subIndex.x < 0 || subIndex.x >= SubSequenceCount) return;
                subSequences[subIndex.x].ResetMaterialPropertyOverrides(subIndex.y);
            }

            public void RemoveMember(int index)
            {

                int2 subIndex = GetSubIndex(index);

                RemoveMember(subIndex);

            }

            public void RemoveMember(int2 index)
            {

                if (index.x < 0 || index.x >= SubSequenceCount) return;

                var sequence = subSequences[index.x];
                sequence.RemoveMember(index.y);

            }

            public bool Override(MaterialPropertyBlock block)
            {

                if (block == null) return false;

                if (globalFloatPropertyOverrides != null) foreach (var property in globalFloatPropertyOverrides) block.SetFloat(property.propertyName, property.value);
                if (globalVectorPropertyOverrides != null) foreach (var property in globalVectorPropertyOverrides) block.SetVector(property.propertyName, property.value);
                if (globalColorPropertyOverrides != null) foreach (var property in globalColorPropertyOverrides) block.SetColor(property.propertyName, property.value);

                return true;

            }

            public void RefreshMaterialPropertyBlocks()
            {

                foreach (var sequence in subSequences) sequence.RefreshMaterialPropertyBlock();

            }

        }

        public class SubRenderSequence
        {

            private RenderSequence sequence;
            private int subIndex;
            public int Index => subIndex;

            private bool dirty;
            public bool IsDirty => dirty;
            public void SetDirty() => dirty = true;

            private int count;
            public int Count => count;
            private RenderParams renderParams;
            public RenderParams RenderParams => renderParams;
            public void SetRenderParams(RenderParams renderParams)
            {

                renderParams.matProps = this.renderParams.matProps;
                this.renderParams = renderParams;

            }

            private Dictionary<string, MaterialPropertyOverride<float>> floatPropertyOverrides;
            private Dictionary<string, MaterialPropertyOverride<Vector4>> vectorPropertyOverrides;

            public SubRenderSequence(RenderSequence sequence, int subIndex, RenderParams renderParams)
            {

                this.sequence = sequence;
                this.subIndex = subIndex;

                this.renderParams = renderParams;

                floatPropertyOverrides = new Dictionary<string, MaterialPropertyOverride<float>>();
                vectorPropertyOverrides = new Dictionary<string, MaterialPropertyOverride<Vector4>>();

            }

            public bool RefreshIfDirty()
            {

                if (!IsDirty) return false;

                RefreshMaterialPropertyBlock();

                dirty = false;
                return true;

            }

            public int AddMember(ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {

                foreach (var override_ in floatPropertyOverrides) override_.Value.SetCount(Count);
                foreach (var override_ in vectorPropertyOverrides) override_.Value.SetCount(Count);

                if (instanceFloatPropertyOverrides != null) foreach (var prop in instanceFloatPropertyOverrides) GetOverrideFloat(prop.propertyName, renderParams.material.GetFloat(prop.propertyName))?.Add(prop.value);
                if (instanceColorPropertyOverrides != null) foreach (var prop in instanceColorPropertyOverrides) GetOverrideVector(prop.propertyName, renderParams.material.GetColor(prop.propertyName).AsLinearColorVector())?.Add(prop.value.AsLinearColorVector());
                if (instanceVectorPropertyOverrides != null) foreach (var prop in instanceVectorPropertyOverrides) GetOverrideVector(prop.propertyName, renderParams.material.GetVector(prop.propertyName))?.Add(prop.value);
                SetDirty();
                count++;
                return count - 1;
            }

            public void InsertMember(int index, ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {

                foreach (var override_ in floatPropertyOverrides) override_.Value.SetCount(Count);
                foreach (var override_ in vectorPropertyOverrides) override_.Value.SetCount(Count);

                if (instanceFloatPropertyOverrides != null) foreach (var prop in instanceFloatPropertyOverrides) GetOverrideFloat(prop.propertyName, renderParams.material.GetFloat(prop.propertyName))?.Insert(prop.value, index);
                if (instanceColorPropertyOverrides != null) foreach (var prop in instanceColorPropertyOverrides) GetOverrideVector(prop.propertyName, renderParams.material.GetColor(prop.propertyName).AsLinearColorVector())?.Insert(prop.value.AsLinearColorVector(), index);
                if (instanceVectorPropertyOverrides != null) foreach (var prop in instanceVectorPropertyOverrides) GetOverrideVector(prop.propertyName, renderParams.material.GetVector(prop.propertyName))?.Insert(prop.value, index);
                SetDirty();
                count++;
            }
            public void SetMaterialPropertyOverrides(int index, ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {

                foreach (var override_ in floatPropertyOverrides) override_.Value.SetCount(Count);
                foreach (var override_ in vectorPropertyOverrides) override_.Value.SetCount(Count);

                if (instanceFloatPropertyOverrides != null) foreach (var prop in instanceFloatPropertyOverrides) GetOverrideFloat(prop.propertyName, renderParams.material.GetFloat(prop.propertyName))?.SetValue(index, prop.value);
                if (instanceColorPropertyOverrides != null) foreach (var prop in instanceColorPropertyOverrides) GetOverrideVector(prop.propertyName, renderParams.material.GetColor(prop.propertyName).AsLinearColorVector())?.SetValue(index, prop.value.AsLinearColorVector());
                if (instanceVectorPropertyOverrides != null) foreach (var prop in instanceVectorPropertyOverrides) GetOverrideVector(prop.propertyName, renderParams.material.GetVector(prop.propertyName))?.SetValue(index, prop.value);
                SetDirty();
            }
            public void ResetMaterialPropertyOverrides(int index)
            {

                foreach (var override_ in floatPropertyOverrides)
                {

                    override_.Value.SetCount(Count);
                    override_.Value.SetValue(index, override_.Value.defaultValue);

                }
                foreach (var override_ in vectorPropertyOverrides)
                {

                    override_.Value.SetCount(Count);
                    override_.Value.SetValue(index, override_.Value.defaultValue);

                }

                SetDirty();
            }

            public void RemoveMember(int index)
            {

                if (index < 0 || index >= Count) return;

                foreach (var override_ in floatPropertyOverrides) override_.Value.RemoveAt(index);
                foreach (var override_ in vectorPropertyOverrides) override_.Value.RemoveAt(index);
                SetDirty();
                count--;
            }

            public bool Override(MaterialPropertyBlock block)
            {

                if (block == null) return false;

                foreach (var property in floatPropertyOverrides) if (!property.Value.Override(block)) return false;
                foreach (var property in vectorPropertyOverrides) if (!property.Value.Override(block)) return false;

                return sequence.Override(block);

            }

            public void RefreshMaterialPropertyBlock()
            {

                var block = renderParams.matProps;
                if (block == null) block = new MaterialPropertyBlock();

                if (!Override(block))
                {

                    block = new MaterialPropertyBlock();
                    Override(block);

                }

                renderParams.matProps = block;

            }

            public MaterialPropertyOverride<float> GetOverrideFloat(string propertyName, float defaultValue = 0)
            {

                if (string.IsNullOrEmpty(propertyName)) return null;

                if (floatPropertyOverrides.TryGetValue(propertyName, out MaterialPropertyOverride<float> propertyOverride)) return propertyOverride;

                floatPropertyOverrides[propertyName] = propertyOverride = new MaterialPropertyOverride<float>(propertyName, defaultValue);
                propertyOverride.SetCount(Count);

                return propertyOverride;

            }

            public MaterialPropertyOverride<Vector4> GetOverrideVector(string propertyName, Vector4 defaultValue = default)
            {

                if (string.IsNullOrEmpty(propertyName)) return null;

                if (vectorPropertyOverrides.TryGetValue(propertyName, out MaterialPropertyOverride<Vector4> propertyOverride)) return propertyOverride;

                vectorPropertyOverrides[propertyName] = propertyOverride = new MaterialPropertyOverride<Vector4>(propertyName, defaultValue);
                propertyOverride.SetCount(Count);

                return propertyOverride;

            }

        }

        public class RenderingInstance<T> : IDisposable where T : unmanaged
        {

            public RenderingInstance(RenderGroup<T> renderGroup, int index, int subSequenceIndex, int subIndex)
            {

                this.renderGroup = renderGroup;
                this.index = index;
                this.subSequenceIndex = subSequenceIndex;
                this.subIndex = subIndex;

            }

            private RenderGroup<T> renderGroup;

            public int index;

            private int subSequenceIndex;
            public int SubSequenceIndex => subSequenceIndex;
            public int subIndex;

            public bool Valid => renderGroup != null;

            public void Dispose()
            {
                if (!destroyed) Destroy();
                index = -1;
                subSequenceIndex = -1;
                subIndex = -1;
                renderGroup = null;

            }

            public bool SetData(T data)
            {
                if (renderGroup == null) return false;
                return renderGroup.SetInstanceData(this, data);
            }

            private bool destroyed;
            public void Destroy()
            {

                destroyed = true;
                if (renderGroup != null) renderGroup.DestroyInstance(this); else Dispose();

            }

        }

        public override void OnDestroyed()
        {

            base.OnDestroyed();

            if (renderGroups != null)
            {

                foreach (IRenderGroup group in renderGroups) group?.Dispose();

                renderGroups.Clear();

            }

        }

        [Serializable]
        public struct InstanceDataFull
        {

            /// <summary>
            /// mandatory: Specifies object-to-world transformation matrix.
            /// </summary>
            public Matrix4x4 objectToWorld;

            /// <summary>
            /// optional: Specifies rendering layer mask per instance. If not defined, uses the renderLayerMask passed in RenderParams.
            /// </summary>
            public uint renderingLayerMask;

            /// <summary>
            /// optional: Specifies previous frame object-to-world transformation matrix (used for motion vector rendering).
            /// </summary>
            public Matrix4x4 prevObjectToWorld;

        }

        [Serializable]
        public struct InstanceDataMatrixAndMotionVectors
        {

            /// <summary>
            /// mandatory: Specifies object-to-world transformation matrix.
            /// </summary>
            public Matrix4x4 objectToWorld;

            /// <summary>
            /// optional: Specifies previous frame object-to-world transformation matrix (used for motion vector rendering).
            /// </summary>
            public Matrix4x4 prevObjectToWorld;

        }

        [Serializable]
        public struct InstanceDataMatrixOnly
        {

            /// <summary>
            /// mandatory: Specifies object-to-world transformation matrix.
            /// </summary>
            public Matrix4x4 objectToWorld;

        }

        public interface IRenderGroup
        {

            public Type GetInstanceDataType();

            public Mesh Mesh { get; }

            public int SubmeshIndex { get; }

            public bool Render();

            public void Dispose();

        }

        public class RenderGroup<T> : IRenderGroup where T : unmanaged
        {

            public Type GetInstanceDataType() => typeof(T);

            protected Mesh mesh;
            public Mesh Mesh => mesh;

            protected int submeshIndex;
            public int SubmeshIndex => submeshIndex;

            public List<RenderSequence> renderSequences;

            public int AddOrGetRenderSequence(Material material)
            {

                var renderParams = GetDefaultRenderParams();
                renderParams.material = material;

                return AddOrGetRenderSequence(renderParams);

            }

            public int AddOrGetRenderSequence(RenderParams renderParams)
            {

                if (renderParams.material == null) return -1;
                if (renderSequences == null) renderSequences = new List<RenderSequence>();
                for (int a = 0; a < renderSequences.Count; a++) if (CheckRenderParamsEquality(renderSequences[a].RenderParams, renderParams)) return a;

                renderSequences.Add(new RenderSequence(renderParams));

                return renderSequences.Count - 1;

            }

            protected NativeList<T> instanceData;
            public NativeArray<T> InstanceData => instanceData.AsArray();

            protected List<RenderingInstance<T>> indexReferences;
            protected int lastCount;
            protected bool refreshIndices;
            protected int refreshStartIndex;

            public RenderingInstance<T> AddNewInstance(Material material, T initialData, bool refreshRenderIndices = true, bool overrideRenderParams = false, RenderParams renderParams = default, ICollection<MaterialPropertyInstanceOverride<float>> instanceFloatPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Color>> instanceColorPropertyOverrides = null, ICollection<MaterialPropertyInstanceOverride<Vector4>> instanceVectorPropertyOverrides = null)
            {

                if (material == null || !instanceData.IsCreated || indexReferences == null) return null;

                renderParams.material = material;

                int renderSequenceIndex;

                if (overrideRenderParams) renderSequenceIndex = AddOrGetRenderSequence(renderParams); else renderSequenceIndex = AddOrGetRenderSequence(material);
                if (renderSequenceIndex < 0) return null;

                var sequence = renderSequences[renderSequenceIndex];

                int instanceIndex = 0;
                for (int a = 0; a < renderSequenceIndex; a++) instanceIndex += renderSequences[a].Count;
                int startIndex = instanceIndex;
                instanceIndex = startIndex + sequence.Count;

                int2 subIndex = sequence.AddMember(instanceFloatPropertyOverrides, instanceColorPropertyOverrides, instanceVectorPropertyOverrides);
                var instance = new RenderingInstance<T>(this, instanceIndex, subIndex.x, subIndex.y);

                if (instanceIndex >= instanceData.Length)
                {

                    instance.index = instanceData.Length;
                    instanceData.Add(initialData);
                    indexReferences.Add(instance);

                }
                else
                {

                    instanceData.InsertRangeWithBeginEnd(instanceIndex, instanceIndex + 1);
                    instanceData[instanceIndex] = initialData;
                    indexReferences.Insert(instanceIndex, instance);

                    if (refreshRenderIndices) RefreshRenderIndices(instanceIndex + 1);

                }

                return instance;

            }

            public bool SetInstanceData(RenderingInstance<T> instance, T data)
            {

                if (!instanceData.IsCreated || instance == null || !instance.Valid || instance.index >= instanceData.Length) return false;

                instanceData[instance.index] = data;

                return true;

            }

            public void DestroyInstance(RenderingInstance<T> instance, bool refreshRenderIndices = true)
            {

                if (!instanceData.IsCreated || instance == null || !instance.Valid || instance.index >= instanceData.Length) return;

                int sequenceIndex = 0;
                int instanceIndexRange = 0;
                int instanceIndexStart = 0;
                for (int a = 0; a < renderSequences.Count; a++)
                {
                    if (instance.index < instanceIndexRange) break;
                    sequenceIndex = a;
                    instanceIndexStart = instanceIndexRange;
                    if (instanceIndexRange == instance.index) break;
                    instanceIndexRange += renderSequences[a].Count;
                }

                var sequence = renderSequences[sequenceIndex];

                indexReferences.RemoveAt(instance.index);
                instanceData.RemoveAt(instance.index);
                sequence.RemoveMember(instance.index - instanceIndexStart);

                if (refreshRenderIndices) RefreshRenderIndices(instance.index);

                instance.Dispose();

            }

            public void Dispose()
            {

                if (instanceData.IsCreated) instanceData.Dispose();
                instanceData = default;

                if (indexReferences != null)
                {
                    foreach (RenderingInstance<T> index in indexReferences) if (index != null && index.Valid) index.Dispose();
                }
                indexReferences = null;

            }

            public RenderGroup(Mesh mesh, int submeshIndex, List<RenderSequence> renderSequences, NativeList<T> instanceData)
            {

                this.mesh = mesh;
                this.submeshIndex = submeshIndex;
                this.renderSequences = renderSequences;
                this.instanceData = instanceData;

                indexReferences = new List<RenderingInstance<T>>();

            }

            public RenderGroup(Mesh mesh, int submeshIndex, NativeList<T> instanceData)
            {

                this.mesh = mesh;
                this.submeshIndex = submeshIndex;
                this.instanceData = instanceData;

                renderSequences = new List<RenderSequence>();
                indexReferences = new List<RenderingInstance<T>>();

            }

            public RenderGroup(Mesh mesh, int submeshIndex)
            {

                this.mesh = mesh;
                this.submeshIndex = submeshIndex;

                renderSequences = new List<RenderSequence>();
                indexReferences = new List<RenderingInstance<T>>();
                instanceData = new NativeList<T>(0, Allocator.Persistent);

            }

            public void RefreshRenderIndices(int startIndex = 0)
            {

                if (indexReferences == null) return;
                startIndex = Mathf.Max(0, startIndex);
                for (int a = startIndex; a < indexReferences.Count; a++) indexReferences[a].index = a;

                lastCount = instanceData.Length;

            }

            public bool Render()
            {

                if (mesh == null || renderSequences == null || !instanceData.IsCreated) return false;

                if (indexReferences != null)
                {

                    if (!refreshIndices && lastCount > instanceData.Length)
                    {
                        refreshIndices = true;
                        refreshStartIndex = 0;
                    }

                    if (refreshIndices)
                    {

                        RefreshRenderIndices(refreshStartIndex);

                        refreshIndices = false;
                        refreshStartIndex = 0;

                    }

                }

                int i = 0;
                for (int s = 0; s < renderSequences.Count; s++)
                {

                    var sequence = renderSequences[s];
                    sequence.RefreshIfDirty();

                    for (int ss = 0; ss < sequence.SubSequenceCount; ss++)
                    {

                        var subSequence = sequence[ss];
                        int count = Mathf.Clamp(subSequence.Count, 0, _maxInstancesPerCall);
                        if (count == 0) continue;

                        Graphics.RenderMeshInstanced(
                            subSequence.RenderParams,
                            mesh,
                            submeshIndex,
                            InstanceData,
                            count,
                            i);

                        i += count;

                    }

                }

                return true;

            }

            public RenderGroup<T> SetCamera(Camera camera, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.camera = camera;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetLayer(int layer, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.layer = layer;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetLightProbeProxyVolume(LightProbeProxyVolume proxyVolume, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.lightProbeProxyVolume = proxyVolume;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetLightProbeUsage(UnityEngine.Rendering.LightProbeUsage usage, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.lightProbeUsage = usage;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetMaterial(Material material, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.material = material;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            /// <summary>
            /// Do not use. Override material properties using the available functions in the RenderSequence and SubRenderSequence classes instead.
            /// </summary>
            public RenderGroup<T> SetMaterialProperties(MaterialPropertyBlock properties, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.matProps = properties;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetMotionVectorMode(MotionVectorGenerationMode mode, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.motionVectorMode = mode;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetReceiveShadows(bool value, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.receiveShadows = value;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetReflectionProbeUsage(UnityEngine.Rendering.ReflectionProbeUsage value, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.reflectionProbeUsage = value;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetRendererPriority(int value, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.rendererPriority = value;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetShadowCastingMode(UnityEngine.Rendering.ShadowCastingMode value, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.shadowCastingMode = value;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

            public RenderGroup<T> SetWorldBounds(Bounds worldBounds, int renderParamsIndex = -1)
            {

                void SetValue(int i)
                {

                    var rParams = renderSequences[i].RenderParams;
                    rParams.worldBounds = worldBounds;
                    renderSequences[i].SetRenderParams(rParams);

                }

                if (renderParamsIndex < 0)
                {

                    for (int a = 0; a < renderSequences.Count; a++) SetValue(a);

                    return this;

                }

                if (renderParamsIndex >= renderSequences.Count) return this;

                SetValue(renderParamsIndex);

                return this;

            }

        }

        protected List<IRenderGroup> renderGroups = new List<IRenderGroup>();

        public bool TryGetRenderGroupLocal(Mesh mesh, int submeshIndex, out IRenderGroup renderGroup, out int renderGroupIndex)
        {

            renderGroup = null;
            renderGroupIndex = -1;

            for (int a = 0; a < renderGroups.Count; a++)
            {

                var group = renderGroups[a];

                if (group.Mesh == mesh && group.SubmeshIndex == submeshIndex)
                {

                    renderGroup = group;
                    renderGroupIndex = a;
                    return true;

                }

            }

            return false;

        }
        public bool TryGetRenderGroup(Mesh mesh, int submeshIndex, out IRenderGroup renderGroup, out int renderGroupIndex)
        {
            renderGroupIndex = -1;
            renderGroup = null;
            var instance = Instance;
            if (instance == null) return false;
            return instance.TryGetRenderGroupLocal(mesh, submeshIndex, out renderGroup, out renderGroupIndex);

        }

        public bool TryGetRenderGroupLocal(Mesh mesh, int submeshIndex, out IRenderGroup renderGroup)
        {

            return TryGetRenderGroupLocal(mesh, submeshIndex, out renderGroup, out _);

        }
        public bool TryGetRenderGroup(Mesh mesh, int submeshIndex, out IRenderGroup renderGroup)
        {
            renderGroup = null;
            var instance = Instance;
            if (instance == null) return false;
            return instance.TryGetRenderGroupLocal(mesh, submeshIndex, out renderGroup);

        }

        public bool TryGetRenderGroupLocal<T>(Mesh mesh, int submeshIndex, out RenderGroup<T> renderGroup, out int renderGroupIndex) where T : unmanaged
        {

            renderGroup = null;
            renderGroupIndex = -1;

            for (int a = 0; a < renderGroups.Count; a++)
            {

                var group = renderGroups[a];

                if (group.Mesh == mesh && group.SubmeshIndex == submeshIndex && group is RenderGroup<T>)
                {

                    renderGroup = (RenderGroup<T>)group;
                    renderGroupIndex = a;
                    return true;

                }

            }

            return false;

        }
        public bool TryGetRenderGroup<T>(Mesh mesh, int submeshIndex, out RenderGroup<T> renderGroup, out int renderGroupIndex) where T : unmanaged
        {
            renderGroupIndex = -1;
            renderGroup = null;
            var instance = Instance;
            if (instance == null) return false;
            return instance.TryGetRenderGroupLocal<T>(mesh, submeshIndex, out renderGroup, out renderGroupIndex);

        }
        public bool TryGetRenderGroupLocal<T>(Mesh mesh, int submeshIndex, out RenderGroup<T> renderGroup) where T : unmanaged
        {

            return TryGetRenderGroupLocal<T>(mesh, submeshIndex, out renderGroup, out _);

        }
        public bool TryGetRenderGroup<T>(Mesh mesh, int submeshIndex, out RenderGroup<T> renderGroup) where T : unmanaged
        {
            renderGroup = null;
            var instance = Instance;
            if (instance == null) return false;
            return instance.TryGetRenderGroupLocal<T>(mesh, submeshIndex, out renderGroup);

        }

        public RenderGroup<T> GetRenderGroupLocal<T>(Mesh mesh, int submeshIndex) where T : unmanaged
        {

            if (mesh == null) return null;
            if (TryGetRenderGroup<T>(mesh, submeshIndex, out RenderGroup<T> renderGroup)) return renderGroup;

            renderGroup = new RenderGroup<T>(mesh, submeshIndex);

            renderGroups.Add(renderGroup);

            return renderGroup;

        }

        public static RenderGroup<T> GetRenderGroup<T>(Mesh mesh, int submeshIndex) where T : unmanaged
        {

            var instance = Instance;
            if (instance == null) return null;
            return instance.GetRenderGroupLocal<T>(mesh, submeshIndex);

        }

        public RenderGroup<InstanceDataMatrixAndMotionVectors> GetDefaultRenderGroupLocal(Mesh mesh, int submeshIndex)
        {

            if (mesh == null) return null;
            if (TryGetRenderGroup<InstanceDataMatrixAndMotionVectors>(mesh, submeshIndex, out var renderGroup)) return renderGroup;

            renderGroup = new RenderGroup<InstanceDataMatrixAndMotionVectors>(mesh, submeshIndex);

            renderGroups.Add(renderGroup);

            return renderGroup;

        }

        public static RenderGroup<InstanceDataMatrixAndMotionVectors> GetDefaultRenderGroup(Mesh mesh, int submeshIndex)
        {

            var instance = Instance;
            if (instance == null) return null;
            return instance.GetDefaultRenderGroupLocal(mesh, submeshIndex);

        }

        public static void Render()
        {

            var instance = Instance;
            if (instance == null) return;
            instance.RenderLocal();

        }

        protected void RefreshRenderGroupCollection()
        {

            renderGroups.RemoveAll(i => i.Mesh == null);

        }

        protected void RenderLocal()
        {

            bool refresh = false;

            foreach (var renderGroup in renderGroups)
            {

                if (!renderGroup.Render()) refresh = true;

            }

            if (refresh) RefreshRenderGroupCollection();

        }

        public override void OnUpdate() { }

        public override void OnLateUpdate()
        {

            RenderLocal();

        }

        public override void OnFixedUpdate() { }

    }

}

#endif
