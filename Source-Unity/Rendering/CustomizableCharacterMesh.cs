#if (UNITY_STANDALONE || UNITY_EDITOR)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Events;

using Unity.Mathematics;
using Unity.Collections;

using Swole.API.Unity;
using Swole.API.Unity.Animation;

namespace Swole.Morphing
{

    public class CustomizableCharacterMesh : InstanceableSkinnedMeshBase
    {

        #region Skinned Mesh Sync

        protected List<BlendShapeSync>[] standaloneShapeSyncs;
        protected List<BlendShapeSyncLR>[] muscleMassShapeSyncs;
        protected List<BlendShapeSyncLR>[] muscleFlexShapeSyncs;
        protected List<BlendShapeSync>[] fatShapeSyncs;
        protected List<BlendShapeSyncLR>[] variationShapeSyncs;

        public static string GetMuscleMassShapeSyncNameLeft(int groupIndex, int shapeIndex) => $"MASS_LEFT:{groupIndex}:{shapeIndex}";
        public static string GetMuscleMassShapeSyncNameRight(int groupIndex, int shapeIndex) => $"MASS_RIGHT:{groupIndex}:{shapeIndex}";
        public static string GetMuscleFlexShapeSyncNameLeft(int groupIndex, int shapeIndex) => $"FLEX_LEFT:{groupIndex}:{shapeIndex}";
        public static string GetMuscleFlexShapeSyncNameRight(int groupIndex, int shapeIndex) => $"FLEX_RIGHT:{groupIndex}:{shapeIndex}";

        public static string GetFatShapeSyncName(int groupIndex, int shapeIndex) => $"FAT:{groupIndex}:{shapeIndex}";

        public static string GetVariationShapeSyncNameLeft(int groupIndex, int shapeIndex) => $"VARIATION_LEFT:{groupIndex}:{shapeIndex}";
        public static string GetVariationShapeSyncNameRight(int groupIndex, int shapeIndex) => $"VARIATION_RIGHT:{groupIndex}:{shapeIndex}";

        protected override void SetupSkinnedMeshSyncs()
        {
            if (standaloneShapeSyncs == null) standaloneShapeSyncs = new List<BlendShapeSync>[CharacterMeshData.StandaloneShapesCount];
            if (muscleMassShapeSyncs == null) muscleMassShapeSyncs = new List<BlendShapeSyncLR>[CharacterMeshData.MuscleShapesCount * CharacterMeshData.MuscleVertexGroupCount];
            if (muscleFlexShapeSyncs == null) muscleFlexShapeSyncs = new List<BlendShapeSyncLR>[CharacterMeshData.FlexShapesCount * CharacterMeshData.MuscleVertexGroupCount];
            if (fatShapeSyncs == null) fatShapeSyncs = new List<BlendShapeSync>[CharacterMeshData.FatShapesCount * CharacterMeshData.FatVertexGroupCount];
            if (variationShapeSyncs == null) variationShapeSyncs = new List<BlendShapeSyncLR>[VariationShapesControlDataSize];

            for (int a = 0; a < standaloneShapeSyncs.Length; a++)
            {
                var list = standaloneShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSync>();

                list.Clear();
                standaloneShapeSyncs[a] = list;
            }
            for (int a = 0; a < muscleMassShapeSyncs.Length; a++)
            {
                var list = muscleMassShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSyncLR>();

                list.Clear();
                muscleMassShapeSyncs[a] = list;
            }
            for (int a = 0; a < muscleFlexShapeSyncs.Length; a++)
            {
                var list = muscleFlexShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSyncLR>();

                list.Clear();
                muscleFlexShapeSyncs[a] = list;
            }
            for (int a = 0; a < fatShapeSyncs.Length; a++)
            {
                var list = fatShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSync>();

                list.Clear();
                fatShapeSyncs[a] = list;
            }
            for (int a = 0; a < variationShapeSyncs.Length; a++)
            {
                var list = variationShapeSyncs[a];
                if (list == null) list = new List<BlendShapeSyncLR>();

                list.Clear();
                variationShapeSyncs[a] = list;
            }

            syncedSkinnedMeshes.RemoveAll(i => i == null || i.sharedMesh == null);

            var charData = CharacterMeshData;

            for (int a = 0; a < syncedSkinnedMeshes.Count; a++)
            {
                var mesh = syncedSkinnedMeshes[a];
                if (mesh == null) continue;

                for (int b = 0; b < charData.StandaloneShapesCount; b++)
                {
                    var shape = charData.GetStandaloneShape(b);

                    int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(shape.name);
                    if (shapeIndex >= 0) standaloneShapeSyncs[b].Add(new BlendShapeSync() { listenerIndex = a, listenerShapeIndex = shapeIndex });
                }

                for(int b = 0; b < charData.MuscleVertexGroupCount; b++)
                {
                    var group = charData.GetMuscleVertexGroup(b);

                    for (int c = 0; c < charData.MuscleShapesCount; c++)
                    {
                        var shape = charData.GetMuscleShape(c);
                        
                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleMassShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleMassShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) muscleMassShapeSyncs[(b * charData.MuscleShapesCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }

                    for (int c = 0; c < charData.FlexShapesCount; c++)
                    {
                        var shape = charData.GetFlexShape(c);

                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleFlexShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetMuscleFlexShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) muscleFlexShapeSyncs[(b * charData.FlexShapesCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }
                }

                for (int b = 0; b < charData.FatVertexGroupCount; b++)
                {
                    var group = charData.GetFatVertexGroup(b);

                    for (int c = 0; c < charData.FatShapesCount; c++)
                    {
                        var shape = charData.GetFatShape(c);

                        int shapeIndex = mesh.sharedMesh.GetBlendShapeIndex(GetFatShapeSyncName(b, c));

                        if (shapeIndex >= 0) fatShapeSyncs[(b * charData.FatShapesCount) + c].Add(new BlendShapeSync() { listenerIndex = a, listenerShapeIndex = shapeIndex });
                    }
                }

                for (int b = 0; b < charData.VariationVertexGroupCount; b++)
                {
                    var group = charData.GetVariationVertexGroup(b);

                    for (int c = 0; c < charData.VariationShapesCount; c++)
                    {
                        var shape = charData.GetVariationShape(c);

                        int shapeIndexL = mesh.sharedMesh.GetBlendShapeIndex(GetVariationShapeSyncNameLeft(b, c));
                        int shapeIndexR = mesh.sharedMesh.GetBlendShapeIndex(GetVariationShapeSyncNameRight(b, c));

                        if (shapeIndexL >= 0 || shapeIndexR >= 0) variationShapeSyncs[(b * charData.VariationShapesCount) + c].Add(new BlendShapeSyncLR() { listenerIndex = a, listenerShapeIndexLeft = shapeIndexL, listenerShapeIndexRight = shapeIndexR });
                    }
                }
            }
        }

        protected void SyncStandaloneShape(int index, float weight)
        {
            var list = standaloneShapeSyncs[index];
            if (list != null && list.Count > 0)
            {
                foreach(var sync in list)
                {
                    var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                    if (mesh != null) mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weight);
                }
            }
        }

        protected void SyncPartialShapeData(List<BlendShapeSync>[] syncs, int groupIndex, float weight, int count, float[] frameWeights)
        {
            int indexY = groupIndex * count;

            if (count == 1)
            {
                float frameWeight = frameWeights == null ? 1 : frameWeights[0];

                var list = syncs[indexY];
                if (list != null && list.Count > 0)
                {
                    foreach (var sync in list)
                    {
                        var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                        if (mesh != null)
                        {
                            mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weight / frameWeight);
                        }
                    }
                }
            }
            else if (count > 1)
            {

                int countM1 = count - 1;
                float countM1f = countM1;
                for (int a = 0; a < countM1; a++)
                {
                    float frameWeightA = frameWeights == null ? (a / countM1f) : frameWeights[a];

                    int b = a + 1;
                    float frameWeightB = frameWeights == null ? (b / countM1f) : frameWeights[b];

                    float weightRange = frameWeightB - frameWeightA;

                    float weightA = 0;
                    float weightB = (weight - frameWeightA) / weightRange;
                    if (weightB < 0)
                    {
                        if (a == 0)
                        {
                            if (frameWeightA != 0)
                            {
                                weightA = weight / frameWeightA;
                                weightB = 0;
                            }
                            else
                            {
                                weightA = 1 + Mathf.Abs(weight / weightRange);
                                weightB = 0;
                            }
                        }
                        else
                        {
                            weightA = Mathf.Abs(weightB);
                            weightB = 0;
                        }
                    }
                    else
                    {
                        weightA = 1 - weightB;
                        if (weightA < 0 && b < countM1)
                        {
                            weightA = 0;
                            weightB = 0;
                        }
                        else
                        {
                            weightA = Mathf.Max(0, weightA);
                        }
                    }

                    var list = syncs[indexY + a];
                    if (list != null && list.Count > 0)
                    {
                        foreach (var sync in list)
                        {
                            var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                            if (mesh != null)
                            {
                                mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weightA);
                            }
                        }
                    }

                    if (a == countM1 - 1)
                    {
                        list = syncs[indexY + b];
                        if (list != null && list.Count > 0)
                        {
                            foreach (var sync in list)
                            {
                                var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                                if (mesh != null)
                                {
                                    mesh.SetBlendShapeWeight(sync.listenerShapeIndex, weightB);
                                }
                            }
                        }
                    }
                }
            }
        }
        protected void SyncPartialShapeData(List<BlendShapeSyncLR>[] syncs, int groupIndex, float weightL, float weightR, int count, float[] frameWeights)
        {
            int indexY = groupIndex * count; 

            if (count == 1)
            {
                float frameWeight = frameWeights == null ? 1 : frameWeights[0];

                var list = syncs[indexY];
                if (list != null && list.Count > 0)
                {
                    foreach (var sync in list)
                    {
                        var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                        if (mesh != null) 
                        { 
                            if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightL / frameWeight);
                            if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightR / frameWeight);
                        }
                    }
                }
            }
            else if (count > 1)
            {

                int countM1 = count - 1;
                float countM1f = countM1;
                for (int a = 0; a < countM1; a++)
                {
                    float frameWeightA = frameWeights == null ? (a / countM1f) : frameWeights[a];

                    int b = a + 1;
                    float frameWeightB = frameWeights == null ? (b / countM1f) : frameWeights[b];

                    float weightRange = frameWeightB - frameWeightA;

                    float weightA_L = 0;
                    float weightB_L = (weightL - frameWeightA) / weightRange;
                    if (weightB_L < 0)
                    {
                        if (a == 0)
                        {
                            if (frameWeightA != 0)
                            {
                                weightA_L = weightL / frameWeightA;
                                weightB_L = 0;
                            }
                            else
                            {
                                weightA_L = 1 + Mathf.Abs(weightL / weightRange);
                                weightB_L = 0;
                            }
                        }
                        else
                        {
                            weightA_L = Mathf.Abs(weightB_L);
                            weightB_L = 0;
                        }
                    }
                    else
                    {
                        weightA_L = 1 - weightB_L;
                        if (weightA_L < 0 && b < countM1)
                        {
                            weightA_L = 0;
                            weightB_L = 0;
                        }
                        else
                        {
                            weightA_L = Mathf.Max(0, weightA_L);
                        }
                    }

                    float weightA_R = 0;
                    float weightB_R = (weightR - frameWeightA) / weightRange;
                    if (weightB_R < 0)
                    {
                        if (a == 0)
                        {
                            if (frameWeightA != 0)
                            {
                                weightA_R = weightR / frameWeightA;
                                weightB_R = 0;
                            }
                            else
                            {
                                weightA_R = 1 + Mathf.Abs(weightR / weightRange);
                                weightB_R = 0;
                            }
                        }
                        else
                        {
                            weightA_R = Mathf.Abs(weightB_R);
                            weightB_R = 0;
                        }
                    }
                    else
                    {
                        weightA_R = 1 - weightB_R;
                        if (weightA_R < 0 && b < countM1)
                        {
                            weightA_R = 0;
                            weightB_R = 0;
                        }
                        else
                        {
                            weightA_R = Mathf.Max(0, weightA_R);
                        }
                    }

                    var list = syncs[indexY + a];
                    if (list != null && list.Count > 0)
                    {
                        foreach (var sync in list)
                        {
                            var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                            if (mesh != null) 
                            {
                                if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightA_L);
                                if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightA_R);
                            }
                        }
                    }

                    if (a == countM1 - 1)
                    {
                        list = syncs[indexY + b];
                        if (list != null && list.Count > 0)
                        {
                            foreach (var sync in list)
                            {
                                var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                                if (mesh != null)
                                {
                                    if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightB_L);
                                    if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightB_R);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected void SyncMuscleMassData(int groupIndex, float massL, float massR)
        {
            var charData = CharacterMeshData;
            var count = charData.MuscleShapesCount;

            SyncPartialShapeData(muscleMassShapeSyncs, groupIndex, massL, massR, count, charData.frameWeightsMuscleShapes);
        }
        protected void SyncMuscleFlexData(int groupIndex, float flexL, float flexR)
        {
            var charData = CharacterMeshData;
            var count = charData.FlexShapesCount;

            SyncPartialShapeData(muscleFlexShapeSyncs, groupIndex, flexL, flexR, count, charData.frameWeightsFlexShapes);
        }
        protected void SyncFatLevel(int groupIndex, float weight)
        {
            var charData = CharacterMeshData;
            var count = charData.FatShapesCount;

            SyncPartialShapeData(fatShapeSyncs, groupIndex, weight, count, charData.frameWeightsFatShapes);
        }
        protected void SyncVariationData(int groupIndex, int shapeIndex, float weightL, float weightR)
        {
            var charData = CharacterMeshData;
            var count = charData.VariationShapesCount;

            var list = variationShapeSyncs[(groupIndex * count) + shapeIndex];
            if (list != null && list.Count > 0)
            {
                foreach (var sync in list)
                {
                    var mesh = syncedSkinnedMeshes[sync.listenerIndex];
                    if (mesh != null)
                    {
                        if (sync.listenerShapeIndexLeft >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexLeft, weightL); 
                        if (sync.listenerShapeIndexRight >= 0) mesh.SetBlendShapeWeight(sync.listenerShapeIndexRight, weightR); 
                    }
                }
            }
        }

        #endregion

        public void UpdateInEditor()
        {
            if (Application.isPlaying && isActiveAndEnabled)
            {
                if (prevBustSizeEditor != bustSizeEditor)
                {
                    prevBustSizeEditor = bustSizeEditor;
                    SetBustSize(bustSizeEditor);
                }
                if (prevShapeWeightsEditor == null || prevShapeWeightsEditor.Length == 0)
                {
                    prevShapeWeightsEditor = new NamedFloat[CharacterMeshData.StandaloneShapesCount];
                    for (int a = 0; a < prevShapeWeightsEditor.Length; a++) prevShapeWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetStandaloneShape(a).name };
                }
                if (shapeWeightsEditor == null || shapeWeightsEditor.Length == 0)
                {
                    shapeWeightsEditor = new NamedFloat[CharacterMeshData.StandaloneShapesCount];
                    for (int a = 0; a < shapeWeightsEditor.Length; a++) shapeWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetStandaloneShape(a).name };
                }

                if (prevMuscleWeightsEditor == null || prevMuscleWeightsEditor.Length == 0)
                {
                    prevMuscleWeightsEditor = new NamedMuscleData[CharacterMeshData.MuscleVertexGroupCount];
                    for (int a = 0; a < prevMuscleWeightsEditor.Length; a++) prevMuscleWeightsEditor[a] = new NamedMuscleData() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.muscleVertexGroupsBufferRange.x).name };
                }
                if (muscleWeightsEditor == null || muscleWeightsEditor.Length == 0)
                {
                    muscleWeightsEditor = new NamedMuscleData[CharacterMeshData.MuscleVertexGroupCount];
                    for (int a = 0; a < muscleWeightsEditor.Length; a++) muscleWeightsEditor[a] = new NamedMuscleData() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.muscleVertexGroupsBufferRange.x).name };
                }

                if (prevFatWeightsEditor == null || prevFatWeightsEditor.Length == 0)
                {
                    prevFatWeightsEditor = new NamedFloat[CharacterMeshData.FatVertexGroupCount];
                    for (int a = 0; a < prevFatWeightsEditor.Length; a++) prevFatWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.fatVertexGroupsBufferRange.x).name };
                }
                if (fatWeightsEditor == null || fatWeightsEditor.Length == 0)
                {
                    fatWeightsEditor = new NamedFloat[CharacterMeshData.FatVertexGroupCount];
                    for (int a = 0; a < fatWeightsEditor.Length; a++) fatWeightsEditor[a] = new NamedFloat() { name = CharacterMeshData.GetVertexGroup(a + CharacterMeshData.fatVertexGroupsBufferRange.x).name };
                }

                if (prevVariationWeightsEditor == null || prevVariationWeightsEditor.Length == 0)
                {
                    prevVariationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                    for (int a = 0; a < prevVariationWeightsEditor.Length; a++)
                    {
                        int groupIndex = a / CharacterMeshData.VariationShapesCount;
                        int shapeIndex = a % CharacterMeshData.VariationShapesCount;
                        prevVariationWeightsEditor[a] = new NamedFloat2() { name = CharacterMeshData.GetVertexGroup(CharacterMeshData.variationGroupIndices[groupIndex]).name + "_" + CharacterMeshData.GetVariationShape(shapeIndex).name };
                    }
                }
                if (variationWeightsEditor == null || variationWeightsEditor.Length == 0)
                {
                    variationWeightsEditor = new NamedFloat2[VariationShapesControlDataSize];
                    for (int a = 0; a < variationWeightsEditor.Length; a++)
                    {
                        int groupIndex = a / CharacterMeshData.VariationShapesCount;
                        int shapeIndex = a % CharacterMeshData.VariationShapesCount;
                        variationWeightsEditor[a] = new NamedFloat2() { name = CharacterMeshData.GetVertexGroup(CharacterMeshData.variationGroupIndices[groupIndex]).name + "_" + CharacterMeshData.GetVariationShape(shapeIndex).name };
                    }
                }

                for (int a = 0; a < shapeWeightsEditor.Length; a++)
                {
                    if (prevShapeWeightsEditor[a].value != shapeWeightsEditor[a].value)
                    {
                        SetStandaloneShapeWeightUnsafe(a, shapeWeightsEditor[a].value);
                        prevShapeWeightsEditor[a].value = shapeWeightsEditor[a].value;
                    }
                }

                if (prevGlobalMass != globalMass)
                {
                    prevGlobalMass = globalMass;
                    for (int a = 0; a < muscleWeightsEditor.Length; a++)
                    {
                        var values = muscleWeightsEditor[a].value;
                        var vl = values.valuesLeft;
                        var vr = values.valuesRight;
                        vl.mass = globalMass;
                        vr.mass = globalMass;
                        values.valuesLeft = vl;
                        values.valuesRight = vr;

                        SetMuscleDataUnsafe(a, values);

                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value = values;
                    }
                }
                if (prevGlobalFlex != globalFlex)
                {
                    prevGlobalFlex = globalFlex;
                    for (int a = 0; a < muscleWeightsEditor.Length; a++)
                    {
                        var values = muscleWeightsEditor[a].value;
                        var vl = values.valuesLeft;
                        var vr = values.valuesRight;
                        vl.flex = globalFlex;
                        vr.flex = globalFlex;
                        values.valuesLeft = vl;
                        values.valuesRight = vr;

                        SetMuscleDataUnsafe(a, values);

                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value = values;
                    }
                }
                if (prevGlobalPump != globalPump)
                {
                    prevGlobalPump = globalPump;
                    for (int a = 0; a < muscleWeightsEditor.Length; a++)
                    {
                        var values = muscleWeightsEditor[a].value;
                        var vl = values.valuesLeft;
                        var vr = values.valuesRight;
                        vl.pump = globalPump;
                        vr.pump = globalPump;
                        values.valuesLeft = vl;
                        values.valuesRight = vr;

                        SetMuscleDataUnsafe(a, values);

                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value = values;
                    }
                }

                for (int a = 0; a < muscleWeightsEditor.Length; a++)
                {
                    if (prevMuscleWeightsEditor[a].value != muscleWeightsEditor[a].value)
                    {
                        SetMuscleDataUnsafe(a, muscleWeightsEditor[a].value);
                        prevMuscleWeightsEditor[a].value = muscleWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < fatWeightsEditor.Length; a++)
                {
                    if (prevFatWeightsEditor[a].value != fatWeightsEditor[a].value)
                    {
                        SetFatLevelUnsafe(a, fatWeightsEditor[a].value);
                        prevFatWeightsEditor[a].value = fatWeightsEditor[a].value;
                    }
                }

                for (int a = 0; a < variationWeightsEditor.Length; a++)
                {
                    if (math.any(prevVariationWeightsEditor[a].value != variationWeightsEditor[a].value))
                    {
                        SetVariationWeightUnsafe(a, variationWeightsEditor[a].value);
                        prevVariationWeightsEditor[a].value = variationWeightsEditor[a].value;
                    }
                }
            }
        }

        public string configSaveDir;
        public string configAssetName;
        public bool saveConfig;

        public EditorCharacterCustomizationConfig editorCustomizationConfig;

        public void LoadEditorConfig(EditorCharacterCustomizationConfig config)
        {
            if (config != null) config.Apply(this);   
        }

#if UNITY_EDITOR

        public void OnValidate() 
        {
            UpdateInEditor();

            if (saveConfig)
            {
                saveConfig = false;
                SaveNewEditorConfig();
            }
        }

        public void SaveNewEditorConfig() => SaveNewEditorConfig(configSaveDir, configAssetName);
        public void SaveNewEditorConfig(string configSaveDir) => SaveNewEditorConfig(configSaveDir, configAssetName);
        public void SaveNewEditorConfig(string configSaveDir, string configAssetName)
        {
            editorCustomizationConfig = EditorCharacterCustomizationConfig.CreateAndSave(configSaveDir, configAssetName, this);
        }

#endif

        [Serializable]
        public struct NamedFloat
        {
            public string name;
            public float value;
        }
        [Serializable]
        public struct NamedFloat2
        {
            public string name;
            public float2 value;
        }
        [Serializable]
        public struct NamedMuscleData
        {
            public string name;
            public MuscleDataLR value;
        }

        private float prevGlobalMass;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 2f)]
        private float globalMass;

        private float prevGlobalFlex;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 2f)]
        private float globalFlex;

        private float prevGlobalPump;
#if UNITY_EDITOR
        [SerializeField]
#endif
        [Range(0f, 1f)]
        private float globalPump;

        private float prevBustSizeEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [Range(0, 2)]
        public float bustSizeEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedFloat[] prevShapeWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat[] shapeWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedMuscleData[] prevMuscleWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedMuscleData[] muscleWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedFloat[] prevFatWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat[] fatWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        [HideInInspector]
        public NamedFloat2[] prevVariationWeightsEditor;
#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public NamedFloat2[] variationWeightsEditor;

        public override void Dispose()
        {
            base.Dispose();

            /*if (standaloneShapesControl.IsCreated)
            {
                standaloneShapesControl.Dispose();
                standaloneShapesControl = default;
            }
            if (muscleGroupsControl.IsCreated) 
            {
                muscleGroupsControl.Dispose();
                muscleGroupsControl = default;
            }
            if (fatGroupsControl.IsCreated)
            {
                fatGroupsControl.Dispose();
                fatGroupsControl = default;
            }
            if (variationShapesControl.IsCreated)
            {
                variationShapesControl.Dispose();
                variationShapesControl = default;  
            }*/
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (children != null)
            {
                children.Clear();
                children = null;
            }

            if (shapesInstanceReference != null)
            {
                shapesInstanceReference.OnCreateInstanceID -= SetShapesInstanceID;
                shapesInstanceReference = null;
            }
            if (rigInstanceReference != null)
            {
                rigInstanceReference.OnCreateInstanceID -= SetRigInstanceID;
                rigInstanceReference = null;
            }
            if (characterInstanceReference != null)
            {
                characterInstanceReference.OnCreateInstanceID -= SetCharacterInstanceID;
                characterInstanceReference.RemoveChild(this);
                characterInstanceReference = null; 
            }

            if (animatablePropertiesController != null)
            {
                string id = GetInstanceID().ToString();
                for (int a = 0; a < animatablePropertiesController.PropertyCount; a++)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(a);
                    prop.ClearListeners(id);
                }
            }
        }

        /*protected virtual void LateUpdate()
        {
            //UpdateBuffers(); // removed
        }*/

        protected List<CustomizableCharacterMesh> children;
        public void AddChild(CustomizableCharacterMesh child)
        {
            if (child == null) return;

            if (children == null) children = new List<CustomizableCharacterMesh>();
            children.Add(child);
        }
        public void RemoveChild(CustomizableCharacterMesh child)
        {
            if (children == null) return;

            children.RemoveAll(i => ReferenceEquals(i, child));
        }

        [NonSerialized]
        protected float bustSize;
        public float BustSize
        {
            get => bustSize;
            set => SetBustSize(value);
        }
        [NonSerialized]
        protected bool hasBustSizeProperty;
        public void SetBustSize(float value)
        {
            bustSize = value;

            if (instance != null)
            {
                if (CharacterMeshData.bustSizeShapeIndex >= 0) SetStandaloneShapeWeightUnsafe(CharacterMeshData.bustSizeShapeIndex, value);

                if (hasBustSizeProperty)
                {
                    instance.SetFloatOverride(CharacterMeshData.BustMixPropertyName, Mathf.Clamp01(value), true);

                    if (children != null)
                    {
                        foreach (var child in children) child.SetBustSize(value);
                    }
                }
            }
        }
        [NonSerialized]
        protected bool hideNipples;
        public bool HideNipples
        {
            get => hideNipples;
            set => SetHideNipples(value);
        }
        [NonSerialized]
        protected bool hasHideNipplesProperty;
        public void SetHideNipples(bool value)
        {
            hideNipples = value;

            if (instance != null && hasHideNipplesProperty)
            {
                instance.SetFloatOverride(CharacterMeshData.HideNipplesPropertyName, hideNipples ? 1 : 0, true);

                if (children != null)
                {
                    foreach (var child in children) child.SetHideNipples(value);
                }
            }
        }
        [NonSerialized]
        protected bool hideGenitals;
        public bool HideGenitals
        {
            get => hideGenitals;
            set => SetHideGenitals(value);
        }
        [NonSerialized]
        protected bool hasHideGenitalsProperty;
        public void SetHideGenitals(bool value)
        {
            hideGenitals = value;
             
            if (instance != null && hasHideGenitalsProperty)
            {
                instance.SetFloatOverride(CharacterMeshData.HideGenitalsPropertyName, hideGenitals ? 1 : 0, true);

                if (children != null)
                {
                    foreach (var child in children) child.SetHideGenitals(value);
                }
            }
        }

        [SerializeField]
        protected CustomizableCharacterMeshData meshData;
        public void SetMeshData(CustomizableCharacterMeshData data) => meshData = data;
        public override InstanceableMeshDataBase MeshData => meshData;
        public virtual CustomizableCharacterMeshData CharacterMeshData => meshData;
        public override InstancedMeshGroup MeshGroup => meshData.meshGroups[meshGroupIndex];

        [SerializeField]
        protected CustomAvatar avatar;
        public void SetAvatar(CustomAvatar av) => avatar = av;
        public CustomAvatar Avatar => avatar;
        [SerializeField]
        protected Transform rigRoot;
        public void SetRigRoot(Transform root) => rigRoot = root;
        public Transform RigRoot
        {
            get
            {
                if (rigRoot == null)
                {
                    if (avatar != null && !string.IsNullOrWhiteSpace(avatar.rigContainer))
                    {
                        rigRoot = (transform.parent == null ? transform : transform.parent).FindDeepChildLiberal(avatar.rigContainer);
                    }

                    if (rigRoot == null) rigRoot = transform;
                }

                return rigRoot;
            }
        }
        public override Transform BoundsRootTransform => RigRoot;

        [SerializeField]
        protected DynamicAnimationProperties animatablePropertiesController;
        [NonSerialized]
        protected List<DynamicAnimationProperties.Property> dynamicAnimationProperties;
        public void SetAnimatablePropertiesController(DynamicAnimationProperties controller)
        {
            if (dynamicAnimationProperties != null)
            {
                if (animatablePropertiesController != null)
                {
                    foreach (var prop in dynamicAnimationProperties) animatablePropertiesController.RemoveProperty(prop);
                }
                dynamicAnimationProperties.Clear();  
            }       

            animatablePropertiesController = controller;
            if (animatablePropertiesController == null) return; 

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif

            if (dynamicAnimationProperties == null) dynamicAnimationProperties = new List<DynamicAnimationProperties.Property>();;

            string id = GetInstanceID().ToString();
            for (int a = 0; a < CharacterMeshData.StandaloneShapesCount; a++)
            {
                var shape = CharacterMeshData.GetStandaloneShape(a);
                if (shape == null || !shape.animatable) continue;

                int shapeIndex = a;

                string name = $"SHAPE:{shape.name}";
                int index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) => SetStandaloneShapeWeightUnsafe(shapeIndex, value));
                    continue;
                }

                var prop_ = animatablePropertiesController.CreateProperty(name, () => GetStandaloneShapeWeightUnsafe(shapeIndex), (float value) => SetStandaloneShapeWeightUnsafe(shapeIndex, value));
                dynamicAnimationProperties.Add(prop_);
            }
            for (int a = 0; a < CharacterMeshData.MuscleVertexGroupCount; a++)
            {
                var group = CharacterMeshData.GetMuscleVertexGroup(a);
                if (group == null || !group.flag) continue; // animatable permission is stored in vertex group flag field

                int shapeIndex = a;

                string name = $"FLEX_LEFT:{group.name}";
                int index = animatablePropertiesController.IndexOf(name); 
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesLeft = values.valuesLeft;
                        valuesLeft.flex = value;
                        values.valuesLeft = valuesLeft;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                }
                else
                {
                    var prop = animatablePropertiesController.CreateProperty(name, () => GetMuscleDataUnsafe(shapeIndex).valuesLeft.flex, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesLeft = values.valuesLeft;
                        valuesLeft.flex = value;
                        values.valuesLeft = valuesLeft;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                    dynamicAnimationProperties.Add(prop);
                }

                name = $"FLEX_RIGHT:{group.name}";
                index = animatablePropertiesController.IndexOf(name);
                if (index >= 0)
                {
                    var prop = animatablePropertiesController.GetPropertyUnsafe(index);
                    prop.Listen(id, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex); 
                        var valuesRight = values.valuesRight;
                        valuesRight.flex = value;
                        values.valuesRight = valuesRight;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                }
                else
                {
                    var prop = animatablePropertiesController.CreateProperty(name, () => GetMuscleDataUnsafe(shapeIndex).valuesLeft.flex, (float value) =>
                    {
                        var values = GetMuscleDataUnsafe(shapeIndex);
                        var valuesRight = values.valuesRight;
                        valuesRight.flex = value;
                        values.valuesRight = valuesRight;
                        SetMuscleDataUnsafe(shapeIndex, values);
                    });
                    dynamicAnimationProperties.Add(prop);
                }
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            var material = MeshGroup.GetMaterial(subMeshIndex);
            if (material != null)
            {
                hasBustSizeProperty = material.HasProperty(CharacterMeshData.BustMixPropertyName);
                hasHideNipplesProperty = material.HasProperty(CharacterMeshData.HideNipplesPropertyName);
                hasHideGenitalsProperty = material.HasProperty(CharacterMeshData.HideGenitalsPropertyName);  
            }

            SetAnimatablePropertiesController(animatablePropertiesController); 

            Animator = animator; // force subscribe listeners
        }

        protected override void OnStart()
        {
            base.OnStart();

            InitInstanceIDs();

            if (editorCustomizationConfig != null) LoadEditorConfig(editorCustomizationConfig);
        }

        public override CustomAnimator Animator
        {
            get => animator;
            set
            {
                if (animator != null)
                {
                    animator.RemoveListener(CustomAnimator.BehaviourEvent.OnResetPose, OnAnimatorResetPose);
                }

                animator = value;
                if (animator != null)
                {
                    animator.AddListener(CustomAnimator.BehaviourEvent.OnResetPose, OnAnimatorResetPose);
                }
            }
        }

        protected void OnAnimatorResetPose()
        {
            if (!HasInstance) return;

            for(int a = 0; a < CharacterMeshData.MuscleVertexGroupCount; a++)
            {
                var data = GetMuscleDataUnsafe(a);
                data.valuesLeft.flex = 0f;
                data.valuesRight.flex = 0f;
                SetMuscleDataUnsafe(a, data);
            }
        }

        public override string RigID => rigRoot.GetInstanceID().ToString();

        [SerializeField]
        protected string rigBufferId;
        public void SetRigBufferID(string id) => rigBufferId = id;
        public string LocalRigBufferID => rigBufferId;
        public override string RigBufferID => rigInstanceReference != null ? rigInstanceReference.RigBufferID : rigBufferId;

        [SerializeField]
        protected string shapeBufferId;
        public void SetShapeBufferID(string id) => shapeBufferId = id;
        public string LocalShapeBufferID => shapeBufferId;
        public override string ShapeBufferID => shapesInstanceReference != null ? shapesInstanceReference.ShapeBufferID : shapeBufferId; 

        [SerializeField]
        protected string morphBufferId;
        public void SetMorphBufferID(string id) => morphBufferId = id;
        public string LocalMorphBufferID => morphBufferId; 
        public virtual string MorphBufferID => characterInstanceReference != null ? characterInstanceReference.MorphBufferID : morphBufferId;

        [NonSerialized]
        protected int shapesInstanceID;
        public int ShapesInstanceID => shapesInstanceID <= 0 ? InstanceSlot : (shapesInstanceID - 1);
        public InstanceableSkinnedMeshBase shapesInstanceReference;

        [NonSerialized]
        protected int rigInstanceID;
        public override int RigInstanceID => rigInstanceID <= 0 ? InstanceSlot : (rigInstanceID - 1);
        public InstanceableSkinnedMeshBase rigInstanceReference;

        public override Rigs.StandaloneSampler RigSampler
        {
            get
            {
                if (rigInstanceReference == null && rigInstanceID <= 0) return base.RigSampler;   
                return null;
            }
        }

        [NonSerialized]
        protected int characterInstanceID;
        public int CharacterInstanceID => characterInstanceID <= 0 ? InstanceSlot : (characterInstanceID - 1);
        public CustomizableCharacterMesh characterInstanceReference;

        public void SetShapesInstanceID(int id) 
        { 
            shapesInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(CharacterMeshData.ShapesInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(CharacterMeshData.ShapesInstanceIDPropertyName, id);
                }
            }
        }
        public void SetRigInstanceID(int id) 
        { 
            rigInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(CharacterMeshData.RigInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(CharacterMeshData.RigInstanceIDPropertyName, id);
                }
            }
        }
        public void SetCharacterInstanceID(int id) 
        { 
            characterInstanceID = id + 1;

            if (instance != null)
            {
                if (id < 0)
                {
                    instance.SetFloatOverride(CharacterMeshData.CharacterInstanceIDPropertyName, instance.slot);
                }
                else
                {
                    instance.SetFloatOverride(CharacterMeshData.CharacterInstanceIDPropertyName, id);
                }
            }
        }

        protected virtual void InitInstanceIDs()
        {
            if (shapesInstanceReference != null) 
            { 
                SetShapesInstanceID(shapesInstanceReference.InstanceSlot);
                shapesInstanceReference.OnCreateInstanceID += SetShapesInstanceID;  
            }
            if (rigInstanceReference != null) 
            { 
                SetRigInstanceID(rigInstanceReference.InstanceSlot);
                rigInstanceReference.OnCreateInstanceID += SetRigInstanceID; 
            }
            if (characterInstanceReference != null) 
            {
                SetCharacterInstanceID(characterInstanceReference.InstanceSlot);
                characterInstanceReference.OnCreateInstanceID += SetCharacterInstanceID;

                characterInstanceReference.AddChild(this);
            }
        }

        protected override void CreateInstance(List<InstancedRendering.MaterialPropertyInstanceOverride<float>> floatOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Color>> colorOverrides,
            List<InstancedRendering.MaterialPropertyInstanceOverride<Vector4>> vectorOverrides)
        {
            base.CreateInstance(floatOverrides, colorOverrides, vectorOverrides);

            instance.SetFloatOverride(CharacterMeshData.ShapesInstanceIDPropertyName, ShapesInstanceID, false);
            instance.SetFloatOverride(CharacterMeshData.RigInstanceIDPropertyName, RigInstanceID, false);
            instance.SetFloatOverride(CharacterMeshData.CharacterInstanceIDPropertyName, CharacterInstanceID, true);  

            InitBuffers();
        }

        protected Transform[] bones;
        public override Transform[] Bones
        {
            get
            {
                if (bones == null)
                {
                    var rig_root = RigRoot;

                    if (avatar == null)
                    {
                        bones = new Transform[] { rig_root }; 
                    }
                    else
                    {
                        bones = new Transform[avatar.bones.Length];
                        for (int a = 0; a < avatar.bones.Length; a++) bones[a] = rig_root.FindDeepChildLiberal(avatar.bones[a]); 
                    }
                }

                return bones;
            }
        }

        public override int BoneCount => avatar == null ? 1 : avatar.bones.Length;

        public override Matrix4x4[] BindPose => meshData.ManagedBindPose;


        /*protected bool dirtyFlag_standaloneShapesControl;
        protected NativeArray<float> standaloneShapesControl;
        public NativeArray<float> StandaloneShapesControl
        {
            get
            {
                if (shapesInstanceReference is CustomizableCharacterMesh ccm) return ccm.StandaloneShapesControl;

                if (!standaloneShapesControl.IsCreated && !IsDestroyed)
                {
                    standaloneShapesControl = new NativeArray<float>(CharacterMeshData.StandaloneShapesCount, Allocator.Persistent);
                }

                return standaloneShapesControl;
            }
        }*/
        public int FirstStandaloneShapesControlIndex => instance.slot * CharacterMeshData.StandaloneShapesCount;
        public float GetStandaloneShapeWeightUnsafe(int shapeIndex) => StandaloneShapeControlBuffer[FirstStandaloneShapesControlIndex + shapeIndex];//StandaloneShapesControl[shapeIndex];
        public float GetStandaloneShapeWeight(int shapeIndex)
        {
            if (instance == null || shapeIndex < 0 || shapeIndex >= CharacterMeshData.StandaloneShapesCount) return 0;
            return GetStandaloneShapeWeightUnsafe(shapeIndex);
        }
        public void SetStandaloneShapeWeightUnsafe(int shapeIndex, float weight)
        {
            //var array = StandaloneShapesControl;
            //array[shapeIndex] = weight;

            if (shapesInstanceReference != null) return;
            StandaloneShapeControlBuffer[FirstStandaloneShapesControlIndex + shapeIndex] = weight;  

            SyncStandaloneShape(shapeIndex, weight);

            //dirtyFlag_standaloneShapesControl = true;
        }
        public void SetStandaloneShapeWeight(int shapeIndex, float weight)
        {
            if (instance == null || shapeIndex < 0 || shapeIndex >= CharacterMeshData.StandaloneShapesCount) return; 
            SetStandaloneShapeWeightUnsafe(shapeIndex, weight);
        }
        internal readonly static Dictionary<string, InstanceBuffer<float>> _standaloneShapeControlBuffers = new Dictionary<string, InstanceBuffer<float>>();
        protected InstanceBuffer<float> standaloneShapeControlBuffer;
        public InstanceBuffer<float> StandaloneShapeControlBuffer
        {
            get
            {
                if (standaloneShapeControlBuffer == null)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{ShapeBufferID}.{nameof(StandaloneShapeControlBuffer)}";
                    string matProperty = CharacterMeshData.StandaloneShapesControlPropertyName;
                    if (!_standaloneShapeControlBuffers.TryGetValue(bufferID, out standaloneShapeControlBuffer) || standaloneShapeControlBuffer == null || !standaloneShapeControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float>(matProperty, CharacterMeshData.StandaloneShapesCount, 2, out standaloneShapeControlBuffer);
                        _standaloneShapeControlBuffers[bufferID] = standaloneShapeControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        meshGroup.BindInstanceMaterialBuffer(matProperty, standaloneShapeControlBuffer);
                        /*for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            standaloneShapeControlBuffer.BindMaterialProperty(material, matProperty);
                        }*/

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return standaloneShapeControlBuffer;
            }
        }

        /*protected bool dirtyFlag_muscleGroupsControl;
        protected NativeArray<MuscleDataLR> muscleGroupsControl;
        public NativeArray<MuscleDataLR> MuscleGroupsControl
        {
            get
            {
                if (characterInstanceReference != null) return characterInstanceReference.MuscleGroupsControl;

                if (!muscleGroupsControl.IsCreated && !IsDestroyed)
                {
                    muscleGroupsControl = new NativeArray<MuscleDataLR>(CharacterMeshData.MuscleVertexGroupCount, Allocator.Persistent);
                }

                return muscleGroupsControl;
            }
        }*/
        public int FirstMuscleGroupsControlIndex => instance.slot * CharacterMeshData.MuscleVertexGroupCount;
        public MuscleDataLR GetMuscleDataUnsafe(int groupIndex) => MuscleGroupsControlBuffer[FirstMuscleGroupsControlIndex + groupIndex];//MuscleGroupsControl[groupIndex];
        public MuscleDataLR GetMuscleData(int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= CharacterMeshData.MuscleVertexGroupCount) return default;
            return GetMuscleDataUnsafe(groupIndex);
        }
        public UnityEvent<int> OnMuscleDataChanged;
        public void SetMuscleDataUnsafe(int groupIndex, MuscleDataLR data)   
        {
            //var array = MuscleGroupsControl;
            //array[groupIndex] = data;

            if (characterInstanceReference != null) return;
            MuscleGroupsControlBuffer[FirstMuscleGroupsControlIndex + groupIndex] = data;

            SyncMuscleMassData(groupIndex, data.valuesLeft.mass, data.valuesRight.mass);
            SyncMuscleFlexData(groupIndex, data.valuesLeft.flex, data.valuesRight.flex);

            //dirtyFlag_muscleGroupsControl = true;

            OnMuscleDataChanged?.Invoke(groupIndex);
        }
        public void SetMuscleData(int groupIndex, MuscleDataLR data)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= CharacterMeshData.MuscleVertexGroupCount) return;
            SetMuscleDataUnsafe(groupIndex, data);
        }
        internal readonly static Dictionary<string, InstanceBuffer<MuscleDataLR>> _muscleGroupsControlBuffers = new Dictionary<string, InstanceBuffer<MuscleDataLR>>();
        protected InstanceBuffer<MuscleDataLR> muscleGroupsControlBuffer;
        public InstanceBuffer<MuscleDataLR> MuscleGroupsControlBuffer
        {
            get
            {
                if (muscleGroupsControlBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{MorphBufferID}.{nameof(MuscleGroupsControlBuffer)}";
                    string matProperty = CharacterMeshData.MuscleGroupsControlPropertyName;
                    if (!_muscleGroupsControlBuffers.TryGetValue(bufferID, out muscleGroupsControlBuffer) || muscleGroupsControlBuffer == null || !muscleGroupsControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<MuscleDataLR>(matProperty, CharacterMeshData.MuscleVertexGroupCount, 2, out muscleGroupsControlBuffer);
                        _muscleGroupsControlBuffers[bufferID] = muscleGroupsControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        meshGroup.BindInstanceMaterialBuffer(matProperty, muscleGroupsControlBuffer);
                        /*for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            muscleGroupsControlBuffer.BindMaterialProperty(material, matProperty);
                        }*/

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return muscleGroupsControlBuffer;
            }
        }

        /*protected bool dirtyFlag_fatGroupsControl;
        protected NativeArray<float3> fatGroupsControl;
        public NativeArray<float3> FatGroupsControl
        {
            get
            {
                if (characterInstanceReference != null) return characterInstanceReference.FatGroupsControl;

                if (!fatGroupsControl.IsCreated && !IsDestroyed)
                {
                    fatGroupsControl = new NativeArray<float3>(CharacterMeshData.FatVertexGroupCount, Allocator.Persistent);
                    if (CharacterMeshData.fatGroupModifiers == null)
                    {
                        for (int a = 0; a < fatGroupsControl.Length; a++) fatGroupsControl[a] = new float3(0, CustomizableCharacterMeshData.DefaultFatGroupModifier.x, CustomizableCharacterMeshData.DefaultFatGroupModifier.y);
                    }
                    else
                    {
                        for (int a = 0; a < fatGroupsControl.Length; a++) 
                        {
                            var modifier = CharacterMeshData.GetFatGroupModifier(a);
                            fatGroupsControl[a] = new float3(0, modifier.x, modifier.y); 
                        }
                    }
                }

                return fatGroupsControl;
            }
        }*/
        public int FirstFatGroupsControlIndex => instance.slot * CharacterMeshData.FatVertexGroupCount;
        public float GetFatLevelUnsafe(int groupIndex) => FatGroupsControlBuffer[FirstFatGroupsControlIndex + groupIndex].x;//FatGroupsControl[groupIndex].x;
        public float GetFatLevel(int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= CharacterMeshData.FatVertexGroupCount) return 0;
            return GetFatLevelUnsafe(groupIndex);
        }
        public UnityEvent<int> OnFatDataChanged;
        public void SetFatLevelUnsafe(int groupIndex, float level)
        {
            //var array = FatGroupsControl;
            //var val = array[groupIndex];
            //val.x = level;
            //array[groupIndex] = val;

            if (characterInstanceReference != null) return;
            int ind = FirstFatGroupsControlIndex + groupIndex;
            var val = FatGroupsControlBuffer[ind];
            val.x = level;
            FatGroupsControlBuffer[ind] = val;

            SyncFatLevel(groupIndex, level);

            //dirtyFlag_fatGroupsControl = true;

            OnFatDataChanged?.Invoke(groupIndex);
        }
        public void SetFatLevel(int groupIndex, float level)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= CharacterMeshData.FatVertexGroupCount) return;
            SetFatLevelUnsafe(groupIndex, level); 
        }
        internal readonly static Dictionary<string, InstanceBuffer<float3>> _fatGroupsControlBuffers = new Dictionary<string, InstanceBuffer<float3>>();
        protected InstanceBuffer<float3> fatGroupsControlBuffer;
        public InstanceBuffer<float3> FatGroupsControlBuffer
        {
            get
            {
                if (fatGroupsControlBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{MorphBufferID}.{nameof(FatGroupsControlBuffer)}";
                    string matProperty = CharacterMeshData.FatGroupsControlPropertyName;
                    if (!_fatGroupsControlBuffers.TryGetValue(bufferID, out fatGroupsControlBuffer) || fatGroupsControlBuffer == null || !fatGroupsControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float3>(matProperty, CharacterMeshData.FatVertexGroupCount, 2, out fatGroupsControlBuffer);
                        _fatGroupsControlBuffers[bufferID] = fatGroupsControlBuffer;

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        meshGroup.BindInstanceMaterialBuffer(matProperty, fatGroupsControlBuffer);
                        /*for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            fatGroupsControlBuffer.BindMaterialProperty(material, matProperty); 
                        }*/

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return fatGroupsControlBuffer;
            }
        }

        /*protected bool dirtyFlag_variationShapesControl;
        protected NativeArray<float2> variationShapesControl;
        public NativeArray<float2> VariationShapesControl
        {
            get
            {
                if (characterInstanceReference != null) return characterInstanceReference.VariationShapesControl;

                if (!variationShapesControl.IsCreated && !IsDestroyed)
                {
                    variationShapesControl = new NativeArray<float2>(CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount, Allocator.Persistent);
                }

                return variationShapesControl;
            }
        }*/
        public int VariationShapesControlDataSize => CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount;
        public int FirstVariationShapesControlIndex => instance.slot * VariationShapesControlDataSize;
        
        public int GetPartialVariationShapeIndex(int variationGroupIndex, int shapeIndex)
        {
            if (variationGroupIndex < 0 || variationGroupIndex >= CharacterMeshData.VariationVertexGroupCount || shapeIndex < 0 || shapeIndex >= CharacterMeshData.VariationShapesCount) return -1;
            return (variationGroupIndex * CharacterMeshData.VariationShapesCount) + shapeIndex;
        }
        
        public float2 GetVariationWeightUnsafe(int variationShapeIndex, int groupIndex) => VariationShapesControlBuffer[FirstVariationShapesControlIndex + (groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex];//VariationShapesControl[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex];
        public float2 GetVariationWeight(int variationShapeIndex, int groupIndex)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= CharacterMeshData.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= CharacterMeshData.VariationShapesCount) return 0;
            return GetVariationWeightUnsafe(variationShapeIndex, groupIndex);  
        }
        public void SetVariationWeightUnsafe(int variationShapeIndex, int groupIndex, float2 weight)
        {
            //var array = VariationShapesControl;
            //array[(groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex] = weight;

            if (characterInstanceReference != null)
            {
                characterInstanceReference.SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
                return;
            }
            VariationShapesControlBuffer[FirstVariationShapesControlIndex + (groupIndex * CharacterMeshData.VariationShapesCount) + variationShapeIndex] = weight;

            SyncVariationData(groupIndex, variationShapeIndex, weight.x, weight.y);  

            //dirtyFlag_variationShapesControl = true;
        }
        public void SetVariationWeight(int variationShapeIndex, int groupIndex, float2 weight)
        {
            if (instance == null || groupIndex < 0 || groupIndex >= CharacterMeshData.VariationVertexGroupCount || variationShapeIndex < 0 || variationShapeIndex >= CharacterMeshData.VariationShapesCount) return;
            SetVariationWeightUnsafe(variationShapeIndex, groupIndex, weight);
        }

        public float2 GetVariationWeightUnsafe(int indexInArray) => VariationShapesControlBuffer[FirstVariationShapesControlIndex + indexInArray]; //VariationShapesControl[indexInArray];
        public float2 GetVariationWeight(int indexInArray)
        {
            if (indexInArray < 0 || instance == null || indexInArray >= VariationShapesControlDataSize) return 0;
            return GetVariationWeightUnsafe(indexInArray);
        }
        public void SetVariationWeightUnsafe(int indexInArray, float2 weight) => SetVariationWeightUnsafe(indexInArray % CharacterMeshData.VariationShapesCount, indexInArray / CharacterMeshData.VariationShapesCount, weight); 
        public void SetVariationWeight(int indexInArray, float2 weight)
        {
            if (indexInArray < 0 || instance == null || indexInArray >= VariationShapesControlDataSize) return;
            SetVariationWeightUnsafe(indexInArray, weight);
        }

        internal readonly static Dictionary<string, InstanceBuffer<float2>> _variationShapesControlBuffers = new Dictionary<string, InstanceBuffer<float2>>(); 
        protected InstanceBuffer<float2> variationShapesControlBuffer;
        public InstanceBuffer<float2> VariationShapesControlBuffer
        {
            get
            {
                if (variationShapesControlBuffer == null && !IsDestroyed)
                {
                    var meshGroup = MeshGroup;
                    string bufferID = $"{MorphBufferID}.{nameof(VariationShapesControlBuffer)}";
                    string matProperty = CharacterMeshData.VariationShapesControlPropertyName;
                    if (!_variationShapesControlBuffers.TryGetValue(bufferID, out variationShapesControlBuffer) || variationShapesControlBuffer == null || !variationShapesControlBuffer.IsValid())
                    {
                        meshGroup.CreateInstanceMaterialBuffer<float2>(matProperty, CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount, 2, out variationShapesControlBuffer);  
                        _variationShapesControlBuffers[bufferID] = variationShapesControlBuffer; 

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                    else if (!meshGroup.HasRuntimeData(bufferID))
                    {
                        meshGroup.BindInstanceMaterialBuffer(matProperty, variationShapesControlBuffer);
                        /*for (int a = 0; a < meshGroup.MaterialCount; a++)
                        {
                            var material = meshGroup.GetMaterial(a);
                            variationShapesControlBuffer.BindMaterialProperty(material, matProperty);
                        }*/

                        meshGroup.SetRuntimeData(bufferID, true);
                    }
                }

                return variationShapesControlBuffer;
            }
        }

        protected void InitBuffers()
        {
            standaloneShapeControlBuffer = StandaloneShapeControlBuffer;
            muscleGroupsControlBuffer = MuscleGroupsControlBuffer;
            fatGroupsControlBuffer = FatGroupsControlBuffer;
            variationShapesControlBuffer = VariationShapesControlBuffer;

            if (instance != null)
            {
                if (shapesInstanceReference == null)
                {
                    if (meshData.StandaloneShapesCount > 0) SetStandaloneShapeWeightUnsafe(0, 0);
                }

                if (characterInstanceReference == null)
                {
                    if (meshData.MuscleVertexGroupCount > 0) SetMuscleDataUnsafe(0, new MuscleDataLR()); 
                    if (meshData.FatVertexGroupCount > 0) // Apply fat group modifiers (.y controls how much mass is nerfed by fat)
                    {
                        int indexStart = FirstFatGroupsControlIndex;
                        if (CharacterMeshData.fatGroupModifiers == null)
                        {
                            for (int a = 0; a < CharacterMeshData.FatVertexGroupCount; a++) fatGroupsControlBuffer.WriteToBufferFast(indexStart + a, new float3(0, CustomizableCharacterMeshData.DefaultFatGroupModifier.x, CustomizableCharacterMeshData.DefaultFatGroupModifier.y));
                        }
                        else
                        {
                            for (int a = 0; a < CharacterMeshData.FatVertexGroupCount; a++)
                            {
                                var modifier = CharacterMeshData.GetFatGroupModifier(a);
                                fatGroupsControlBuffer.WriteToBufferFast(indexStart + a, new float3(0, modifier.x, modifier.y));
                            }
                        }

                        fatGroupsControlBuffer.TrySetWriteIndices(indexStart, CharacterMeshData.FatVertexGroupCount);
                        fatGroupsControlBuffer.RequestUpload();
                    }
                    if (meshData.VariationVertexGroupCount > 0) SetVariationWeightUnsafe(0, 0);
                }
            }
        }
        public void UpdateBuffers()
        {
            //if (instance == null || IsDestroyed) return;

            /*if (dirtyFlag_standaloneShapesControl)
            {
                if (StandaloneShapeControlBuffer.WriteToBuffer(StandaloneShapesControl, 0, InstanceSlot * CharacterMeshData.StandaloneShapesCount, CharacterMeshData.StandaloneShapesCount)) dirtyFlag_standaloneShapesControl = false;
            }

            if (dirtyFlag_muscleGroupsControl)
            {
                if (MuscleGroupsControlBuffer.WriteToBuffer(MuscleGroupsControl, 0, InstanceSlot * CharacterMeshData.MuscleVertexGroupCount, CharacterMeshData.MuscleVertexGroupCount)) dirtyFlag_muscleGroupsControl = false;
            }

            if (dirtyFlag_fatGroupsControl)
            {
                if (FatGroupsControlBuffer.WriteToBuffer(FatGroupsControl, 0, InstanceSlot * CharacterMeshData.FatVertexGroupCount, CharacterMeshData.FatVertexGroupCount)) dirtyFlag_fatGroupsControl = false; 
            }

            if (dirtyFlag_variationShapesControl)
            {
                int count = CharacterMeshData.VariationShapesCount * CharacterMeshData.VariationVertexGroupCount;
                if (VariationShapesControlBuffer.WriteToBuffer(VariationShapesControl, 0, InstanceSlot * count, count)) dirtyFlag_variationShapesControl = false;
            }*/
        }

        #region Events

        [Serializable]
        public enum ListenableEvent
        {
            OnMuscleDataChanged,
            OnFatDataChanged
        }

        public void AddListener(ListenableEvent event_, UnityAction<int> listener)
        {
            switch (event_)
            {
                case ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChanged == null) OnMuscleDataChanged = new UnityEvent<int>(); 
                    OnMuscleDataChanged.AddListener(listener);
                    break;
                case ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChanged == null) OnFatDataChanged = new UnityEvent<int>();
                    OnFatDataChanged.AddListener(listener);
                    break;
            }
        }
        public void RemoveListener(ListenableEvent event_, UnityAction<int> listener)
        {
            switch (event_)
            {
                case ListenableEvent.OnMuscleDataChanged:
                    if (OnMuscleDataChanged != null) OnMuscleDataChanged.RemoveListener(listener);
                    break;
                case ListenableEvent.OnFatDataChanged:
                    if (OnFatDataChanged != null) OnFatDataChanged.RemoveListener(listener);
                    break;
            }
        }
        public void ClearListeners()
        {
            if (OnMuscleDataChanged != null) OnMuscleDataChanged.RemoveAllListeners();
            if (OnFatDataChanged != null) OnFatDataChanged.RemoveAllListeners();
        }

        #endregion
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleData : IEquatable<MuscleData>
    {
        [Range(0, 2)]
        public float mass;
        [Range(0, 1.5f)]
        public float flex;
        [Range(0, 1)]
        public float pump;

        public static implicit operator MuscleData(float3 data) => new MuscleData() { mass = data.x, flex = data.y, pump = data.z };
        public static implicit operator float3(MuscleData data) => new float3(data.mass, data.flex, data.pump);

        public override bool Equals(object obj)
        {
            if (obj is MuscleData dat) return dat.mass == mass && dat.flex == flex && dat.pump == pump;
            return false;
        }
        public bool Equals(MuscleData dat) => dat.mass == mass && dat.flex == flex && dat.pump == pump;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MuscleData dat1, MuscleData dat2) => dat1.Equals(dat2);
        public static bool operator !=(MuscleData dat1, MuscleData dat2) => !dat1.Equals(dat2);
    }

    [Serializable, StructLayout(LayoutKind.Sequential)]
    public struct MuscleDataLR : IEquatable<MuscleDataLR>
    {
        public MuscleData valuesLeft;
        public MuscleData valuesRight;

        public override bool Equals(object obj)
        {
            if (obj is MuscleDataLR dat) return dat.valuesLeft == valuesLeft && dat.valuesRight == valuesRight;
            return false;
        }
        public bool Equals(MuscleDataLR dat) => dat.valuesLeft == valuesLeft && dat.valuesRight == valuesRight;

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(MuscleDataLR dat1, MuscleDataLR dat2) => dat1.Equals(dat2);
        public static bool operator !=(MuscleDataLR dat1, MuscleDataLR dat2) => !dat1.Equals(dat2);
    }

}

#endif