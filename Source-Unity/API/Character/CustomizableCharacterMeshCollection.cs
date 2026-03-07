#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity;
using Unity.Mathematics;

namespace Swole.Morphing
{
    public class CustomizableCharacterMeshCollection : MonoBehaviour, IMuscularBasic
    {

        #region Sub Types

        [Serializable]
        public struct V1
        {
            public string id;
            public CustomizableCharacterMesh mesh;
        }
        [Serializable]
        public struct V2
        {
            public string id;
            public CustomizableCharacterMeshV2 mesh;
        }

        #endregion

        #region Main

        [SerializeField]
        protected V1[] meshesV1;

        [SerializeField]
        protected V2[] meshesV2;

        public void AddToCollection(string id, ICustomizableCharacter mesh)
        {
            if (mesh is CustomizableCharacterMesh v1)
            {
                if (meshesV1 == null) meshesV1 = new V1[0];
                meshesV1 = (V1[])meshesV1.Add(new V1()
                {
                    id = id,
                    mesh = v1
                });
            } 
            else if (mesh is CustomizableCharacterMeshV2 v2)
            {
                if (meshesV2 == null) meshesV2 = new V2[0];
                meshesV2 = (V2[])meshesV2.Add(new V2()
                {
                    id = id,
                    mesh = v2
                });
            }
        }

        public bool TryGetMesh(string id, out ICustomizableCharacter mesh)
        {
            mesh = null;

            if (meshesV2 != null && meshesV2.Length > 0)
            {
                foreach(var mesh_ in meshesV2)
                {
                    if (mesh_.id == id)
                    {
                        mesh = mesh_.mesh;
                        return true;
                    }
                }
            }

            if (meshesV1 != null && meshesV1.Length > 0)
            {
                foreach (var mesh_ in meshesV1)
                {
                    if (mesh_.id == id)
                    {
                        mesh = mesh_.mesh;
                        return true;
                    }
                }
            }

            return false;
        }

        public ICustomizableCharacter this[int index]
        {
            get
            {
                int indexOffset = 0;
                if (meshesV1 != null)
                {
                    if (index >= 0 && index < meshesV1.Length) return meshesV1[index].mesh;
                    indexOffset += meshesV1.Length;
                }

                if (meshesV2 != null)
                {
                    int ind = index - indexOffset;
                    if (ind >= 0 && ind < meshesV2.Length) return meshesV2[index].mesh;
                    indexOffset += meshesV2.Length;
                }

                return null;
            }
        }

        public int MeshCount => (meshesV1 == null ? 0 : meshesV1.Length) + (meshesV2 == null ? 0 : meshesV2.Length);

        public ICustomizableCharacter First => this[0];
        public bool HasMeshes => MeshCount > 0;

        #endregion

        #region IMuscularBasic

        public string GetMuscleGroupName(int index)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupName(index);
            }

            return null;
        }

        public string GetMuscleGroupNameUnsafe(int index)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupNameUnsafe(index);
            }

            return null;
        }

        public int GetMuscleGroupIndex(string muscleGroupName)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupIndex(muscleGroupName);
            }

            return -1;
        }

        public int GetMuscleGroupIndex(MuscleGroupIdentifier identifier)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupIndex(identifier);
            }

            return -1;
        }

        public int GetMuscleGroupIndexForArray(string muscleGroupName)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupIndexForArray(muscleGroupName);
            }

            return -1;
        }

        public int GetMuscleGroupIndexForArray(MuscleGroupIdentifier identifier)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupIndexForArray(identifier);
            }

            return -1;
        }

        public int FindMuscleGroup(string muscleGroupName)
        {
            var main = First;
            if (main != null)
            {
                return main.FindMuscleGroup(muscleGroupName);
            }

            return -1;
        }

        public int FindMuscleGroup(MuscleGroupIdentifier identifier)
        {
            var main = First;
            if (main != null)
            {
                return main.FindMuscleGroup(identifier);
            }

            return -1;
        }

        public bool SetMuscleGroupValues(int muscleGroupIndex, float3 values, bool updateDependencies = true)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupValues(muscleGroupIndex, values, updateDependencies);
            }

            return false;
        }

        public bool SetMuscleGroupMass(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupMass(muscleGroupIndex, mass, updateDependencies, hasUpdated);
            }

            return false;
        }

        public bool SetMuscleGroupFlex(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupFlex(muscleGroupIndex, flex, updateDependencies, hasUpdated);
            }

            return false;
        }

        public bool SetMuscleGroupPump(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupPump(muscleGroupIndex, pump, updateDependencies, hasUpdated);
            }

            return false;
        }

        public bool SetMuscleGroupValuesUnsafe(int muscleGroupIndex, float3 values, bool updateDependencies = true)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupValuesUnsafe(muscleGroupIndex, values, updateDependencies);
            }

            return false;
        }

        public bool SetMuscleGroupMassUnsafe(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupMassUnsafe(muscleGroupIndex, mass, updateDependencies, hasUpdated);
            }

            return false;
        }

        public bool SetMuscleGroupFlexUnsafe(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupFlexUnsafe(muscleGroupIndex, flex, updateDependencies, hasUpdated);
            }

            return false;
        }

        public bool SetMuscleGroupPumpUnsafe(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false)
        {
            var main = First;
            if (main != null)
            {
                return main.SetMuscleGroupPumpUnsafe(muscleGroupIndex, pump, updateDependencies, hasUpdated);
            }

            return false;
        }

        public float3 GetMuscleGroupValues(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupValues(muscleGroupIndex);
            }

            return default;
        }

        public float3 GetMuscleGroupValuesUnsafe(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupValuesUnsafe(muscleGroupIndex);
            }

            return default;
        }

        public float GetMuscleGroupMass(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupMass(muscleGroupIndex);
            }

            return default;
        }

        public float GetMuscleGroupMassUnsafe(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupMassUnsafe(muscleGroupIndex);
            }

            return default;
        }

        public float GetMuscleGroupFlex(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupFlex(muscleGroupIndex);
            }

            return default;
        }

        public float GetMuscleGroupFlexUnsafe(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupFlexUnsafe(muscleGroupIndex);
            }

            return default;
        }

        public float GetMuscleGroupPump(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupPump(muscleGroupIndex);
            }

            return default;
        }

        public float GetMuscleGroupPumpUnsafe(int muscleGroupIndex)
        {
            var main = First;
            if (main != null)
            {
                return main.GetMuscleGroupPumpUnsafe(muscleGroupIndex);
            }

            return default;
        }

        public void SetGlobalMuscleValues(float3 values)
        {
            var main = First;
            if (main != null)
            {
                main.SetGlobalMuscleValues(values); 
            }
        }

        public void SetGlobalMass(float mass)
        {
            var main = First;
            if (main != null)
            {
                main.SetGlobalMass(mass);
            }
        }

        public void SetGlobalFlex(float flex)
        {
            var main = First;
            if (main != null)
            {
                main.SetGlobalFlex(flex);
            }
        }

        public void SetGlobalPump(float pump)
        {
            var main = First;
            if (main != null)
            {
                main.SetGlobalPump(pump);
            }
        }

        public float3 GetAverageMuscleValues()
        {
            var main = First;
            if (main != null)
            {
                return main.GetAverageMuscleValues();
            }

            return default;
        }

        public float GetAverageMass()
        {
            var main = First;
            if (main != null)
            {
                return main.GetAverageMass();
            }

            return default;
        }

        public float GetAverageFlex()
        {
            var main = First;
            if (main != null)
            {
                return main.GetAverageFlex();
            }

            return default;
        }

        public float GetAveragePump()
        {
            var main = First;
            if (main != null)
            {
                return main.GetAveragePump();
            }

            return default;
        }

        public void ClearEventListeners()
        {
            var main = First;
            if (main != null)
            {
                ClearEventListeners();
            }
        }

        public bool Listen(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject, MuscleValueListenerDelegate callback, out MuscleValueListener listener)
        {
            listener = null;

            var main = First;
            if (main != null)
            {
                return main.Listen(muscleGroupIndex, listeningObject, callback, out listener);
            }

            return false;
        }

        public bool StopListening(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject)
        {
            var main = First;
            if (main != null)
            {
                return main.StopListening(muscleGroupIndex, listeningObject);
            }

            return false;
        }

        public int StopListening(EngineInternal.IEngineObject listeningObject)
        {
            var main = First;
            if (main != null)
            {
                return main.StopListening(listeningObject);
            }

            return -1;
        }

        public int MuscleGroupCount
        {
            get
            {
                var main = First;
                if (main != null)
                {
                    return main.MuscleGroupCount;
                }

                return -1;
            }
        }

        public float BreastPresence 
        { 
            get
            {
                var main = First;
                if (main != null)
                {
                    return main.BreastPresence;
                }

                return -1;
            }

            set
            {
                var main = First;
                if (main != null)
                {
                    main.BreastPresence = value;
                }
            }
        }

        #endregion

    }
}

#endif