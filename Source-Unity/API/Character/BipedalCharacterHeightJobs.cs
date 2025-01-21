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
                if (limbAxis.IsCreated)
                {
                    limbAxis.Dispose();
                    limbAxis = default;
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
                if (previousLimbLengths.IsCreated)
                {
                    previousLimbLengths.Dispose();
                    previousLimbLengths = default;
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
                if (currentLimbLengths.IsCreated)
                {
                    currentLimbLengths.Dispose();
                    currentLimbLengths = default;
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
                if (previousSpineLengths.IsCreated)
                {
                    previousSpineLengths.Dispose(); 
                    previousSpineLengths = default;
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
                if (currentSpineLengths.IsCreated)
                {
                    currentSpineLengths.Dispose();
                    currentSpineLengths = default;
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
                if (limbPreservationWeights.IsCreated)
                {
                    limbPreservationWeights.Dispose();
                    limbPreservationWeights = default;
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
                if (kneePreservationWeights.IsCreated)
                {
                    kneePreservationWeights.Dispose();
                    kneePreservationWeights = default;
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
            if (destroyed || !updateFlag) return;

            updateFlag = false;

            JobHandle handle = new FetchTransformDataJob()
            {

                transformData = transformData

            }.Schedule(transforms, default);

            handle = new UpdateLimbsJob()
            {

                limbAxis = limbAxis,
                transformData = transformData,
                previousLimbLengths = previousLimbLengths,
                currentLimbLengths = currentLimbLengths,
                previousSpineLengths = previousSpineLengths,
                currentSpineLengths = currentSpineLengths,
                limbPreservationWeights = limbPreservationWeights,
                kneePreservationWeights = kneePreservationWeights

            }.Schedule(registered.Count, 1, handle);
             
            handle = new UpdateTransformDataJob()
            {

                transformData = transformData

            }.Schedule(transforms, handle);

            handle.Complete(); 
        }

        public override void OnUpdate()
        {
        }

        protected override void OnAwake()
        {

            base.OnAwake();

            registered = new List<IndexReference>();

            transforms = new TransformAccessArray(21);
            transformData = new NativeList<TransformDataWorldLocal>(21, Allocator.Persistent);

            limbAxis = new NativeList<float3>(10, Allocator.Persistent);

            previousLimbLengths = new NativeList<float4>(2, Allocator.Persistent);
            currentLimbLengths = new NativeList<float4>(2, Allocator.Persistent);

            previousSpineLengths = new NativeList<float>(1, Allocator.Persistent);
            currentSpineLengths = new NativeList<float>(1, Allocator.Persistent);

            limbPreservationWeights = new NativeList<float4>(1, Allocator.Persistent);
            kneePreservationWeights = new NativeList<float2>(1, Allocator.Persistent);
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

                    owner.registered[index] = owner.registered[swapIndex];
                    owner.registered.RemoveAt(swapIndex);

                    int transformIndex = index * 21;
                    for(int a = 20; a >= 0; a--)
                    {
                        owner.transforms.RemoveAtSwapBack(transformIndex + a);
                        owner.transformData.RemoveAtSwapBack(transformIndex + a); 
                    }

                    int limbAxisIndex = index * 10;
                    for (int a = 9; a >= 0; a--)
                    {
                        owner.limbAxis.RemoveAtSwapBack(limbAxisIndex + a);
                    }

                    int limbIndex = index * 2;
                    for (int a = 1; a >= 0; a--)
                    {
                        owner.previousLimbLengths.RemoveAtSwapBack(limbIndex + a);
                        owner.currentLimbLengths.RemoveAtSwapBack(limbIndex + a); 
                    }

                    owner.previousSpineLengths.RemoveAtSwapBack(index);
                    owner.currentSpineLengths.RemoveAtSwapBack(index);

                    owner.limbPreservationWeights.RemoveAtSwapBack(index);
                    owner.kneePreservationWeights.RemoveAtSwapBack(index);

                    if (!ReferenceEquals(this, swapped)) swapped.SetIndex(index);
                }

                index = -1;
                owner = null;
            }


            public void GetLimbLengths(out float4 armLengths, out float4 legLengths)
            {
                int limbIndex = index * 2;
                armLengths = owner.currentLimbLengths[limbIndex];
                legLengths = owner.currentLimbLengths[limbIndex + 1];
            }
            public float4 GetArmLengths()
            {
                int limbIndex = index * 2;
                return owner.currentLimbLengths[limbIndex];
            }
            public float4 GetLegLengths()
            {
                int limbIndex = index * 2;
                return owner.currentLimbLengths[limbIndex + 1];
            }
            public float GetSpineLength()
            {
                return owner.currentSpineLengths[index];
            }


            public void SetLimbLengths(float4 armLengths, float4 legLengths)
            {
                int limbIndex = index * 2;
                owner.currentLimbLengths[limbIndex] = armLengths;
                owner.currentLimbLengths[limbIndex + 1] = legLengths;

                owner.updateFlag = true;
            }
            public void SetArmLengths(float4 armLengths)
            {
                int limbIndex = index * 2;
                owner.currentLimbLengths[limbIndex] = armLengths;

                owner.updateFlag = true;
            }
            public void SetLegLengths(float4 legLengths)
            {
                int limbIndex = index * 2;
                owner.currentLimbLengths[limbIndex + 1] = legLengths;

                owner.updateFlag = true;
            }
            public void SetSpineLength(float spineLength)
            {
                owner.currentSpineLengths[index] = spineLength;

                owner.updateFlag = true;
            }


            public void GetPreviousLimbLengths(out float4 armLengths, out float4 legLengths)
            {
                int limbIndex = index * 2;
                armLengths = owner.previousLimbLengths[limbIndex];
                legLengths = owner.previousLimbLengths[limbIndex + 1];
            }
            public float4 GetPreviousArmLengths()
            {
                int limbIndex = index * 2;
                return owner.previousLimbLengths[limbIndex];
            }
            public float4 GetPreviousLegLengths()
            {
                int limbIndex = index * 2;
                return owner.previousLimbLengths[limbIndex + 1]; 
            }
            public float GetPreviousSpineLength()
            {
                return owner.previousSpineLengths[index];
            }


            public void SetPrevousLimbLengths(float4 armLengths, float4 legLengths)
            {
                int limbIndex = index * 2;
                owner.previousLimbLengths[limbIndex] = armLengths;
                owner.previousLimbLengths[limbIndex + 1] = legLengths;
            }
            public void SetPrevousArmLengths(float4 armLengths)
            {
                int limbIndex = index * 2;
                owner.previousLimbLengths[limbIndex] = armLengths;
            }
            public void SetPreviousLegLengths(float4 legLengths)
            {
                int limbIndex = index * 2;
                owner.previousLimbLengths[limbIndex + 1] = legLengths;
            }
            public void SetPrevousSpineLength(float spineLength)
            {
                owner.previousSpineLengths[index] = spineLength;
            }


            public float4 GetLimbPreservationWeights()
            {
                return owner.limbPreservationWeights[index];
            }
            public float2 GetKneePreservationWeights()
            {
                return owner.kneePreservationWeights[index]; 
            }

            public void SetLimbPreservationWeights(float4 weights)
            {
                owner.limbPreservationWeights[index] = weights;

                owner.updateFlag = true;
            }
            public void SetKneePreservationWeights(float2 weights)
            {
                owner.kneePreservationWeights[index] = weights;

                owner.updateFlag = true;
            }
        }

        protected List<IndexReference> registered;

        protected TransformAccessArray transforms;
        protected NativeList<TransformDataWorldLocal> transformData;
        protected void AddTransform(Transform transform)
        {
            transforms.Add(transform);
            transformData.Add(new TransformDataWorldLocal());   
        }

        protected NativeList<float3> limbAxis;

        protected NativeList<float4> previousLimbLengths;
        protected NativeList<float4> currentLimbLengths;

        protected NativeList<float> previousSpineLengths;
        protected NativeList<float> currentSpineLengths;

        protected NativeList<float4> limbPreservationWeights;
        protected NativeList<float2> kneePreservationWeights;

        public IndexReference RegisterLocal(float3 extensionAxisSpine, float3 bendAxisSpine,
            Transform spine1, Transform spine2, Transform spine3, float lengthSpine,
            float3 extensionAxisArms, float3 bendAxisArms, float3 extensionAxisLegs, float3 bendAxisLegs, bool flipArmAxisForMirror, bool flipLegAxisForMirror,
            Transform parentArmLeft, Transform armLeft, Transform forearmLeft, Transform wristLeft, float lengthArmLeft, float lengthForearmLeft,
            Transform parentArmRight, Transform armRight, Transform forearmRight, Transform wristRight, float lengthArmRight, float lengthForearmRight,
            Transform parentLegLeft, Transform legLeft, Transform calfLeft, Transform footLeft, float lengthLegLeft, float lengthCalfLeft,
            Transform parentLegRight, Transform legRight, Transform calfRight, Transform footRight, float lengthLegRight, float lengthCalfRight,
            Transform pelvis)
        {
            if (destroyed) return null;

            int index = registered.Count;
             
            AddTransform(parentArmLeft); // 1
            AddTransform(armLeft); // 2
            AddTransform(forearmLeft); // 3
            AddTransform(wristLeft); // 4
            AddTransform(parentArmRight); // 5
            AddTransform(armRight); // 6
            AddTransform(forearmRight); // 7
            AddTransform(wristRight); // 8
            AddTransform(parentLegLeft); // 9
            AddTransform(legLeft); // 10
            AddTransform(calfLeft); // 11
            AddTransform(footLeft); // 12
            AddTransform(parentLegRight); // 13
            AddTransform(legRight); // 14
            AddTransform(calfRight); // 15
            AddTransform(footRight); // 16
            AddTransform(spine1); // 17
            AddTransform(spine2); // 18
            AddTransform(spine3); // 19
            AddTransform(pelvis); // 20
            AddTransform(pelvis.parent); // 21

            limbAxis.Add(extensionAxisArms);
            limbAxis.Add(bendAxisArms);
            limbAxis.Add(extensionAxisArms);
            limbAxis.Add(flipArmAxisForMirror ? -bendAxisArms : bendAxisArms);
            limbAxis.Add(extensionAxisLegs);
            limbAxis.Add(bendAxisLegs);
            limbAxis.Add(extensionAxisLegs);
            limbAxis.Add(flipLegAxisForMirror ? -bendAxisLegs : bendAxisLegs);
            limbAxis.Add(extensionAxisSpine);
            limbAxis.Add(bendAxisSpine);

            float4 armLengths = new float4(lengthArmLeft, lengthForearmLeft, lengthArmRight, lengthForearmRight);
            float4 legLengths = new float4(lengthLegLeft, lengthCalfLeft, lengthLegRight, lengthCalfRight);

            previousLimbLengths.Add(armLengths);
            previousLimbLengths.Add(legLengths);
            currentLimbLengths.Add(armLengths);
            currentLimbLengths.Add(legLengths);

            previousSpineLengths.Add(lengthSpine);
            currentSpineLengths.Add(lengthSpine); 

            limbPreservationWeights.Add(new float4(1, 1, 1, 1));
            kneePreservationWeights.Add(new float2(1, 1));

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
            float3 extensionAxisArms, float3 bendAxisArms, float3 extensionAxisLegs, float3 bendAxisLegs, bool flipArmAxisForMirror, bool flipLegAxisForMirror,
            Transform parentArmLeft, Transform armLeft, Transform forearmLeft, Transform wristLeft, float lengthArmLeft, float lengthForearmLeft,
            Transform parentArmRight, Transform armRight, Transform forearmRight, Transform wristRight, float lengthArmRight, float lengthForearmRight,
            Transform parentLegLeft, Transform legLeft, Transform calfLeft, Transform footLeft, float lengthLegLeft, float lengthCalfLeft,
            Transform parentLegRight, Transform legRight, Transform calfRight, Transform footRight, float lengthLegRight, float lengthCalfRight,
            Transform pelvis)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.RegisterLocal(extensionAxisSpine, bendAxisSpine,
                spine1, spine2, spine3, lengthSpine,
                extensionAxisArms, bendAxisArms, extensionAxisLegs, bendAxisLegs, flipArmAxisForMirror, flipLegAxisForMirror,
                parentArmLeft, armLeft, forearmLeft, wristLeft, lengthArmLeft, lengthForearmLeft,
                parentArmRight, armRight, forearmRight, wristRight, lengthArmRight, lengthForearmRight,
                parentLegLeft, legLeft, calfLeft, footLeft, lengthLegLeft, lengthCalfLeft,
                parentLegRight, legRight, calfRight, footRight, lengthLegRight, lengthCalfRight,
                pelvis
                );
        }

        public static void Unregister(IndexReference indRef)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;
             
            instance.UnregisterLocal(indRef); 
        }
        
        [BurstCompile]
        public struct FetchTransformDataJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<TransformDataWorldLocal> transformData; 

            public void Execute(int index, TransformAccess transform)
            {
                //transform.GetLocalPositionAndRotation(out var lpos, out var lrot); // broken wtf? // TODO: uncomment this after updating project editor version, apparently it's fixed in up-to-date versions
                var lpos = transform.localPosition; 
                var lrot = transform.localRotation; 
                transform.GetPositionAndRotation(out var pos, out var rot);  
                transformData[index] = new TransformDataWorldLocal() { position = pos, rotation = rot, localPosition = lpos, localRotation = lrot };
            }
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
            public NativeList<float3> limbAxis;

            [NativeDisableParallelForRestriction]
            public NativeList<TransformDataWorldLocal> transformData;

            [NativeDisableParallelForRestriction]
            public NativeList<float4> previousLimbLengths;
            [ReadOnly]
            public NativeList<float4> currentLimbLengths;

            [NativeDisableParallelForRestriction]
            public NativeList<float> previousSpineLengths;
            [ReadOnly]
            public NativeList<float> currentSpineLengths;

            [ReadOnly]
            public NativeList<float4> limbPreservationWeights;
            [ReadOnly]
            public NativeList<float2> kneePreservationWeights;

            public void ChangeSpineLength(int index, float3 lengthDelta)
            {
                TransformDataWorldLocal data = transformData[index];
                data.localPosition = data.localPosition + lengthDelta;              
                transformData[index] = data; 
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

            public void ChangeLimbLength(float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int startIndex, int middleIndex, int endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal parentData = transformData[parentIndex];
                TransformDataWorldLocal startData = transformData[startIndex];
                TransformDataWorldLocal middleData = transformData[middleIndex];
                TransformDataWorldLocal endData = transformData[endIndex];

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

                transformData[startIndex] = startData;
                transformData[middleIndex] = middleData;
                transformData[endIndex] = endData;
            }
            public void ChangeLimbLengthWorld(float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int startIndex, int middleIndex, int endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal parentData = transformData[parentIndex];
                TransformDataWorldLocal startData = transformData[startIndex];
                TransformDataWorldLocal middleData = transformData[middleIndex];
                TransformDataWorldLocal endData = transformData[endIndex];

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

                transformData[startIndex] = startData;
                transformData[middleIndex] = middleData;
                transformData[endIndex] = endData;
            }

            public void ChangeLegLength(TransformDataWorldLocal pelvisTransformData, TransformDataWorldLocal pelvisParentTransformData, out TransformDataWorldLocal pelvisTransformDataOffset, float kneePreservingWeight, float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int startIndex, int middleIndex, int endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal startData = transformData[startIndex];
                TransformDataWorldLocal middleData = transformData[middleIndex];

                ChangeLimbLength(weight * (1f - kneePreservingWeight), extensionAxis, bendAxis, parentIndex, startIndex, middleIndex, endIndex, newLengthA, newLengthB, prevLengthA, prevLengthB, lengthChangeA, lengthChangeB);

                float3 offsetA = math.rotate(startData.rotation, extensionAxis) * lengthChangeA;
                float3 offsetB = math.rotate(middleData.rotation, extensionAxis) * lengthChangeB;

                quaternion inverseParentRotation = math.inverse(pelvisParentTransformData.rotation);

                pelvisTransformData.localPosition = math.lerp(pelvisTransformData.localPosition, pelvisTransformData.localPosition - math.rotate(inverseParentRotation, offsetA + offsetB), weight * kneePreservingWeight);

                pelvisTransformDataOffset = pelvisTransformData;
            }

            public void ChangeLegLengthWorld(TransformDataWorldLocal pelvisTransformData, TransformDataWorldLocal pelvisParentTransformData, out TransformDataWorldLocal pelvisTransformDataOffset, float kneePreservingWeight, float weight, float3 extensionAxis, float3 bendAxis, int parentIndex, int startIndex, int middleIndex, int endIndex, float newLengthA, float newLengthB, float prevLengthA, float prevLengthB, float lengthChangeA, float lengthChangeB)
            {
                TransformDataWorldLocal startData = transformData[startIndex];
                TransformDataWorldLocal middleData = transformData[middleIndex];

                ChangeLimbLengthWorld(weight * (1f - kneePreservingWeight), extensionAxis, bendAxis, parentIndex, startIndex, middleIndex, endIndex, newLengthA, newLengthB, prevLengthA, prevLengthB, lengthChangeA, lengthChangeB);

                float3 offsetA = math.rotate(startData.rotation, extensionAxis) * lengthChangeA;
                float3 offsetB = math.rotate(middleData.rotation, extensionAxis) * lengthChangeB;

                quaternion inverseParentRotation = math.inverse(pelvisParentTransformData.rotation);

                pelvisTransformData.localPosition = math.lerp(pelvisTransformData.localPosition, pelvisTransformData.localPosition - math.rotate(inverseParentRotation, offsetA + offsetB), weight * kneePreservingWeight);

                pelvisTransformDataOffset = pelvisTransformData;
            }

            public void Execute(int index)
            {

                float4 limbPreservation = limbPreservationWeights[index];
                float2 kneePreservation = kneePreservationWeights[index];

                int transformIndex = index * 21;

                int armIndex = index * 2;
                int legIndex = armIndex + 1;

                float4 prevArmLengths = previousLimbLengths[armIndex];
                float4 prevLegLengths = previousLimbLengths[legIndex];
                float prevSpineLength = previousSpineLengths[index];

                float4 armLengths = currentLimbLengths[armIndex];
                float4 legLengths = currentLimbLengths[legIndex];
                float spineLength = currentSpineLengths[index];

                float4 armLengthChanges = armLengths - prevArmLengths;
                float4 legLengthChanges = legLengths - prevLegLengths;

                int limbAxisIndex = index * 10;

                float3 leftArmExtensionAxis = limbAxis[limbAxisIndex];
                float3 leftArmRotationAxis = limbAxis[limbAxisIndex + 1];

                float3 rightArmExtensionAxis = limbAxis[limbAxisIndex + 2];
                float3 rightArmRotationAxis = limbAxis[limbAxisIndex + 3];

                float3 leftLegExtensionAxis = limbAxis[limbAxisIndex + 4];
                float3 leftLegRotationAxis = limbAxis[limbAxisIndex + 5];

                float3 rightLegExtensionAxis = limbAxis[limbAxisIndex + 6];
                float3 rightLegRotationAxis = limbAxis[limbAxisIndex + 7];

                float3 spineExtensionAxis = limbAxis[limbAxisIndex + 8];
                //float3 spineRotationAxis = limbAxis[limbAxisIndex + 9];

                int pelvisIndex = transformIndex + 19;
                int pelvisParentIndex = transformIndex + 20;
                TransformDataWorldLocal pelvisTransformData = transformData[pelvisIndex];
                TransformDataWorldLocal pelvisParentTransformData = transformData[pelvisParentIndex];  

                ChangeLimbLengthWorld(limbPreservation.x, leftArmExtensionAxis, leftArmRotationAxis, transformIndex + 0, transformIndex + 1, transformIndex + 2, transformIndex + 3, armLengths.x, armLengths.y, prevArmLengths.x, prevArmLengths.y, armLengthChanges.x, armLengthChanges.y); // left arm
                ChangeLimbLengthWorld(limbPreservation.y, rightArmExtensionAxis, rightArmRotationAxis, transformIndex + 4, transformIndex + 5, transformIndex + 6, transformIndex + 7, armLengths.z, armLengths.w, prevArmLengths.z, prevArmLengths.w, armLengthChanges.z, armLengthChanges.w); // right arm

                // Legs blend weights between moving pelvis and rotating legs
                TransformDataWorldLocal pelvisTransformDataA, pelvisTransformDataB;
                ChangeLegLengthWorld(pelvisTransformData, pelvisParentTransformData, out pelvisTransformDataA, kneePreservation.x, limbPreservation.z, leftLegExtensionAxis, leftLegRotationAxis, transformIndex + 8, transformIndex + 9, transformIndex + 10, transformIndex + 11, legLengths.x, legLengths.y, prevLegLengths.x, prevLegLengths.y, legLengthChanges.x, legLengthChanges.y); // left leg
                ChangeLegLengthWorld(pelvisTransformData, pelvisParentTransformData, out pelvisTransformDataB, kneePreservation.y, limbPreservation.w, rightLegExtensionAxis, rightLegRotationAxis, transformIndex + 12, transformIndex + 13, transformIndex + 14, transformIndex + 15, legLengths.z, legLengths.w, prevLegLengths.z, prevLegLengths.w, legLengthChanges.z, legLengthChanges.w); // right leg

                pelvisTransformData.localPosition = (pelvisTransformDataA.localPosition + pelvisTransformDataB.localPosition) * 0.5f;
                //pelvisTransformData.localRotation = math.slerp(pelvisTransformDataA.localRotation, pelvisTransformDataB.localRotation, 0.5f);
                 
                transformData[pelvisIndex] = pelvisTransformData;

                float3 spineLengthDelta = spineExtensionAxis * (spineLength - prevSpineLength);
                ChangeSpineLength(transformIndex + 16, spineLengthDelta);
                ChangeSpineLength(transformIndex + 17, spineLengthDelta);
                ChangeSpineLength(transformIndex + 18, spineLengthDelta);

                previousLimbLengths[armIndex] = armLengths;
                previousLimbLengths[legIndex] = legLengths;
                previousSpineLengths[index] = spineLength;

            }
        }

    }
}

#endif