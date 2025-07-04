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

    public static class SharedJobs
    {

        #region Utils

        public static NativeArray<T> AsNativeArray<T>(this IEnumerable<T> obj, out bool isNewlyAllocated) where T : unmanaged
        {
            isNewlyAllocated = false;

            NativeArray<T> array;
            if (obj is NativeList<T>)
            {
                array = ((NativeList<T>)obj).AsArray();
            }
            else if (obj is NativeArray<T>)
            {
                array = (NativeArray<T>)obj;
            }
            else
            {
                int count = 0;
                if (obj is ICollection<T> collection)
                {
                    count = collection.Count;
                }
                else
                {
                    foreach (var _ in obj) count++;
                }

                array = new NativeArray<T>(count, Allocator.Persistent);
                if (obj is T[] managedArray)
                {
                    array.CopyFrom(managedArray); 
                }
                else
                {
                    int i = 0;
                    foreach(var val in obj)
                    {
                        array[i] = val;
                        i++;
                    }
                }

                isNewlyAllocated = true;
            }

            return array;
        }

        #endregion

        [BurstCompile]
        public struct FetchTransformDataJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<TransformDataWorldLocal> transformData; 

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
        public struct FetchTransformDataWithMatrixJob : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<TransformDataWorldLocalAndMatrix> transformData;

            public void Execute(int index, TransformAccess transform)
            {
                //transform.GetLocalPositionAndRotation(out var lpos, out var lrot); // broken wtf? // TODO: uncomment this after updating project editor version, apparently it's fixed in up-to-date versions
                var lpos = transform.localPosition;
                var lrot = transform.localRotation;
                transform.GetPositionAndRotation(out var pos, out var rot);
                transformData[index] = new TransformDataWorldLocalAndMatrix() { toWorld = transform.localToWorldMatrix, position = pos, rotation = rot, localPosition = lpos, localRotation = lrot };
            }
        }

        [BurstCompile]
        public struct FetchTransformDataJobLocal : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<TransformDataState> transformData;

            public void Execute(int index, TransformAccess transform)
            {
                //transform.GetLocalPositionAndRotation(out var lpos, out var lrot); // broken wtf? // TODO: uncomment this after updating project editor version, apparently it's fixed in up-to-date versions
                var lpos = transform.localPosition;
                var lrot = transform.localRotation;
                transformData[index] = new TransformDataState() { position = lpos, rotation = lrot };
            }
        }

        [BurstCompile]
        public struct FetchTemporalTransformDataJobLocal : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<TransformDataStateTemporal> transformData;

            public void Execute(int index, TransformAccess transform)
            {
                var data = transformData[index];

                data.prevPosition = data.position;
                data.prevRotation = data.rotation;
                //transform.GetLocalPositionAndRotation(out var lpos, out var lrot); // broken wtf? // TODO: uncomment this after updating project editor version, apparently it's fixed in up-to-date versions
                data.position = transform.localPosition;
                data.rotation = transform.localRotation;

                transformData[index] = data;
            }
        }

        [BurstCompile]
        public struct FetchTransformDataJobWorld : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<TransformDataState> transformData;

            public void Execute(int index, TransformAccess transform)
            {
                transform.GetPositionAndRotation(out var pos, out var rot);
                transformData[index] = new TransformDataState() { position = pos, rotation = rot };
            }
        }

        [BurstCompile]
        public struct FetchTemporalTransformDataJobWorld : IJobParallelForTransform
        {

            [NativeDisableParallelForRestriction]
            public NativeArray<TransformDataStateTemporal> transformData;

            public void Execute(int index, TransformAccess transform)
            {
                var data = transformData[index];

                data.prevPosition = data.position;
                data.prevRotation = data.rotation;
                transform.GetPositionAndRotation(out var pos, out var rot);
                data.position = pos;
                data.rotation = rot;

                transformData[index] = data;
            }
        }
    }

}

#endif