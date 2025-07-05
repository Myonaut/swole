#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Jobs;

using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

using Swole.API.Unity.Animation;
using Swole.DataStructures;

namespace Swole.API.Unity
{
    public class BipedalCharacterHeightJobs : SingletonBehaviour<BipedalCharacterHeightJobs>
    {

        [NonSerialized]
        protected bool destroyed;

        public override void OnDestroyed()
        {

            destroyed = true;

            registered.Clear();

            base.OnDestroyed();

            try
            {
                if (transforms.isCreated)
                {
                    transforms.Dispose();
                    transforms = default;
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
                if (transformsToSet.isCreated)
                {
                    transformsToSet.Dispose();
                    transformsToSet = default;
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
                if (transformData.IsCreated)
                {
                    transformData.Dispose();
                    transformData = default;
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
                if (transformDataToSet.IsCreated)
                {
                    transformDataToSet.Dispose();
                    transformDataToSet = default;
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
                if (limbData.IsCreated)
                {
                    limbData.Dispose();
                    limbData = default;
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
                if (previousSizes.IsCreated)
                {
                    previousSizes.Dispose();
                    previousSizes = default;
                }
            } 
            catch(Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            try
            {
                if (currentSizes.IsCreated)
                {
                    currentSizes.Dispose();
                    currentSizes = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

        }

        public static int ExecutionPriority => CustomIKManagerUpdater.ExecutionPriority - 1; // Update before IK
        public override int Priority => base.Priority;

        public override void OnFixedUpdate()
        {
        }

        protected bool updateFlag;
        public override void OnLateUpdate()
        {
            if (destroyed /*|| !updateFlag*/) return; 

            updateFlag = false;

            JobHandle handle = new SharedJobs.FetchTransformDataJob()
            {

                transformData = transformData.AsArray()

            }.Schedule(transforms, default);

            handle = new UpdateLimbsJob()
            {

                limbData = limbData,
                transformData_READ = transformData,
                transformData_WRITE = transformDataToSet,
                previousSizes = previousSizes,
                currentSizes = currentSizes

            }.Schedule(registered.Count, 1, handle);
             
            handle = new UpdateTransformDataJob()
            {

                transformData = transformDataToSet

            }.Schedule(transformsToSet, handle);

            handle.Complete();
        }

        public override void OnUpdate()
        {
        }

        [Serializable]
        public struct LimbData
        {
            public float2 socketLimbSplits;

            public float4 limbPreservation;
            public float2 kneePreservation;

            public float3 leftShoulderExtensionAxis;
            public float3 leftArmWidthExtensionAxis;
            public float3 leftArmExtensionAxis;
            public float3 leftArmRotationAxis;

            public float3 rightShoulderExtensionAxis;
            public float3 rightArmWidthExtensionAxis;
            public float3 rightArmExtensionAxis;
            public float3 rightArmRotationAxis;

            public float3 leftHipExtensionAxis;
            public float3 leftLegWidthExtensionAxis;
            public float3 leftLegExtensionAxis;
            public float3 leftLegRotationAxis;

            public float3 rightHipExtensionAxis;
            public float3 rightLegWidthExtensionAxis;
            public float3 rightLegExtensionAxis;
            public float3 rightLegRotationAxis;

            public float3 spineExtensionAxis;

            public float3 neckExtensionAxis;
        }

        [Serializable]
        public struct Sizes
        {
            public float spineLength;
            public float neckLength;
            public float4 widths;
            public float4 armLengths;
            public float4 legLengths;
        }

        protected override void OnAwake()
        {

            base.OnAwake();

            registered = new List<IndexReference>();

            transforms = new TransformAccessArray(TransformCount);
            transformsToSet = new TransformAccessArray(TransformCount);
            transformData = new NativeList<TransformDataWorldLocal>(TransformCount, Allocator.Persistent);
            transformDataToSet = new NativeList<TransformDataWorldLocal>(TransformCount, Allocator.Persistent); 

            limbData = new NativeList<LimbData>(10, Allocator.Persistent);

            previousSizes = new NativeList<Sizes>(2, Allocator.Persistent);
            currentSizes = new NativeList<Sizes>(2, Allocator.Persistent);
        }

        public class IndexReference : IDisposable
        {
            private BipedalCharacterHeightJobs owner;

            private int index;
            public int Index => index;

            public delegate void SetIndexDelegate(int previousIndex, int newIndex);
            public event SetIndexDelegate OnSetIndex;

            internal void SetIndex(int newIndex)
            {
                OnSetIndex?.Invoke(index, newIndex);
                index = newIndex;
            }

            internal IndexReference(BipedalCharacterHeightJobs owner, int index)
            {
                this.owner = owner;
                this.index = index;
            }

            public bool IsValid => index >= 0;

            public void Dispose()
            {
                if (index >= 0 && owner != null && !owner.destroyed)
                {

                    int swapIndex = owner.registered.Count - 1;
                    var swapped = owner.registered[swapIndex];

                    owner.registered[index] = swapped;
                    owner.registered.RemoveAt(swapIndex);

                    int transformIndex = index * TransformCount;
                    for(int a = TransformCount - 1; a >= 0; a--)
                    {
                        int ind = transformIndex + a;

                        owner.transforms.RemoveAtSwapBack(ind);
                        owner.transformData.RemoveAtSwapBack(ind);
                    }
                    transformIndex = index * SettableTransformCount; 
                    for (int a = SettableTransformCount - 1; a >= 0; a--)
                    {
                        int ind = transformIndex + a;

                        owner.transformsToSet.RemoveAtSwapBack(ind); 
                        owner.transformDataToSet.RemoveAtSwapBack(ind);
                    }

                    owner.limbData.RemoveAtSwapBack(index);

                    owner.previousSizes.RemoveAtSwapBack(index);
                    owner.currentSizes.RemoveAtSwapBack(index);

                    if (!ReferenceEquals(this, swapped)) swapped.SetIndex(index);
                }

                index = -1;
                owner = null;
            }

            public float4 GetWidths() => owner.currentSizes[index].widths;
            public void GetLimbLengths(out float4 armLengths, out float4 legLengths)
            {
                var data = owner.currentSizes[index];
                armLengths = data.armLengths;
                legLengths = data.legLengths;
            }

            public float2 GetShoulderWidths() => owner.currentSizes[index].widths.xy;
            public float4 GetArmLengths() => owner.currentSizes[index].armLengths;

            public float2 GetHipWidths() => owner.currentSizes[index].widths.zw;
            public float4 GetLegLengths() => owner.currentSizes[index].legLengths;
            
            public float GetSpineLength() => owner.currentSizes[index].spineLength;
            public float GetNeckLength() => owner.currentSizes[index].neckLength;


            public void SetWidths(float4 widths)
            {
                var data = owner.currentSizes[index];
                data.widths = widths;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }
            public void SetShoulderWidths(float2 shoulderWidths)
            {
                var data = owner.currentSizes[index];
                data.widths.xy = shoulderWidths;
                owner.currentSizes[index] = data; 

                owner.updateFlag = true;
            }
            public void SetHipWidths(float2 hipWidths)
            {
                var data = owner.currentSizes[index];
                data.widths.zw = hipWidths;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }
            public void SetLimbLengths(float4 armLengths, float4 legLengths)
            {
                var data = owner.currentSizes[index];
                data.armLengths = armLengths;
                data.legLengths = legLengths;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }
            public void SetArmLengths(float4 armLengths)
            {
                var data = owner.currentSizes[index];
                data.armLengths = armLengths;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }
            public void SetLegLengths(float4 legLengths)
            {
                var data = owner.currentSizes[index];
                data.legLengths = legLengths;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }
            public void SetSpineLength(float spineLength)
            {
                var data = owner.currentSizes[index];
                data.spineLength = spineLength;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }
            public void SetNeckLength(float neckLength)
            {
                var data = owner.currentSizes[index];
                data.neckLength = neckLength;
                owner.currentSizes[index] = data;

                owner.updateFlag = true;
            }


            public float4 GetPreviousWidths() => owner.previousSizes[index].widths;
            public float2 GetPreviousShoulderWidths() => owner.previousSizes[index].widths.xy;
            public float2 GetPreviousHipWidths() => owner.previousSizes[index].widths.zw;

            public void GetPreviousLimbLengths(out float4 armLengths, out float4 legLengths)
            {
                var data = owner.previousSizes[index];
                armLengths = data.armLengths;
                legLengths = data.legLengths;
            }
            public float4 GetPreviousArmLengths() => owner.previousSizes[index].armLengths;
            public float4 GetPreviousLegLengths() => owner.previousSizes[index].legLengths;
            public float GetPreviousSpineLength() => owner.previousSizes[index].spineLength;
            public float GetPreviousNeckLength() => owner.previousSizes[index].neckLength;


            public void SetPreviousWidths(float4 widths)
            {
                var data = owner.previousSizes[index];
                data.widths = widths;
                owner.previousSizes[index] = data;
            }
            public void SetPreviousShoulderWidths(float2 shoulderWidths)
            {
                var data = owner.previousSizes[index];
                data.widths.xy = shoulderWidths;
                owner.previousSizes[index] = data;
            }
            public void SetPreviousHipWidths(float2 hipWidths)
            {
                var data = owner.previousSizes[index];
                data.widths.zw = hipWidths;
                owner.previousSizes[index] = data;
            }
            public void SetPrevousLimbLengths(float4 armLengths, float4 legLengths)
            {
                var data = owner.previousSizes[index];
                data.armLengths = armLengths;
                data.legLengths = legLengths;
                owner.previousSizes[index] = data;
            }
            public void SetPrevousArmLengths(float4 armLengths)
            {
                var data = owner.previousSizes[index];
                data.armLengths = armLengths;
                owner.previousSizes[index] = data;
            }
            public void SetPreviousLegLengths(float4 legLengths)
            {
                var data = owner.previousSizes[index];
                data.legLengths = legLengths;
                owner.previousSizes[index] = data;
            }
            public void SetPrevousSpineLength(float spineLength)
            {
                var data = owner.previousSizes[index];
                data.spineLength = spineLength;
                owner.previousSizes[index] = data;
            }
            public void SetPreviousNeckLength(float neckLength)
            {
                var data = owner.previousSizes[index];
                data.neckLength = neckLength;
                owner.previousSizes[index] = data;
            }


            public float4 GetLimbPreservationWeights()
            {
                return owner.limbData[index].limbPreservation;
            }
            public float2 GetKneePreservationWeights()
            {
                return owner.limbData[index].kneePreservation; 
            }

            public void SetLimbPreservationWeights(float4 weights)
            {
                var data = owner.limbData[index];
                data.limbPreservation = weights;
                owner.limbData[index] = data;

                owner.updateFlag = true;
            }
            public void SetKneePreservationWeights(float2 weights)
            {
                var data = owner.limbData[index];
                data.kneePreservation = weights;
                owner.limbData[index] = data;

                owner.updateFlag = true;
            }

            public float2 GetSocketLimbSplits()
            {
                return owner.limbData[index].socketLimbSplits;
            }
            public void SetSocketLimbSplits(float2 socketLimbSplits)
            {
                var data = owner.limbData[index];
                data.socketLimbSplits = socketLimbSplits;
                owner.limbData[index] = data;

                owner.updateFlag = true;
            }
        }

        protected List<IndexReference> registered;

        protected TransformAccessArray transforms;
        protected TransformAccessArray transformsToSet;
        protected NativeList<TransformDataWorldLocal> transformData;
        protected NativeList<TransformDataWorldLocal> transformDataToSet;
        protected void AddTransform(Transform transform, bool isSettable = true)
        {
            transforms.Add(transform);
            transformData.Add(new TransformDataWorldLocal()); 
            if (isSettable) 
            {
                transformsToSet.Add(transform);
                transformDataToSet.Add(new TransformDataWorldLocal());
            }
        }

        protected NativeList<LimbData> limbData;

        protected NativeList<Sizes> previousSizes;
        protected NativeList<Sizes> currentSizes;

        public const int TransformCount = 26;
        public const int SettableTransformCount = 21;

        public const int ShoulderLeftIndex = 0;
        public const int ArmLeftIndex = 1;
        public const int ForearmLeftIndex = 2;
        public const int WristLeftIndex = 3;

        public const int ShoulderRightIndex = 4;
        public const int ArmRightIndex = 5;
        public const int ForearmRightIndex = 6;
        public const int WristRightIndex = 7;

        public const int HipLeftIndex = 8;
        public const int LegLeftIndex = 9;
        public const int CalfLeftIndex = 10;
        public const int FootLeftIndex = 11;

        public const int HipRightIndex = 12;
        public const int LegRightIndex = 13;
        public const int CalfRightIndex = 14;
        public const int FootRightIndex = 15;

        public const int Spine1Index = 16;
        public const int Spine2Index = 17;
        public const int Spine3Index = 18;

        public const int PelvisIndex = 19;

        public const int NeckIndex = 20;

        public const int ParentArmLeftIndex = 21;
        public const int ParentArmRightIndex = 22;
        public const int ParentLegLeftIndex = 23;
        public const int ParentLegRightIndex = 24;
        public const int ParentPelvisIndex = 25;

        public IndexReference RegisterLocal(float3 extensionAxisSpine, float3 bendAxisSpine,
            Transform spine1, Transform spine2, Transform spine3, float lengthSpine,
            float3 extensionAxisShoulders, float3 widthExtensionAxisArms, float3 extensionAxisArms, float3 bendAxisArms, float3 extensionAxisHips, float3 widthExtensionAxisLegs, float3 extensionAxisLegs, float3 bendAxisLegs, bool flipShoulderAxisForMirror, bool flipArmWidthAxisForMirror, bool flipArmAxisForMirror, bool flipHipAxisForMirror, bool flipLegWidthAxisForMirror, bool flipLegAxisForMirror,
            float shoulderArmWidthSplit,
            Transform shoulderLeft, float shoulderWidthLeft, Transform parentArmLeft, Transform armLeft, Transform forearmLeft, Transform wristLeft, float lengthArmLeft, float lengthForearmLeft,
            Transform shoulderRight, float shoulderWidthRight, Transform parentArmRight, Transform armRight, Transform forearmRight, Transform wristRight, float lengthArmRight, float lengthForearmRight,
            float hipThighWidthSplit,
            Transform hipLeft, float hipWidthLeft, Transform parentLegLeft, Transform legLeft, Transform calfLeft, Transform footLeft, float lengthLegLeft, float lengthCalfLeft,
            Transform hipRight, float hipWidthRight, Transform parentLegRight, Transform legRight, Transform calfRight, Transform footRight, float lengthLegRight, float lengthCalfRight,
            Transform pelvis,
            float3 extensionAxisNeck, float3 bendAxisNeck, Transform neck, float lengthNeck)
        {
            if (destroyed) return null;

            int index = registered.Count;

            AddTransform(shoulderLeft); // 1
            AddTransform(armLeft); // 2
            AddTransform(forearmLeft); // 3
            AddTransform(wristLeft); // 4
            AddTransform(shoulderRight); // 5
            AddTransform(armRight); // 6
            AddTransform(forearmRight); // 7
            AddTransform(wristRight); // 8
            AddTransform(hipLeft); // 9
            AddTransform(legLeft); // 10
            AddTransform(calfLeft); // 11
            AddTransform(footLeft); // 12
            AddTransform(hipRight); // 13
            AddTransform(legRight); // 14
            AddTransform(calfRight); // 15
            AddTransform(footRight); // 16
            AddTransform(spine1); // 17
            AddTransform(spine2); // 18
            AddTransform(spine3); // 19
            AddTransform(pelvis); // 20
            AddTransform(neck); // 21

            AddTransform(parentArmLeft, false); // 22
            AddTransform(parentArmRight, false); // 23
            AddTransform(parentLegLeft, false); // 24
            AddTransform(parentLegRight, false); // 25
            AddTransform(pelvis.parent, false); // 26

            var limbData_ = new LimbData();
            limbData_.limbPreservation = new float4(1, 1, 1, 1);
            limbData_.kneePreservation = new float2(1, 1);

            limbData_.socketLimbSplits = new float2(shoulderArmWidthSplit, hipThighWidthSplit);

            limbData_.leftShoulderExtensionAxis = extensionAxisShoulders;
            limbData_.leftArmWidthExtensionAxis = widthExtensionAxisArms;
            limbData_.leftArmExtensionAxis = extensionAxisArms;
            limbData_.leftArmRotationAxis = bendAxisArms;

            limbData_.rightShoulderExtensionAxis = flipShoulderAxisForMirror ? -extensionAxisShoulders : extensionAxisShoulders;
            limbData_.rightArmWidthExtensionAxis = flipArmWidthAxisForMirror ? -widthExtensionAxisArms : widthExtensionAxisArms;
            limbData_.rightArmExtensionAxis = extensionAxisArms;
            limbData_.rightArmRotationAxis = flipArmAxisForMirror ? -bendAxisArms : bendAxisArms;

            limbData_.leftHipExtensionAxis = extensionAxisHips;
            limbData_.leftLegWidthExtensionAxis = widthExtensionAxisLegs;
            limbData_.leftLegExtensionAxis = extensionAxisLegs;
            limbData_.leftLegRotationAxis = bendAxisLegs;

            limbData_.rightHipExtensionAxis = flipHipAxisForMirror ? -extensionAxisHips : extensionAxisHips; 
            limbData_.rightLegWidthExtensionAxis = flipLegWidthAxisForMirror ? -widthExtensionAxisLegs : widthExtensionAxisLegs;
            limbData_.rightLegExtensionAxis = extensionAxisLegs;
            limbData_.rightLegRotationAxis = flipLegAxisForMirror ? -bendAxisLegs : bendAxisLegs; 

            limbData_.spineExtensionAxis = extensionAxisSpine;
            limbData_.neckExtensionAxis = extensionAxisNeck;

            limbData.Add(limbData_); 

            float4 armLengths = new float4(lengthArmLeft, lengthForearmLeft, lengthArmRight, lengthForearmRight);
            float4 legLengths = new float4(lengthLegLeft, lengthCalfLeft, lengthLegRight, lengthCalfRight); 

            var sizes = new Sizes();
            sizes.spineLength = lengthSpine;
            sizes.neckLength = lengthNeck;
            sizes.armLengths = armLengths;
            sizes.legLengths = legLengths;
            sizes.widths = new float4(shoulderWidthLeft, shoulderWidthRight, hipWidthLeft, hipWidthRight);

            previousSizes.Add(sizes);
            currentSizes.Add(sizes);

            updateFlag = true;

            var indRef = new IndexReference(this, index);
            registered.Add(indRef);
            return indRef;
        }
        public void UnregisterLocal(IndexReference indRef)
        {
            if (destroyed || indRef == null) return;

            indRef.Dispose(); 
        }

        public static IndexReference Register(float3 extensionAxisSpine, float3 bendAxisSpine,
            Transform spine1, Transform spine2, Transform spine3, float lengthSpine,
            float3 extensionAxisShoulders, float3 widthExtensionAxisArms, float3 extensionAxisArms, float3 bendAxisArms, float3 extensionAxisHips, float3 widthExtensionAxisLegs, float3 extensionAxisLegs, float3 bendAxisLegs, bool flipShoulderAxisForMirror, bool flipArmWidthAxisForMirror, bool flipArmAxisForMirror, bool flipHipAxisForMirror, bool flipLegWidthAxisForMirror, bool flipLegAxisForMirror,
            float shoulderArmWidthSplit,
            Transform shoulderLeft, float shoulderWidthLeft, Transform parentArmLeft, Transform armLeft, Transform forearmLeft, Transform wristLeft, float lengthArmLeft, float lengthForearmLeft,
            Transform shoulderRight, float shoulderWidthRight, Transform parentArmRight, Transform armRight, Transform forearmRight, Transform wristRight, float lengthArmRight, float lengthForearmRight,
            float hipThighWidthSplit,
            Transform hipLeft, float hipWidthLeft, Transform parentLegLeft, Transform legLeft, Transform calfLeft, Transform footLeft, float lengthLegLeft, float lengthCalfLeft,
            Transform hipRight, float hipWidthRight, Transform parentLegRight, Transform legRight, Transform calfRight, Transform footRight, float lengthLegRight, float lengthCalfRight,
            Transform pelvis,
            float3 extensionAxisNeck, float3 bendAxisNeck, Transform neck, float lengthNeck)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.RegisterLocal(extensionAxisSpine, bendAxisSpine,
                spine1, spine2, spine3, lengthSpine,
                extensionAxisShoulders, widthExtensionAxisArms, extensionAxisArms, bendAxisArms, extensionAxisHips, widthExtensionAxisLegs, extensionAxisLegs, bendAxisLegs, flipShoulderAxisForMirror, flipArmWidthAxisForMirror, flipArmAxisForMirror, flipHipAxisForMirror, flipLegWidthAxisForMirror, flipLegAxisForMirror,
                shoulderArmWidthSplit,
                shoulderLeft, shoulderWidthLeft, parentArmLeft, armLeft, forearmLeft, wristLeft, lengthArmLeft, lengthForearmLeft,
                shoulderRight, shoulderWidthRight, parentArmRight, armRight, forearmRight, wristRight, lengthArmRight, lengthForearmRight,
                hipThighWidthSplit,
                hipLeft, hipWidthLeft, parentLegLeft, legLeft, calfLeft, footLeft, lengthLegLeft, lengthCalfLeft,
                hipRight, hipWidthRight, parentLegRight, legRight, calfRight, footRight, lengthLegRight, lengthCalfRight,
                pelvis,
                extensionAxisNeck, bendAxisNeck, neck, lengthNeck);
        }

        public static void Unregister(IndexReference indRef)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;
             
            instance.UnregisterLocal(indRef); 
        }

        [BurstCompile]
        public struct UpdateTransformDataJob : IJobParallelForTransform
        {

            [ReadOnly]
            public NativeList<TransformDataWorldLocal> transformData;

            public void Execute(int index, TransformAccess transform)
            {
                var data = transformData[index];
                transform.SetLocalPositionAndRotation(data.localPosition, data.localRotation);
            }
        }

        [BurstCompile]
        public struct UpdateLimbsJob : IJobParallelFor
        {

            [ReadOnly]
            public NativeList<LimbData> limbData;

            [ReadOnly]
            public NativeList<TransformDataWorldLocal> transformData_READ;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformDataWorldLocal> transformData_WRITE;

            [NativeDisableParallelForRestriction]
            public NativeList<Sizes> previousSizes;
            [ReadOnly]
            public NativeList<Sizes> currentSizes;

            public void ChangeSpineLength(int2 index, float3 lengthDelta)
            {
                TransformDataWorldLocal data = transformData_READ[index.x];
                data.localPosition = data.localPosition + lengthDelta; 
                transformData_WRITE[index.y] = data; 
            }

            public bool CalcRiseBend(float3 bendAxis,/* quaternion parentRot,*/ quaternion startToEndRot, float height, float lengthA, float lengthB, out quaternion riseRot, out quaternion bendRot)
            {
                riseRot = quaternion.identity;
                bendRot = quaternion.identity;

                float rise = math.acos(((height * height) + (lengthA * lengthA) - (lengthB * lengthB)) / (2 * height * lengthA));
                float bend = math.acos(((lengthA * lengthA) + (lengthB * lengthB) - (height * height)) / (2 * lengthA * lengthB));

                if (float.IsNaN(rise) || float.IsInfinity(rise) || float.IsNaN(bend) || float.IsInfinity(bend)) return false;

                riseRot = quaternion.AxisAngle(bendAxis, -rise);
                bendRot = quaternion.AxisAngle(bendAxis, math.PI - bend);

                riseRot = /*math.mul(parentRot, */math.mul(startToEndRot, riseRot)/*)*/;
                //bendRot = math.mul(riseRot, bendRot);

                return true;
            }
            public bool CalcRiseBendWorld(float3 bendAxis, quaternion parentRot, quaternion startToEndRot, float height, float lengthA, float lengthB, out quaternion riseWorldRot, out quaternion bendWorldRot)
            {
                riseWorldRot = quaternion.identity;
                bendWorldRot = quaternion.identity;

                float rise = math.acos(((height * height) + (lengthA * lengthA) - (lengthB * lengthB)) / (2 * height * lengthA));
                float bend = math.acos(((lengthA * lengthA) + (lengthB * lengthB) - (height * height)) / (2 * lengthA * lengthB));

                if (float.IsNaN(rise) || float.IsInfinity(rise) || float.IsNaN(bend) || float.IsInfinity(bend)) return false;

                riseWorldRot = quaternion.AxisAngle(bendAxis, -rise);
                bendWorldRot = quaternion.AxisAngle(bendAxis, math.PI - bend);

                riseWorldRot = math.mul(parentRot, math.mul(startToEndRot, riseWorldRot));
                bendWorldRot = math.mul(riseWorldRot, bendWorldRot);

                return true;
            }

            public void ChangeLimbLength(float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int2 startIndex, int2 middleIndex, int2 endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal parentData = transformData_READ[parentIndex];
                TransformDataWorldLocal startData = transformData_READ[startIndex.x];
                TransformDataWorldLocal middleData = transformData_READ[middleIndex.x];
                TransformDataWorldLocal endData = transformData_READ[endIndex.x];

                quaternion inverseParentRot = math.inverse(parentData.rotation);

                float3 toEnd = endData.position - startData.position;
                float height = math.length(toEnd);
                float3 toEndDir = math.rotate(inverseParentRot, toEnd / height);

                quaternion startToEndRot = Quaternion.FromToRotation(extensionAxis, toEndDir);
                bool flagA = CalcRiseBend(bendAxis, startToEndRot, height, prevLengthA, prevLengthB, out var oldRiseRot, out var oldBendRot);
                bool flagB = CalcRiseBend(bendAxis, startToEndRot, height, newLengthA, newLengthB, out var riseRot, out var bendRot);
                bool flag = flagA && flagB;

                riseRot = math.mul((math.mul(math.inverse(oldRiseRot), riseRot)), startData.localRotation);
                bendRot = math.mul((math.mul(math.inverse(oldBendRot), bendRot)), middleData.localRotation);

                middleData.localPosition = middleData.localPosition + extensionAxis * lengthChangeA;
                endData.localPosition = endData.localPosition + extensionAxis * lengthChangeB;

                quaternion initialStartRot = startData.localRotation;
                quaternion initialMiddleRot = middleData.localRotation;

                startData.localRotation = math.slerp(startData.localRotation, math.select(startData.localRotation.value, riseRot.value, flag), weight);
                middleData.localRotation = math.slerp(middleData.localRotation, math.select(middleData.localRotation.value, bendRot.value, flag), weight);

                quaternion endOffsetRot = math.inverse(math.mul(math.mul(math.inverse(initialStartRot), startData.localRotation), math.mul(math.inverse(initialMiddleRot), middleData.localRotation)));
                endData.localRotation = math.mul(endOffsetRot, endData.localRotation);

                transformData_WRITE[startIndex.y] = startData;
                transformData_WRITE[middleIndex.y] = middleData;
                transformData_WRITE[endIndex.y] = endData;
            }
            public void ChangeLimbLengthWorld(float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int2 startIndex, int2 middleIndex, int2 endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal parentData = transformData_READ[parentIndex];
                TransformDataWorldLocal startData = transformData_READ[startIndex.x];
                TransformDataWorldLocal middleData = transformData_READ[middleIndex.x];
                TransformDataWorldLocal endData = transformData_READ[endIndex.x];

                quaternion inverseParentRot = math.inverse(parentData.rotation);

                float3 toEnd = endData.position - startData.position;
                float height = math.length(toEnd);
                float3 toEndDir = math.rotate(inverseParentRot, toEnd / height);

                quaternion startToEndRot = Quaternion.FromToRotation(extensionAxis, toEndDir);
                bool flagA = CalcRiseBendWorld(bendAxis, parentData.rotation, startToEndRot, height, prevLengthA, prevLengthB, out var oldRiseRot, out var oldBendRot);
                bool flagB = CalcRiseBendWorld(bendAxis, parentData.rotation, startToEndRot, height, newLengthA, newLengthB, out var riseRot, out var bendRot);
                bool flag = flagA && flagB;

                riseRot = math.mul(startData.rotation, (math.mul(math.inverse(oldRiseRot), riseRot)));
                bendRot = math.mul(middleData.rotation, (math.mul(math.inverse(oldBendRot), bendRot)));

                middleData.localPosition = middleData.localPosition + extensionAxis * lengthChangeA;
                endData.localPosition = endData.localPosition + extensionAxis * lengthChangeB;

                quaternion initialStartRot = startData.localRotation;
                quaternion initialMiddleRot = middleData.localRotation;

                startData.localRotation = math.slerp(startData.localRotation, math.select(startData.localRotation.value, math.mul(inverseParentRot, riseRot).value, flag), weight); 
                middleData.localRotation = math.slerp(middleData.localRotation, math.select(middleData.localRotation.value, math.mul(math.inverse(math.mul(parentData.rotation, startData.localRotation)), bendRot).value, flag), weight);

                quaternion endOffsetRot = math.inverse(math.mul(math.mul(math.inverse(initialStartRot), startData.localRotation), math.mul(math.inverse(initialMiddleRot), middleData.localRotation)));
                endData.localRotation = math.mul(endOffsetRot, endData.localRotation);

                transformData_WRITE[startIndex.y] = startData;
                transformData_WRITE[middleIndex.y] = middleData;
                transformData_WRITE[endIndex.y] = endData;
            }

            public void ChangeLegLength(TransformDataWorldLocal pelvisTransformData, TransformDataWorldLocal pelvisParentTransformData, out TransformDataWorldLocal pelvisTransformDataOffset, float kneePreservingWeight, float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int2 startIndex, int2 middleIndex, int2 endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal startData = transformData_READ[startIndex.x];
                TransformDataWorldLocal middleData = transformData_READ[middleIndex.x];

                ChangeLimbLength(weight * (1f - kneePreservingWeight), extensionAxis, bendAxis, parentIndex, startIndex, middleIndex, endIndex, newLengthA, newLengthB, prevLengthA, prevLengthB, lengthChangeA, lengthChangeB);

                float3 offsetA = math.rotate(startData.rotation, extensionAxis) * lengthChangeA;
                float3 offsetB = math.rotate(middleData.rotation, extensionAxis) * lengthChangeB;

                quaternion inverseParentRotation = math.inverse(pelvisParentTransformData.rotation);

                pelvisTransformData.localPosition = math.lerp(pelvisTransformData.localPosition, pelvisTransformData.localPosition - math.rotate(inverseParentRotation, offsetA + offsetB), weight * kneePreservingWeight);

                pelvisTransformDataOffset = pelvisTransformData;
            }

            public void ChangeLegLengthWorld(TransformDataWorldLocal pelvisTransformData, TransformDataWorldLocal pelvisParentTransformData, out TransformDataWorldLocal pelvisTransformDataOffset, float kneePreservingWeight, float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int2 startIndex, int2 middleIndex, int2 endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal startData = transformData_READ[startIndex.x];
                TransformDataWorldLocal middleData = transformData_READ[middleIndex.x];

                ChangeLimbLengthWorld(weight * (1f - kneePreservingWeight), extensionAxis, bendAxis, parentIndex, startIndex, middleIndex, endIndex, newLengthA, newLengthB, prevLengthA, prevLengthB, lengthChangeA, lengthChangeB);

                float3 offsetA = math.rotate(startData.rotation, extensionAxis) * lengthChangeA;
                float3 offsetB = math.rotate(middleData.rotation, extensionAxis) * lengthChangeB;

                quaternion inverseParentRotation = math.inverse(pelvisParentTransformData.rotation);

                pelvisTransformData.localPosition = math.lerp(pelvisTransformData.localPosition, pelvisTransformData.localPosition - math.rotate(inverseParentRotation, offsetA + offsetB), weight * kneePreservingWeight);

                pelvisTransformDataOffset = pelvisTransformData;
            }

            public void ChangeShoulderWidth() { }
            public void ChangeHipWidth() { }

            public void Execute(int index)
            {

                var limbData_ = limbData[index];

                var prevSizes = previousSizes[index];
                var sizes = currentSizes[index];
                
                int2 transformIndex = new int2(index * TransformCount, index * SettableTransformCount);

                int2 armLeftIndex = transformIndex + ArmLeftIndex;
                int2 armRightIndex = transformIndex + ArmRightIndex;
                int2 legLeftIndex = transformIndex + LegLeftIndex;
                int2 legRightIndex = transformIndex + LegRightIndex;

                float4 armLengthChanges = sizes.armLengths - prevSizes.armLengths;
                float4 legLengthChanges = sizes.legLengths - prevSizes.legLengths;

                int2 pelvisIndex = transformIndex + PelvisIndex;
                int pelvisParentIndex = transformIndex.x + ParentPelvisIndex;
                TransformDataWorldLocal pelvisTransformData = transformData_READ[pelvisIndex.x];
                TransformDataWorldLocal pelvisParentTransformData = transformData_READ[pelvisParentIndex];             

                //ChangeLimbLengthWorld(limbData_.limbPreservation.x, limbData_.leftArmExtensionAxis, limbData_.leftArmRotationAxis, transformIndex + ParentArmLeftIndex, armLeftIndex, transformIndex + ForearmLeftIndex, transformIndex + WristLeftIndex, sizes.armLengths.x, sizes.armLengths.y, prevSizes.armLengths.x, prevSizes.armLengths.y, armLengthChanges.x, armLengthChanges.y); // left arm
                //ChangeLimbLengthWorld(limbData_.limbPreservation.y, limbData_.rightArmExtensionAxis, limbData_.rightArmRotationAxis, transformIndex + ParentArmRightIndex, armRightIndex, transformIndex + ForearmRightIndex, transformIndex + WristRightIndex, sizes.armLengths.z, sizes.armLengths.w, prevSizes.armLengths.z, prevSizes.armLengths.w, armLengthChanges.z, armLengthChanges.w); // right arm

                // forget bending preservation for now and use more performant option
                //transformData_WRITE[armLeftIndex] = transformData_READ[armLeftIndex]; // not needed because of width split as bottom of method
                //transformData_WRITE[armRightIndex] = transformData_READ[armRightIndex];
                ChangeSpineLength(transformIndex + ForearmLeftIndex, limbData_.leftArmExtensionAxis * armLengthChanges.x);
                ChangeSpineLength(transformIndex + ForearmRightIndex, limbData_.rightArmExtensionAxis * armLengthChanges.z);
                ChangeSpineLength(transformIndex + WristLeftIndex, limbData_.leftArmExtensionAxis * armLengthChanges.y);
                ChangeSpineLength(transformIndex + WristRightIndex, limbData_.rightArmExtensionAxis * armLengthChanges.w);
                
                // Legs blend weights between moving pelvis and rotating legs
                /*TransformDataWorldLocal pelvisTransformDataA, pelvisTransformDataB;
                ChangeLegLengthWorld(pelvisTransformData, pelvisParentTransformData, out pelvisTransformDataA, limbData_.kneePreservation.x, limbData_.limbPreservation.z, limbData_.leftLegExtensionAxis, limbData_.leftLegRotationAxis, transformIndex + ParentLegLeftIndex, legLeftIndex, transformIndex + CalfLeftIndex, transformIndex + FootLeftIndex, sizes.legLengths.x, sizes.legLengths.y, prevSizes.legLengths.x, prevSizes.legLengths.y, legLengthChanges.x, legLengthChanges.y); // left leg
                ChangeLegLengthWorld(pelvisTransformData, pelvisParentTransformData, out pelvisTransformDataB, limbData_.kneePreservation.y, limbData_.limbPreservation.w, limbData_.rightLegExtensionAxis, limbData_.rightLegRotationAxis, transformIndex + ParentLegRightIndex, legRightIndex, transformIndex + CalfRightIndex, transformIndex + FootRightIndex, sizes.legLengths.z, sizes.legLengths.w, prevSizes.legLengths.z, prevSizes.legLengths.w, legLengthChanges.z, legLengthChanges.w); // right leg

                pelvisTransformData.localPosition = (pelvisTransformDataA.localPosition + pelvisTransformDataB.localPosition) * 0.5f;*/
                //pelvisTransformData.localRotation = math.slerp(pelvisTransformDataA.localRotation, pelvisTransformDataB.localRotation, 0.5f);// do not use

                // forget bending preservation for now and use more performant option
                //transformData_WRITE[legLeftIndex] = transformData_READ[legLeftIndex]; // not needed because of width split at bottom of method
                //transformData_WRITE[legRightIndex] = transformData_READ[legRightIndex]; 
                ChangeSpineLength(transformIndex + CalfLeftIndex, limbData_.leftLegExtensionAxis * legLengthChanges.x);
                ChangeSpineLength(transformIndex + CalfRightIndex, limbData_.rightLegExtensionAxis * legLengthChanges.z);
                ChangeSpineLength(transformIndex + FootLeftIndex, limbData_.leftLegExtensionAxis * legLengthChanges.y); 
                ChangeSpineLength(transformIndex + FootRightIndex, limbData_.rightLegExtensionAxis * legLengthChanges.w);
                
                transformData_WRITE[pelvisIndex.y] = pelvisTransformData;
                
                float3 spineLengthDelta = limbData_.spineExtensionAxis * (sizes.spineLength - prevSizes.spineLength); 
                ChangeSpineLength(transformIndex + Spine1Index, spineLengthDelta);
                ChangeSpineLength(transformIndex + Spine2Index, spineLengthDelta);
                ChangeSpineLength(transformIndex + Spine3Index, spineLengthDelta);

                float3 neckLengthDelta = limbData_.neckExtensionAxis * (sizes.neckLength - prevSizes.neckLength); 
                ChangeSpineLength(transformIndex + NeckIndex, neckLengthDelta);

                float2 socketLimbSplitsInv = 1f - limbData_.socketLimbSplits;
                float4 widthsDelta = sizes.widths - prevSizes.widths;
                float4 widthsDeltaAlt = widthsDelta * new float4(socketLimbSplitsInv.x, socketLimbSplitsInv.x, socketLimbSplitsInv.y, socketLimbSplitsInv.y);
                widthsDelta = widthsDelta * new float4(limbData_.socketLimbSplits.x, limbData_.socketLimbSplits.x, limbData_.socketLimbSplits.y, limbData_.socketLimbSplits.y);  

                ChangeSpineLength(transformIndex + ShoulderLeftIndex, limbData_.leftShoulderExtensionAxis * widthsDeltaAlt.x);
                ChangeSpineLength(transformIndex + ShoulderRightIndex, limbData_.rightShoulderExtensionAxis * widthsDeltaAlt.y);  
                ChangeSpineLength(transformIndex + HipLeftIndex, limbData_.leftHipExtensionAxis * widthsDeltaAlt.z);
                ChangeSpineLength(transformIndex + HipRightIndex, limbData_.rightHipExtensionAxis * widthsDeltaAlt.w);  

                ChangeSpineLength(armLeftIndex, limbData_.leftArmWidthExtensionAxis * widthsDelta.x);
                ChangeSpineLength(armRightIndex, limbData_.rightArmWidthExtensionAxis * widthsDelta.y);
                ChangeSpineLength(legLeftIndex, limbData_.leftLegWidthExtensionAxis * widthsDelta.z);
                ChangeSpineLength(legRightIndex, limbData_.rightLegWidthExtensionAxis * widthsDelta.w);

                //previousSizes[index] = sizes; // animators always running and resetting limbs, so this should never be done. If animators are disabled so are the height controller components.
            }
        }

    }
}

#endif