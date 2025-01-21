#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Unity.Collections;
using Unity.Mathematics;

namespace Swole.API.Unity
{
     
    public class MuscularRenderedCharacter : RenderedCharacter
    {

        public MuscularRenderedCharacter parent;

        [Serializable]
        public enum MuscularSurfaceState
        {

            NonMuscular, Muscular, Veins

        }

        [Tooltip("Links to the array above and determines if a surface has muscle data.")]
        public MuscularSurfaceState[] inputMuscularSurfaceStates;

        public CustomSkinnedMeshRenderer[] inputCustomSkinnedRenderers;
        [Tooltip("Links to the array above and determines if a renderer has muscle data.")]
        public MuscularSurfaceState[] inputMuscularCustomSkinnedRendererStates;

        public InputMuscleDependentBlendShape[] inputMuscleDependentBlendShapes;

#if UNITY_EDITOR

        /// <summary>
        /// !!! ONLY USED IN UNITY EDITOR
        /// </summary>
        [Range(0, 2)]
        public float breastPresenceEditor;

        /// <summary>
        /// !!! ONLY USED IN UNITY EDITOR
        /// </summary>
        [Serializable]
        public struct MuscleGroupEditor
        {

            public string name;

            [Range(0, 2)]
            public float mass;

            [Range(0, 1.5f)]
            public float flex;

            [Range(0, 1.5f)]
            public float pump;

            [NonSerialized]
            public float prevMass;

            [NonSerialized]
            public float prevFlex;

            [NonSerialized]
            public float prevPump;

            public bool HasUpdated => prevMass != mass || prevFlex != flex || prevPump != pump;

        }

        /// <summary>
        /// !!! ONLY USED IN UNITY EDITOR
        /// </summary>
        public MuscleGroupEditor allMuscleGroupsEditor = new MuscleGroupEditor() { name = "All Muscle Groups" };

        /// <summary>
        /// !!! ONLY USED IN UNITY EDITOR
        /// </summary>
        public MuscleGroupEditor[] muscleGroupEditors;

        protected override void OnValidate()
        {

            base.OnValidate();

            BreastPresence = breastPresenceEditor;

            if (m_muscleGroupNames == null || !m_muscleGroupValues.IsCreated) return;

            if (muscleGroupEditors == null || (muscleGroupEditors.Length != m_muscleGroupNames.Length))
            {

                muscleGroupEditors = new MuscleGroupEditor[m_muscleGroupNames.Length];

                for (int a = 0; a < m_muscleGroupNames.Length; a++) muscleGroupEditors[a] = new MuscleGroupEditor() { name = m_muscleGroupNames[a] };

            }

            bool setAll = allMuscleGroupsEditor.HasUpdated;

            allMuscleGroupsEditor.prevFlex = allMuscleGroupsEditor.flex;
            allMuscleGroupsEditor.prevMass = allMuscleGroupsEditor.mass;
            allMuscleGroupsEditor.prevPump = allMuscleGroupsEditor.pump;

            for (int a = 0; a < muscleGroupEditors.Length; a++)
            {

                MuscleGroupEditor editor = muscleGroupEditors[a];

                if (setAll)
                {

                    editor.mass = allMuscleGroupsEditor.mass;
                    editor.flex = allMuscleGroupsEditor.flex;
                    editor.pump = allMuscleGroupsEditor.pump;

                }

                if (editor.HasUpdated)
                {

                    m_muscleGroupValues[a] = new MuscleGroupInfo(m_muscleGroupValues[a].mirroredIndex, editor.mass, editor.flex, editor.pump);

                    UpdateMuscleGroupDependenciesUnsafe(a);

                }

                editor.prevMass = editor.mass;
                editor.prevFlex = editor.flex;
                editor.prevPump = editor.pump;

                muscleGroupEditors[a] = editor;

            }

        }

#endif

        //

        public MuscularCharacterSharedData inputData;

        public bool instantiateMaterials = true;

        public override void Initialize()
        {

            InitializationStart();

            if (inputSurfaceRenderers != null)
            {

                ClearMuscleMaterials();

                bool createSurfaces = (m_Surfaces == null || m_Surfaces.Length == 0);

                //m_Surfaces = new CharacterSurface[inputSurfaceRenderers.Length];
                List<CharacterSurface> surfaces = new List<CharacterSurface>();

                for (int a = 0; a < inputSurfaceRenderers.Length; a++)
                {

                    SkinnedMeshRenderer renderer = inputSurfaceRenderers[a];
                    bool isMuscular = inputMuscularSurfaceStates[a] == MuscularSurfaceState.Muscular || inputMuscularSurfaceStates[a] == MuscularSurfaceState.Veins;//inputMuscularSurfaceStates == null || a >= inputMuscularSurfaceStates.Length ? false : inputMuscularSurfaceStates[a];

                    if (inputMuscularSurfaceStates[a] == MuscularSurfaceState.Muscular || inputMuscularSurfaceStates[a] == MuscularSurfaceState.NonMuscular)
                    {

                        if (createSurfaces) surfaces.Add(isMuscular ? new MuscularCharacterSurface(inputData, this, inputSurfaceRenderers[a]) : new CharacterSurface(this, renderer));

                    }

                    if (isMuscular && renderer != null)
                    {

                        Material[] materials = renderer.sharedMaterials;

                        for (int b = 0; b < materials.Length; b++)
                        {

                            if (materials[b] == null) continue;

                            if (instantiateMaterials) materials[b] = Instantiate<Material>(materials[b]);

                            AddMuscleMaterial(materials[b]);

                        }

                        renderer.sharedMaterials = materials;

                    }

                }

                if (createSurfaces) m_Surfaces = surfaces.ToArray();

                if (inputCustomSkinnedRenderers != null && inputMuscularCustomSkinnedRendererStates != null)
                {
                    for (int a = 0; a < Mathf.Min(inputCustomSkinnedRenderers.Length, inputMuscularCustomSkinnedRendererStates.Length); a++)
                    {

                        var customRenderer = inputCustomSkinnedRenderers[a];

                        if (customRenderer == null) continue;
                        customRenderer.Initialize();

                        bool isMuscular = inputMuscularCustomSkinnedRendererStates[a] == MuscularSurfaceState.Muscular || inputMuscularCustomSkinnedRendererStates[a] == MuscularSurfaceState.Veins;
                        if (!isMuscular) continue;

                        if (customRenderer.meshRenderer == null) continue;

                        Material[] materials = customRenderer.meshRenderer.sharedMaterials;

                        for (int b = 0; b < materials.Length; b++)
                        {

                            //if (materials[b] == null) continue;

                            //if (instantiateMaterials && !customRenderer.instantiateMaterials) materials[b] = Instantiate<Material>(materials[b]); Instantiating materials on custom skinned renderer does not copy skinning buffers, so custom renderer must instantiate its own materials

                            AddMuscleMaterial(materials[b]);

                        }

                        //customRenderer.meshRenderer.sharedMaterials = materials;

                    }

                }

            }

            if (inputData != null && inputData.muscleGroups != null)
            {

                if ((!m_muscleGroupValues.IsCreated || m_muscleGroupValues.Length == 0))
                {

                    m_muscleGroupValues = new NativeArray<MuscleGroupInfo>(inputData.muscleGroups.Length, Allocator.Persistent);
                    m_muscleGroupNames = new string[inputData.muscleGroups.Length];

                    for (int a = 0; a < inputData.muscleGroups.Length; a++)
                    {

                        m_muscleGroupValues[a] = new MuscleGroupInfo(inputData.muscleGroups[a].mirroredIndex, 0, 0, 0);
                        m_muscleGroupNames[a] = inputData.muscleGroups[a] == null ? "null" : inputData.muscleGroups[a].name;

                    }

                }

                if (inputMuscleDependentBlendShapes != null)
                {

                    muscleDependentBlendShapes = new List<MuscleDependentBlendShape>[inputData.muscleGroups.Length];
                    List<MuscleDependentBlendShape> GetMuscleDependentBlendShapeList(int muscleGroupIndex)
                    {

                        var list = muscleDependentBlendShapes[muscleGroupIndex];

                        if (list == null)
                        {

                            list = new List<MuscleDependentBlendShape>();
                            muscleDependentBlendShapes[muscleGroupIndex] = list;

                        }

                        return list;

                    }

                    foreach (var input in inputMuscleDependentBlendShapes)
                    {

                        if (string.IsNullOrEmpty(input.muscleGroupName) || string.IsNullOrEmpty(input.shapeName)) continue;

                        int muscleGroupIndex = GetMuscleGroupIndex(input.muscleGroupName);
                        int shapeIndex = GetBlendShapeIndex(input.shapeName);

                        if (muscleGroupIndex < 0 || shapeIndex < 0) continue;

                        var dependencyList = GetMuscleDependentBlendShapeList(muscleGroupIndex);
                        var dependency = input.dependency;
                        dependency.shapeIndex = shapeIndex;
                        dependencyList.Add(dependency);

                    }

                }

            }

        }

        //

        protected string m_massMaterialPropertyHeader = "_Mass_";
        protected string m_flexMaterialPropertyHeader = "_Flex_";
        protected string m_pumpMaterialPropertyHeader = "_Pump_";

        public string MassMaterialPropertyHeader => m_massMaterialPropertyHeader;
        public string FlexMaterialPropertyHeader => m_flexMaterialPropertyHeader;
        public string PumpMaterialPropertyHeader => m_pumpMaterialPropertyHeader;

        protected List<Material> m_muscleMaterials = new List<Material>();
        public void AddMuscleMaterial(Material material)
        {

            if (m_muscleMaterials == null) m_muscleMaterials = new List<Material>();

            if (material != null) m_muscleMaterials.Add(material);

        }

        protected void ClearMuscleMaterials()
        {

            if (m_muscleMaterials == null) m_muscleMaterials = new List<Material>();

            m_muscleMaterials.Clear();

        }

        protected string[] m_muscleGroupNames;
        public string GetMuscleGroupName(int index) => m_muscleGroupNames == null ? "null" : (index >= 0 && index < m_muscleGroupNames.Length ? GetMuscleGroupNameUnsafe(index) : "null");
        public string GetMuscleGroupNameUnsafe(int index) => m_muscleGroupNames[index];
        public int GetMuscleGroupIndex(string muscleGroupName)
        {

            if (m_muscleGroupNames == null) return -1;

            for (int a = 0; a < m_muscleGroupNames.Length; a++) if (muscleGroupName == m_muscleGroupNames[a]) return a;
            muscleGroupName = muscleGroupName.ToLower().Trim();
            for (int a = 0; a < m_muscleGroupNames.Length; a++) if (muscleGroupName == m_muscleGroupNames[a].ToLower().Trim()) return a;
            muscleGroupName = muscleGroupName.Replace("_", "");
            for (int a = 0; a < m_muscleGroupNames.Length; a++) if (muscleGroupName == m_muscleGroupNames[a].ToLower().Trim().Replace("_", "")) return a;

            return -1; 

        }
        public int GetMuscleGroupIndex(MuscleGroupIdentifier identifier) => GetMuscleGroupIndex(identifier.ToString());
        public int FindMuscleGroup(string muscleGroupName) => GetMuscleGroupIndex(muscleGroupName);
        public int FindMuscleGroup(MuscleGroupIdentifier identifier) => GetMuscleGroupIndex(identifier);


        protected NativeArray<MuscleGroupInfo> m_muscleGroupValues;
        public NativeArray<MuscleGroupInfo> MuscleGroupValues => m_muscleGroupValues;

        public int MuscleGroupCount => m_muscleGroupValues.IsCreated ? m_muscleGroupValues.Length : 0;

        protected string m_breastPresenceProperty = "_BreastPresence";
        protected float m_breastPresence;
        public float BreastPresence
        {

            get => m_breastPresence;

            set
            {

                m_breastPresence = value;

                foreach (Material material in m_muscleMaterials) material.SetFloat(m_breastPresenceProperty, m_breastPresence);

                if (children != null)
                {
                    foreach(var child in children)
                    {
                        if (child is MuscularRenderedCharacter msr) msr.BreastPresence = m_breastPresence;
                    }
                }

            }

        }

        protected override void OnDestroy()
        {

            base.OnDestroy();

            if (m_muscleGroupValues.IsCreated) m_muscleGroupValues.Dispose();

            m_muscleGroupValues = default;

        }

        public bool SetMuscleGroupValues(int muscleGroupIndex, float3 values, bool updateDependencies = true)
        {

            bool b1 = SetMuscleGroupMass(muscleGroupIndex, values.x, false);
            bool b2 = SetMuscleGroupFlex(muscleGroupIndex, values.y, false);
            bool b3 = SetMuscleGroupPump(muscleGroupIndex, values.z, updateDependencies, b1 || b2);

            return b1 || b2 || b3;

        }
        public bool SetMuscleGroupMass(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false)
        {

            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return false;

            return SetMuscleGroupMassUnsafe(muscleGroupIndex, mass, updateDependencies, hasUpdated);

        }
        public bool SetMuscleGroupFlex(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false)
        {

            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return false;

            return SetMuscleGroupFlexUnsafe(muscleGroupIndex, flex, updateDependencies, hasUpdated);

        }
        public bool SetMuscleGroupPump(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false)
        {

            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return false;

            return SetMuscleGroupPumpUnsafe(muscleGroupIndex, pump, updateDependencies, hasUpdated);

        }

        public bool SetMuscleGroupValuesUnsafe(int muscleGroupIndex, float3 values, bool updateDependencies = true)
        {

            bool b1 = SetMuscleGroupMassUnsafe(muscleGroupIndex, values.x, false);
            bool b2 = SetMuscleGroupFlexUnsafe(muscleGroupIndex, values.y, false);
            bool b3 = SetMuscleGroupPumpUnsafe(muscleGroupIndex, values.z, updateDependencies, b1 || b2);

            return b1 || b2 || b3;

        }
        public bool SetMuscleGroupMassUnsafe(int muscleGroupIndex, float mass, bool updateDependencies = true, bool hasUpdated = false)
        {

            var values = m_muscleGroupValues[muscleGroupIndex];

            float prevMass = values.mass;

            values.mass = mass;

            m_muscleGroupValues[muscleGroupIndex] = values;

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is MuscularRenderedCharacter msr) msr.SetMuscleGroupMassUnsafe(muscleGroupIndex, mass, false, hasUpdated);
                }
            }

            if (updateDependencies && (mass != prevMass || hasUpdated)) UpdateMuscleGroupDependenciesUnsafe(muscleGroupIndex);
            
            bool updated = mass != prevMass;
            try
            {
                if (updated && OnMuscleMassChanged != null) OnMuscleMassChanged.Invoke(m_muscleGroupNames[muscleGroupIndex], muscleGroupIndex, values);
            } 
            catch(Exception ex)
            {
                swole.LogError(ex);
            }

            return updated;

        }
        public bool SetMuscleGroupFlexUnsafe(int muscleGroupIndex, float flex, bool updateDependencies = true, bool hasUpdated = false)
        {

            var values = m_muscleGroupValues[muscleGroupIndex];

            float prevFlex = values.flex;

            values.flex = flex;

            m_muscleGroupValues[muscleGroupIndex] = values;

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is MuscularRenderedCharacter msr) msr.SetMuscleGroupFlexUnsafe(muscleGroupIndex, flex, false, hasUpdated);
                }
            }

            if (updateDependencies && (flex != prevFlex || hasUpdated)) UpdateMuscleGroupDependenciesUnsafe(muscleGroupIndex);

            bool updated = flex != prevFlex;
            try
            {
                if (updated && OnMuscleFlexChanged != null) OnMuscleFlexChanged.Invoke(m_muscleGroupNames[muscleGroupIndex], muscleGroupIndex, values);
            }
            catch (Exception ex)
            {
                swole.LogError(ex);
            }

            return flex != prevFlex;

        }
        public bool SetMuscleGroupPumpUnsafe(int muscleGroupIndex, float pump, bool updateDependencies = true, bool hasUpdated = false)
        {

            var values = m_muscleGroupValues[muscleGroupIndex];

            float prevPump = values.pump;

            values.pump = pump;

            m_muscleGroupValues[muscleGroupIndex] = values;

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is MuscularRenderedCharacter msr) msr.SetMuscleGroupPumpUnsafe(muscleGroupIndex, pump, false, hasUpdated);
                }
            }

            if (updateDependencies && (pump != prevPump || hasUpdated)) UpdateMuscleGroupDependenciesUnsafe(muscleGroupIndex);

            bool updated = pump != prevPump;
            try
            {
                if (updated && OnMusclePumpChanged != null) OnMusclePumpChanged.Invoke(m_muscleGroupNames[muscleGroupIndex], muscleGroupIndex, values);
            }
            catch (Exception ex)
            {
                swole.LogError(ex);
            }

            return pump != prevPump;

        }

        public float3 GetMuscleGroupValues(int muscleGroupIndex)
        {
            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return float3.zero;
            return GetMuscleGroupValuesUnsafe(muscleGroupIndex);
        }
        public float3 GetMuscleGroupValuesUnsafe(int muscleGroupIndex) 
        { 
            var values = m_muscleGroupValues[muscleGroupIndex];
            return new float3(values.mass, values.flex, values.pump);
        }

        public float GetMuscleGroupMass(int muscleGroupIndex)
        {
            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return 0;
            return GetMuscleGroupMassUnsafe(muscleGroupIndex);
        }
        public float GetMuscleGroupMassUnsafe(int muscleGroupIndex) => m_muscleGroupValues[muscleGroupIndex].mass;

        public float GetMuscleGroupFlex(int muscleGroupIndex)
        {
            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return 0;
            return GetMuscleGroupFlexUnsafe(muscleGroupIndex);
        }
        public float GetMuscleGroupFlexUnsafe(int muscleGroupIndex) => m_muscleGroupValues[muscleGroupIndex].flex;

        public float GetMuscleGroupPump(int muscleGroupIndex)
        {
            if (muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupValues.Length) return 0;
            return GetMuscleGroupPumpUnsafe(muscleGroupIndex);
        }
        public float GetMuscleGroupPumpUnsafe(int muscleGroupIndex) => m_muscleGroupValues[muscleGroupIndex].pump;

        public void SetGlobalMuscleValues(float3 values)
        {
            for (int a = 0; a < MuscleGroupCount; a++) SetMuscleGroupValuesUnsafe(a, values);
        }
        public void SetGlobalMass(float mass)
        {
            for (int a = 0; a < MuscleGroupCount; a++) SetMuscleGroupMassUnsafe(a, mass);
        }
        public void SetGlobalFlex(float flex)
        {
            for (int a = 0; a < MuscleGroupCount; a++) SetMuscleGroupFlexUnsafe(a, flex);
        }
        public void SetGlobalPump(float pump)
        {
            for (int a = 0; a < MuscleGroupCount; a++) SetMuscleGroupPumpUnsafe(a, pump);
        }

        public float3 GetAverageMuscleValues()
        {
            float3 values = default;
            int count = MuscleGroupCount;
            for (int a = 0; a < count; a++) values = values + GetMuscleGroupValuesUnsafe(a);
            return values / Mathf.Max(1, count);
        }
        public float GetAverageMass()
        {
            float mass = 0;
            int count = MuscleGroupCount;
            for (int a = 0; a < count; a++) mass = mass + GetMuscleGroupMassUnsafe(a);
            return mass / Mathf.Max(1, count);
        }
        public float GetAverageFlex()
        {
            float flex = 0;
            int count = MuscleGroupCount;
            for (int a = 0; a < count; a++) flex = flex + GetMuscleGroupFlexUnsafe(a);
            return flex / Mathf.Max(1, count);
        }
        public float GetAveragePump()
        {
            float pump = 0;
            int count = MuscleGroupCount;
            for (int a = 0; a < count; a++) pump = pump + GetMuscleGroupPumpUnsafe(a);
            return pump / Mathf.Max(1, count);
        }

        [Serializable]
        public struct ValueRange
        {

            public float minValue;
            public float maxValue;

            public bool allowMinOverflow;
            public bool allowMaxOverflow;

            public bool invert;

            public float GetValue(float muscleValue)
            {

                float val = (muscleValue - minValue) / (maxValue - minValue);

                if (!allowMinOverflow) val = math.max(0, val);
                if (!allowMaxOverflow) val = math.min(1, val);

                return invert ? (1 - val) : val;

            }

        }

        [Serializable]
        public struct MuscleDependentValue
        {

            public ValueRange mainValue;

            public bool useReversion;

            public ValueRange reversionValue;

            public float GetValue(float muscleValue)
            {

                return mainValue.GetValue(muscleValue) * (useReversion ? (1 - reversionValue.GetValue(muscleValue)) : 1);

            }

        }

        /// <summary>
        /// Used for shapes such as those on character heads, where a mass shape is used to keep the neck seam connected to the body mesh neck.
        /// </summary>
        [Serializable]
        public struct MuscleDependentBlendShape
        {

            [HideInInspector]
            public int shapeIndex;

            public bool isMassDependent;
            public MuscleDependentValue massDependency;

            public bool isFlexDependent;
            public MuscleDependentValue flexDependency;

            public bool isPumpDependent;
            public MuscleDependentValue pumpDependency;

            public float GetMassDependentValue(MuscleGroupInfo muscleData) => isMassDependent ? massDependency.GetValue(muscleData.mass) : 1;
            public float GetFlexDependentValue(MuscleGroupInfo muscleData) => isFlexDependent ? flexDependency.GetValue(muscleData.flex) : 1;
            public float GetPumpDependentValue(MuscleGroupInfo muscleData) => isPumpDependent ? pumpDependency.GetValue(muscleData.pump) : 1;

            public float GetWeight(MuscleGroupInfo muscleData, float initialWeight = 1)
            {

                return initialWeight * GetMassDependentValue(muscleData) * GetFlexDependentValue(muscleData) * GetPumpDependentValue(muscleData);

            }

        }

        [Serializable]
        public struct InputMuscleDependentBlendShape
        {

            public string shapeName;
            public string muscleGroupName;

            public MuscleDependentBlendShape dependency;

        }
        protected List<MuscleDependentBlendShape>[] muscleDependentBlendShapes;

        protected List<MuscleValueListener>[] muscleListeners;

        public event MuscleValueChangeDelegate OnMuscleMassChanged;
        public event MuscleValueChangeDelegate OnMuscleFlexChanged;
        public event MuscleValueChangeDelegate OnMusclePumpChanged;

        public void ClearEventListeners()
        {
            OnMuscleMassChanged = null;
            OnMuscleFlexChanged = null;
            OnMusclePumpChanged = null;
        }

        public bool Listen(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject, MuscleValueListenerDelegate callback, out MuscleValueListener listener)
        {
            listener = null;
            if (listeningObject == null || callback == null || muscleGroupIndex < 0 || muscleGroupIndex >= MuscleGroupCount) return false;

            if (muscleListeners == null) muscleListeners = new List<MuscleValueListener>[MuscleGroupCount];

            List<MuscleValueListener> listeners = muscleListeners[muscleGroupIndex];
            if (listeners == null)
            {
                listeners = new List<MuscleValueListener>();
                muscleListeners[muscleGroupIndex] = listeners;
            }

            listener = new MuscleValueListener() { listeningObject = listeningObject, callback = callback };
            listeners.Add(listener);

            return true;

        }

        public bool StopListening(int muscleGroupIndex, EngineInternal.IEngineObject listeningObject)
        {

            if (listeningObject == null || muscleListeners == null || muscleGroupIndex < 0 || muscleGroupIndex >= MuscleGroupCount) return false;

            List<MuscleValueListener> listeners = muscleListeners[muscleGroupIndex]; 
            if (listeners != null)
            {

                return listeners.RemoveAll(i => i.listeningObject == listeningObject) > 0;

            }

            return false;

        }

        public int StopListening(EngineInternal.IEngineObject listeningObject)
        {

            if (listeningObject == null || muscleListeners == null) return 0;

            int removed = 0;

            for (int a = 0; a < muscleListeners.Length; a++)
            {

                List<MuscleValueListener> listeners = muscleListeners[a];
                if (listeners != null) removed += listeners.RemoveAll(i => i.listeningObject == listeningObject);

            }

            return removed;

        }

        public void UpdateMuscleGroupDependencies(int muscleGroupIndex)
        {

            if (m_muscleGroupNames == null || muscleGroupIndex < 0 || muscleGroupIndex >= m_muscleGroupNames.Length) return;

            UpdateMuscleGroupDependenciesUnsafe(muscleGroupIndex);

        }

        public void UpdateMuscleGroupDependenciesUnsafe(int muscleGroupIndex)
        {

            string property = m_muscleGroupNames[muscleGroupIndex];
            var muscleData = m_muscleGroupValues[muscleGroupIndex];

            foreach (Material material in m_muscleMaterials)
            {

                material.SetFloat(m_massMaterialPropertyHeader + property, muscleData.mass);
                material.SetFloat(m_flexMaterialPropertyHeader + property, muscleData.flex);
                material.SetFloat(m_pumpMaterialPropertyHeader + property, muscleData.pump);
                
            }

            if (muscleDependentBlendShapes != null)
            {

                var shapes = muscleDependentBlendShapes[muscleGroupIndex];
                if (shapes != null)
                {

                    foreach (var shape in shapes) 
                    {
                        SetBlendShapeWeight(shape.shapeIndex, shape.GetWeight(muscleData)); 
                    }

                }

            }

            if (muscleListeners != null)
            {

                var listeners = muscleListeners[muscleGroupIndex];
                if (listeners != null)
                {

                    listeners.RemoveAll(i => (i.DisposeIfNull() || !i.Valid));
                    foreach (var listener in listeners) listener.callback(muscleData);

                }

            }

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is MuscularRenderedCharacter msr) msr.UpdateMuscleGroupDependenciesUnsafe(muscleGroupIndex);  
                }
            }

        }
        public void UpdateMuscleGroupMaterialPropertiesUnsafe(int muscleGroupIndex)
        {
            string property = m_muscleGroupNames[muscleGroupIndex];
            var muscleData = m_muscleGroupValues[muscleGroupIndex];

            foreach (Material material in m_muscleMaterials)
            {
                material.SetFloat(m_massMaterialPropertyHeader + property, muscleData.mass);
                material.SetFloat(m_flexMaterialPropertyHeader + property, muscleData.flex);
                material.SetFloat(m_pumpMaterialPropertyHeader + property, muscleData.pump);
            }

            if (children != null)
            {
                foreach (var child in children)
                {
                    if (child is MuscularRenderedCharacter msr) msr.UpdateMuscleGroupMaterialPropertiesUnsafe(muscleGroupIndex);
                }
            }
        }

    }

}

#endif
