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

using RandomJobs = Unity.Mathematics.Random;

namespace Swole.API.Unity
{

    public class CharacterExpressionsJobs : SingletonBehaviour<CharacterExpressionsJobs>, IDisposable
    {

        [NonSerialized]
        protected bool destroyed;

        public void Dispose()
        {
            try
            {
                if (headBoneTransforms.isCreated)
                {
                    headBoneTransforms.Dispose();
                    headBoneTransforms = default;
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
                if (headStates.IsCreated)
                {
                    headStates.Dispose();
                    headStates = default;
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
                if (eyeTrackingBoneTransforms.isCreated)
                {
                    eyeTrackingBoneTransforms.Dispose();
                    eyeTrackingBoneTransforms = default;
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
                if (eyeTrackingStates.IsCreated)
                {
                    eyeTrackingStates.Dispose();
                    eyeTrackingStates = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }

            if (eyeControlSettingsFetchers != null)
            {
                eyeControlSettingsFetchers.Clear();
                eyeControlSettingsFetchers = null;
            }

            if (eyeTrackingSubscribers != null)
            {
                eyeTrackingSubscribers.Clear();
                eyeTrackingSubscribers = null;
            }

            try
            {
                if (eyeballTransforms.isCreated)
                {
                    eyeballTransforms.Dispose();
                    eyeballTransforms = default;
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
                if (eyeballSettings.IsCreated)
                {
                    eyeballSettings.Dispose();
                    eyeballSettings = default;
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
                if (eyeballTransformData.IsCreated)
                {
                    eyeballTransformData.Dispose();
                    eyeballTransformData = default;
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
                if (eyelidTransforms.isCreated)
                {
                    eyelidTransforms.Dispose();
                    eyelidTransforms = default;
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
                if (eyelidSettings.IsCreated)
                {
                    eyelidSettings.Dispose();
                    eyelidSettings = default;
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
                if (eyelidPreviousOffsets.IsCreated)
                {
                    eyelidPreviousOffsets.Dispose();
                    eyelidPreviousOffsets = default;
                }
            }
            catch (Exception ex)
            {
#if UNITY_EDITOR
                Debug.LogException(ex);
#endif
            }
        }

        public override void OnDestroyed()
        {
            destroyed = true;

            registeredEyes.Clear();

            base.OnDestroyed();

            Dispose();
        }

        public static int ExecutionPriority => ProxyTransformSingleton.ExecutionPriority + 5; // Update after animation, ik, and transform proxies
        public override int Priority => ExecutionPriority;
        
        [Serializable]
        public struct EyeballStateData 
        {
            public float alignmentVertical;
            public float alignmentHorizontal;
 
            public float corneaRadius;
            public float3 corneaWorldPosition;

            public float3 forwardWorld;

            public quaternion lookOffset;

            public float closeFactor;

            public TransformDataWorldLocal transformData; 
        }

        [Serializable]
        public struct EyelidOffsetData
        {
            public float3 offset;
        }

        [Serializable]
        public struct HeadState
        {
            public float3 eyesCenterWorld;
            public float3 upAxisWorld;
            public float3 rightAxisWorld;

            public quaternion rotationWorld;
            public quaternion rotationToLocal;

            public CharacterExpressions.HeadSettings settings;
        }
        [Serializable]
        public struct EyeTrackingState
        {
            public int headBoneIndex;
            public float3 trackingTargetPosition;
            public CharacterExpressions.EyeControlSettings eyeControlSettings; 

            public quaternion eyeTrackingRotationWorld;

            public float blinkFactor;

            public float3 jitterOffset;
            public float3 dartOffset;
            public float3 jitterOffsetPrevious;
            public float3 dartOffsetPrevious;

            public float blinkTime;
            public float jitterTime;
            public float dartTime;

            public float blinkWait;
            public float jitterWait;
            public float dartWait;

            public bool blinkSwitch;
            public bool jitterSwitch;
            public bool dartSwitch;
        }

        public delegate CharacterExpressions.EyeControlSettings GetEyeControlSettingsDelegate();

        protected TransformAccessArray headBoneTransforms;
        protected NativeList<HeadState> headStates;
        protected TransformAccessArray eyeTrackingBoneTransforms;
        protected NativeList<EyeTrackingState> eyeTrackingStates;
        protected List<GetEyeControlSettingsDelegate> eyeControlSettingsFetchers;
        protected List<int> eyeTrackingSubscribers;
        protected int IndexOfEyeTrackingBone(Transform eyeTrackingBone)
        {
            for (int a = 0; a < eyeTrackingBoneTransforms.length; a++)
            {
                if (ReferenceEquals(eyeTrackingBone, eyeTrackingBoneTransforms[a].transform)) return a;
            }

            return -1; 
        }
        protected int IndexOfHeadTrackingBone(Transform headBone)
        {
            for (int a = 0; a < headBoneTransforms.length; a++)
            {
                if (ReferenceEquals(headBone, headBoneTransforms[a].transform)) return a;
            }

            return -1; 
        }
        protected void AddEyeTrackingSubscriber(int index)
        {
            eyeTrackingSubscribers[index] = eyeTrackingSubscribers[index] + 1;
        }
        protected void RemoveEyeTrackingSubscriber(int index)
        {
            var subs = eyeTrackingSubscribers[index];
            subs = subs - 1;

            if (subs < 0)
            {
                int swappedIndex = eyeTrackingStates.Length - 1;
                int swappedHeadIndex = headStates.Length - 1;

                int headIndex = eyeTrackingStates[index].headBoneIndex;

                headBoneTransforms.RemoveAtSwapBack(headIndex);
                headStates.RemoveAtSwapBack(headIndex);

                eyeTrackingBoneTransforms.RemoveAtSwapBack(index);
                eyeTrackingStates.RemoveAtSwapBack(index);
                eyeControlSettingsFetchers.RemoveAtSwapBack(index);
                eyeTrackingSubscribers.RemoveAtSwapBack(index);
                 
                for (int a = 0; a < eyeTrackingStates.Length; a++)
                {
                    var settings = eyeTrackingStates[a];
                    if (settings.headBoneIndex == swappedHeadIndex) settings.headBoneIndex = headIndex; 
                    eyeTrackingStates[a] = settings; 
                }

                if (eyeballSettings.IsCreated)
                {
                    for(int a = 0; a < eyeballSettings.Length; a++)
                    {
                        var settings = eyeballSettings[a];
                        if (settings.eyeTrackingBoneIndex == swappedIndex) settings.eyeTrackingBoneIndex = index;
                        eyeballSettings[a] = settings;
                    }
                }
            } 
            else
            {
                eyeTrackingSubscribers[index] = subs;
            }
        }

        protected TransformAccessArray eyeballTransforms;
        protected NativeList<CharacterExpressions.EyeballSettings> eyeballSettings;
        protected NativeList<EyeballStateData> eyeballTransformData;

        protected TransformAccessArray eyelidTransforms;  
        protected NativeList<CharacterExpressions.EyelidSettings> eyelidSettings;
        protected NativeList<EyelidOffsetData> eyelidPreviousOffsets;

        protected override void OnAwake()
        {
            base.OnAwake();

            Dispose();

            headBoneTransforms = new TransformAccessArray(1);
            headStates = new NativeList<HeadState>(1, Allocator.Persistent);
            eyeTrackingBoneTransforms = new TransformAccessArray(1);
            eyeTrackingStates = new NativeList<EyeTrackingState>(1, Allocator.Persistent); 
            eyeControlSettingsFetchers = new List<GetEyeControlSettingsDelegate>(1);
            eyeTrackingSubscribers = new List<int>(1);

            eyeballTransforms = new TransformAccessArray(1);
            eyeballSettings = new NativeList<CharacterExpressions.EyeballSettings>(1, Allocator.Persistent);
            eyeballTransformData = new NativeList<EyeballStateData>(1, Allocator.Persistent);

            eyelidTransforms = new TransformAccessArray(6);
            eyelidSettings = new NativeList<CharacterExpressions.EyelidSettings>(6, Allocator.Persistent);
            eyelidPreviousOffsets = new NativeList<EyelidOffsetData>(6, Allocator.Persistent);  

            registeredEyes = new List<IndexReferenceEye>();
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnUpdate()
        {
        }

        private JobHandle lateJobHandle;
        public override void OnLateUpdate()
        {
            if (destroyed) return;

            lateJobHandle.Complete();
            if (eyeControlSettingsFetchers != null)
            {
                for (int a = 0; a < eyeControlSettingsFetchers.Count; a++)
                {
                    var fetcher = eyeControlSettingsFetchers[a];
                    if (fetcher != null)
                    {
                        var state = eyeTrackingStates[a];
                        state.eyeControlSettings = fetcher();
                        eyeTrackingStates[a] = state;
                    }
                }
            }

            lateJobHandle = new UpdateHeadStatesJob()
            {

                headStates = headStates

            }.Schedule(headBoneTransforms);

            lateJobHandle = new UpdateEyeTrackingStatesJob()
            {
                 
                seed = (uint)UnityEngine.Random.Range(0, 999999),
                deltaTime = Time.deltaTime,
                headStates = headStates,
                eyeTrackingStates = eyeTrackingStates

            }.Schedule(eyeTrackingBoneTransforms, lateJobHandle);

            lateJobHandle = new FetchEyeballTransformDataJob()
            {

                eyeballSettings = eyeballSettings,
                eyeballTransformData = eyeballTransformData,
                eyeTrackingStates = eyeTrackingStates,
                headStates = headStates

            }.Schedule(eyeballTransforms, lateJobHandle);

            lateJobHandle = new UpdateEyelidsJob()
            {

                eyeballTransformData = eyeballTransformData,
                eyeballSettings = eyeballSettings,
                eyeTrackingStates = eyeTrackingStates,
                headStates = headStates,
                eyelidSettings = eyelidSettings,
                eyelidPreviousOffsets = eyelidPreviousOffsets

            }.Schedule(eyelidTransforms, lateJobHandle);

            lateJobHandle.Complete();
        }

        [BurstCompile]
        public struct UpdateHeadStatesJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeList<HeadState> headStates;

            public void Execute(int index, TransformAccess transform)
            {
                var state = headStates[index];

                var l2w = transform.localToWorldMatrix;
                state.eyesCenterWorld = math.transform(l2w, state.settings.eyesCenter);
                state.upAxisWorld = math.rotate(l2w, state.settings.upAxis);
                state.rightAxisWorld = math.rotate(l2w, state.settings.rightAxis);

                state.rotationWorld = transform.rotation;
                state.rotationToLocal = math.inverse(state.rotationWorld);

                //state.toLocal = transform.worldToLocalMatrix;
                //state.toWorld = transform.localToWorldMatrix;

                headStates[index] = state;
            }
        }

        [BurstCompile]
        public struct UpdateEyeTrackingStatesJob : IJobParallelForTransform
        {
            public uint seed;

            public float deltaTime;

            [ReadOnly]
            public NativeList<HeadState> headStates;

            [NativeDisableParallelForRestriction]
            public NativeList<EyeTrackingState> eyeTrackingStates;

            public void Execute(int index, TransformAccess transform)
            {
                var rand = RandomJobs.CreateFromIndex(((uint)index) + seed);

                var state = eyeTrackingStates[index];
                var head = headStates[state.headBoneIndex];

                state.trackingTargetPosition = transform.position;

                state.blinkTime += deltaTime;
                state.dartTime += deltaTime;
                state.jitterTime += deltaTime;

                float blinkMix = state.eyeControlSettings.eyeBlinkMix * state.eyeControlSettings.eyeControlMix;
                if (state.blinkSwitch)
                {
                    state.blinkFactor = (1 - math.abs((math.pow(math.min(1, state.blinkTime / state.blinkWait), 0.8f) - 0.5f) / 0.5f)) * blinkMix;

                    if (state.blinkTime > state.blinkWait) 
                    {
                        state.blinkTime = 0;
                        state.blinkSwitch = false;
                        state.blinkWait = rand.NextFloat(state.eyeControlSettings.eyeBlinkFrequencyMin, state.eyeControlSettings.eyeBlinkFrequencyMax); // how long to wait before next blink
                    } 
                }
                else
                {
                    state.blinkFactor = 0;

                    if (state.blinkTime > state.blinkWait)
                    {
                        state.blinkTime = 0;
                        state.blinkSwitch = true;

                        state.blinkWait = rand.NextFloat(0.09f, 0.25f); // how long the blink lasts
                    }
                }

                if (state.dartSwitch)
                {
                    state.trackingTargetPosition = state.trackingTargetPosition + math.lerp(state.dartOffsetPrevious, state.dartOffset, math.min(1, state.dartTime / state.dartWait)) * state.eyeControlSettings.eyeDartMix;

                    if (state.dartTime > state.dartWait)
                    {
                        state.dartTime = 0;
                        state.dartSwitch = false;
                        state.dartWait = rand.NextFloat(state.eyeControlSettings.eyeDartFrequencyMin, state.eyeControlSettings.eyeDartFrequencyMax); 
                    }
                }
                else
                {
                    state.trackingTargetPosition = state.trackingTargetPosition + state.dartOffset * state.eyeControlSettings.eyeDartMix; 

                    if (state.dartTime > state.dartWait)
                    {
                        state.dartTime = 0;
                        state.dartSwitch = true;

                        float distanceFromHead = math.distance(state.trackingTargetPosition, head.eyesCenterWorld);
                        float offsetMax = rand.NextFloat(state.eyeControlSettings.eyeDartDistanceMin, state.eyeControlSettings.eyeDartDistanceMax * distanceFromHead);
                        state.dartOffsetPrevious = state.dartOffset;
                        state.dartOffset = rand.NextFloat3(-offsetMax, offsetMax);
                        state.dartWait = rand.NextFloat(0.075f, 0.195f);
                    }
                }

                if (state.jitterSwitch)
                {
                    state.trackingTargetPosition = state.trackingTargetPosition + math.lerp(state.jitterOffsetPrevious, state.jitterOffset, math.min(1, state.jitterTime / state.jitterWait)) * state.eyeControlSettings.eyeJitterMix;

                    if (state.jitterTime > state.jitterWait)
                    {
                        state.jitterTime = 0;
                        state.jitterSwitch = false;
                        state.jitterWait = rand.NextFloat(0.02f, 0.19f); 
                    }
                } 
                else
                {
                    state.trackingTargetPosition = state.trackingTargetPosition + state.jitterOffset * state.eyeControlSettings.eyeJitterMix;  

                    if (state.jitterTime > state.jitterWait)
                    {
                        state.jitterTime = 0;
                        state.jitterSwitch = true;

                        float distanceFromHead = math.distance(state.trackingTargetPosition, head.eyesCenterWorld);
                        float offsetMax = 0.0125f * distanceFromHead;  
                        state.jitterOffsetPrevious = state.jitterOffset;
                        state.jitterOffset = rand.NextFloat3(-offsetMax, offsetMax);
                        state.jitterWait = rand.NextFloat(0.0375f, 0.0975f);
                    }
                }

                eyeTrackingStates[index] = state;   
            }
        }

        [BurstCompile]
        public struct FetchEyeballTransformDataJob : IJobParallelForTransform
        {

            [ReadOnly]
            public NativeList<CharacterExpressions.EyeballSettings> eyeballSettings;
            [ReadOnly]
            public NativeList<EyeTrackingState> eyeTrackingStates;
            [ReadOnly]
            public NativeList<HeadState> headStates;

            [NativeDisableParallelForRestriction]
            public NativeList<EyeballStateData> eyeballTransformData;

            public void Execute(int index, TransformAccess transform)
            {
                var state = eyeballTransformData[index]; 
                var settings = eyeballSettings[index]; 

                var eyeTrackingState = eyeTrackingStates[settings.eyeTrackingBoneIndex];
                var head = headStates[eyeTrackingState.headBoneIndex];

                transform.GetPositionAndRotation(out var pos, out var rot);

                float3 lookDir = math.rotate(head.rotationToLocal, math.normalize(eyeTrackingState.trackingTargetPosition - (float3)pos));
                float trackingHorizontal = math.dot(lookDir, head.settings.rightAxis) * eyeTrackingState.eyeControlSettings.eyeTrackingMix;
                float trackingVertical = math.dot(lookDir, head.settings.upAxis) * eyeTrackingState.eyeControlSettings.eyeTrackingMix;
                quaternion eyeTrackingRotation = math.mul(quaternion.AxisAngle(head.settings.upAxis, trackingHorizontal * settings.maxRadiansYaw), quaternion.AxisAngle(head.settings.rightAxis, trackingVertical * settings.maxRadiansPitch));
                eyeTrackingRotation = math.slerp(quaternion.identity, eyeTrackingRotation, eyeTrackingState.eyeControlSettings.eyeControlMix);

                quaternion undoRot = math.select(quaternion.identity.value, math.inverse(state.lookOffset).value, settings.undoPreviousOffset);    
                var rotHS = math.mul(head.rotationToLocal, rot);
                var newRotHS = math.mul(eyeTrackingRotation, math.mul(undoRot, rotHS)); 

                var newRot = math.mul(head.rotationWorld, newRotHS);

                state.lookOffset = eyeTrackingRotation.value;
                   
                rot = newRot;
                transform.rotation = rot;

                //transform.GetLocalPositionAndRotation(out var lpos, out var lrot); // broken wtf? // TODO: uncomment this after updating project editor version, apparently it's fixed in up-to-date versions
                var lpos = transform.localPosition;  
                var lrot = transform.localRotation;

                float3 fwdWorld = math.rotate(rot, settings.localForwardAxis);
                float3 fwdLocal= math.rotate(lrot, settings.localForwardAxis); 
                float alignmentVertical = math.dot(fwdLocal, settings.localRollVerticalAxis);
                float alignmentHorizontal = math.dot(fwdLocal, settings.localRollHorizontalAxis);

                var l2w = transform.localToWorldMatrix;
                Vector3 corneaPosition = math.transform(l2w, settings.corneaCenter);

                state.corneaRadius = settings.corneaRadius;
                state.corneaWorldPosition = corneaPosition;

                state.forwardWorld = fwdWorld;

                state.alignmentVertical = alignmentVertical;
                state.alignmentHorizontal = alignmentHorizontal;
                
                state.transformData = new TransformDataWorldLocal()
                {
                    position = pos,
                    rotation = rot,
                    localPosition = lpos,
                    localRotation = lrot
                };

                state.closeFactor = math.max(settings.closeFactor, eyeTrackingState.blinkFactor);

                eyeballTransformData[index] = state;
            }
        }

        [BurstCompile]
        public struct UpdateEyelidsJob : IJobParallelForTransform
        {
            [ReadOnly]
            public NativeList<EyeballStateData> eyeballTransformData;
            [ReadOnly]
            public NativeList<CharacterExpressions.EyeballSettings> eyeballSettings;
            [ReadOnly]
            public NativeList<EyeTrackingState> eyeTrackingStates;
            [ReadOnly]
            public NativeList<HeadState> headStates;
            [ReadOnly]
            public NativeList<CharacterExpressions.EyelidSettings> eyelidSettings;  

            [NativeDisableParallelForRestriction]
            public NativeList<EyelidOffsetData> eyelidPreviousOffsets;

            public void Execute(int index, TransformAccess transform) 
            {
                int eyeballIndex = index / 6;

                var eyeballSettings_ = eyeballSettings[eyeballIndex];
                var eyeballData = eyeballTransformData[eyeballIndex];
                var eyeTrackingState = eyeTrackingStates[eyeballSettings_.eyeTrackingBoneIndex];
                var head = headStates[eyeTrackingState.headBoneIndex];
                var eyelidSettings_ = eyelidSettings[index];

                var prevOffsets = eyelidPreviousOffsets[index];

                var wpos = transform.position;

                // TODO: Fix degredation of original eyelid bone positions when undo is applied (probably caused by floating point error or something...)
                var undoOffset = math.select(float3.zero, math.rotate(head.rotationWorld, prevOffsets.offset), eyelidSettings_.undoPreviousOffset);

                wpos = wpos - (Vector3)undoOffset;
                 
                float distanceToCornea = math.max(0, math.distance(eyeballData.corneaWorldPosition, wpos) - eyelidSettings_.radius);  
                float corneaPushOutFactor = 1f - math.saturate(distanceToCornea / eyeballData.corneaRadius);
                float corneaPushInFactor = math.saturate((distanceToCornea - eyelidSettings_.defaultDistanceFromCornea) / eyelidSettings_.defaultDistanceFromCornea);

                float openFactor = 1 - eyeballData.closeFactor;

                float alignVert = eyeballData.alignmentVertical * openFactor; 
                float alignVerPos = math.max(0, alignVert);
                float alignVerNeg = math.min(0, alignVert);

                float alignHorPos = math.max(0, eyeballData.alignmentHorizontal);
                float alignHorNeg = math.min(0, eyeballData.alignmentHorizontal); 

                var offset = eyelidSettings_.pushVerticalAxisHS * ((eyelidSettings_.closeVertical * eyeballData.closeFactor) + (eyelidSettings_.pushVerticalRollDown * alignVerNeg) + (eyelidSettings_.pushVerticalRollUp * alignVerPos));
                offset = offset + (eyelidSettings_.pushHorizontalAxisHS * ((eyelidSettings_.pushHorizontalRollIn * alignHorNeg) + (eyelidSettings_.pushHorizontalRollOut * alignHorPos)));
                offset = offset + (eyelidSettings_.pushFwdAxisHS * ((eyelidSettings_.pushFwdRollAway * corneaPushInFactor * (1 - corneaPushOutFactor)) + eyelidSettings_.closeFwd * eyeballData.closeFactor));  

                var worldOffset = (eyeballData.forwardWorld * eyelidSettings_.pushFwdRollNear * corneaPushOutFactor) + math.rotate(head.rotationWorld, offset);
                worldOffset = math.lerp(float3.zero, worldOffset, eyeTrackingState.eyeControlSettings.eyeControlMix * eyeTrackingState.eyeControlSettings.eyelidMovementMix); 

                wpos = wpos + (Vector3)worldOffset;
                transform.position = wpos;

                undoOffset = math.rotate(head.rotationToLocal, worldOffset);

                eyelidPreviousOffsets[index] = new EyelidOffsetData() { offset = undoOffset };
            }
        }

        protected List<IndexReferenceEye> registeredEyes;

        protected int AddEyeTracking(Transform headBoneTransform, CharacterExpressions.HeadSettings headSettings, Transform eyeTrackingBoneTransform, GetEyeControlSettingsDelegate fetchEyeControlSettings)
        {
            int index = eyeTrackingStates.Length;

            headBoneTransforms.Add(headBoneTransform);
            headStates.Add(new HeadState()
            {
                eyesCenterWorld = headBoneTransform.TransformPoint(headSettings.eyesCenter),
                upAxisWorld = headBoneTransform.TransformDirection(headSettings.upAxis),
                rightAxisWorld = headBoneTransform.TransformDirection(headSettings.rightAxis),
                settings = headSettings
            });
            eyeTrackingBoneTransforms.Add(eyeTrackingBoneTransform);
            eyeTrackingStates.Add(new EyeTrackingState()
            {
                headBoneIndex = index,
                trackingTargetPosition = eyeTrackingBoneTransform.position,
                eyeControlSettings = fetchEyeControlSettings()
            });
            eyeControlSettingsFetchers.Add(fetchEyeControlSettings);
            eyeTrackingSubscribers.Add(0);

            return index;
        }
        protected int AddEyeball(CharacterExpressions.Eyeball eyeball, int eyeTrackingBoneIndex)
        {
            int index = eyeballSettings.Length;

            eyeball.settings.eyeTrackingBoneIndex = eyeTrackingBoneIndex;

            eyeballTransforms.Add(eyeball.eyeballBone);
            eyeballSettings.Add(eyeball.settings);
            eyeballTransformData.Add(new EyeballStateData() { lookOffset = quaternion.identity }); 

            return index;
        }
        protected int AddEyelid(CharacterExpressions.Eyelid eyelid)
        {
            int index = eyelidSettings.Length;

            eyelidTransforms.Add(eyelid.eyelidBone);
            eyelidSettings.Add(eyelid.settings);
            eyelidPreviousOffsets.Add(default); 

            return index;
        }

        public class IndexReferenceEye : IDisposable
        {
            private CharacterExpressionsJobs owner;

            private int index;
            public int Index => index;

            public delegate void SetIndexDelegate(int previousIndex, int newIndex);
            public event SetIndexDelegate OnSetIndex;

            internal void SetIndex(int newIndex)
            {
                OnSetIndex?.Invoke(index, newIndex);
                index = newIndex;
            }

            internal IndexReferenceEye(CharacterExpressionsJobs owner, int index)
            {
                this.owner = owner;
                this.index = index;
            }

            public bool IsValid => index >= 0;

            public void Dispose()
            {
                if (index >= 0 && owner != null && !owner.destroyed)
                {
                    int swapIndex = owner.registeredEyes.Count - 1;
                    var swapped = owner.registeredEyes[swapIndex];

                    owner.registeredEyes[index] = owner.registeredEyes[swapIndex];
                    owner.registeredEyes.RemoveAt(swapIndex);

                    owner.RemoveEyeTrackingSubscriber(owner.eyeballSettings[index].eyeTrackingBoneIndex); 

                    owner.eyeballTransforms.RemoveAtSwapBack(index);
                    owner.eyeballSettings.RemoveAtSwapBack(index);
                    owner.eyeballTransformData.RemoveAtSwapBack(index);

                    int transformIndex = index * 6;
                    for (int a = 5; a >= 0; a--)
                    {
                        int ind = transformIndex + a;

                        owner.eyelidTransforms.RemoveAtSwapBack(ind);
                        owner.eyelidSettings.RemoveAtSwapBack(ind);
                        owner.eyelidPreviousOffsets.RemoveAtSwapBack(ind);
                    }

                    if (!ReferenceEquals(this, swapped)) swapped.SetIndex(index);
                }

                index = -1;
                owner = null;
            }

            public CharacterExpressions.EyeballSettings GetEyeballSettings()
            {
                if (!IsValid || owner == null || owner.destroyed) return default;

                return owner.eyeballSettings[index];
            }

            public void SetEyeballSettings(CharacterExpressions.EyeballSettings settings)
            {
                if (!IsValid || owner == null || owner.destroyed) return;

                owner.eyeballSettings[index] = settings;
            }

            public CharacterExpressions.EyelidSettings GetEyelidSettings(int eyelidIndex)
            {
                if (!IsValid || owner == null || owner.destroyed) return default;

                int transformIndex = index * 6 + eyelidIndex;
                return owner.eyelidSettings[transformIndex];
            }

            public void SetEyelidSettings(int eyelidIndex, CharacterExpressions.EyelidSettings settings)
            {
                if (!IsValid || owner == null || owner.destroyed) return;

                int transformIndex = index * 6 + eyelidIndex;
                owner.eyelidSettings[transformIndex] = settings;
            }
        }

        public IndexReferenceEye RegisterEyeLocal(CharacterExpressions.Eye eye, Transform headBone, CharacterExpressions.HeadSettings headSettings, Transform eyeTrackingBone, GetEyeControlSettingsDelegate fetchEyeControlSettings)
        {
            if (destroyed) return null;

            int eyeTrackingIndex = IndexOfEyeTrackingBone(eyeTrackingBone);
            if (eyeTrackingIndex < 0)
            {
                eyeTrackingIndex = AddEyeTracking(headBone, headSettings, eyeTrackingBone, fetchEyeControlSettings); 
            }
            else
            {
                AddEyeTrackingSubscriber(eyeTrackingIndex);
            }
            AddEyeball(eye.eyeball, eyeTrackingIndex);
             
            AddEyelid(eye.eyelidUpperInnerBone); // 1
            AddEyelid(eye.eyelidUpperMiddleBone); // 2
            AddEyelid(eye.eyelidUpperOuterBone); // 3
            AddEyelid(eye.eyelidLowerInnerBone); // 4
            AddEyelid(eye.eyelidLowerMiddleBone); // 5
            AddEyelid(eye.eyelidLowerOuterBone); // 6
                
            int index = registeredEyes.Count; 

            var indRef = new IndexReferenceEye(this, index);
            registeredEyes.Add(indRef);

            return indRef;
        }
        public void UnregisterEyeLocal(IndexReferenceEye indRef)
        {
            if (destroyed || indRef == null) return;

            indRef.Dispose();
        }

        public static IndexReferenceEye RegisterEye(CharacterExpressions.Eye eye, Transform headBone, CharacterExpressions.HeadSettings headSettings, Transform eyeTrackingBone, GetEyeControlSettingsDelegate fetchEyeControlSettings)
        {
            var instance = Instance;
            if (instance == null) return null;

            return instance.RegisterEyeLocal(eye, headBone, headSettings, eyeTrackingBone, fetchEyeControlSettings);
        }
        public static void UnregisterEye(IndexReferenceEye indRef)
        {
            var instance = InstanceOrNull;
            if (instance == null) return;

            instance.UnregisterEyeLocal(indRef); 
        }
    }
}

#endif