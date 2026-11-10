#if UNITY_2017_1_OR_NEWER

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

/*
 * ==========================================================================================
 *  CustomizableCharacterMeshV2 - Dynamic Shaping Backends
 * ==========================================================================================
 *
 *  Three interchangeable backends compute the per-instance final vertex deltas:
 *
 *   - ShapingBackend.CPUJobsLegacy : the original job chain (MeshGroupV2.BeginNewJobLegacy). Reference implementation.
 *   - ShapingBackend.CPUJobs       : (default) fused per-vertex "gather" Burst jobs. Same math, same intermediate buffers,
 *                                    same accumulation order per vertex, but 4 wide jobs instead of hundreds of serial batch-1 jobs.
 *   - ShapingBackend.GPUCompute    : compute kernels in bin/Shaders/Resources/CCMV2Shaping.compute that mirror the CPU gather jobs 1:1
 *                                    and write directly into the same _PerVertexDeltaData / _PerVertexPreviousDeltaData instance buffers.
 *                                    FinalVertexDeltas (CPU copy) is filled via async readback (1-2 frame latency, blocking for updateImmediately).
 *
 *  Toggle globally with CustomizableCharacterMeshV2.Backend.
 *
 *  CPU <-> GPU correspondence markers (search for these tags in both files):
 *   [CCMV2-SAMPLE]    MorphUtils.SampleDeltaShapeBuffer            <-> SampleShape()
 *   [CCMV2-NERF]      CustomizableCharacterMeshBase.CalculateBustNerfFactor <-> BustNerfFactor()
 *   [CCMV2-LR]        left/right/midline weighting                 <-> WeightLeftRight()
 *   [CCMV2-FAT]       FatGatherJob                                 <-> kernel FatGather
 *   [CCMV2-MUSCLE]    MuscleGatherJob                              <-> kernel MuscleGather
 *   [CCMV2-VARIATION] VariationGatherJob                           <-> kernel VariationGather
 *   [CCMV2-FINAL]     FinalizeGatherJob (+ breast)                 <-> kernel FinalizeGather
 *   [CCMV2-COPY]      InstanceBuffer.WriteToBuffer ranges          <-> kernel CopyDeltas
 *   [CCMV2-MAPS]      BuildShapingVertexEntryMaps                  (shared by both backends)
 *   [CCMV2-GPU-SYNC]  async readback of final deltas into FinalVertexDeltas
 *
 *  Data layout (identical on CPU and GPU, any change MUST be mirrored in the compute shader):
 *   MeshVertexDelta        : float3 position, float3 normal, float3 tangent  (36 bytes)
 *   MeshVertexDeltaLR      : MeshVertexDelta left, MeshVertexDelta right      (72 bytes)
 *   GroupControlWeight2    : int groupIndex, int vertexSequenceStartIndex, int vertexCount, float2 weight (20 bytes)
 *   GroupVertexControlWeight : int groupIndex, int vertexIndex, float weight  (12 bytes)
 *   Vertex entry maps      : ranges[vertex] = int2(start, count); entries[k] = int2(sequenceIndex or local offset, groupIndex)
 *
 *  NOTE: Per-vertex fat/muscle values are indexed per instance ((meshIndex * vertexCount) + vertexIndex) in ALL backends.
 *  Any behavioral change must be applied to the legacy jobs, the gather jobs and the compute kernels together.
 */

namespace Swole.Morphing
{

    public partial class CustomizableCharacterMeshV2
    {

        #region Backend Selection

        public enum ShapingBackend
        {
            CPUJobsLegacy,
            CPUJobs,
            GPUCompute
        }

        /// <summary>
        /// Global backend used by all mesh groups to compute dynamic mesh shaping.
        /// </summary>
        public static ShapingBackend Backend = ShapingBackend.CPUJobs;

        public const string ShapingComputeShaderResourceName = "CCMV2Shaping";

        private static ComputeShader shapingComputeShader;
        private static bool shapingComputeShaderLoadAttempted;
        public static ComputeShader ShapingComputeShader
        {
            get
            {
                if (shapingComputeShader == null && !shapingComputeShaderLoadAttempted)
                {
                    shapingComputeShaderLoadAttempted = true;
                    shapingComputeShader = Resources.Load<ComputeShader>(ShapingComputeShaderResourceName);
                    if (shapingComputeShader == null) Debug.LogWarning($"[CustomizableCharacterMeshV2] Could not load compute shader '{ShapingComputeShaderResourceName}'. GPU shaping backend will fall back to CPU jobs.");
                }

                return shapingComputeShader;
            }
        }

        /// <summary>
        /// The backend that will actually be used (GPU falls back to CPU jobs if unsupported).
        /// </summary>
        public static ShapingBackend EffectiveBackend
        {
            get
            {
                var backend = Backend;
                if (backend == ShapingBackend.GPUCompute && (!SystemInfo.supportsComputeShaders || !SystemInfo.supportsAsyncGPUReadback || ShapingComputeShader == null)) return ShapingBackend.CPUJobs;
                return backend;
            }
        }

        #endregion

        public partial class MeshGroupV2
        {

            #region Shared State

            private readonly HashSet<int> indicesToPhysiqueUpdateNextSet = new HashSet<int>();
            private readonly HashSet<int> indicesToVariationUpdateNextSet = new HashSet<int>();

            private bool activeJobIsGPU;
            private bool activeJobIsLegacy;

            private bool hasLastBackend;
            private ShapingBackend lastBackend;

            private readonly List<int> previouslyUpdatedIndices = new List<int>();
            private readonly List<int> previousDeltaWriteIndices = new List<int>();

            // [CCMV2-MAPS]
            private NativeArray<int2> fatVertexRanges;
            private NativeArray<int2> fatVertexEntries;
            private NativeArray<int2> muscleVertexRanges;
            private NativeArray<int2> muscleVertexEntries;
            private NativeArray<int2> variationVertexRanges;
            private NativeArray<int2> variationVertexEntries;

            #endregion

            #region Entry Maps

            /// <summary>
            /// [CCMV2-MAPS] Builds per-vertex lists of group entries, in the exact order the legacy per-group jobs accumulated them (group-major, then sequence order).
            /// </summary>
            private void BuildShapingVertexEntryMaps()
            {
                int vertexCount = data.vertexCount;

                BuildVertexEntryMap(blankFatGroupControlWeights, data.FatGroupsCount, 1, fatGroupVertexWeights, vertexCount, false, out fatVertexRanges, out fatVertexEntries);
                BuildVertexEntryMap(blankMuscleGroupControlWeights, data.MuscleGroupsCount, 1, muscleGroupVertexWeights, vertexCount, false, out muscleVertexRanges, out muscleVertexEntries);
                // variation entries store the local offset inside the group (the start index is read per shape at runtime, like the legacy job)
                BuildVertexEntryMap(blankVariationGroupControlWeights, data.VariationGroupsCount, math.max(1, data.VariationShapesCount), variationGroupVertexWeights, vertexCount, true, out variationVertexRanges, out variationVertexEntries);
            }

            private static void BuildVertexEntryMap(NativeArray<GroupControlWeight2> blankControlWeights, int groupCount, int groupStride, NativeArray<GroupVertexControlWeight> vertexWeights, int vertexCount, bool storeLocalOffset, out NativeArray<int2> ranges, out NativeArray<int2> entries)
            {
                int safeVertexCount = math.max(1, vertexCount);
                var counts = new int[safeVertexCount];
                int total = 0;

                for (int a = 0; a < groupCount; a++)
                {
                    int controlIndex = a * groupStride;
                    if (controlIndex >= blankControlWeights.Length) break;

                    var info = blankControlWeights[controlIndex];
                    int end = math.min(vertexWeights.Length, info.vertexSequenceStartIndex + info.vertexCount);
                    for (int s = info.vertexSequenceStartIndex; s < end; s++)
                    {
                        counts[vertexWeights[s].vertexIndex]++;
                        total++;
                    }
                }

                ranges = new NativeArray<int2>(safeVertexCount, Allocator.Persistent);
                int running = 0;
                for (int v = 0; v < safeVertexCount; v++)
                {
                    ranges[v] = new int2(running, 0);
                    running += counts[v];
                }

                entries = new NativeArray<int2>(math.max(1, total), Allocator.Persistent);
                for (int a = 0; a < groupCount; a++)
                {
                    int controlIndex = a * groupStride;
                    if (controlIndex >= blankControlWeights.Length) break;

                    var info = blankControlWeights[controlIndex];
                    int end = math.min(vertexWeights.Length, info.vertexSequenceStartIndex + info.vertexCount);
                    for (int s = info.vertexSequenceStartIndex; s < end; s++)
                    {
                        int v = vertexWeights[s].vertexIndex;
                        var range = ranges[v];
                        entries[range.x + range.y] = new int2(storeLocalOffset ? (s - info.vertexSequenceStartIndex) : s, a);
                        range.y++;
                        ranges[v] = range;
                    }
                }
            }

            #endregion

            #region Scheduling

            public void BeginNewJob()
            {
                var backend = EffectiveBackend;

                if (!hasLastBackend || backend != lastBackend)
                {
                    if (hasLastBackend)
                    {
                        // Switching backends: intermediate state lives on the other side, so recompute everything.
                        activeJob.Complete();
                        ProcessGPUReadbacks(true);
                        ForceFullUpdate();
                    }
                    if (backend == ShapingBackend.GPUCompute) gpuStateInvalid = true;

                    lastBackend = backend;
                    hasLastBackend = true;
                }

                if (backend == ShapingBackend.CPUJobsLegacy)
                {
                    activeJobIsGPU = false;
                    activeJobIsLegacy = true;
                    BeginNewJobLegacy();
                    return;
                }

                hasActiveJob = false;
                activeJob.Complete();

                if (!IsInitialized || IsDisposed) return;

                if (backend == ShapingBackend.GPUCompute && gpuStateInvalid && gpuDynamicCreated) ForceFullUpdate(); // instance capacity changed while on GPU

                // [CCMV2-DIRTY-GATE] nothing to do -> no copies, no jobs, no uploads
                if (indicesToPhysiqueUpdate.Length <= 0 && indicesToVariationUpdate.Length <= 0) return;

                if (backend != ShapingBackend.GPUCompute && pendingReadbacks.Count > 0) ProcessGPUReadbacks(true); // never let a readback write while CPU jobs run

#if UNITY_EDITOR
                if (data.midlineVertexGroup * data.vertexCount < 0)
                {
                    Debug.LogError($"[{debugName}] Midline vertex group premul index is negative, which likely means no midline vertex group exists! ({data.midlineVertexGroup * data.vertexCount})");
                    return;
                }
#endif

                insertionFlags.Clear();
                insertionFlags.AddReplicated(false, MaxInstanceCount);
                indicesToUpdate.Clear();

                foreach (var index in indicesToPhysiqueUpdate)
                {
                    if (!insertionFlags[index])
                    {
                        indicesToUpdate.Add(index);
                        insertionFlags[index] = true;
                    }
                }
                foreach (var index in indicesToVariationUpdate)
                {
                    if (!insertionFlags[index])
                    {
                        indicesToUpdate.Add(index);
                        insertionFlags[index] = true;
                    }
                }

                muscleGroupControlWeights.CopyFrom(muscleGroupControlWeightsNext);
                fatGroupControlWeights.CopyFrom(fatGroupControlWeightsNext);
                variationGroupControlWeights.CopyFrom(variationGroupControlWeightsNext);

                activeJobIsLegacy = false;

                if (backend == ShapingBackend.GPUCompute && DispatchGPUShaping())
                {
                    activeJobIsGPU = true;
                    activeJob = default;
                    hasActiveJob = true;
                    return;
                }

                activeJobIsGPU = false;
                ScheduleCPUShaping();
            }

            private void ForceFullUpdate()
            {
                if (!IsInitialized || IsDisposed) return;

                foreach (var index in activeIndices)
                {
                    if (!indicesToPhysiqueUpdate.Contains(index)) indicesToPhysiqueUpdate.Add(index);
                    if (!indicesToVariationUpdate.Contains(index)) indicesToVariationUpdate.Add(index);
                }
            }

            /// <summary>
            /// Indices whose previous-delta range must be refreshed: everything updated last time (so previous == current again) plus everything updated now.
            /// Equivalent to the legacy full-array previous write, but only touches ranges that can differ.
            /// </summary>
            private void CollectPreviousDeltaWriteIndices()
            {
                previousDeltaWriteIndices.Clear();
                foreach (var index in previouslyUpdatedIndices) if (index < MaxInstanceCount) previousDeltaWriteIndices.Add(index);
                foreach (var index in indicesToUpdate) if (!previousDeltaWriteIndices.Contains(index)) previousDeltaWriteIndices.Add(index);

                previouslyUpdatedIndices.Clear();
                foreach (var index in indicesToUpdate) previouslyUpdatedIndices.Add(index);
            }

            private void UploadFinalDeltasCPU()
            {
                if (finalVertexDeltasBufferIndex < 0) return;
                if (!(instanceBuffers[finalVertexDeltasBufferIndex].buffer is InstanceBuffer<MeshVertexDelta> finalDeltasBuffer)) return;

                var array = finalVertexDeltas.AsArray();
                if (activeJobIsLegacy)
                {
                    finalDeltasBuffer.WriteToBuffer(array, 0, 0, finalVertexDeltas.Length);
                    return;
                }

                // [CCMV2-COPY] CPU counterpart of the CopyDeltas kernel
                int vertexCount = data.vertexCount;
                foreach (var index in indicesToUpdate) finalDeltasBuffer.WriteToBuffer(array, index * vertexCount, index * vertexCount, vertexCount);
            }

            private void ScheduleCPUShaping()
            {
                int vertexCount = data.vertexCount;
                int midlineVertexGroupPreMul = data.midlineVertexGroup * vertexCount;

                if (finalVertexPreviousDeltasBufferIndex >= 0 && instanceBuffers[finalVertexPreviousDeltasBufferIndex].buffer is InstanceBuffer<MeshVertexDelta> previousBuffer)
                {
                    CollectPreviousDeltaWriteIndices();
                    var array = finalVertexDeltas.AsArray();
                    foreach (var index in previousDeltaWriteIndices) previousBuffer.WriteToBuffer(array, index * vertexCount, index * vertexCount, vertexCount);
                }

                JobHandle physiqueHandle = default;
                if (indicesToPhysiqueUpdate.Length > 0)
                {
                    // [CCMV2-FAT]
                    var fatHandle = new FatGatherJob()
                    {
                        vertexCount = vertexCount,
                        combinedVertexCount = fatGroupVertexWeights.Length,
                        controlGroupCount = data.FatGroupsCount,
                        bustNerfVertexGroupIndex = data.bustNerfVertexGroup,
                        fatShapeIndex = meshShapeInfos[data.fatShape],
                        midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        vertexGroups = vertexGroups,
                        meshShapeDeltas = meshShapeDeltas,
                        meshShapeFrameWeights = meshShapeFrameWeights,
                        bustSizes = bustSizes,
                        fatGroupControlWeights = fatGroupControlWeights,
                        fatGroupVertexWeights = fatGroupVertexWeights,
                        leftRightFlagBuffer = leftRightFlagBuffer,
                        vertexRanges = fatVertexRanges,
                        vertexEntries = fatVertexEntries,

                        vertexDeltas = fatGroupVertexDeltasLR,
                        fatData = fatGroupVertexDataLR,
                        finalFatData = fatValuesPerVertex
                    }.Schedule(indicesToPhysiqueUpdate.Length * vertexCount, 64, default);

                    // [CCMV2-MUSCLE]
                    physiqueHandle = new MuscleGatherJob()
                    {
                        vertexCount = vertexCount,
                        combinedVertexCount = muscleGroupVertexWeights.Length,
                        controlGroupCount = data.MuscleGroupsCount,
                        muscleShapeIndex = meshShapeInfos[data.massShape],
                        fatMuscleBlendShapeIndex = meshShapeInfos[data.fatMuscleBlendShape],
                        minShapeMassWeight = data.minMassShapeWeight,
                        defaultShapeMassWeight = data.defaultMassShapeWeight,
                        muscleMassRange = 1f - data.defaultMassShapeWeight,
                        midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                        meshIndicesToUpdate = indicesToPhysiqueUpdate,
                        vertexGroups = vertexGroups,
                        meshShapeDeltas = meshShapeDeltas,
                        meshShapeFrameWeights = meshShapeFrameWeights,
                        muscleGroupControlWeights = muscleGroupControlWeights,
                        muscleGroupVertexWeights = muscleGroupVertexWeights,
                        leftRightFlagBuffer = leftRightFlagBuffer,
                        fatValuesPerVertex = fatValuesPerVertex,
                        vertexRanges = muscleVertexRanges,
                        vertexEntries = muscleVertexEntries,

                        vertexDeltas = muscleGroupVertexDeltasLR,
                        muscleData = muscleGroupVertexDataLR,
                        finalMuscleData = muscleValuesPerVertex
                    }.Schedule(indicesToPhysiqueUpdate.Length * vertexCount, 64, fatHandle);
                }

                JobHandle variationHandle = default;
                if (indicesToVariationUpdate.Length > 0)
                {
                    // [CCMV2-VARIATION]
                    variationHandle = new VariationGatherJob()
                    {
                        vertexCount = vertexCount,
                        variationShapesCount = data.VariationShapesCount,
                        variationShapesStartIndex = data.variationShapes.x,
                        controlGroupCount = data.VariationGroupsCount * data.VariationShapesCount,
                        midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                        meshIndicesToUpdate = indicesToVariationUpdate,
                        vertexGroups = vertexGroups,
                        meshShapeDeltas = meshShapeDeltas,
                        meshShapeFrameWeights = meshShapeFrameWeights,
                        meshShapeIndices = meshShapeInfos,
                        variationGroupControlWeights = variationGroupControlWeights,
                        variationGroupVertexWeights = variationGroupVertexWeights,
                        leftRightFlagBuffer = leftRightFlagBuffer,
                        vertexRanges = variationVertexRanges,
                        vertexEntries = variationVertexEntries,

                        variationVertexDeltas = variationVertexDeltas
                    }.Schedule(indicesToVariationUpdate.Length * vertexCount, 64, default);
                }

                bool hasBreast = data.bustSizeShape >= 0 && data.bustShapeShape >= 0;

                // [CCMV2-FINAL]
                activeJob = new FinalizeGatherJob()
                {
                    vertexCount = vertexCount,
                    fatCombinedVertexCount = fatGroupVertexWeights.Length,
                    muscleCombinedVertexCount = muscleGroupVertexWeights.Length,
                    midlineVertexGroupIndexPreMul = midlineVertexGroupPreMul,

                    applyBreast = hasBreast,
                    bustSizeShapeIndex = hasBreast ? meshShapeInfos[data.bustSizeShape] : default,
                    breastShapeIndex = hasBreast ? meshShapeInfos[data.bustShapeShape] : default,
                    bustSizeMuscleShapeIndex = hasBreast ? meshShapeInfos[data.bustSizeMuscleShape] : default,
                    muscleMassStartWeight = data.defaultMassShapeWeight,
                    muscleMassRange = 1f - data.defaultMassShapeWeight,

                    meshIndicesToUpdate = indicesToUpdate,
                    vertexGroups = vertexGroups,
                    leftRightFlagBuffer = leftRightFlagBuffer,
                    meshShapeDeltas = meshShapeDeltas,
                    meshShapeFrameWeights = meshShapeFrameWeights,
                    bustSizes = bustSizes,
                    muscleData = muscleValuesPerVertex,
                    fatVertexRanges = fatVertexRanges,
                    fatVertexEntries = fatVertexEntries,
                    muscleVertexRanges = muscleVertexRanges,
                    muscleVertexEntries = muscleVertexEntries,
                    fatDeltasLR = fatGroupVertexDeltasLR,
                    muscleDeltasLR = muscleGroupVertexDeltasLR,
                    variationVertexDeltas = variationVertexDeltas,

                    finalVertexDeltas = finalVertexDeltas
                }.Schedule(indicesToUpdate.Length * vertexCount, 64, JobHandle.CombineDependencies(physiqueHandle, variationHandle));

                hasActiveJob = true;
            }

            #endregion

            #region GPU Backend

            private bool gpuStateInvalid = true;
            private bool gpuDynamicCreated;
            private bool gpuStaticCreated;

            private readonly List<ComputeBuffer> gpuStaticBuffers = new List<ComputeBuffer>();
            private readonly List<ComputeBuffer> gpuDynamicBuffers = new List<ComputeBuffer>();

            private ComputeBuffer gShapeDeltas, gFrameWeights, gShapeInfos, gVertexGroups, gLeftRight;
            private ComputeBuffer gFatWeights, gMuscleWeights, gVariationWeights;
            private ComputeBuffer gFatRanges, gFatEntries, gMuscleRanges, gMuscleEntries, gVariationRanges, gVariationEntries;

            private ComputeBuffer gFatControl, gMuscleControl, gVariationControl, gBustSizes;
            private ComputeBuffer gIndicesPhysique, gIndicesVariation, gIndicesUpdate;
            private ComputeBuffer gFatDeltasLR, gFatData, gFatValues, gMuscleDeltasLR, gMuscleData, gMuscleValues, gVariationDeltas, gFinalDeltas;

            private struct PendingReadback
            {
                public AsyncGPUReadbackRequest request;
                public int[] indices;
            }
            private readonly Queue<PendingReadback> pendingReadbacks = new Queue<PendingReadback>();

            private static ComputeBuffer CreateGPUBuffer<T>(List<ComputeBuffer> owner, NativeArray<T> source, int stride) where T : struct
            {
                var buffer = new ComputeBuffer(math.max(1, source.Length), stride, ComputeBufferType.Structured);
                if (source.Length > 0) buffer.SetData(source);
                owner.Add(buffer);
                return buffer;
            }
            private static ComputeBuffer CreateGPUBuffer<T>(List<ComputeBuffer> owner, int count, int stride) where T : struct
            {
                var buffer = new ComputeBuffer(math.max(1, count), stride, ComputeBufferType.Structured);
                owner.Add(buffer);
                return buffer;
            }
            private static void ReleaseGPUBuffers(List<ComputeBuffer> buffers)
            {
                foreach (var buffer in buffers) if (buffer != null && buffer.IsValid()) buffer.Release();
                buffers.Clear();
            }

            private const int strideDelta = 36;
            private const int strideDeltaLR = 72;
            private const int strideControl = 20;
            private const int strideVertexWeight = 12;

            private void EnsureGPUResources()
            {
                if (!gpuStaticCreated)
                {
                    gShapeDeltas = CreateGPUBuffer(gpuStaticBuffers, meshShapeDeltas, strideDelta);
                    gFrameWeights = CreateGPUBuffer(gpuStaticBuffers, meshShapeFrameWeights, sizeof(float));
                    gShapeInfos = CreateGPUBuffer(gpuStaticBuffers, meshShapeInfos, sizeof(int) * 2);
                    gVertexGroups = CreateGPUBuffer(gpuStaticBuffers, vertexGroups, sizeof(float));

                    using (var leftRight = new NativeArray<uint>(math.max(1, leftRightFlagBuffer.Length), Allocator.Temp))
                    {
                        var lr = leftRight;
                        for (int a = 0; a < leftRightFlagBuffer.Length; a++) lr[a] = leftRightFlagBuffer[a] ? 1u : 0u;
                        gLeftRight = CreateGPUBuffer(gpuStaticBuffers, lr, sizeof(uint));
                    }

                    gFatWeights = CreateGPUBuffer(gpuStaticBuffers, fatGroupVertexWeights, strideVertexWeight);
                    gMuscleWeights = CreateGPUBuffer(gpuStaticBuffers, muscleGroupVertexWeights, strideVertexWeight);
                    gVariationWeights = CreateGPUBuffer(gpuStaticBuffers, variationGroupVertexWeights, strideVertexWeight);

                    gFatRanges = CreateGPUBuffer(gpuStaticBuffers, fatVertexRanges, sizeof(int) * 2);
                    gFatEntries = CreateGPUBuffer(gpuStaticBuffers, fatVertexEntries, sizeof(int) * 2);
                    gMuscleRanges = CreateGPUBuffer(gpuStaticBuffers, muscleVertexRanges, sizeof(int) * 2);
                    gMuscleEntries = CreateGPUBuffer(gpuStaticBuffers, muscleVertexEntries, sizeof(int) * 2);
                    gVariationRanges = CreateGPUBuffer(gpuStaticBuffers, variationVertexRanges, sizeof(int) * 2);
                    gVariationEntries = CreateGPUBuffer(gpuStaticBuffers, variationVertexEntries, sizeof(int) * 2);

                    gpuStaticCreated = true;
                }

                if (gpuStateInvalid || !gpuDynamicCreated)
                {
                    ReleaseGPUBuffers(gpuDynamicBuffers);

                    gFatControl = CreateGPUBuffer<GroupControlWeight2>(gpuDynamicBuffers, fatGroupControlWeights.Length, strideControl);
                    gMuscleControl = CreateGPUBuffer<GroupControlWeight2>(gpuDynamicBuffers, muscleGroupControlWeights.Length, strideControl);
                    gVariationControl = CreateGPUBuffer<GroupControlWeight2>(gpuDynamicBuffers, variationGroupControlWeights.Length, strideControl);
                    gBustSizes = CreateGPUBuffer<float2>(gpuDynamicBuffers, bustSizes.Length, sizeof(float) * 2);

                    gIndicesPhysique = CreateGPUBuffer<int>(gpuDynamicBuffers, MaxInstanceCount, sizeof(int));
                    gIndicesVariation = CreateGPUBuffer<int>(gpuDynamicBuffers, MaxInstanceCount, sizeof(int));
                    gIndicesUpdate = CreateGPUBuffer<int>(gpuDynamicBuffers, MaxInstanceCount, sizeof(int));

                    // seed working state from the CPU copies so both backends share the same starting point
                    gFatDeltasLR = CreateGPUBuffer(gpuDynamicBuffers, fatGroupVertexDeltasLR.AsArray(), strideDeltaLR);
                    gFatData = CreateGPUBuffer(gpuDynamicBuffers, fatGroupVertexDataLR.AsArray(), sizeof(float) * 4);
                    gFatValues = CreateGPUBuffer(gpuDynamicBuffers, fatValuesPerVertex.AsArray(), sizeof(float) * 2);
                    gMuscleDeltasLR = CreateGPUBuffer(gpuDynamicBuffers, muscleGroupVertexDeltasLR.AsArray(), strideDeltaLR);
                    gMuscleData = CreateGPUBuffer(gpuDynamicBuffers, muscleGroupVertexDataLR.AsArray(), sizeof(float) * 2);
                    gMuscleValues = CreateGPUBuffer(gpuDynamicBuffers, muscleValuesPerVertex.AsArray(), sizeof(float));
                    gVariationDeltas = CreateGPUBuffer(gpuDynamicBuffers, variationVertexDeltas.AsArray(), strideDelta);
                    gFinalDeltas = CreateGPUBuffer(gpuDynamicBuffers, finalVertexDeltas.AsArray(), strideDelta);

                    gpuDynamicCreated = true;
                    gpuStateInvalid = false;
                }
            }

            private static int kFat = -1, kMuscle, kVariation, kFinalize, kCopy;
            private static ComputeShader kernelShader;
            private static void CacheKernels(ComputeShader cs)
            {
                if (kernelShader == cs && kFat >= 0) return;
                kernelShader = cs;
                kFat = cs.FindKernel("FatGather");
                kMuscle = cs.FindKernel("MuscleGather");
                kVariation = cs.FindKernel("VariationGather");
                kFinalize = cs.FindKernel("FinalizeGather");
                kCopy = cs.FindKernel("CopyDeltas");
            }

            private const int threadGroupSize = 64;
            private static void DispatchLinear(ComputeShader cs, int kernel, int count)
            {
                if (count <= 0) return;
                cs.SetInt("_WorkCount", count);
                cs.Dispatch(kernel, (count + threadGroupSize - 1) / threadGroupSize, 1, 1);
            }

            // [CCMV2-COPY]
            private static void CopyDeltaRange(ComputeShader cs, ComputeBuffer src, ComputeBuffer dst, int start, int count)
            {
                if (src == null || dst == null || count <= 0) return;
                if (start + count > dst.count || start + count > src.count) return;

                cs.SetBuffer(kCopy, "_CopySrc", src);
                cs.SetBuffer(kCopy, "_CopyDst", dst);
                cs.SetInt("_CopyStart", start);
                DispatchLinear(cs, kCopy, count);
            }

            private bool DispatchGPUShaping()
            {
                var cs = ShapingComputeShader;
                if (cs == null) return false;

                try
                {
                    EnsureGPUResources();
                    CacheKernels(cs);

                    int vertexCount = data.vertexCount;

                    gFatControl.SetData(fatGroupControlWeights.AsArray());
                    gMuscleControl.SetData(muscleGroupControlWeights.AsArray());
                    gVariationControl.SetData(variationGroupControlWeights.AsArray());
                    gBustSizes.SetData(bustSizes.AsArray());
                    if (indicesToPhysiqueUpdate.Length > 0) gIndicesPhysique.SetData(indicesToPhysiqueUpdate.AsArray(), 0, 0, indicesToPhysiqueUpdate.Length);
                    if (indicesToVariationUpdate.Length > 0) gIndicesVariation.SetData(indicesToVariationUpdate.AsArray(), 0, 0, indicesToVariationUpdate.Length);
                    gIndicesUpdate.SetData(indicesToUpdate.AsArray(), 0, 0, indicesToUpdate.Length);

                    cs.SetInt("_VertexCount", vertexCount);
                    cs.SetInt("_MidlinePreMul", data.midlineVertexGroup * vertexCount);
                    cs.SetInt("_BustNerfGroup", data.bustNerfVertexGroup);
                    cs.SetInt("_FatCombined", fatGroupVertexWeights.Length);
                    cs.SetInt("_MuscleCombined", muscleGroupVertexWeights.Length);
                    cs.SetInt("_FatControlGroupCount", data.FatGroupsCount);
                    cs.SetInt("_MuscleControlGroupCount", data.MuscleGroupsCount);
                    cs.SetInt("_VariationShapesCount", data.VariationShapesCount);
                    cs.SetInt("_VariationShapesStart", data.variationShapes.x);
                    cs.SetInt("_VariationControlGroupCount", data.VariationGroupsCount * data.VariationShapesCount);
                    cs.SetFloat("_MinMassShapeWeight", data.minMassShapeWeight);
                    cs.SetFloat("_DefaultMassShapeWeight", data.defaultMassShapeWeight);
                    cs.SetFloat("_MuscleMassRange", 1f - data.defaultMassShapeWeight);

                    var fatShape = meshShapeInfos[data.fatShape];
                    var massShape = meshShapeInfos[data.massShape];
                    var blendShape = meshShapeInfos[data.fatMuscleBlendShape];
                    cs.SetInt("_FatShapeStart", fatShape.x); cs.SetInt("_FatShapeFrames", fatShape.y);
                    cs.SetInt("_MassShapeStart", massShape.x); cs.SetInt("_MassShapeFrames", massShape.y);
                    cs.SetInt("_BlendShapeStart", blendShape.x); cs.SetInt("_BlendShapeFrames", blendShape.y);

                    bool hasBreast = data.bustSizeShape >= 0 && data.bustShapeShape >= 0;
                    cs.SetInt("_ApplyBreast", hasBreast ? 1 : 0);
                    if (hasBreast)
                    {
                        var bs = meshShapeInfos[data.bustSizeShape];
                        var bsh = meshShapeInfos[data.bustShapeShape];
                        var bsm = meshShapeInfos[data.bustSizeMuscleShape];
                        cs.SetInt("_BustSizeShapeStart", bs.x); cs.SetInt("_BustSizeShapeFrames", bs.y);
                        cs.SetInt("_BreastShapeStart", bsh.x); cs.SetInt("_BreastShapeFrames", bsh.y);
                        cs.SetInt("_BustMuscleShapeStart", bsm.x); cs.SetInt("_BustMuscleShapeFrames", bsm.y);
                    }

                    InstanceBuffer<MeshVertexDelta> finalBuffer = finalVertexDeltasBufferIndex >= 0 ? instanceBuffers[finalVertexDeltasBufferIndex].buffer as InstanceBuffer<MeshVertexDelta> : null;
                    InstanceBuffer<MeshVertexDelta> previousBuffer = finalVertexPreviousDeltasBufferIndex >= 0 ? instanceBuffers[finalVertexPreviousDeltasBufferIndex].buffer as InstanceBuffer<MeshVertexDelta> : null;

                    // previous deltas = current GPU result before this update (legacy: full write of finalVertexDeltas before scheduling)
                    if (previousBuffer != null)
                    {
                        CollectPreviousDeltaWriteIndices();
                        int poolSize = previousBuffer.BufferPoolSize;
                        foreach (var index in previousDeltaWriteIndices)
                        {
                            for (int p = 0; p < poolSize; p++) CopyDeltaRange(cs, gFinalDeltas, previousBuffer.GetPooledBuffer(p), index * vertexCount, vertexCount);
                        }
                    }

                    // shared bindings
                    void BindCommon(int k)
                    {
                        cs.SetBuffer(k, "_ShapeDeltas", gShapeDeltas);
                        cs.SetBuffer(k, "_FrameWeights", gFrameWeights);
                        cs.SetBuffer(k, "_VertexGroups", gVertexGroups);
                        cs.SetBuffer(k, "_LeftRight", gLeftRight);
                    }

                    if (indicesToPhysiqueUpdate.Length > 0)
                    {
                        // [CCMV2-FAT]
                        BindCommon(kFat);
                        cs.SetBuffer(kFat, "_Indices", gIndicesPhysique);
                        cs.SetBuffer(kFat, "_BustSizes", gBustSizes);
                        cs.SetBuffer(kFat, "_FatControl", gFatControl);
                        cs.SetBuffer(kFat, "_FatWeights", gFatWeights);
                        cs.SetBuffer(kFat, "_FatRanges", gFatRanges);
                        cs.SetBuffer(kFat, "_FatEntries", gFatEntries);
                        cs.SetBuffer(kFat, "_FatDeltasLR", gFatDeltasLR);
                        cs.SetBuffer(kFat, "_FatData", gFatData);
                        cs.SetBuffer(kFat, "_FatValues", gFatValues);
                        DispatchLinear(cs, kFat, indicesToPhysiqueUpdate.Length * vertexCount);

                        // [CCMV2-MUSCLE]
                        BindCommon(kMuscle);
                        cs.SetBuffer(kMuscle, "_Indices", gIndicesPhysique);
                        cs.SetBuffer(kMuscle, "_MuscleControl", gMuscleControl);
                        cs.SetBuffer(kMuscle, "_MuscleWeights", gMuscleWeights);
                        cs.SetBuffer(kMuscle, "_MuscleRanges", gMuscleRanges);
                        cs.SetBuffer(kMuscle, "_MuscleEntries", gMuscleEntries);
                        cs.SetBuffer(kMuscle, "_FatValuesRO", gFatValues);
                        cs.SetBuffer(kMuscle, "_MuscleDeltasLR", gMuscleDeltasLR);
                        cs.SetBuffer(kMuscle, "_MuscleData", gMuscleData);
                        cs.SetBuffer(kMuscle, "_MuscleValues", gMuscleValues);
                        DispatchLinear(cs, kMuscle, indicesToPhysiqueUpdate.Length * vertexCount);
                    }

                    if (indicesToVariationUpdate.Length > 0)
                    {
                        // [CCMV2-VARIATION]
                        BindCommon(kVariation);
                        cs.SetBuffer(kVariation, "_Indices", gIndicesVariation);
                        cs.SetBuffer(kVariation, "_ShapeInfos", gShapeInfos);
                        cs.SetBuffer(kVariation, "_VariationControl", gVariationControl);
                        cs.SetBuffer(kVariation, "_VariationWeights", gVariationWeights);
                        cs.SetBuffer(kVariation, "_VariationRanges", gVariationRanges);
                        cs.SetBuffer(kVariation, "_VariationEntries", gVariationEntries);
                        cs.SetBuffer(kVariation, "_VariationDeltas", gVariationDeltas);
                        DispatchLinear(cs, kVariation, indicesToVariationUpdate.Length * vertexCount);
                    }

                    // [CCMV2-FINAL]
                    BindCommon(kFinalize);
                    cs.SetBuffer(kFinalize, "_Indices", gIndicesUpdate);
                    cs.SetBuffer(kFinalize, "_BustSizes", gBustSizes);
                    cs.SetBuffer(kFinalize, "_FatRanges", gFatRanges);
                    cs.SetBuffer(kFinalize, "_FatEntries", gFatEntries);
                    cs.SetBuffer(kFinalize, "_MuscleRanges", gMuscleRanges);
                    cs.SetBuffer(kFinalize, "_MuscleEntries", gMuscleEntries);
                    cs.SetBuffer(kFinalize, "_FatDeltasLRRO", gFatDeltasLR);
                    cs.SetBuffer(kFinalize, "_MuscleDeltasLRRO", gMuscleDeltasLR);
                    cs.SetBuffer(kFinalize, "_MuscleValuesRO", gMuscleValues);
                    cs.SetBuffer(kFinalize, "_VariationDeltasRO", gVariationDeltas);
                    cs.SetBuffer(kFinalize, "_FinalDeltas", gFinalDeltas);
                    DispatchLinear(cs, kFinalize, indicesToUpdate.Length * vertexCount);

                    // [CCMV2-COPY] write straight into every pooled buffer of the material-bound instance buffer
                    if (finalBuffer != null)
                    {
                        int poolSize = finalBuffer.BufferPoolSize;
                        foreach (var index in indicesToUpdate)
                        {
                            for (int p = 0; p < poolSize; p++) CopyDeltaRange(cs, gFinalDeltas, finalBuffer.GetPooledBuffer(p), index * vertexCount, vertexCount);
                        }
                    }

                    // [CCMV2-GPU-SYNC]
                    var updated = new int[indicesToUpdate.Length];
                    for (int a = 0; a < updated.Length; a++) updated[a] = indicesToUpdate[a];
                    pendingReadbacks.Enqueue(new PendingReadback() { request = AsyncGPUReadback.Request(gFinalDeltas), indices = updated });

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    return false;
                }
            }

            /// <summary>
            /// [CCMV2-GPU-SYNC] Copies completed GPU results into FinalVertexDeltas (and the instance buffer's CPU mirror, without re-uploading).
            /// </summary>
            public void ProcessGPUReadbacks(bool block)
            {
                while (pendingReadbacks.Count > 0)
                {
                    var pending = pendingReadbacks.Peek();
                    if (!pending.request.done)
                    {
                        if (!block) return;
                        pending.request.WaitForCompletion();
                    }

                    pendingReadbacks.Dequeue();
                    if (pending.request.hasError || IsDisposed || !IsInitialized) continue;

                    var source = pending.request.GetData<MeshVertexDelta>();
                    var destination = finalVertexDeltas.AsArray();
                    InstanceBuffer<MeshVertexDelta> finalBuffer = finalVertexDeltasBufferIndex >= 0 ? instanceBuffers[finalVertexDeltasBufferIndex].buffer as InstanceBuffer<MeshVertexDelta> : null;

                    int vertexCount = data.vertexCount;
                    foreach (var index in pending.indices)
                    {
                        int start = index * vertexCount;
                        if (start + vertexCount > source.Length || start + vertexCount > destination.Length) continue;

                        NativeArray<MeshVertexDelta>.Copy(source, start, destination, start, vertexCount);
                        if (finalBuffer != null && start + vertexCount <= finalBuffer.Size) finalBuffer.WriteToBufferFast(source, start, start, vertexCount);
                    }
                }
            }

            private void DisposeShapingAcceleration()
            {
                pendingReadbacks.Clear();

                ReleaseGPUBuffers(gpuDynamicBuffers);
                ReleaseGPUBuffers(gpuStaticBuffers);
                gpuDynamicCreated = false;
                gpuStaticCreated = false;
                gpuStateInvalid = true;

                if (fatVertexRanges.IsCreated) fatVertexRanges.Dispose();
                if (fatVertexEntries.IsCreated) fatVertexEntries.Dispose();
                if (muscleVertexRanges.IsCreated) muscleVertexRanges.Dispose();
                if (muscleVertexEntries.IsCreated) muscleVertexEntries.Dispose();
                if (variationVertexRanges.IsCreated) variationVertexRanges.Dispose();
                if (variationVertexEntries.IsCreated) variationVertexEntries.Dispose();
            }

            #endregion

        }

        #region Optimized CPU Gather Jobs

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static float2 WeightLeftRight(NativeArray<float> vertexGroups, NativeArray<bool> leftRightFlagBuffer, int midlineVertexGroupIndexPreMul, int vertexIndex)
        {
            // [CCMV2-LR] identical to the legacy per-group jobs
            float midlineWeight = vertexGroups[midlineVertexGroupIndexPreMul + vertexIndex];
            return math.lerp(math.select(new float2(1f, 0f), new float2(0f, 1f), leftRightFlagBuffer[vertexIndex]), new float2(0.5f, 0.5f), midlineWeight);
        }

        /// <summary>
        /// [CCMV2-FAT] Fused UpdateMeshFatVertexDeltasJob + ResetFatDataJob + ApplyFatDataJob. One work item per (instance, vertex). GPU: FatGather.
        /// </summary>
        [BurstCompile]
        public struct FatGatherJob : IJobParallelFor
        {
            public int vertexCount;
            public int combinedVertexCount;
            public int controlGroupCount;
            public int bustNerfVertexGroupIndex;
            public int2 fatShapeIndex;
            public int midlineVertexGroupIndexPreMul;

            [ReadOnly] public NativeList<int> meshIndicesToUpdate;
            [ReadOnly] public NativeArray<float> vertexGroups;
            [ReadOnly] public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly] public NativeArray<float> meshShapeFrameWeights;
            [ReadOnly] public NativeList<float2> bustSizes;
            [ReadOnly] public NativeList<GroupControlWeight2> fatGroupControlWeights;
            [ReadOnly] public NativeArray<GroupVertexControlWeight> fatGroupVertexWeights;
            [ReadOnly] public NativeArray<bool> leftRightFlagBuffer;
            [ReadOnly] public NativeArray<int2> vertexRanges;
            [ReadOnly] public NativeArray<int2> vertexEntries;

            [NativeDisableParallelForRestriction] public NativeList<MeshVertexDeltaLR> vertexDeltas;
            [NativeDisableParallelForRestriction] public NativeList<float4> fatData;
            [NativeDisableParallelForRestriction] public NativeList<float2> finalFatData;

            public void Execute(int workIndex)
            {
                int slot = workIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[slot];
                int vertexIndex = workIndex - (slot * vertexCount);

                float2 weightLeftRight = WeightLeftRight(vertexGroups, leftRightFlagBuffer, midlineVertexGroupIndexPreMul, vertexIndex);

                float2 accumulated = float2.zero;
                var range = vertexRanges[vertexIndex];
                for (int k = range.x; k < range.x + range.y; k++)
                {
                    int sequenceIndex = vertexEntries[k].x;
                    var groupVertexWeight = fatGroupVertexWeights[sequenceIndex];

                    GroupControlWeight2 control = fatGroupControlWeights[(meshIndex * controlGroupCount) + groupVertexWeight.groupIndex];

                    float bustNerfFactor = CalculateBustNerfFactor(bustSizes[meshIndex].x, vertexGroups, bustNerfVertexGroupIndex, vertexCount, groupVertexWeight.vertexIndex, 0.6f);

                    var shapeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(groupVertexWeight.vertexIndex, meshShapeFrameWeights, fatShapeIndex.x, fatShapeIndex.y, control.weight.x, vertexCount) * groupVertexWeight.weight * bustNerfFactor;
                    var fatDelta = control.weight * groupVertexWeight.weight;

                    int bufferIndex = (meshIndex * combinedVertexCount) + sequenceIndex;
                    vertexDeltas[bufferIndex] = new MeshVertexDeltaLR() { deltaLeft = shapeDelta, deltaRight = shapeDelta };
                    var dataLR = new float4(fatDelta.x, fatDelta.y, fatDelta.x, fatDelta.y);
                    fatData[bufferIndex] = dataLR;

                    accumulated = accumulated + (new float2(dataLR.x, dataLR.y) * weightLeftRight.x) + (new float2(dataLR.z, dataLR.w) * weightLeftRight.y);
                }

                finalFatData[(meshIndex * vertexCount) + vertexIndex] = accumulated;
            }
        }

        /// <summary>
        /// [CCMV2-MUSCLE] Fused ResetMuscleDataJob + UpdateMeshMuscleVertexDeltasJob + ApplyMuscleDataJob. GPU: MuscleGather.
        /// </summary>
        [BurstCompile]
        public struct MuscleGatherJob : IJobParallelFor
        {
            public int vertexCount;
            public int combinedVertexCount;
            public int controlGroupCount;
            public int2 muscleShapeIndex;
            public int2 fatMuscleBlendShapeIndex;
            public float minShapeMassWeight;
            public float defaultShapeMassWeight;
            public float muscleMassRange;
            public int midlineVertexGroupIndexPreMul;

            [ReadOnly] public NativeList<int> meshIndicesToUpdate;
            [ReadOnly] public NativeArray<float> vertexGroups;
            [ReadOnly] public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly] public NativeArray<float> meshShapeFrameWeights;
            [ReadOnly] public NativeList<GroupControlWeight2> muscleGroupControlWeights;
            [ReadOnly] public NativeArray<GroupVertexControlWeight> muscleGroupVertexWeights;
            [ReadOnly] public NativeArray<bool> leftRightFlagBuffer;
            [ReadOnly] public NativeList<float2> fatValuesPerVertex;
            [ReadOnly] public NativeArray<int2> vertexRanges;
            [ReadOnly] public NativeArray<int2> vertexEntries;

            [NativeDisableParallelForRestriction] public NativeList<MeshVertexDeltaLR> vertexDeltas;
            [NativeDisableParallelForRestriction] public NativeList<float2> muscleData;
            [NativeDisableParallelForRestriction] public NativeList<float> finalMuscleData;

            public void Execute(int workIndex)
            {
                int slot = workIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[slot];
                int vertexIndex = workIndex - (slot * vertexCount);

                float2 weightLeftRight = WeightLeftRight(vertexGroups, leftRightFlagBuffer, midlineVertexGroupIndexPreMul, vertexIndex);

                float accumulated = 0f;
                var range = vertexRanges[vertexIndex];
                for (int k = range.x; k < range.x + range.y; k++)
                {
                    int sequenceIndex = vertexEntries[k].x;
                    var groupVertexWeight = muscleGroupVertexWeights[sequenceIndex];

                    int controlIndex = (meshIndex * controlGroupCount) + groupVertexWeight.groupIndex;
                    var deltas = UpdateMeshMuscleVertexDeltasJob.CalculateMuscleVertexDelta(controlIndex, groupVertexWeight.weight, groupVertexWeight.vertexIndex, (meshIndex * vertexCount) + groupVertexWeight.vertexIndex, vertexCount, minShapeMassWeight, defaultShapeMassWeight, muscleMassRange, muscleShapeIndex, fatMuscleBlendShapeIndex, muscleGroupControlWeights, meshShapeFrameWeights, meshShapeDeltas, fatValuesPerVertex, out var muscleGroupMassWeight);

                    int bufferIndex = (meshIndex * combinedVertexCount) + sequenceIndex;
                    vertexDeltas[bufferIndex] = deltas;
                    var dataLR = muscleGroupMassWeight * groupVertexWeight.weight;
                    muscleData[bufferIndex] = dataLR;

                    accumulated = accumulated + (dataLR.x * weightLeftRight.x) + (dataLR.y * weightLeftRight.y);
                }

                finalMuscleData[(meshIndex * vertexCount) + vertexIndex] = accumulated;
            }
        }

        /// <summary>
        /// [CCMV2-VARIATION] Fused ResetFinalVertexDeltasJob(variation) + all UpdateMeshVariationVertexDeltasJob(group, shape). GPU: VariationGather.
        /// </summary>
        [BurstCompile]
        public struct VariationGatherJob : IJobParallelFor
        {
            public int vertexCount;
            public int variationShapesCount;
            public int variationShapesStartIndex;
            public int controlGroupCount;
            public int midlineVertexGroupIndexPreMul;

            [ReadOnly] public NativeList<int> meshIndicesToUpdate;
            [ReadOnly] public NativeArray<float> vertexGroups;
            [ReadOnly] public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly] public NativeArray<float> meshShapeFrameWeights;
            [ReadOnly] public NativeArray<int2> meshShapeIndices;
            [ReadOnly] public NativeList<GroupControlWeight2> variationGroupControlWeights;
            [ReadOnly] public NativeArray<GroupVertexControlWeight> variationGroupVertexWeights;
            [ReadOnly] public NativeArray<bool> leftRightFlagBuffer;
            [ReadOnly] public NativeArray<int2> vertexRanges;
            [ReadOnly] public NativeArray<int2> vertexEntries;

            [NativeDisableParallelForRestriction] public NativeList<MeshVertexDelta> variationVertexDeltas;

            public void Execute(int workIndex)
            {
                int slot = workIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[slot];
                int vertexIndex = workIndex - (slot * vertexCount);

                int controlOffset = meshIndex * controlGroupCount;

                MeshVertexDelta accumulated = MeshVertexDelta.Default;
                var range = vertexRanges[vertexIndex];
                for (int k = range.x; k < range.x + range.y; k++)
                {
                    var entry = vertexEntries[k]; // x = local offset in group, y = group index
                    for (int b = 0; b < variationShapesCount; b++)
                    {
                        GroupControlWeight2 control = variationGroupControlWeights[controlOffset + (entry.y * variationShapesCount) + b];
                        var groupVertexWeight = variationGroupVertexWeights[control.vertexSequenceStartIndex + entry.x];
                        int vi = groupVertexWeight.vertexIndex;

                        int2 shapeIndex = meshShapeIndices[variationShapesStartIndex + b];
                        var shapeDeltaL = meshShapeDeltas.SampleDeltaShapeBuffer(vi, meshShapeFrameWeights, shapeIndex.x, shapeIndex.y, control.weight.x, vertexCount) * groupVertexWeight.weight;
                        var shapeDeltaR = meshShapeDeltas.SampleDeltaShapeBuffer(vi, meshShapeFrameWeights, shapeIndex.x, shapeIndex.y, control.weight.y, vertexCount) * groupVertexWeight.weight;

                        float2 weightLeftRight = WeightLeftRight(vertexGroups, leftRightFlagBuffer, midlineVertexGroupIndexPreMul, vi);
                        accumulated = accumulated + (shapeDeltaL * weightLeftRight.x) + (shapeDeltaR * weightLeftRight.y);
                    }
                }

                variationVertexDeltas[(meshIndex * vertexCount) + vertexIndex] = accumulated;
            }
        }

        /// <summary>
        /// [CCMV2-FINAL] Fused ResetFinalVertexDeltasToTargetDeltasJob + ApplyGroupVertexDeltasJob(fat, muscle) + ApplyBreastShapeJob. GPU: FinalizeGather.
        /// </summary>
        [BurstCompile]
        public struct FinalizeGatherJob : IJobParallelFor
        {
            public int vertexCount;
            public int fatCombinedVertexCount;
            public int muscleCombinedVertexCount;
            public int midlineVertexGroupIndexPreMul;

            public bool applyBreast;
            public int2 bustSizeShapeIndex;
            public int2 breastShapeIndex;
            public int2 bustSizeMuscleShapeIndex;
            public float muscleMassStartWeight;
            public float muscleMassRange;

            [ReadOnly] public NativeList<int> meshIndicesToUpdate;
            [ReadOnly] public NativeArray<float> vertexGroups;
            [ReadOnly] public NativeArray<bool> leftRightFlagBuffer;
            [ReadOnly] public NativeArray<MeshVertexDelta> meshShapeDeltas;
            [ReadOnly] public NativeArray<float> meshShapeFrameWeights;
            [ReadOnly] public NativeList<float2> bustSizes;
            [ReadOnly] public NativeList<float> muscleData;
            [ReadOnly] public NativeArray<int2> fatVertexRanges;
            [ReadOnly] public NativeArray<int2> fatVertexEntries;
            [ReadOnly] public NativeArray<int2> muscleVertexRanges;
            [ReadOnly] public NativeArray<int2> muscleVertexEntries;
            [ReadOnly] public NativeList<MeshVertexDeltaLR> fatDeltasLR;
            [ReadOnly] public NativeList<MeshVertexDeltaLR> muscleDeltasLR;
            [ReadOnly] public NativeList<MeshVertexDelta> variationVertexDeltas;

            [NativeDisableParallelForRestriction] public NativeList<MeshVertexDelta> finalVertexDeltas;

            public void Execute(int workIndex)
            {
                int slot = workIndex / vertexCount;
                int meshIndex = meshIndicesToUpdate[slot];
                int vertexIndex = workIndex - (slot * vertexCount);
                int finalIndex = (meshIndex * vertexCount) + vertexIndex;

                float2 weightLeftRight = WeightLeftRight(vertexGroups, leftRightFlagBuffer, midlineVertexGroupIndexPreMul, vertexIndex);

                MeshVertexDelta accumulated = variationVertexDeltas[finalIndex];

                var range = fatVertexRanges[vertexIndex];
                for (int k = range.x; k < range.x + range.y; k++)
                {
                    var deltaLR = fatDeltasLR[(fatCombinedVertexCount * meshIndex) + fatVertexEntries[k].x];
                    accumulated = accumulated + (deltaLR.deltaLeft * weightLeftRight.x) + (deltaLR.deltaRight * weightLeftRight.y);
                }

                range = muscleVertexRanges[vertexIndex];
                for (int k = range.x; k < range.x + range.y; k++)
                {
                    var deltaLR = muscleDeltasLR[(muscleCombinedVertexCount * meshIndex) + muscleVertexEntries[k].x];
                    accumulated = accumulated + (deltaLR.deltaLeft * weightLeftRight.x) + (deltaLR.deltaRight * weightLeftRight.y);
                }

                if (applyBreast)
                {
                    float2 bustData = bustSizes[meshIndex];
                    float2 bustDataSat = math.saturate(bustData.x);
                    float muscleMass = math.saturate((muscleData[finalIndex] - muscleMassStartWeight) / muscleMassRange);
                    var sizeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, bustSizeShapeIndex.x, bustSizeShapeIndex.y, bustData.x, vertexCount);
                    var sizeMuscleDelta = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, bustSizeMuscleShapeIndex.x, bustSizeMuscleShapeIndex.y, muscleMass, vertexCount);
                    var shapeDelta = meshShapeDeltas.SampleDeltaShapeBuffer(vertexIndex, meshShapeFrameWeights, breastShapeIndex.x, breastShapeIndex.y, bustData.y, vertexCount);

                    sizeDelta = sizeDelta + (sizeMuscleDelta + shapeDelta) * bustDataSat.x;
                    accumulated = accumulated + sizeDelta;
                }

                finalVertexDeltas[finalIndex] = accumulated;
            }
        }

        #endregion

    }

}

#endif
