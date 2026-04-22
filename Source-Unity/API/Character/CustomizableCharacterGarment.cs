using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Swole.Morphing;

namespace Swole.API
{
    public class CustomizableCharacterGarment : MonoBehaviour, ICustomUpdatableBehaviour 
    {

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

            public MuscleRipContributor[] muscleRipContributors;
            public FatRipContributor[] fatRipContributors;

            public int maxChanceRipEventCalls;
            public RipEvent[] chanceRipEvents;

            public UnityEvent OnRip;
            public UnityEvent<float> OnRipDelta;

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
        public class GarmentPiece
        {

            public string name;
            public bool disable;

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
                            foreach (var ripGroup in ripGroups)
                            {
                                ripGroup.Update(deltaTime);
                                if (!hasRipped && ripGroup.VisualRippage > firstRipThreshold)
                                {
                                    OnFirstRip?.Invoke();
                                    hasRipped = true;
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

                if (ripGroups != null)
                {
                    foreach (var ripGroup in ripGroups) ripGroup.Reset();
                }

                hasRipped = false;

                OnRestore?.Invoke();
            }

            public void RipOff()
            {
                if (disable || IsRippedOff) return;

                Debug.Log($"RIPPED OFF {name}"); 
                isRippedOff = true;
                OnRipOff?.Invoke();
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

        public virtual void CustomLateUpdate() { }
        public virtual void CustomFixedUpdate() { } 

    }

    public class CustomizableCharacterGarmentUpdater : CustomBehaviourUpdater<CustomizableCharacterGarmentUpdater, CustomizableCharacterGarment>
    {
    }
}
