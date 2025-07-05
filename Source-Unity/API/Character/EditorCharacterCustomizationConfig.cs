#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Swole.Morphing;

namespace Swole.API.Unity
{

    [CreateAssetMenu(fileName = "CustomizationConfig", menuName = "Swole/Character/CharacterCustomizationConfig", order = 1)]
    public class EditorCharacterCustomizationConfig : ScriptableObject
    {

        [Range(0, 2)]
        public float bustSizeEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public CustomizableCharacterMesh.NamedFloat[] shapeWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public CustomizableCharacterMesh.NamedMuscleData[] muscleWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public CustomizableCharacterMesh.NamedFloat[] fatWeightsEditor;

#if !UNITY_EDITOR
        [NonSerialized]
#endif
        public CustomizableCharacterMesh.NamedFloat2[] variationWeightsEditor;

        public void Apply(CustomizableCharacterMesh ccm)
        {
            ccm.bustSizeEditor = bustSizeEditor;
            //ccm.shapeWeightsEditor = shapeWeightsEditor;
            //ccm.muscleWeightsEditor = muscleWeightsEditor;
            //ccm.fatWeightsEditor = fatWeightsEditor;
            //ccm.variationWeightsEditor = variationWeightsEditor;

            if (shapeWeightsEditor != null && ccm.shapeWeightsEditor != null)
            {
                for(int a = 0; a < shapeWeightsEditor.Length; a++)
                {
                    var weight = shapeWeightsEditor[a];
                    for(int b = 0; b < ccm.shapeWeightsEditor.Length; b++)
                    {
                        var weight2 = ccm.shapeWeightsEditor[b];
                        if (weight2.name == weight.name)
                        {
                            weight2.value = weight.value;
                            ccm.shapeWeightsEditor[b] = weight2; 
                            break;
                        }
                    }
                }
            }
            if (muscleWeightsEditor != null && ccm.muscleWeightsEditor != null)
            {
                for (int a = 0; a < muscleWeightsEditor.Length; a++)
                {
                    var weight = muscleWeightsEditor[a];
                    for (int b = 0; b < ccm.muscleWeightsEditor.Length; b++)
                    {
                        var weight2 = ccm.muscleWeightsEditor[b];
                        if (weight2.name == weight.name)
                        {
                            weight2.value = weight.value;
                            ccm.muscleWeightsEditor[b] = weight2;
                            break;
                        }
                    }
                }
            }
            if (fatWeightsEditor != null && ccm.fatWeightsEditor != null)
            {
                for (int a = 0; a < fatWeightsEditor.Length; a++)
                {
                    var weight = fatWeightsEditor[a];
                    for (int b = 0; b < ccm.fatWeightsEditor.Length; b++)
                    {
                        var weight2 = ccm.fatWeightsEditor[b];
                        if (weight2.name == weight.name)
                        {
                            weight2.value = weight.value;
                            ccm.fatWeightsEditor[b] = weight2;
                            break;
                        }
                    }
                }
            }
            if (variationWeightsEditor != null && ccm.variationWeightsEditor != null)
            {
                for (int a = 0; a < variationWeightsEditor.Length; a++)
                {
                    var weight = variationWeightsEditor[a];
                    for (int b = 0; b < ccm.variationWeightsEditor.Length; b++)
                    {
                        var weight2 = ccm.variationWeightsEditor[b];
                        if (weight2.name == weight.name)
                        {
                            weight2.value = weight.value;
                            ccm.variationWeightsEditor[b] = weight2;
                            break;
                        }
                    }
                }
            }

            ccm.UpdateInEditor();
        }

        public static EditorCharacterCustomizationConfig CreateAndSave(string saveDir, string assetName, CustomizableCharacterMesh ccm)
        {
            var config = ScriptableObject.CreateInstance<EditorCharacterCustomizationConfig>();

            config.bustSizeEditor = ccm.bustSizeEditor;
            config.shapeWeightsEditor = ccm.shapeWeightsEditor;
            config.muscleWeightsEditor = ccm.muscleWeightsEditor;
            config.fatWeightsEditor = ccm.fatWeightsEditor;
            config.variationWeightsEditor = ccm.variationWeightsEditor;

            config.name = string.IsNullOrWhiteSpace(assetName) ? ccm.name : assetName;
            config.CreateOrReplaceAsset(config.CreateUnityAssetPathString(saveDir));

            return config;
        }

    }

}

#endif