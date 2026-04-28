using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Swole.Morphing;
using Swole.Cloth;

namespace Swole.API
{
    public class CustomizableCharacterGarment : MonoBehaviour, ICustomUpdatableBehaviour 
    {

#if UNITY_EDITOR
        [SerializeField]
#endif
        protected bool applyAutoRipGroupCreators;

#if UNITY_EDITOR
        protected void OnValidate()
        {
            if (applyAutoRipGroupCreators)
            {
                applyAutoRipGroupCreators = false; 
                if (pieces != null)
                {
                    foreach (var piece in pieces)
                    {
                        if (piece != null && piece.ripGroupCreators != null && piece.ripGroupCreators.Length > 0) piece.CreateRipGroupsFromCreators(piece.ripGroupCreators); 
                    }
                }
            }
        }
#endif

        [Serializable]
        public class RipEvent
        {
            [Range(0f, 1f)]
            public float minChance;
            [Range(0f, 1f)]
            public float maxChance;

            public bool clampRange;
            public Vector2 ripDeltaRange;

            public UnityEvent OnCall;

            private float deltaAccum;

            public void Reset()
            {
                deltaAccum = 0f;
            }

            public bool TryCall(float ripDelta)
            {
                if (ripDeltaRange.x == ripDeltaRange.y)
                {
                    if (UnityEngine.Random.value > minChance) return false;
                }
                else
                {
                    deltaAccum += ripDelta;
                    float t = (deltaAccum - ripDeltaRange.x) / (ripDeltaRange.y - ripDeltaRange.x); 
                    if (clampRange) t = Mathf.Clamp01(t);
                    
                    if (UnityEngine.Random.value > Mathf.LerpUnclamped(minChance, maxChance, t)) return false; 
                }

                Call();
                return true;
            }

            public void Call()
            {
                OnCall?.Invoke();
                deltaAccum = 0f;
            }
        }

        [Serializable]
        public class RipLevel : IComparable<RipLevel>
        {
            public float threshold;
            public float visualRippage;
            public float ripSpeed;

            public bool copyFirstContributors;
            public float copyMuscleContributorsMassMultiplier = 1f;
            public float copyFatContributorsMultiplier = 1f;

            public MuscleRipContributor[] muscleRipContributors;
            public FatRipContributor[] fatRipContributors;

            public void CopyContributorsFrom(RipLevel level)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return;
#endif

                if (level.muscleRipContributors != null && level.muscleRipContributors.Length > 0)
                {
                    float massMul = copyMuscleContributorsMassMultiplier == 0f ? 1f : copyMuscleContributorsMassMultiplier;
                    if (muscleRipContributors != null && muscleRipContributors.Length > 0)
                    {
                        var tempArray = new MuscleRipContributor[muscleRipContributors.Length + level.muscleRipContributors.Length];
                        Array.Copy(level.muscleRipContributors, tempArray, muscleRipContributors.Length);
                        Array.Copy(muscleRipContributors, 0, tempArray, level.muscleRipContributors.Length, muscleRipContributors.Length); 
                        muscleRipContributors = tempArray;
                    } 
                    else
                    {
                        muscleRipContributors = (MuscleRipContributor[])level.muscleRipContributors.Clone();
                    }
                    for(int i = 0; i < level.muscleRipContributors.Length; i++)
                    {
                        var cont = muscleRipContributors[i].Duplicate();
                        cont.massRange = cont.massRange * massMul;
                        muscleRipContributors[i] = cont;
                    }
                }

                if (level.fatRipContributors != null && level.fatRipContributors.Length > 0)
                {
                    float fatMul = copyFatContributorsMultiplier == 0f ? 1f : copyFatContributorsMultiplier;
                    if (fatRipContributors != null && fatRipContributors.Length > 0)
                    {
                        var tempArray = new FatRipContributor[fatRipContributors.Length + level.fatRipContributors.Length];
                        Array.Copy(level.fatRipContributors, tempArray, fatRipContributors.Length);
                        Array.Copy(fatRipContributors, 0, tempArray, level.fatRipContributors.Length, fatRipContributors.Length);
                        fatRipContributors = tempArray;
                    }
                    else
                    {
                        fatRipContributors = (FatRipContributor[])level.fatRipContributors.Clone();
                    }
                    for (int i = 0; i < level.fatRipContributors.Length; i++)
                    {
                        var cont = fatRipContributors[i].Duplicate();
                        cont.fatRange = cont.fatRange * fatMul;
                        fatRipContributors[i] = cont;
                    }
                }
            }

            public bool copyFirstChanceEvents;

            public int maxChanceRipEventCalls;
            public RipEvent[] chanceRipEvents;

            public void CopyChanceEventsFrom(RipLevel level)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return;
#endif

                if (level.chanceRipEvents != null && level.chanceRipEvents.Length > 0)
                {
                    if (chanceRipEvents != null && chanceRipEvents.Length > 0)
                    {
                        var tempArray = new RipEvent[chanceRipEvents.Length + level.chanceRipEvents.Length];
                        Array.Copy(level.chanceRipEvents, tempArray, chanceRipEvents.Length);
                        Array.Copy(chanceRipEvents, 0, tempArray, level.chanceRipEvents.Length, chanceRipEvents.Length);
                        chanceRipEvents = tempArray;
                    }
                    else
                    {
                        chanceRipEvents = level.chanceRipEvents;
                    }
                }
            }

            public bool copyFirstEvents;

            public UnityEvent OnRip;
            public UnityEvent<float> OnRipDelta;

            public void CopyEventsFrom(RipLevel level)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return;
#endif

                if (level.OnRip != null)
                {
                    if (OnRip != null)
                    {
                        OnRip.AddListener(level.OnRip.Invoke);
                    }
                    else
                    {
                        OnRip = level.OnRip;
                    }
                }

                if (level.OnRipDelta != null)
                {
                    if (OnRipDelta != null)
                    {
                        OnRipDelta.AddListener(level.OnRipDelta.Invoke);
                    }
                    else
                    {
                        OnRipDelta = level.OnRipDelta;
                    }
                }
            }

            public int CompareTo(RipLevel other)
            {
                return threshold.CompareTo(other.threshold);
            }

            public void Reset()
            {
                if (chanceRipEvents != null)
                {
                    foreach(var event_ in chanceRipEvents) event_.Reset();
                }
            }

            public void CopySettingsFrom(RipLevel level)
            {
                var fields = typeof(RipLevel).GetFields();
                foreach (var field in fields)
                {
                    field.SetValue(this, field.GetValue(level));
                }
            }
            public void CopyNonNullSettingsFrom(RipLevel level)
            {
                var fields = typeof(RipLevel).GetFields();
                foreach (var field in fields)
                {
                    var val = field.GetValue(level);
                    if (!ReferenceEquals(val, null)) field.SetValue(this, val);
                }
            }
        }

        [Serializable]
        public class MuscleRipContributor
        {
            public string muscleGroupName;
            [NonSerialized]
            private int muscleGroupIndex;
            public int MuscleGroupIndex => muscleGroupIndex; 

            public Side side;
            public Vector2 massRange;
            [NonSerialized]
            private float massRangeSize;

            public float minRipDelta;
            public float maxRipDelta;

            public Vector2 flexRange;
            [NonSerialized]
            private float flexRangeSize;

            public float minFlexMassMultiplier = 1f;
            public float maxFlexMassMultiplier = 1f;

            public Vector2 pumpRange;
            [NonSerialized]
            private float pumpRangeSize;

            public float minPumpMassMultiplier = 1f;
            public float maxPumpMassMultiplier = 1f;

            public CustomizableCharacterMeshV2 characterMeshOverride;

            public MuscleRipContributor Duplicate()
            {
                MuscleRipContributor newCont = new MuscleRipContributor();
                var fields = typeof(MuscleRipContributor).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    field.SetValue(newCont, field.GetValue(this));
                }
                return newCont;
            }

            public MuscleRipContributor LerpInto(MuscleRipContributor other, float t)
            {
                MuscleRipContributor newCont = new MuscleRipContributor();

                newCont.muscleGroupName = other.muscleGroupName;
                newCont.muscleGroupIndex = other.muscleGroupIndex;
                newCont.side = other.side;

                newCont.massRange = Vector2.LerpUnclamped(massRange, other.massRange, t);
                newCont.minRipDelta = Mathf.LerpUnclamped(minRipDelta, other.minRipDelta, t);
                newCont.maxRipDelta = Mathf.LerpUnclamped(maxRipDelta, other.maxRipDelta, t);

                newCont.flexRange = Vector2.LerpUnclamped(flexRange, other.flexRange, t);
                newCont.minFlexMassMultiplier = Mathf.LerpUnclamped(minFlexMassMultiplier, other.minFlexMassMultiplier, t);
                newCont.maxFlexMassMultiplier = Mathf.LerpUnclamped(maxFlexMassMultiplier, other.maxFlexMassMultiplier, t);

                newCont.pumpRange = Vector2.LerpUnclamped(pumpRange, other.pumpRange, t);
                newCont.minPumpMassMultiplier = Mathf.LerpUnclamped(minPumpMassMultiplier, other.minPumpMassMultiplier, t);
                newCont.maxPumpMassMultiplier = Mathf.LerpUnclamped(maxPumpMassMultiplier, other.maxPumpMassMultiplier, t);

                newCont.characterMeshOverride = other.characterMeshOverride;

                return newCont;
            }

            public void Init(CustomizableCharacterGarment garment)
            {
                if (characterMeshOverride == null) characterMeshOverride = garment.MainCharacterMesh;
                if (characterMeshOverride != null)
                {
                    muscleGroupIndex = characterMeshOverride.IndexOfMuscleGroup(muscleGroupName);
                }

                massRangeSize = massRange.y - massRange.x; // small optimization
                flexRangeSize = flexRange.y - flexRange.x;
                pumpRangeSize = pumpRange.y - pumpRange.x;  
            }

            public float CurrentRipDelta
            {
                get
                {
                    if (characterMeshOverride == null || muscleGroupIndex < 0) return 0f;

                    var muscleData = characterMeshOverride.GetMuscleDataUnsafe(muscleGroupIndex);
                    float mass = side == Side.Both ? ((muscleData.valuesLeft.mass + muscleData.valuesRight.mass) * 0.5f) : (side == Side.Left ? muscleData.valuesLeft.mass : muscleData.valuesRight.mass);
                    if (flexRange.x != flexRange.y)
                    {
                        float flex = side == Side.Both ? ((muscleData.valuesLeft.flex + muscleData.valuesRight.flex) * 0.5f) : (side == Side.Left ? muscleData.valuesLeft.flex : muscleData.valuesRight.flex);
                        if (flex >= flexRange.x)
                        {
                            mass = mass * Mathf.LerpUnclamped(minFlexMassMultiplier, maxFlexMassMultiplier, (flex - flexRange.x) / flexRangeSize); 
                        }
                    }
                    if (pumpRange.x != pumpRange.y)
                    {
                        float pump = side == Side.Both ? ((muscleData.valuesLeft.pump + muscleData.valuesRight.pump) * 0.5f) : (side == Side.Left ? muscleData.valuesLeft.pump : muscleData.valuesRight.pump);
                        if (pump >= pumpRange.x)
                        {
                            mass = mass * Mathf.LerpUnclamped(minPumpMassMultiplier, maxPumpMassMultiplier, (pump - pumpRange.x) / pumpRangeSize);
                        }
                    }

                    if (mass < massRange.x) return 0f;

                    if (massRange.x == massRange.y) return minRipDelta;
                    return Mathf.LerpUnclamped(minRipDelta, maxRipDelta, (mass - massRange.x) / massRangeSize); 
                }
            }
        }
        [Serializable]
        public class FatRipContributor
        {
            public string fatGroupName;
            [NonSerialized]
            private int fatGroupIndex;
            public int FatGroupIndex => fatGroupIndex;

            public Side side;
            public Vector2 fatRange;
            [NonSerialized]
            private float fatRangeSize;

            public float minRipDelta;
            public float maxRipDelta;

            public CustomizableCharacterMeshV2 characterMeshOverride;

            public FatRipContributor Duplicate()
            {
                FatRipContributor newCont = new FatRipContributor();
                var fields = typeof(FatRipContributor).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    field.SetValue(newCont, field.GetValue(this));
                }
                return newCont;
            }

            public FatRipContributor LerpInto(FatRipContributor other, float t)
            {
                FatRipContributor newCont = new FatRipContributor();

                newCont.fatGroupName = other.fatGroupName;
                newCont.fatGroupIndex = other.fatGroupIndex;
                newCont.side = other.side;

                newCont.fatRange = Vector2.LerpUnclamped(fatRange, other.fatRange, t);

                newCont.minRipDelta = Mathf.LerpUnclamped(minRipDelta, other.minRipDelta, t);
                newCont.maxRipDelta = Mathf.LerpUnclamped(maxRipDelta, other.maxRipDelta, t); 

                newCont.characterMeshOverride = other.characterMeshOverride;

                return newCont;
            }

            public void Init(CustomizableCharacterGarment garment)
            {
                if (characterMeshOverride == null) characterMeshOverride = garment.MainCharacterMesh;
                if (characterMeshOverride != null)
                {
                    fatGroupIndex = characterMeshOverride.IndexOfFatGroup(fatGroupName);
                }

                fatRangeSize = fatRange.y - fatRange.x; // small optimization
            }

            public float CurrentRipDelta
            {
                get
                {
                    if (characterMeshOverride == null || fatGroupIndex < 0) return 0f;

                    var fatLevel = characterMeshOverride.GetFatLevelUnsafe(fatGroupIndex);
                    if (fatLevel < fatRange.x) return 0f;

                    if (fatRange.x == fatRange.y) return minRipDelta;
                    return Mathf.LerpUnclamped(minRipDelta, maxRipDelta, (fatLevel - fatRange.x) / fatRangeSize);
                }
            }
        }

        [Serializable]
        public class RipGroup
        {
            public string name;

            public float pieceRipOffRippageThreshold = 0.9f;

            public List<RipLevel> ripLevels;

            public CustomizableCharacterMeshVertexControlGroup.SubGroup.Reference[] visualControlGroups;

            [NonSerialized]
            protected float rippage;
            public float Rippage
            {
                get => rippage;
                set
                {
                    rippage = value;
                }
            }

            [NonSerialized]
            protected float lastRipDelta;
            public float LastRipDelta => lastRipDelta;

            [NonSerialized]
            protected float visualRippage;
            public float VisualRippage => visualRippage;
            public void RefreshVisualRippage()
            {
                if (visualControlGroups != null)
                {
                    foreach (var group in visualControlGroups)
                    {
                        var inst = group.Instance;
                        if (inst != null) inst.ControlWeight = visualRippage;
                    }
                }
            }

            private CustomizableCharacterGarment garment;
            public CustomizableCharacterGarment Garment => garment;

            private GarmentPiece piece;
            public GarmentPiece Piece => piece;

            public void Init(CustomizableCharacterGarment garment, GarmentPiece piece)
            {
                this.garment = garment; 
                this.piece = piece;

                if (ripLevels != null)
                {
                    ripLevels.Sort();

                    var firstRipLevel = ripLevels.Count > 0 ? ripLevels[0] : null;
                    if (firstRipLevel != null)
                    {
                        for (int i = 1; i < ripLevels.Count; i++)
                        {
                            var level = ripLevels[i];
                            if (level.copyFirstContributors) level.CopyContributorsFrom(firstRipLevel);
                            if (level.copyFirstChanceEvents) level.CopyChanceEventsFrom(firstRipLevel);
                            if (level.copyFirstEvents) level.CopyEventsFrom(firstRipLevel); 
                        }
                    }
                    foreach (var level in ripLevels)
                    {
                        if (level.muscleRipContributors != null)
                        {
                            foreach (var cont in level.muscleRipContributors) cont.Init(garment);
                        }
                        if (level.fatRipContributors != null)
                        {
                            foreach (var cont in level.fatRipContributors) cont.Init(garment);
                        }
                    }
                }
            }

            private RipLevel contributorRipLevel;
            private RipLevel currentRipLevel;
            public void Update(float deltaTime)
            {
                contributorRipLevel = default;
                currentRipLevel = default;
                if (ripLevels != null)
                {
                    for (int i = 0; i < ripLevels.Count; i++)
                    {
                        var level = ripLevels[i];
                        contributorRipLevel = level;
                        if (rippage < level.threshold) break;

                        currentRipLevel = level;
                    }
                }

                lastRipDelta = 0f;
                if (contributorRipLevel != null)
                {
                    if (contributorRipLevel.muscleRipContributors != null)
                    {
                        foreach (var cont in contributorRipLevel.muscleRipContributors) lastRipDelta += cont.CurrentRipDelta;
                    }
                    if (contributorRipLevel.fatRipContributors != null)
                    {
                        foreach (var cont in contributorRipLevel.fatRipContributors) lastRipDelta += cont.CurrentRipDelta;
                    }
                }
                rippage = Mathf.Min(1f, rippage + lastRipDelta * deltaTime);



                UpdateVisually(deltaTime);
            }

            public void UpdateVisually(float deltaTime)
            {
                float prevVisualRippage = visualRippage;

                if (currentRipLevel != null)
                {
                    if (visualRippage < currentRipLevel.visualRippage) visualRippage = Mathf.MoveTowards(visualRippage, currentRipLevel.visualRippage, deltaTime * (currentRipLevel.ripSpeed == 0f ? 1f : currentRipLevel.ripSpeed));

                    if (visualRippage != prevVisualRippage)
                    {
                        float delta = visualRippage - prevVisualRippage;

                        if (delta > 0f)
                        {
                            currentRipLevel.OnRip?.Invoke();
                            if (currentRipLevel.chanceRipEvents != null)
                            {
                                int count = 0;
                                foreach (var e in currentRipLevel.chanceRipEvents)
                                {
                                    if (e.TryCall(delta)) count++;
                                    if (count >= currentRipLevel.maxChanceRipEventCalls) break;
                                }
                            }
                        }

                        currentRipLevel.OnRipDelta?.Invoke(delta);

                        RefreshVisualRippage();
                    }
                }
            }

            public void Reset()
            {
                Rippage = 0f;

                if (ripLevels != null)
                {
                    foreach (var level in ripLevels) level.Reset();
                }
            }

        }

        [Serializable]
        public class AutoRipGroupCreator
        {
            public string groupName;

            public string mainContributingGroupName;
            public bool isFatGroup;

            public Side side;
            public bool mirror;

            public MuscleRipContributor muscleContributorStart;
            public MuscleRipContributor muscleContributorEnd;

            public FatRipContributor fatContributorStart;
            public FatRipContributor fatContributorEnd;

            public RipLevel[] baseRipLevels;

            public CustomizableCharacterMeshVertexControlGroups controlGroupManager;
            public string controlGroupId;
            public string optionalSubGroupId;

            public AutoRipGroupCreator Duplicate()
            {
                var newCreator = new AutoRipGroupCreator();

                var fields = typeof(AutoRipGroupCreator).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    field.SetValue(newCreator, field.GetValue(this));
                }

                return newCreator;
            }
        }

        [Serializable]
        public class GarmentPiece
        {

            public string name;
            public bool disable;

#if UNITY_EDITOR
            [SerializeField]
#else
            [NonSerialized]
#endif
            public AutoRipGroupCreator[] ripGroupCreators;

            public void CreateRipGroupsFromCreators(AutoRipGroupCreator[] creators)
            {
                List<RipGroup> ripGroups = new List<RipGroup>();
                if (this.ripGroups != null)
                {
                    ripGroups.AddRange(this.ripGroups);
                }

                if (creators != null)
                {
                    void EvaluateCreator(AutoRipGroupCreator creator)
                    {
                        RipGroup ripGroup = null;
                        int existingIndex = ripGroups.FindIndex(g => g.name == creator.groupName);
                        if (existingIndex >= 0)
                        {
                            ripGroup = ripGroups[existingIndex];
                        }
                        else
                        {
                            ripGroup = new RipGroup() { name = creator.groupName };
                            ripGroups.Add(ripGroup);
                        }

                        if (ripGroup.ripLevels == null) ripGroup.ripLevels = new List<RipLevel>();
                        while (ripGroup.ripLevels.Count < creator.baseRipLevels.Length) ripGroup.ripLevels.Add(new RipLevel());
                        if (ripGroup.ripLevels.Count > creator.baseRipLevels.Length) ripGroup.ripLevels.RemoveRange(creator.baseRipLevels.Length, ripGroup.ripLevels.Count - creator.baseRipLevels.Length);

                        for (int i = 0; i < creator.baseRipLevels.Length; i++)
                        {
                            float t = i / (float)(creator.baseRipLevels.Length - 1f);

                            var baseLevel = creator.baseRipLevels[i];
                            var level = ripGroup.ripLevels[i];
                            bool copyFirstContributors = level.copyFirstContributors;
                            bool copyFirstChanceEvents = level.copyFirstChanceEvents;
                            bool copyFirstEvents = level.copyFirstEvents;
                            level.CopyNonNullSettingsFrom(baseLevel);
                            level.copyFirstContributors = copyFirstContributors;
                            level.copyFirstChanceEvents = copyFirstChanceEvents; 
                            level.copyFirstEvents = copyFirstEvents;
                            if (!level.copyFirstContributors)
                            {
                                if (creator.isFatGroup)
                                {
                                    if (level.fatRipContributors == null) level.fatRipContributors = new FatRipContributor[0];
                                    var cont = creator.fatContributorStart.LerpInto(creator.fatContributorEnd, t);
                                    cont.fatGroupName = creator.mainContributingGroupName;
                                    cont.side = creator.side;
                                    level.fatRipContributors = (FatRipContributor[])level.fatRipContributors.Add(cont);
                                }
                                else
                                {
                                    if (level.muscleRipContributors == null) level.muscleRipContributors = new MuscleRipContributor[0];
                                    var cont = creator.muscleContributorStart.LerpInto(creator.muscleContributorEnd, t);
                                    cont.muscleGroupName = creator.mainContributingGroupName;
                                    cont.side = creator.side;
                                    level.muscleRipContributors = (MuscleRipContributor[])level.muscleRipContributors.Add(cont);
                                }
                            }
                        }

                        if (creator.controlGroupManager != null)
                        {
                            if (creator.controlGroupManager.TryGetControlGroup(creator.controlGroupId, out var controlGroup) && controlGroup != null)
                            {
                                var subGroupName = string.IsNullOrWhiteSpace(creator.optionalSubGroupId) ? (creator.side != Side.Both ? (creator.mainContributingGroupName == Utils.GetMirroredName(creator.mainContributingGroupName) ? $"{creator.mainContributingGroupName}{creator.side.AsSuffix("_L", "_R")}" : creator.mainContributingGroupName) : creator.mainContributingGroupName) : creator.optionalSubGroupId;
                                if (controlGroup.TryGetSubGroup(subGroupName, out CustomizableCharacterMeshVertexControlGroup.SubGroup subGroup) && subGroup != null)
                                {
                                    ripGroup.visualControlGroups = new CustomizableCharacterMeshVertexControlGroup.SubGroup.Reference[] { new CustomizableCharacterMeshVertexControlGroup.SubGroup.Reference() { controlGroupManager = creator.controlGroupManager, controlGroupId = creator.controlGroupId, subControlGroupName = subGroup.displayName } };
                                }
                            }
                        }
                    }

                    foreach (var creator in creators)
                    {
                        EvaluateCreator(creator);
                        if (creator.mirror && creator.side != Side.Both)
                        {
                            var mirroredCreator = creator.Duplicate();
                            mirroredCreator.groupName = Utils.GetMirroredName(creator.groupName);
                            mirroredCreator.mainContributingGroupName = Utils.GetMirroredName(creator.mainContributingGroupName); 
                            mirroredCreator.side = creator.side == Side.Left ? Side.Right : Side.Left; 
                            EvaluateCreator(mirroredCreator);
                        }
                    }
                }

                this.ripGroups = ripGroups.ToArray();
            }

            public RipGroup[] ripGroups;

            [NonSerialized]
            protected bool isRippedOff;
            public bool IsRippedOff => isRippedOff;

            public UnityEvent OnInit;

            public float firstRipThreshold = 0.001f;
            public UnityEvent OnFirstRip;

            public UnityEvent OnRipOff;

            public UnityEvent OnRestore;

            private CustomizableCharacterGarment garment;
            public CustomizableCharacterGarment Garment => garment;

            [NonSerialized]
            protected bool hasRipped;
            public bool HasRipped => hasRipped;

            public void Init(CustomizableCharacterGarment garment)
            {
                this.garment = garment;

                if (ripGroups != null)
                {
                    foreach (var ripGroup in ripGroups) ripGroup.Init(garment, this);
                }

                OnInit?.Invoke();
            }

            public void Update(float deltaTime)
            {
                if (disable) return;

                if (!IsRippedOff)
                {
                    if (ripGroups != null)
                    {
                        bool canRipOff = true;
                        foreach (var ripGroup in ripGroups)
                        {
                            if (ripGroup.Rippage < ripGroup.pieceRipOffRippageThreshold)
                            {
                                canRipOff = false;
                                break;
                            }
                        }

                        if (canRipOff)
                        {
                            RipOff();
                        }
                        else
                        {
                            if (HasRipped)
                            {
                                foreach (var ripGroup in ripGroups)
                                {
                                    ripGroup.Update(deltaTime);
                                }
                            }
                            else
                            {
                                foreach (var ripGroup in ripGroups)
                                {
                                    ripGroup.Update(deltaTime);
                                    if (!HasRipped && ripGroup.VisualRippage > firstRipThreshold)
                                    {
                                        OnFirstRip?.Invoke();
                                        hasRipped = true;

                                        bool isFirst = true;
                                        foreach (var piece in garment.pieces)
                                        {
                                            if (piece != this && piece.HasRipped)
                                            {
                                                isFirst = false;
                                                break;
                                            }
                                        }

                                        if (isFirst) garment.OnFirstRipAny?.Invoke();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public void UpdateVisually(float deltaTime)
            {
                if (disable) return;

                if (!IsRippedOff)
                {
                    if (ripGroups != null)
                    {
                        foreach (var ripGroup in ripGroups)
                        {
                            ripGroup.UpdateVisually(deltaTime);
                        }
                    }
                }
            }

            public void Restore()
            {
                if (disable) return;

                bool wasFullyRippedOff = true;
                foreach (var piece in garment.pieces)
                {
                    if (!piece.IsRippedOff)
                    {
                        wasFullyRippedOff = false;
                        break;
                    }
                }

                if (ripGroups != null)
                {
                    foreach (var ripGroup in ripGroups) ripGroup.Reset();
                }

                hasRipped = false;

                OnRestore?.Invoke();

                if (wasFullyRippedOff) garment.OnRestoreFromRipOff?.Invoke(); 
            }

            public void RipOff()
            {
                if (disable || IsRippedOff) return;

                isRippedOff = true;
                OnRipOff?.Invoke();

                if (garment != null)
                {
                    bool isFinal = true;
                    foreach (var piece in garment.pieces) 
                    {
                        if (!piece.IsRippedOff)
                        {
                            isFinal = false;
                            break;
                        }
                    }

                    if (isFinal) garment.OnFinalRipOff?.Invoke();
                }
            }

        }

        public CustomizableCharacterMeshV2 mainCharacterMesh;
        public CustomizableCharacterMeshV2 MainCharacterMesh => mainCharacterMesh;

        public bool onlyUpdateIfDirty;

        protected bool isDirty;
        public bool IsDirty => isDirty;
        public virtual void MarkDirty()
        {
            isDirty = true;
        }
        protected virtual void MarkDirtyFromMuscleUpdate(int muscleGroupIndex)
        {
            if (validMuscleGroupIndices == null || !validMuscleGroupIndices.Contains(muscleGroupIndex)) return;
            MarkDirty();
        }
        protected virtual void MarkDirtyFromFatUpdate(int fatGroupIndex)
        {
            if (validFatGroupIndices == null || !validFatGroupIndices.Contains(fatGroupIndex)) return;
            MarkDirty();
        }

        public GarmentPiece[] pieces;

        public UnityEvent OnInit;
        public UnityEvent OnFirstRipAny;
        public UnityEvent OnFinalRipOff;
        public UnityEvent OnRestoreFromRipOff;

        private List<CustomizableCharacterMeshV2> dependentMeshes;
        private List<int> validMuscleGroupIndices;
        private List<int> validFatGroupIndices; 
        protected virtual void Start()
        {
            if (pieces != null)
            {
                foreach (var piece in pieces) piece.Init(this);
                isDirty = true;
            }

            if (onlyUpdateIfDirty)
            {
                dependentMeshes = new List<CustomizableCharacterMeshV2>(); 

                if (pieces != null)
                {
                    foreach(var piece in pieces)
                    {
                        if (piece.ripGroups != null)
                        {
                            foreach(var ripGroup in piece.ripGroups)
                            {
                                if (ripGroup.ripLevels != null)
                                {
                                    foreach (var ripLevel in ripGroup.ripLevels)
                                    {
                                        if (ripLevel.muscleRipContributors != null && ripLevel.muscleRipContributors.Length > 0)
                                        {
                                            foreach (var cont in ripLevel.muscleRipContributors)
                                            {
                                                if (cont.characterMeshOverride == null) continue;
                                                if (dependentMeshes == null) dependentMeshes = new List<CustomizableCharacterMeshV2>();
                                                if (!dependentMeshes.Contains(cont.characterMeshOverride))
                                                {
                                                    dependentMeshes.Add(cont.characterMeshOverride);
                                                    cont.characterMeshOverride.AddListener(Unity.ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, MarkDirtyFromMuscleUpdate);
                                                }

                                                if (cont.MuscleGroupIndex >= 0)
                                                {
                                                    if (validMuscleGroupIndices == null) validMuscleGroupIndices = new List<int>();
                                                    if (!validMuscleGroupIndices.Contains(cont.MuscleGroupIndex)) validMuscleGroupIndices.Add(cont.MuscleGroupIndex);
                                                }
                                            }
                                        }

                                        if (ripLevel.fatRipContributors != null && ripLevel.fatRipContributors.Length > 0)
                                        {
                                            foreach (var cont in ripLevel.fatRipContributors)
                                            {
                                                if (cont.characterMeshOverride == null) continue;
                                                if (dependentMeshes == null) dependentMeshes = new List<CustomizableCharacterMeshV2>();
                                                if (!dependentMeshes.Contains(cont.characterMeshOverride))
                                                {
                                                    dependentMeshes.Add(cont.characterMeshOverride);
                                                    cont.characterMeshOverride.AddListener(Unity.ICustomizableCharacter.ListenableEvent.OnFatDataChanged, MarkDirtyFromFatUpdate);
                                                }

                                                if (cont.FatGroupIndex >= 0)
                                                {
                                                    if (validFatGroupIndices == null) validFatGroupIndices = new List<int>();
                                                    if (!validFatGroupIndices.Contains(cont.FatGroupIndex)) validFatGroupIndices.Add(cont.FatGroupIndex);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            OnInit?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            if (dependentMeshes != null)
            {
                foreach(var mesh in dependentMeshes)
                {
                    if (mesh == null) continue;

                    mesh.RemoveListener(Unity.ICustomizableCharacter.ListenableEvent.OnMuscleDataChanged, MarkDirtyFromMuscleUpdate);
                    mesh.RemoveListener(Unity.ICustomizableCharacter.ListenableEvent.OnFatDataChanged, MarkDirtyFromFatUpdate); 
                }

                dependentMeshes.Clear();
                dependentMeshes = null;
            }
        }

        protected virtual void OnEnable()
        {
            CustomizableCharacterGarmentUpdater.Register(this);
        }

        protected virtual void OnDisable()
        {
            CustomizableCharacterGarmentUpdater.Unregister(this);
        }

        public virtual void CustomUpdate()
        {
        }

        public virtual void CustomLateUpdate() 
        {
            if (!onlyUpdateIfDirty || IsDirty)
            {
                if (pieces != null)
                {
                    foreach (var piece in pieces) piece.Update(Time.deltaTime);
                }

                isDirty = false;
            }
            else
            {
                foreach (var piece in pieces)
                {
                    piece.UpdateVisually(Time.deltaTime);
                }
            }
        }
        public virtual void CustomFixedUpdate() { } 

    }

    public class CustomizableCharacterGarmentUpdater : CustomBehaviourUpdater<CustomizableCharacterGarmentUpdater, CustomizableCharacterGarment>
    {
        public static int ExecutionPriority => CustomizableCharacterClothBoneUpdater.ExecutionPriority + 5; 

        public override int Priority => ExecutionPriority;
    }
}
