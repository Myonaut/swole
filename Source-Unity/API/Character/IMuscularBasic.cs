using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Mathematics;

namespace Swole.API.Unity
{
    public interface IMuscularBasic
    {

        public string GetMuscleGroupName(int index);
        public string GetMuscleGroupNameUnsafe(int index);
        public int GetMuscleGroupIndex(string muscleGroupName);
        public int GetMuscleGroupIndex(MuscleGroupIdentifier identifier);
        public int GetMuscleGroupIndexForArray(string muscleGroupName);
        public int GetMuscleGroupIndexForArray(MuscleGroupIdentifier identifier);
        public int FindMuscleGroup(string muscleGroupName);
        public int FindMuscleGroup(MuscleGroupIdentifier identifier);

        public int MuscleGroupCount { get; }

        public float BreastPresence
        {
            get;
            set;
        }

        public bool SetMuscleGroupValues(int muscleGroupIndex, float3 values, bool updateDependencies = true);
        public bool SetMuscleGroupMass(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false);
        public bool SetMuscleGroupFlex(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false);
        public bool SetMuscleGroupPump(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false);

        public bool SetMuscleGroupValuesUnsafe(int muscleGroupIndex, float3 values, bool updateDependencies = true);
        public bool SetMuscleGroupMassUnsafe(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false);
        public bool SetMuscleGroupFlexUnsafe(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false);
        public bool SetMuscleGroupPumpUnsafe(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false);

        public float3 GetMuscleGroupValues(int muscleGroupIndex);
        public float3 GetMuscleGroupValuesUnsafe(int muscleGroupIndex);

        public float GetMuscleGroupMass(int muscleGroupIndex);
        public float GetMuscleGroupMassUnsafe(int muscleGroupIndex);

        public float GetMuscleGroupFlex(int muscleGroupIndex);
        public float GetMuscleGroupFlexUnsafe(int muscleGroupIndex);

        public float GetMuscleGroupPump(int muscleGroupIndex);
        public float GetMuscleGroupPumpUnsafe(int muscleGroupIndex);

        public void SetGlobalMuscleValues(float3 values);
        public void SetGlobalMass(float mass);
        public void SetGlobalFlex(float flex);
        public void SetGlobalPump(float pump);

        public float3 GetAverageMuscleValues();
        public float GetAverageMass();
        public float GetAverageFlex();
        public float GetAveragePump();

        public void ClearEventListeners();

        public bool Listen(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject, MuscleValueListenerDelegate callback, out MuscleValueListener listener);

        public bool StopListening(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject);

        public int StopListening(EngineInternal.IEngineObject listeningObject);

    }
}
