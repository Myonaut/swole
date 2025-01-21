#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.API.Unity.Animation;

namespace Swole.API.Unity
{

    public class ToonClothingUpdater : SingletonBehaviour<ToonClothingUpdater>
    {
        public static int ExecutionPriority => CustomAnimatorUpdater.FinalAnimationBehaviourPriority + 1;
        public override int Priority => ExecutionPriority;
        public override bool DestroyOnLoad => false;

        protected readonly List<ToonClothingController> controllers = new List<ToonClothingController>();
        public static bool Register(ToonClothingController controller)
        {
            var instance = Instance;
            if (controller == null || instance == null) return false;

            if (!instance.controllers.Contains(controller)) instance.controllers.Add(controller);
            return true;
        }
        public static bool Unregister(ToonClothingController controller)
        {
            var instance = Instance;
            if (controller == null || instance == null) return false;

            return instance.controllers.Remove(controller);
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnLateUpdate()
        {
            controllers.RemoveAll(i => i == null);
            foreach (var controller in controllers) if (controller != null && controller.enabled) controller.LateUpdateStep();
        }

        public override void OnUpdate()
        {
            //foreach (var controller in controllers) if (controller != null && controller.enabled) controller.UpdateStep();
        }
    }

    public delegate void SetExternalRippageValueDelegate(string key, float value);
    public class ToonClothingController : MonoBehaviour
    {



        public const string _rippageMaterialPropertyPrefix = "_Rip_";

        public ToonMaterialMaskController maskController;
        public MuscularRenderedCharacter muscularCharacter;

        public void Awake()
        {
            if (maskController == null) maskController = gameObject.GetComponentInChildren<ToonMaterialMaskController>(true); 
            if (muscularCharacter == null) muscularCharacter = gameObject.GetComponentInChildren<MuscularRenderedCharacter>(true);

            if (clothing != null)
            {
                foreach(var cloth in clothing)
                {
                    cloth.controller = this;

                    if (cloth.maskController == null) cloth.maskController = maskController;
                    if (cloth.rippables != null)
                    {
                        foreach (var rippable in cloth.rippables) 
                        {
                            rippable.clothingInstance = cloth;
                        }
                    }
                }
            }

            if (muscularCharacter != null)
            {
                muscularCharacter.OnMuscleMassChanged += OnMuscleUpdate;
                muscularCharacter.OnMuscleFlexChanged += OnMuscleFlexUpdate;
            }

            ToonClothingUpdater.Register(this);
        }

        public void OnDestroy()
        {
            if (muscularCharacter != null)
            {
                muscularCharacter.OnMuscleMassChanged -= OnMuscleUpdate;
                muscularCharacter.OnMuscleFlexChanged -= OnMuscleFlexUpdate;
                muscularCharacter = null;
            }

            ToonClothingUpdater.Unregister(this); 
        }

        private readonly HashSet<string> updatedMuscles = new HashSet<string>();
        protected void OnMuscleUpdate(string muscleName, int muscleIndex, MuscleGroupInfo muscleInfo)
        {
            updatedMuscles.Add(muscleName);
        }
        protected void OnMuscleFlexUpdate(string muscleName, int muscleIndex, MuscleGroupInfo muscleInfo)
        {
            if (muscleInfo.flex > 0.3f) updatedMuscles.Add(muscleName);
        }
        protected readonly HashSet<Rippable> reupdateQueue = new HashSet<Rippable>();
        protected readonly List<Rippable> toUpdateQueue = new List<Rippable>();
        public bool UpdateRequeuedRippables(HashSet<string> targetMusclesOutput)
        {
            if (reupdateQueue.Count <= 0) return false;

            toUpdateQueue.Clear();
            foreach(var rippable in reupdateQueue) toUpdateQueue.Add(rippable);
            reupdateQueue.Clear();
            
            float deltaTime = Time.deltaTime;
            foreach(var rippable in toUpdateQueue)
            {
                if (rippable.clothingInstance == null || !rippable.clothingInstance.isActive) continue;

                rippable.UpdateTargetRippageValuesFromCache(targetMusclesOutput, muscularCharacter, rippable.clothingInstance.SetRippageValueIfGreater, deltaTime, out bool changed);
                if (changed) reupdateQueue.Add(rippable); 
            }

            return reupdateQueue.Count > 0;
        }
        public void UpdateAllRippageValues()
        {
            if (clothing != null)
            {
                reupdateQueue.Clear();
                float deltaTime = Time.deltaTime;
                foreach(var cloth in clothing)
                {
                    if (!cloth.isActive || cloth.rippables == null) continue;

                    UpdateRippageValues(deltaTime, cloth.SetRippageValueIfGreater, cloth.rippables, reupdateQueue);
                }
            }
        }
        public void UpdateRippageValues(float deltaTime, SetExternalRippageValueDelegate setRippage, ICollection<Rippable> queue, ICollection<Rippable> reupdateQueue)
        {
            foreach (var rippable in queue)
            {
                rippable.UpdateRippageValues(muscularCharacter, setRippage, deltaTime, out bool changed);
                if (changed) reupdateQueue.Add(rippable);
            }
        }

        private static readonly List<MuscleGroupRippageTarget> _tempTargets = new List<MuscleGroupRippageTarget>();
        public void UpdateRippageValuesPerUpdatedMuscle(ICollection<string> updatedMuscles)
        {
            if (clothing != null)
            {
                float deltaTime = Time.deltaTime;
                foreach (var cloth in clothing)
                {
                    if (!cloth.isActive || cloth.rippables == null) continue;

                    _tempTargets.Clear();
                    cloth.FillRippageTargets(updatedMuscles, _tempTargets);
                    UpdateTargetRippageValues(deltaTime, _tempTargets, cloth.SetRippageValueIfGreater, cloth.rippables, reupdateQueue);
                }
            }
        }
        public void UpdateTargetRippageValues(float deltaTime, ICollection<MuscleGroupRippageTarget> rippageTargets, SetExternalRippageValueDelegate setRippage, ICollection<Rippable> queue, ICollection<Rippable> reupdateQueue)
        {
            foreach (var rippable in queue)
            {
                rippable.UpdateTargetRippageValues(muscularCharacter, rippageTargets, setRippage, deltaTime, out bool changed);
                if (changed) reupdateQueue.Add(rippable);
            }
        }

        // Body Alpha Mask
        // R - control the alpha of the vertex. Lower means less visible

        // Body Vein Mask
        // R - force all veins to be shown where the values are high
        // A - used to mask out veins. Higher means less visible


        // Vein Alpha Mask
        // R - control the alpha of the vein vertex. Lower means less visible

        // Veins Vein Mask
        // R - control the size of the vein vertex. Higher means less size

        [Serializable]
        public struct TargetRipRange
        {
            public string muscleGroupName;
            public Vector2 ripRange;
        }
        [Serializable]
        public struct RippageRequirement
        {
            public string name;
            [Tooltip("Only used if not targeting any specific muscles!")]
            public Vector2 ripRange;
            [Tooltip("Does the requirement always need to be fulfilled?")]
            public bool mandatory;

            [Tooltip("Will target all muscles for this rippable group if left empty.")]
            public TargetRipRange[] targets;

            public bool IsSatisfied(float rippage, Dictionary<string, float> rippageValues) => IsSatisfied(rippage, rippageValues, out _);
            public bool IsSatisfied(float rippage, Dictionary<string, float> rippageValues, out float satisfiedRippage)
            {
                satisfiedRippage = rippage;
                if (targets == null || targets.Length <= 0) return rippage >= ripRange.x && rippage <= ripRange.y;

                foreach(var target in targets)
                {
                    rippageValues.TryGetValue(target.muscleGroupName, out var value);
                    if (value >= target.ripRange.x && value <= target.ripRange.y) 
                    {
                        satisfiedRippage = value;
                        return true;
                    }
                }
                return false;
            }

            public bool IsSatisfied(float rippage, Dictionary<string, float> rippageValues, Vector2 exemptRange, out Vector2 satisfiedRange) => IsSatisfied(rippage, rippageValues, exemptRange, out satisfiedRange, out _);
            public bool IsSatisfied(float rippage, Dictionary<string, float> rippageValues, Vector2 exemptRange, out Vector2 satisfiedRange, out float satisfiedRippage)
            {
                satisfiedRange = Vector2.zero;
                satisfiedRippage = 0;
                if (targets == null || targets.Length <= 0) 
                {
                    if (rippage >= ripRange.x && rippage <= ripRange.y && (rippage < satisfiedRange.x || rippage > satisfiedRange.y))
                    {
                        satisfiedRange = ripRange;
                        satisfiedRippage = rippage;
                        return true;
                    }
                }

                foreach (var target in targets)
                {
                    rippageValues.TryGetValue(target.muscleGroupName, out var value);
                    if (value >= target.ripRange.x && value <= target.ripRange.y && (value < satisfiedRange.x || value > satisfiedRange.y)) 
                    {
                        satisfiedRange = target.ripRange;
                        satisfiedRippage = value;
                        return true; 
                    }
                }
                return false;
            }
        }
        [Serializable]
        public struct MaskEdit
        {
            public string name;
            public RippageRequirement[] rippageRequirements;
            public TargetMask targetMask;

            public bool invertRequirements;
            public bool ShouldApply(float rippage, bool rippedAway, Dictionary<string, float> rippageValues)
            {
                if (rippedAway) return invertRequirements;

                bool flag = true;
                if (rippageRequirements != null && rippageRequirements.Length > 0)
                {
                    flag = false;
                    foreach(var requirement in rippageRequirements)
                    {
                        bool satisfied = requirement.IsSatisfied(rippage, rippageValues);
                        if (!satisfied && requirement.mandatory)
                        {
                            flag = false;
                            break;
                        }

                        flag = flag || satisfied;
                    }
                }

                return invertRequirements ? !flag : flag;
            }
            public void Refresh(ToonMaterialMaskController maskController, float rippage, bool rippedAway, Dictionary<string, float> rippageValues)
            {
                if (ShouldApply(rippage, rippedAway, rippageValues)) targetMask.Apply(maskController); else targetMask.Unapply(maskController);
            }
        }
        [Serializable]
        public class ObjectHideProperties
        {
            public string[] objectsToHide;
            public RippageRequirement[] rippageRequirements;
            public bool invertRequirements;

            [NonSerialized]
            private int[] indicesToHide;
            public int[] GetIndicesToHide(GameObject[] hidableObjects)
            {
                if (objectsToHide == null || (indicesToHide != null && indicesToHide.Length == objectsToHide.Length)) return indicesToHide;

                indicesToHide = new int[objectsToHide.Length];
                for(int a = 0; a < objectsToHide.Length; a++)
                {
                    indicesToHide[a] = -1;
                    string objName = objectsToHide[a];
                    if (string.IsNullOrWhiteSpace(objName)) continue;

                    for (int b = 0; b < hidableObjects.Length; b++)
                    {
                        var go = hidableObjects[b];
                        if (go == null || go.name != objName) continue;

                        indicesToHide[a] = b;
                        break;
                    }
                }

                return indicesToHide;
            }
            public int[] GetIndicesToHide(IList<GameObject> hidableObjects)
            {
                if (objectsToHide == null || (indicesToHide != null && indicesToHide.Length == objectsToHide.Length)) return indicesToHide;

                indicesToHide = new int[objectsToHide.Length];
                for (int a = 0; a < objectsToHide.Length; a++)
                {
                    indicesToHide[a] = -1;
                    string objName = objectsToHide[a];
                    if (string.IsNullOrWhiteSpace(objName)) continue;

                    for (int b = 0; b < hidableObjects.Count; b++)
                    {
                        var go = hidableObjects[b];
                        if (go == null || go.name != objName) continue;

                        indicesToHide[a] = b;
                        break;
                    }
                }

                return indicesToHide;
            }

            public bool ShouldHide(float rippage, bool rippedAway, Dictionary<string, float> rippageValues)
            {
                if (rippedAway) return invertRequirements;

                bool flag = true;
                if (rippageRequirements != null && rippageRequirements.Length > 0)
                {
                    flag = false;
                    foreach (var requirement in rippageRequirements)
                    {
                        bool satisfied = requirement.IsSatisfied(rippage, rippageValues);
                        if (!satisfied && requirement.mandatory)
                        {
                            flag = false;
                            break;
                        }

                        flag = flag || satisfied;
                    }
                }

                return invertRequirements ? !flag : flag;
            }
        }

        [Serializable]
        public class MuscleGroupRipIdentifier
        {
            public string muscleGroupName;
            public string rippageMaterialProperty;

            [NonSerialized]
            private int cachedMuscleGroupIndex;
            public int GetMuscleGroupIndex(MuscularRenderedCharacter character)
            {
                if (cachedMuscleGroupIndex > 0) return cachedMuscleGroupIndex - 1;

                cachedMuscleGroupIndex = character.GetMuscleGroupIndex(muscleGroupName) + 1;
                return cachedMuscleGroupIndex - 1;
            }
        }
        public class MuscleGroupRippageTarget
        {
            public string muscleGroupName;
            public string rippageMaterialProperty;
            public float rippageValue;
        }
        [Serializable]
        public class MuscleMassRipProperties
        {
            public string name;
            public MuscleGroupRipIdentifier[] muscleGroups;
            private Dictionary<string, int> muscleGroupsIndexCache;
            public int GetIdentifierIndexFor(string muscleGroupName)
            { 
                if (muscleGroupsIndexCache == null)
                {
                    muscleGroupsIndexCache = new Dictionary<string, int>();
                    if (muscleGroups != null)
                    {
                        for (int a = 0; a < muscleGroups.Length; a++)
                        {
                            var mg = muscleGroups[a];
                            if (!string.IsNullOrWhiteSpace(mg.muscleGroupName)) muscleGroupsIndexCache[mg.muscleGroupName] = a;
                        }
                    }
                }

                if (muscleGroupsIndexCache.TryGetValue(muscleGroupName, out int index)) return index;
                return -1;
            }

            public Vector2 muscleMassRange;
            [Tooltip("The minimum and maximum rip speed mapped to muscle mass range.")]
            public Vector2 ripSpeedRange;
            [Tooltip("The maximum amount of rippage that can be reached.")]
            public float maxRippage;

            [NonSerialized]
            private int[] muscleIndices;
            public int[] GetMuscleIndices(MuscularRenderedCharacter character)
            {
                if (muscleIndices == null) 
                {
                    if (muscleGroups == null) return null;

                    muscleIndices = new int[muscleGroups.Length];
                    for (int a = 0; a < muscleGroups.Length; a++) muscleIndices[a] = muscleGroups[a].GetMuscleGroupIndex(character);
                }

                return muscleIndices;
            }

            [NonSerialized]
            private float[] rippages;
            [NonSerialized]
            private float rippageSum;
            public void RecalculateRippageSum()
            {
                rippageSum = 0;
                if (rippages != null) for (int a = 0; a < rippages.Length; a++) rippageSum += rippages[a];
            }
            public float AverageRippage
            {
                get
                {
                    //Debug.Log(name + " : " + rippageSum + " / " + muscleGroups.Length);
                    if (muscleGroups != null && muscleGroups.Length > 0) return rippageSum / muscleGroups.Length;
                    return rippageSum;
                }
            }
            public int RippageCount => rippages == null ? 0 : rippages.Length;
            public float GetRippage(int index) => index < 0 || rippages == null || index >= rippages.Length ? 0 : GetRippageUnsafe(index);
            public float GetRippageUnsafe(int index) => rippages[index];
            public void SetRippage(int index, float rippage)
            {
                if (index < 0 || rippages == null || index >= rippages.Length) return;
                SetRippageUnsafe(index, rippage);
            }
            public void SetRippageUnsafe(int index, float rippage)
            {
                float currentRippage = rippages[index];
                rippageSum += (rippage - currentRippage);
                rippages[index] = rippage;
            }
            
            public void Reset(bool forceCreateArray = false)
            {
                rippageSum = 0;

                if (rippages == null && forceCreateArray) 
                { 
                    rippages = muscleGroups == null ? new float[0] : new float[muscleGroups.Length]; 
                }
                else if (rippages != null) 
                { 
                    for (int a = 0; a < rippages.Length; a++) rippages[a] = 0; 
                }
            }

            public void ForceSetRippages(float[] rippages) 
            { 
                this.rippages = rippages;
                RecalculateRippageSum();
            }

            public float[] GetRippages(MuscularRenderedCharacter character, float deltaTime, out bool changed)
            {
                changed = false;
                if (rippages == null) 
                {
                    if (muscleGroups == null) return null;
                    rippages = new float[muscleGroups.Length];
                }

                float massRangeSize = muscleMassRange.y - muscleMassRange.x;
                var indices = GetMuscleIndices(character);
                rippageSum = 0;
                for (int a = 0; a < indices.Length; a++) 
                {
                    int ind = indices[a];
                    float flex = character.GetMuscleGroupFlexUnsafe(ind);
                    var mass = character.GetMuscleGroupMassUnsafe(ind) * (1 + (flex * 0.15f));
                    if (mass < muscleMassRange.x) continue;

                    float massT = Mathf.Clamp01((mass - muscleMassRange.x) / massRangeSize);
                    float prevRippage = rippages[a];
                    float rippage = Mathf.Min(maxRippage, prevRippage + Mathf.LerpUnclamped(ripSpeedRange.x, ripSpeedRange.y, massT) * deltaTime * (1 + flex * 2));
                    rippageSum += rippage;
                    rippages[a] = rippage;
                    changed = changed || prevRippage != rippage;
                }

                return rippages;
            }
            public float[] GetRippagesAndSetExternalRippageValues(MuscularRenderedCharacter character, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                float[] rippages = GetRippages(character, deltaTime, out changed);
                SetExternalRippageValues(rippages, setRippage);

                return rippages;
            }
            public void SetExternalRippageValues(SetExternalRippageValueDelegate setRippage) => SetExternalRippageValues(rippages, setRippage);
            public void SetExternalRippageValues(float[] rippages, SetExternalRippageValueDelegate setRippage)
            {
                if (setRippage != null && rippages != null)
                {
                    for (int a = 0; a < rippages.Length; a++)
                    {
                        var muscleGroup = muscleGroups[a];
                        var muscleName = muscleGroup.muscleGroupName;
                        var materialProperty = muscleGroup.rippageMaterialProperty;

                        var rippage = rippages[a];

                        setRippage(muscleName, rippage);
                        setRippage(materialProperty, rippage);
                    }
                }
            }
            public float GetMaxRippageAndSetExternalRippageValues(MuscularRenderedCharacter character, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                float[] rippages = GetRippagesAndSetExternalRippageValues(character, setRippage, deltaTime, out changed);

                float rippageMax = 0;
                foreach (var rippage in rippages) rippageMax = Mathf.Max(rippageMax, rippage);

                return rippageMax;
            }
            public float GetMaxRippage(MuscularRenderedCharacter character, float deltaTime, out bool changed)
            {
                float[] rippages = GetRippages(character, deltaTime, out changed);

                float rippageMax = 0;
                foreach (var rippage in rippages) rippageMax = Mathf.Max(rippageMax, rippage);

                return rippageMax;
            }

            public bool GetTargetRippages(MuscularRenderedCharacter character, ICollection<MuscleGroupRippageTarget> targetMuscles, float deltaTime)
            {
                bool changed = false;
                if (rippages == null)
                {
                    if (muscleGroups == null) return false;
                    rippages = new float[muscleGroups.Length];
                }

                float massRangeSize = muscleMassRange.y - muscleMassRange.x;

                foreach(var target in targetMuscles)
                {
                    int localIndex = GetIdentifierIndexFor(target.muscleGroupName);
                    if (localIndex < 0) continue;

                    var muscleGroup = muscleGroups[localIndex];
                    int ind = muscleGroup.GetMuscleGroupIndex(character);
                    float flex = character.GetMuscleGroupFlexUnsafe(ind);
                    var mass = character.GetMuscleGroupMassUnsafe(ind) * (1 + (flex * 0.15f));  
                    if (mass < muscleMassRange.x) continue;

                    float massT = Mathf.Clamp01((mass - muscleMassRange.x) / massRangeSize);
                    float prevRippage = rippages[localIndex];
                    float rippage = Mathf.Min(maxRippage, prevRippage + Mathf.LerpUnclamped(ripSpeedRange.x, ripSpeedRange.y, massT) * deltaTime * (1 + flex * 2));
                    rippages[localIndex] = target.rippageValue = rippage;
                    rippageSum = rippageSum + (rippage - prevRippage);
                    changed = changed || prevRippage != rippage;
                }

                return changed;
            }
            public bool GetTargetRippagesAndSetExternalRippageValues(MuscularRenderedCharacter character, ICollection<MuscleGroupRippageTarget> targetMuscles, SetExternalRippageValueDelegate setRippage, float deltaTime)
            {
                bool changed = GetTargetRippages(character, targetMuscles, deltaTime);
                SetTargetExternalRippageValues(targetMuscles, setRippage);

                return changed;
            }
            public void SetTargetExternalRippageValues(ICollection<MuscleGroupRippageTarget> targetMuscles, SetExternalRippageValueDelegate setRippage)
            {
                if (setRippage != null && targetMuscles != null)
                {
                    foreach (var target in targetMuscles)
                    {
                        int localIndex = GetIdentifierIndexFor(target.muscleGroupName); 
                        if (localIndex < 0) continue;

                        var muscleGroup = muscleGroups[localIndex];
                        var muscleName = muscleGroup.muscleGroupName;
                        var materialProperty = muscleGroup.rippageMaterialProperty; 
                        setRippage(muscleName, target.rippageValue);
                        setRippage(materialProperty, target.rippageValue);
                    }
                }
            }
            public float GetMaxRippageAndSetExternalRippageValues(MuscularRenderedCharacter character, ICollection<MuscleGroupRippageTarget> targetMuscles, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                changed = GetTargetRippagesAndSetExternalRippageValues(character, targetMuscles, setRippage, deltaTime);

                float rippageMax = 0;
                foreach (var rippage in rippages) rippageMax = Mathf.Max(rippageMax, rippage);

                return rippageMax;
            }
            public float GetMaxRippage(MuscularRenderedCharacter character, ICollection<MuscleGroupRippageTarget> targetMuscles, float deltaTime, out bool changed)
            {
                changed = GetTargetRippages(character, targetMuscles, deltaTime);

                float rippageMax = 0;
                foreach (var rippage in rippages) rippageMax = Mathf.Max(rippageMax, rippage);

                return rippageMax;
            }
        }

        [Serializable]
        public struct TriggerableSettings
        {
            public Triggerable triggerable;
            public int triggerIndex;
        }
        [Serializable]
        public class TriggerProperties
        {
            public string name;
            public TriggerableSettings[] triggerables;
            public RippageRequirement[] rippageRequirements;

            public bool playOnce;
            public float cooldown;
            public float cooldownRandomAdd;
            private float nextCooldown;

            public float minRippageDelta;

            private Vector2 lastTriggeredRange;
            private float lastTime;

            private float lastRippage;

            public void Reset()
            {
                lastTriggeredRange = Vector2.zero;
                lastTime = 0;
                lastRippage = 0;
            }

            public bool TryToTrigger(float rippage, Dictionary<string, float> rippageValues, float currentTime)
            {
                if (lastTime > 0 && (currentTime - lastTime < nextCooldown || (playOnce && rippage >= lastTriggeredRange.x && rippage <= lastTriggeredRange.y))) return false;
                 
                float satisfiedRippage = 0;
                if (rippageRequirements != null && rippageRequirements.Length > 0)
                {
                    bool flag = false;
                    foreach (var requirement in rippageRequirements)
                    {
                        bool satisfied = requirement.IsSatisfied(rippage, rippageValues, lastTriggeredRange, out var satisfiedRange, out var satisfiedRippage_);
                        if (satisfiedRippage_ - lastRippage < minRippageDelta) satisfied = false;
                        if (!satisfied && requirement.mandatory)
                        {
                            flag = false;
                            break;
                        }

                        if (satisfied)
                        {
                            satisfiedRippage = satisfiedRippage_;  
                            if (flag)
                            {
                                lastTriggeredRange = new Vector2(Mathf.Min(satisfiedRange.x, lastTriggeredRange.x), Mathf.Max(satisfiedRange.y, lastTriggeredRange.y)); 
                            }
                            else
                            {
                                lastTriggeredRange = satisfiedRange;
                            }
                        }

                        flag = flag || satisfied;
                    }

                    if (!flag) return false;
                }

                if (triggerables != null)
                {
                    foreach (var triggerSettings in triggerables) if (triggerSettings.triggerable != null) triggerSettings.triggerable.Trigger(triggerSettings.triggerIndex); 
                }

                lastRippage = satisfiedRippage;
                lastTime = currentTime;
                nextCooldown = cooldown + UnityEngine.Random.value * cooldownRandomAdd;
                return true;
            }
        }

        [Serializable]
        public class Rippable
        {
            public string name;

            public float averageRippageToRipAway = 1;

            public MuscleMassRipProperties[] rippageProperties;

            [NonSerialized]
            private string[] affectedMaterialProperties;
            private static readonly HashSet<string> _affectedMaterialProperties = new HashSet<string>();

            public string[] AffectedMaterialProperties
            {
                get
                {
                    if (affectedMaterialProperties != null) return affectedMaterialProperties;

                    _affectedMaterialProperties.Clear();
                    if (rippageProperties != null)
                    {
                        foreach(var prop in rippageProperties)
                        {
                            if (prop.muscleGroups == null) continue;

                            foreach (var group in prop.muscleGroups) if (!string.IsNullOrWhiteSpace(group.rippageMaterialProperty)) _affectedMaterialProperties.Add(group.rippageMaterialProperty);
                        }
                    }
                    affectedMaterialProperties = new string[_affectedMaterialProperties.Count];
                    _affectedMaterialProperties.CopyTo(affectedMaterialProperties, 0);
                    _affectedMaterialProperties.Clear();

                    return affectedMaterialProperties;
                }
            }

            public MaskEdit[] masksEdits; 

            public ObjectHideProperties[] objectsToHide;

            public TriggerProperties[] triggers;

            public TriggerableSettings[] rippedAwayTriggers;
            public TriggerableSettings[] restorationTriggers;

            [NonSerialized]
            public float rippage;

            public float AverageRippage
            {
                get
                {
                    if (rippageProperties == null) return rippage;

                    float average = 0;
                    foreach (var prop in rippageProperties) average = Mathf.Max(average, prop.AverageRippage);

                    return average;
                }
            }

            private float prevRippage;
            private float prevRippageSum;
            private bool prevRippedAway;

            public void Reset()
            {
                if (triggers != null)
                {
                    foreach (var trigger in triggers) trigger.Reset();
                }
                if (rippageProperties != null)
                {
                    foreach(var prop in rippageProperties) prop.Reset();
                }
                
                isRippedAway = false;
                SetRippage(0);
            }

            public void UpdateRippageValues(MuscularRenderedCharacter character, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                cachedRippageTargets.Clear();
                changed = false;

                float maxRippage = 0;
                if (rippageProperties != null)
                {
                    foreach (var prop in rippageProperties)
                    {
                        maxRippage = Mathf.Max(maxRippage, prop.GetMaxRippageAndSetExternalRippageValues(character, setRippage, deltaTime, out bool changed_));
                        changed = changed || changed_; 
                    }
                }

                if (rippage != maxRippage) SetRippage(maxRippage); else if (changed) MarkAsDirty(); 
            }

            private readonly List<MuscleGroupRippageTarget> cachedRippageTargets = new List<MuscleGroupRippageTarget>();
            public void UpdateTargetRippageValues(MuscularRenderedCharacter character, ICollection<MuscleGroupRippageTarget> rippageTargets, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                cachedRippageTargets.Clear();
                foreach (var target in rippageTargets) cachedRippageTargets.Add(target);

                UpdateTargetRippageValuesNoCache(character, rippageTargets, setRippage, deltaTime, out changed);
            }
            public void UpdateTargetRippageValuesFromCache(HashSet<string> targetMusclesOutput, MuscularRenderedCharacter character, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                if (cachedRippageTargets == null || cachedRippageTargets.Count <= 0)
                {
                    UpdateRippageValues(character, setRippage, deltaTime, out changed);
                    return;
                }

                if (targetMusclesOutput != null)
                {
                    foreach (var target in cachedRippageTargets) targetMusclesOutput.Add(target.muscleGroupName);
                }
                UpdateTargetRippageValuesNoCache(character, cachedRippageTargets, setRippage, deltaTime, out changed);
            }
            public void UpdateTargetRippageValuesNoCache(MuscularRenderedCharacter character, ICollection<MuscleGroupRippageTarget> rippageTargets, SetExternalRippageValueDelegate setRippage, float deltaTime, out bool changed)
            {
                changed = false;

                float maxRippage = rippage;
                if (rippageProperties != null)
                {
                    foreach (var prop in rippageProperties)
                    {
                        maxRippage = Mathf.Max(maxRippage, prop.GetMaxRippageAndSetExternalRippageValues(character, rippageTargets, setRippage, deltaTime, out bool changed_));
                        changed = changed || changed_;
                    }
                }

                if (rippage != maxRippage) SetRippage(maxRippage); else if (changed) MarkAsDirty();
            }

            private bool isDirty;
            public bool IsDirty => isDirty;
            [NonSerialized]
            public Clothing clothingInstance;
            public void MarkAsDirty()
            {
                isDirty = true;
                if (clothingInstance != null) clothingInstance.MarkAsDirty();
            }
            private bool isRippedAway;
            public bool IsRippedAway => isRippedAway;
            public void SetRippage(float rippage)
            {
                if (!isDirty)
                { 
                    prevRippage = this.rippage;
                    MarkAsDirty();
                }

                this.rippage = rippage;
            }
            public void HideObjects(IList<GameObject> objects, HashSet<int> hiddenIndices, float rippage, bool rippedAway, Dictionary<string, float> rippageValues)
            {
                if (objectsToHide != null && objects != null)
                {                 
                    foreach (var hideProps in objectsToHide)
                    {
                        if (!hideProps.ShouldHide(rippage, rippedAway, rippageValues)) continue; 

                        var indicesToHide = hideProps.GetIndicesToHide(objects);
                        if (indicesToHide == null) continue;

                        foreach (var index in indicesToHide)
                        {
                            if (index < 0) continue;

                            var go = objects[index];
                            if (go == null) continue;

                            hiddenIndices?.Add(index);
                            if (go.activeSelf) go.SetActive(false);
                        }
                    }
                }
            }
            public void UnhideObjects(IList<GameObject> objects)
            {
                if (objectsToHide != null && objects != null)
                {
                    foreach (var hideProps in objectsToHide)
                    {
                        var indicesToHide = hideProps.GetIndicesToHide(objects);
                        if (indicesToHide == null) continue;

                        foreach (var index in indicesToHide)
                        {
                            if (index < 0) continue;

                            var go = objects[index];
                            if (go == null) continue;

                            if (!go.activeSelf) go.SetActive(true);
                        }
                    }
                }
            }

            public bool ApplyChanges(bool isFullyRippedAway, IList<GameObject> hidableObjects, HashSet<int> hiddenIndices, Dictionary<string, float> rippageValues, ICollection<Material> materials, ToonMaterialMaskController maskController, bool forceUpdate = false)
            {
                bool rippedAway = isFullyRippedAway || IsRippedAway;
                if (!rippedAway)
                {
                    if (AverageRippage >= averageRippageToRipAway)
                    {
                        prevRippedAway = false;
                        isRippedAway = rippedAway = true;
                        isDirty = true;
                    }
                    else if (prevRippedAway)
                    {
                        prevRippedAway = false;
                        if (restorationTriggers != null)
                        {
                            foreach (var trigger in restorationTriggers) if (trigger.triggerable != null) trigger.triggerable.Trigger(trigger.triggerIndex);
                        }
                    }
                }        
                if (rippedAway && !prevRippedAway)
                {
                    prevRippedAway = true;
                    isDirty = true;
                    if (rippedAwayTriggers != null)
                    {
                        foreach (var trigger in rippedAwayTriggers) if (trigger.triggerable != null) trigger.triggerable.Trigger(trigger.triggerIndex); 
                    }
                }

                try
                {
                    HideObjects(hidableObjects, hiddenIndices, rippage, rippedAway, rippageValues);
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                if (!isDirty && !forceUpdate) return false;
                isDirty = false;

                try
                {
                    if (masksEdits != null)
                    {
                        foreach (var mask in masksEdits) mask.Refresh(maskController, rippage, rippedAway, rippageValues); 
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                try
                {
                    if (materials != null && rippageValues != null)
                    {
                        var matProps = AffectedMaterialProperties;
                        foreach (var prop in matProps)
                        {
                            if (rippageValues.TryGetValue(prop, out var rippage))
                            {
                                foreach (var mat in materials)
                                {
                                    if (!mat.HasProperty(prop)) continue; 
                                    mat.SetFloat(prop, rippage);
                                }
                            }
                            else
                            {
                                foreach (var mat in materials)
                                {
                                    if (!mat.HasProperty(prop)) continue;
                                    mat.SetFloat(prop, 0);
                                }
                            }
                        }
                    }
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex);
                }

                try
                {
                    if (!rippedAway && triggers != null)
                    {
                        float currentTime = Time.time;
                        foreach (var trigger in triggers)
                        {
                            trigger.TryToTrigger(rippage, rippageValues, currentTime); 
                        }
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                prevRippage = rippage;
                return true;
            }
            public void ApplyTargetChanges(bool isFullyRippedAway, ICollection<MuscleGroupRippageTarget> rippageTargets, IList<GameObject> hidableObjects, Dictionary<string, float> rippageValues, ICollection<Material> materials, ToonMaterialMaskController maskController)
            {
                bool rippedAway = isFullyRippedAway || IsRippedAway; 
                if (!rippedAway)
                {
                    if (AverageRippage >= averageRippageToRipAway)
                    {
                        prevRippedAway = false;
                        isRippedAway = rippedAway = true;
                    }
                    else if (prevRippedAway)
                    {
                        prevRippedAway = false;
                        if (restorationTriggers != null)
                        {
                            foreach (var trigger in restorationTriggers) if (trigger.triggerable != null) trigger.triggerable.Trigger(trigger.triggerIndex);
                        }
                    }
                }
                if (rippedAway && !prevRippedAway)
                {
                    prevRippedAway = true;
                    if (rippedAwayTriggers != null)
                    {
                        foreach (var trigger in rippedAwayTriggers) if (trigger.triggerable != null) trigger.triggerable.Trigger(trigger.triggerIndex);
                    }
                }

                try
                {
                    HideObjects(hidableObjects, null, rippage, rippedAway, rippageValues);
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                try
                {
                    if (masksEdits != null)
                    {
                        foreach (var mask in masksEdits) mask.Refresh(maskController, rippage, rippedAway, rippageValues);
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                try
                {
                    if (materials != null && rippageValues != null)
                    {
                        foreach (var target in rippageTargets)
                        {
                            if (rippageValues.TryGetValue(target.rippageMaterialProperty, out var rippage))
                            {
                                foreach (var mat in materials)
                                {
                                    if (!mat.HasProperty(target.rippageMaterialProperty)) continue;
                                    mat.SetFloat(target.rippageMaterialProperty, rippage);  
                                }
                            } 
                            else
                            {
                                foreach (var mat in materials)
                                {
                                    if (!mat.HasProperty(target.rippageMaterialProperty)) continue;
                                    mat.SetFloat(target.rippageMaterialProperty, 0);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    swole.LogError(ex);
                }

                try
                {
                    if (!rippedAway && triggers != null)
                    {
                        float currentTime = Time.time;
                        foreach(var trigger in triggers)
                        {
                            trigger.TryToTrigger(rippage, rippageValues, currentTime);   
                        }
                    }
                } 
                catch(Exception ex)
                {
                    swole.LogError(ex);
                }
            }
        }

        [Serializable]
        public class Clothing
        {
            public string name;

            public bool disable;
            public bool isActive;

            [NonSerialized, Tooltip("Determines if the clothing has been activated before.")]
            private bool hasActivated;
            
            [Tooltip("Will use default if left blank.")]
            public ToonMaterialMaskController maskController;

            public MeshRenderer[] renderers;
            public MaskNameRendererIndexMaterialSlotAndProperty[] maskDestinationsRenderers;

            public SkinnedMeshRenderer[] skinnedRenderers;
            public MaskNameRendererIndexMaterialSlotAndProperty[] maskDestinationsSkinnedRenderers;

            public CustomSkinnedMeshRenderer[] customSkinnedRenderers;
            public MaskNameRendererIndexMaterialSlotAndProperty[] maskDestinationsCustomSkinnedRenderers;

            public GameObject[] hideableObjects;
            [NonSerialized]
            private List<GameObject> objects;
            private readonly HashSet<int> hiddenIndices = new HashSet<int>();
            public List<GameObject> GetObjects(bool refresh = false)
            {
                if (objects == null)
                {
                    objects = new List<GameObject>();
                    refresh = true;
                }

                if (refresh)
                {
                    if (hideableObjects != null)
                    {
                        foreach(var obj in hideableObjects)
                        {
                            if (obj == null) continue;
                            objects.Add(obj);
                        }
                    }
                    if (renderers != null)
                    {
                        foreach (var renderer in renderers)
                        {
                            if (renderer == null) continue;
                            objects.Add(renderer.gameObject);
                        }
                    }
                    if (skinnedRenderers != null)
                    {
                        foreach (var renderer in skinnedRenderers)
                        {
                            if (renderer == null) continue;
                            objects.Add(renderer.gameObject);
                        }
                    }
                    if (customSkinnedRenderers != null)
                    {
                        foreach (var renderer in customSkinnedRenderers)
                        {
                            if (renderer == null) continue;
                            objects.Add(renderer.gameObject);
                        }
                    }
                }

                return objects;
            }
            [NonSerialized]
            private List<Material> materials;
            public List<Material> GetMaterials(bool refresh = false)
            {
                if (materials == null) 
                { 
                    materials = new List<Material>();
                    refresh = true; 
                }

                if (refresh)
                {
                    if (renderers != null)
                    {
                        foreach(var renderer in renderers)
                        {
                            if (renderer == null) continue;
                            renderer.GetSharedMaterials(materials);
                        }
                    }
                    if (skinnedRenderers != null)
                    {
                        foreach (var renderer in skinnedRenderers)
                        {
                            if (renderer == null) continue;
                            renderer.GetSharedMaterials(materials);
                        }
                    }
                    if (customSkinnedRenderers != null)
                    {
                        foreach (var renderer in customSkinnedRenderers)
                        {
                            if (renderer == null) continue;
                            renderer.meshRenderer.GetSharedMaterials(materials);
                        }
                    }
                }

                return materials;
            }

            public MeshRenderer[] renderersToDisable;
            public SkinnedMeshRenderer[] skinnedRenderersToDisable;
            public CustomSkinnedMeshRenderer[] customSkinnedRenderersToDisable;

            public void Activate(ToonMaterialMaskController maskController)
            {
                if (disable) return;

                isActive = true;
                if (!hasActivated)
                {
                    hasActivated = true;
                    if (rippables != null)
                    {
                        foreach (var rippable in rippables) rippable.MarkAsDirty(); 
                    }
                    MarkAsDirty();
                }

                if (renderersToDisable != null) foreach(var renderer in renderersToDisable) if (renderer != null) renderer.gameObject.SetActive(false);
                if (skinnedRenderersToDisable != null) foreach (var renderer in skinnedRenderersToDisable) if (renderer != null) renderer.gameObject.SetActive(false);
                if (customSkinnedRenderersToDisable != null) foreach (var renderer in customSkinnedRenderersToDisable) if (renderer != null) renderer.gameObject.SetActive(false);

                if (renderers != null)
                {
                    foreach (var renderer in renderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(true);
                            renderer.enabled = true;
                        }

                    if (maskDestinationsRenderers != null && maskController != null)
                    {
                        foreach(var dest in maskDestinationsRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= renderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = renderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, maskController.GetMask(dest.name)); 
                        }
                    }
                }
                if (skinnedRenderers != null)
                {
                    foreach (var renderer in skinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(true);
                            renderer.enabled = true;
                        }

                    if (maskDestinationsSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= skinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = skinnedRenderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, maskController.GetMask(dest.name));
                        }
                    }
                }
                if (customSkinnedRenderers != null)
                {
                    foreach (var renderer in customSkinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(true);
                            renderer.enabled = true;
                        }

                    if (maskDestinationsCustomSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsCustomSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= customSkinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = customSkinnedRenderers[dest.rendererIndex];
                            if (renderer == null || renderer.meshRenderer == null) continue;

                            var mats = renderer.meshRenderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, maskController.GetMask(dest.name));
                        }
                    }
                }

                if (rippables != null)
                {
                    hiddenIndices.Clear();
                    var mats = GetMaterials();
                    var objs = GetObjects();
                    foreach (var rippable in rippables)
                    {
                        rippable.ApplyChanges(isRippedAway, objs, hiddenIndices, rippageValues, mats, maskController, true);
                    }

                    for (int a = 0; a < objs.Count; a++)
                    {
                        var obj = objs[a];
                        if (obj == null || obj.activeSelf || hiddenIndices.Contains(a)) continue;

                        obj.SetActive(true);
                    }

                }

                if (isRippedAway)
                {
                    TriggerRippedAwayTriggers();
                }
            }

            public void Deactivate(ToonMaterialMaskController maskController)
            {
                if (disable && !isActive) return;

                isActive = false;

                if (renderers != null)
                {
                    foreach (var renderer in renderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }

                    if (maskDestinationsRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= renderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = renderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, null);
                        }
                    }
                }
                if (skinnedRenderers != null)
                {
                    foreach (var renderer in skinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }

                    if (maskDestinationsSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= skinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = skinnedRenderers[dest.rendererIndex];
                            if (renderer == null) continue;

                            var mats = renderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, null);
                        }
                    }
                }
                if (customSkinnedRenderers != null)
                {
                    foreach (var renderer in customSkinnedRenderers) if (renderer != null)
                        {
                            renderer.gameObject.SetActive(false);
                        }

                    if (maskDestinationsCustomSkinnedRenderers != null && maskController != null)
                    {
                        foreach (var dest in maskDestinationsCustomSkinnedRenderers)
                        {
                            if (dest.rendererIndex < 0 || dest.rendererIndex >= customSkinnedRenderers.Length || dest.slotProp.slot < 0) continue;
                            var renderer = customSkinnedRenderers[dest.rendererIndex];
                            if (renderer == null || renderer.meshRenderer == null) continue;

                            var mats = renderer.meshRenderer.sharedMaterials;
                            ToonMaterialMaskController.SetMaterialMaskProperties(dest.slotProp, mats, null);   
                        }
                    }
                }

                if (rippables != null)
                {
                    foreach (var rippable in rippables) if (rippable.IsRippedAway) rippable.UnhideObjects(GetObjects()); 
                }
            }

            public void Reset()
            {
                isRippedAway = false;
                rippageValues.Clear();
                rippageTargets.Clear();

                if (rippables != null)
                {
                    foreach (var rippable in rippables) rippable.Reset();
                }
                
                TriggerRestorationTriggers();
                MarkAsDirty();
            }

            public TriggerableSettings[] rippedAwayTriggers;
            public void TriggerRippedAwayTriggers()
            {
                if (rippedAwayTriggers != null)
                {
                    foreach (var trigger in rippedAwayTriggers) if (trigger.triggerable != null) trigger.triggerable.Trigger(trigger.triggerIndex);
                }
            }
            public TriggerableSettings[] restorationTriggers;
            public void TriggerRestorationTriggers()
            {
                if (restorationTriggers != null)
                {
                    foreach (var trigger in restorationTriggers) if (trigger.triggerable != null) trigger.triggerable.Trigger(trigger.triggerIndex);
                }
            }

            private bool isRippedAway;
            public bool IsRippedAway => isRippedAway; 

            [Serializable]
            public struct RipAwayRippableIndex
            {
                public int index;
                public bool required;
            }

            [Tooltip("Rippable indices that must be ripped away for the clothing to fully rip away.")]
            public RipAwayRippableIndex[] fullRipAwayIndices;

            public Rippable[] rippables;
            private readonly Dictionary<string, MuscleGroupRippageTarget> rippageTargets = new Dictionary<string, MuscleGroupRippageTarget>();
            public void FillRippageTargets(ICollection<string> muscleGroups, IList<MuscleGroupRippageTarget> targets)
            {
                foreach(var mg in muscleGroups)
                {
                    if (!rippageTargets.TryGetValue(mg, out var target))
                    {
                        target = new MuscleGroupRippageTarget() { muscleGroupName = mg, rippageMaterialProperty = $"{_rippageMaterialPropertyPrefix}{mg}", rippageValue = 0 };
                        rippageTargets[mg] = target;
                    }

                    targets.Add(target); 
                }
            }
            private readonly Dictionary<string, float> rippageValues = new Dictionary<string, float>();
            public bool TryGetRippageValue(string key, out float value) => rippageValues.TryGetValue(key, out value);
            public void SetRippageValue(string key, float value) => rippageValues[key] = value;
            public void SetRippageValueIfGreater(string key, float value)
            {
                if (rippageValues.TryGetValue(key, out var currentVal))
                {
                    rippageValues[key] = Mathf.Max(value, currentVal);
                } 
                else
                {
                    rippageValues[key] = value;
                }
            }
            public void SetRippageValueIfSmaller(string key, float value)
            {
                if (rippageValues.TryGetValue(key, out var currentVal))
                {
                    rippageValues[key] = Mathf.Min(value, currentVal);
                }
                else
                {
                    rippageValues[key] = value;
                }
            }

            public void UpdateVisualRipping() => UpdateVisualRipping(rippables);
            public void UpdateVisualRipping(ICollection<Rippable> rippables)
            {
                if (rippables == null) return;

                bool rippedAway = IsRippedAway; 

                var objs = GetObjects();
                var mats = GetMaterials();
                hiddenIndices.Clear();
                bool updated = false;
                foreach (var rippable in rippables) 
                {
                    if (rippable.ApplyChanges(rippedAway, objs, hiddenIndices, rippageValues, mats, maskController)) updated = true; 
                }

                if (updated) // only refresh object visibility if something changed
                {
                    for (int a = 0; a < objs.Count; a++)
                    {
                        var obj = objs[a];
                        if (obj == null || obj.activeSelf || hiddenIndices.Contains(a)) continue;

                        obj.SetActive(true);
                    }
                }
            }
            public void UpdateTargetVisualRipping(ICollection<Rippable> rippables, ICollection<MuscleGroupRippageTarget> rippageTargets)
            {
                if (rippables == null) return;

                var objs = GetObjects();
                var mats = GetMaterials();
                foreach (var rippable in rippables)
                {
                    rippable.ApplyTargetChanges(IsRippedAway, rippageTargets, objs, rippageValues, mats, maskController);
                }
            }

            private static readonly List<float> tempRippages = new List<float>();
            public string RipState
            {
                get
                {
                    if (rippables == null) return string.Empty;

                    string state = string.Empty;
                    for (int a = 0; a < rippables.Length; a++) 
                    {
                        var rippable = rippables[a];
                        if (rippable.rippageProperties == null) continue;

                        int propsCount = rippable.rippageProperties.Length;
                        if (propsCount <= 0) continue;

                        state = state + "{" + a + ":";
                        for(int b = 0; b < propsCount; b++)
                        {
                            var props = rippable.rippageProperties[b];
                            if (props == null) continue;

                            var rippageCount = props.RippageCount;
                            if (rippageCount <= 0) continue;

                            state = state + $"[{b}:";
                            for (int c = 0; c < rippageCount; c++) state = state + $"{props.GetRippageUnsafe(c)}" + (c < rippageCount - 1 ? "|" : "");
                            state = state + "]";
                        }
                        state = state + "}";
                    }

                    return state;
                }
                set
                {
                    if (rippables == null) return;
                    Reset();

                    string state = value;
                    while(true)
                    {
                        var startIndex = state.IndexOf("{");
                        if (startIndex < 0) break;
                        startIndex = startIndex + 1;

                        var endIndex = state.IndexOf("}");
                        if (endIndex < 0) break;

                        var inner1 = state.Substring(startIndex, endIndex - startIndex);
                        var colonIndex = inner1.IndexOf(":");
                        if (colonIndex > 0 && int.TryParse(inner1.Substring(0, colonIndex), out int rippableIndex))
                        {
                            if (rippableIndex >= 0 && rippableIndex < rippables.Length)
                            {
                                var rippable = rippables[rippableIndex];
                                if (rippable.rippageProperties != null)
                                {
                                    while (true)
                                    {
                                        var startIndex_ = inner1.IndexOf("[");
                                        var endIndex_ = inner1.IndexOf("]");
                                        if (startIndex_ >= 0 && endIndex_ > 0)
                                        {
                                            startIndex_ = startIndex_ + 1;
                                            var inner2 = inner1.Substring(startIndex_, endIndex_ - startIndex_); // propIndex:0.42|0.241|0.353
                                            colonIndex = inner2.IndexOf(":");
                                            if (colonIndex > 0 && colonIndex + 1 < inner2.Length && int.TryParse(inner2.Substring(0, colonIndex), out int propIndex))
                                            {
                                                if (propIndex >= 0 && propIndex < rippable.rippageProperties.Length)
                                                {
                                                    var prop = rippable.rippageProperties[propIndex];
                                                    prop.Reset(true);
                                                    var inner3 = inner2.Substring(colonIndex + 1);
                                                    var dividerIndex = inner3.IndexOf("|");
                                                    int i = 0;
                                                    while (dividerIndex >= 0 && i < prop.RippageCount)
                                                    {
                                                        if (dividerIndex > 0 && float.TryParse(inner3.Substring(0, dividerIndex), out float rippage))
                                                        {
                                                            prop.SetRippage(i, rippage);
                                                        }

                                                        i++;
                                                        if (dividerIndex + 1 < inner3.Length)
                                                        {
                                                            inner3 = inner3.Substring(dividerIndex + 1);
                                                            dividerIndex = inner3.IndexOf("|");
                                                        }
                                                        else break;
                                                    }

                                                    if (float.TryParse(inner3, out float finalRippage))
                                                    {
                                                        prop.SetRippage(i, finalRippage);
                                                    }
                                                }
                                            }

                                            if (endIndex_ + 1 < inner1.Length)
                                            {
                                                inner1 = inner1.Substring(endIndex_ + 1);
                                            }
                                            else break;
                                        }
                                        else break;
                                    }
                                }
                            }
                        }

                        if (endIndex + 1 >= state.Length) break;
                        state = state.Substring(endIndex + 1); 
                    }

                    ForceUpdate();
                }
            }

            [NonSerialized]
            private bool isDirty;
            public bool IsDirty => isDirty;
            [NonSerialized]
            public ToonClothingController controller;
            public void MarkAsDirty()
            {
                isDirty = true;

                if (controller != null) controller.MarkForUpdate();
            }
            public bool MarkAsDirty(ICollection<string> updatedMuscles)
            {
                if (isDirty) return true;

                if (rippables != null)
                {
                    foreach(var rippable in rippables)
                    {
                        if (rippable.rippageProperties == null) continue;

                        foreach(var prop in rippable.rippageProperties)
                        {
                            if (prop.muscleGroups == null) continue;

                            foreach(var group in prop.muscleGroups)
                            {
                                if (updatedMuscles.Contains(group.muscleGroupName)) 
                                {
                                    MarkAsDirty();
                                    return true; 
                                }
                            }
                        }
                    }
                }

                return false; 
            }

            public void UpdateIfDirty()
            {
                if (!isDirty) return;
                isDirty = false;

                ForceUpdate();
            }

            public void ForceUpdate()
            {
                TryRipAway();
                UpdateVisualRipping(); 
            }
            public void UpdateUsingTargetMuscles(ICollection<string> targetMuscles)
            {
                _tempTargets.Clear();
                FillRippageTargets(targetMuscles, _tempTargets);
                if (TryRipAway())
                {
                    ForceUpdate();
                    return;
                }

                UpdateTargetVisualRipping(rippables, _tempTargets);
            }
            protected bool TryRipAway()
            {
                if (!isRippedAway && fullRipAwayIndices != null)
                {
                    foreach(var ind in fullRipAwayIndices)
                    {
                        var rippable = rippables[ind.index];
                        if (ind.required)
                        {
                            if (!rippable.IsRippedAway)
                            {
                                isRippedAway = false;
                                break;
                            }
                        }

                        isRippedAway = isRippedAway || rippable.IsRippedAway; 
                    }

                    if (isRippedAway)
                    {
                        TriggerRippedAwayTriggers();

                        return true;
                    }
                }
                
                return false;
            }
        }

        public Clothing[] clothing;

        public int GetClothingIndex(string clothingName, bool caseSensitive = false)
        {
            if (clothing == null) return -1;

            if (!caseSensitive) clothingName = clothingName.AsID();

            if (caseSensitive)
            {
                for(int a = 0; a < clothing.Length; a++) if (clothing[a].name == clothingName) return a;
            } 
            else
            {
                for (int a = 0; a < clothing.Length; a++) if (clothing[a].name != null && clothing[a].name.AsID() == clothingName) return a;
            }

            return -1;
        }
        public Clothing GetClothing(string clothingName, bool caseSensitive = false)
        {
            int index = GetClothingIndex(clothingName, caseSensitive);
            if (index < 0) return null;

            return clothing[index];
        }

        public void ActivateClothing(string clothingName) => ActivateClothing(GetClothingIndex(clothingName));
        public void ActivateClothing(int clothingIndex)
        {
            if (clothing == null || clothingIndex < 0 || clothingIndex >= clothing.Length) return;
            clothing[clothingIndex].Activate(maskController);  
        }

        public void ActivateAndResetClothing(string clothingName) => ActivateAndResetClothing(GetClothingIndex(clothingName));
        public void ActivateAndResetClothing(int clothingIndex)
        {
            if (clothing == null || clothingIndex < 0 || clothingIndex >= clothing.Length) return;
            var cloth = clothing[clothingIndex];
            cloth.Reset();
            cloth.Activate(maskController);  
        }

        public void DeactivateClothing(string clothingName) => DeactivateClothing(GetClothingIndex(clothingName));
        public void DeactivateClothing(int clothingIndex)
        {
            if (clothing == null || clothingIndex < 0 || clothingIndex >= clothing.Length) return;
            clothing[clothingIndex].Deactivate(maskController);
        }

        public void ResetClothing(string clothingName) => ResetClothing(GetClothingIndex(clothingName));
        public void ResetClothing(int clothingIndex)
        {
            if (clothing == null || clothingIndex < 0 || clothingIndex >= clothing.Length) return;
            clothing[clothingIndex].Reset();
        }

        protected bool needsUpdate;
        public void MarkForUpdate()
        {
            needsUpdate = true;
        }

        //private static readonly HashSet<string> _tempUpdatedMuscles = new HashSet<string>();
        public void LateUpdateStep()
        {
            if (updatedMuscles.Count > 0)
            {
                if (updatedMuscles.Count > 24)
                {
                    UpdateAllRippageValues();
                }
                else
                {
                    UpdateRequeuedRippables(updatedMuscles);
                    UpdateRippageValuesPerUpdatedMuscle(updatedMuscles);
                }
            } 
            else
            {
                UpdateRequeuedRippables(updatedMuscles);
            }

            if (needsUpdate)
            {
                needsUpdate = false;
                UpdateClothes();
            } 
            else if (updatedMuscles.Count > 0)
            {
                UpdateClothesUsingTargetMuscles(updatedMuscles);
            }

            updatedMuscles.Clear();
        }

        public void UpdateClothes()
        {
            if (clothing == null) return;

            foreach (var cloth in clothing) if (cloth.isActive) cloth.UpdateIfDirty();
        }
        public void UpdateClothesUsingTargetMuscles(ICollection<string> targetMuscles)
        {
            if (clothing == null) return;
            
            foreach (var cloth in clothing) if (cloth.isActive) cloth.UpdateUsingTargetMuscles(targetMuscles); 
        }

    }
}

#endif