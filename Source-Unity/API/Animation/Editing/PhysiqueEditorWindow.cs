#if (UNITY_STANDALONE || UNITY_EDITOR)

using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using Unity.Mathematics;

using Swole.UI;

namespace Swole.API.Unity
{
    public class PhysiqueEditorWindow : MonoBehaviour
    {
        public bool includeAdditionalSettings = true;

        public UICategorizedList list;

        [SerializeField]
        protected MuscularRenderedCharacter character;
        public MuscularRenderedCharacter Character
        {
            get => character;
            set
            {
                character = value;
                Refresh();
            }
        }

        protected void Awake()
        {
            if (list == null) list = gameObject.GetComponent<UICategorizedList>();
        }

        public const string _globalParameterName = "GLOBAL";
        public const string _breastParameterName = "BREAST PRESENCE";
        public const string _massParameterName = "MASS";
        public const string _flexParameterName = "FLEX";
        public const string _pumpParameterName = "PUMP";

        public float maxBreast = 2;
        public float maxMass = 2;
        public float maxFlex = 1.5f;
        public float maxPump = 1;

        public void Refresh() 
        {
            if (character == null) return;
            if (list == null) 
            { 
                list = gameObject.GetComponent<UICategorizedList>();
                if (list == null) return;
            }
            list.Clear();

            if (includeAdditionalSettings)
            {
                int categoryIndex = list.CategoryCount;
                var category = list.AddNewCategory("Other Settings");

                var memBreast = list.AddNewListMember($"{_breastParameterName}", category);
                var memMass = list.AddNewListMember($"{_globalParameterName} {_massParameterName}", category);
                var memFlex = list.AddNewListMember($"{_globalParameterName} {_flexParameterName}", category);
                var memPump = list.AddNewListMember($"{_globalParameterName} {_pumpParameterName}", category);

                var sliderBreast = memBreast == null || memBreast.gameObject == null ? null : memBreast.gameObject.GetComponentInChildren<Slider>();
                if (sliderBreast != null)
                {
                    sliderBreast.minValue = 0;
                    sliderBreast.maxValue = maxBreast;
                    sliderBreast.SetValueWithoutNotify(character.BreastPresence);

                    if (sliderBreast.onValueChanged == null) sliderBreast.onValueChanged = new Slider.SliderEvent(); else sliderBreast.onValueChanged.RemoveAllListeners();
                    sliderBreast.onValueChanged.AddListener((float val) =>
                    {
                        character.BreastPresence = val; 
                    });
                }

                float3 averageValues = character.GetAverageMuscleValues();
                var sliderMass = memMass == null || memMass.gameObject == null ? null : memMass.gameObject.GetComponentInChildren<Slider>();
                var sliderFlex = memFlex == null || memFlex.gameObject == null ? null : memFlex.gameObject.GetComponentInChildren<Slider>();
                var sliderPump = memPump == null || memPump.gameObject == null ? null : memPump.gameObject.GetComponentInChildren<Slider>();
                if (sliderMass != null)
                {
                    sliderMass.minValue = 0;
                    sliderMass.maxValue = maxMass;
                    sliderMass.SetValueWithoutNotify(averageValues.x);

                    if (sliderMass.onValueChanged == null) sliderMass.onValueChanged = new Slider.SliderEvent(); else sliderMass.onValueChanged.RemoveAllListeners();
                    sliderMass.onValueChanged.AddListener((float val) => 
                    { 
                        character.SetGlobalMass(val); 
                        for(int a = 0; a < character.MuscleGroupCount; a++)
                        {
                            int i = a + categoryIndex + 1;
                            var cat = list.GetCategory(i);
                            var slider = cat[0].gameObject.GetComponentInChildren<Slider>(); 
                            if (slider != null) slider.SetValueWithoutNotify(val);
                        }
                    });
                }
                if (sliderFlex != null)
                {
                    sliderFlex.minValue = 0;
                    sliderFlex.maxValue = maxFlex;
                    sliderFlex.SetValueWithoutNotify(averageValues.y);

                    if (sliderFlex.onValueChanged == null) sliderFlex.onValueChanged = new Slider.SliderEvent(); else sliderFlex.onValueChanged.RemoveAllListeners();
                    sliderFlex.onValueChanged.AddListener((float val) => 
                    { 
                        character.SetGlobalFlex(val);
                        for (int a = 0; a < character.MuscleGroupCount; a++)
                        {
                            int i = a + categoryIndex + 1;
                            var cat = list.GetCategory(i);
                            var slider = cat[1].gameObject.GetComponentInChildren<Slider>();
                            if (slider != null) slider.SetValueWithoutNotify(val);
                        }
                    });
                }
                if (sliderPump != null)
                {
                    sliderPump.minValue = 0;
                    sliderPump.maxValue = maxPump;
                    sliderPump.SetValueWithoutNotify(averageValues.z);

                    if (sliderPump.onValueChanged == null) sliderPump.onValueChanged = new Slider.SliderEvent(); else sliderPump.onValueChanged.RemoveAllListeners();
                    sliderPump.onValueChanged.AddListener((float val) => 
                    { 
                        character.SetGlobalPump(val);
                        for (int a = 0; a < character.MuscleGroupCount; a++)
                        {
                            int i = a + categoryIndex + 1;
                            var cat = list.GetCategory(i);
                            var slider = cat[2].gameObject.GetComponentInChildren<Slider>();
                            if (slider != null) slider.SetValueWithoutNotify(val);
                        }
                    });
                }
            }

            for(int a = 0; a < character.MuscleGroupCount; a++)
            {
                int index = a;

                var muscleValues = character.GetMuscleGroupValuesUnsafe(index);

                var category = list.AddNewCategory(character.GetMuscleGroupNameUnsafe(index));

                var memMass = list.AddNewListMember(_massParameterName, category);
                var memFlex = list.AddNewListMember(_flexParameterName, category);
                var memPump = list.AddNewListMember(_pumpParameterName, category);

                var sliderMass = memMass == null || memMass.gameObject == null ? null : memMass.gameObject.GetComponentInChildren<Slider>();
                var sliderFlex = memFlex == null || memFlex.gameObject == null ? null : memFlex.gameObject.GetComponentInChildren<Slider>();
                var sliderPump = memPump == null || memPump.gameObject == null ? null : memPump.gameObject.GetComponentInChildren<Slider>();

                if (sliderMass != null)
                {
                    sliderMass.minValue = 0;
                    sliderMass.maxValue = maxMass;
                    sliderMass.SetValueWithoutNotify(muscleValues.x);

                    if (sliderMass.onValueChanged == null) sliderMass.onValueChanged = new Slider.SliderEvent(); else sliderMass.onValueChanged.RemoveAllListeners();
                    sliderMass.onValueChanged.AddListener((float val) => character.SetMuscleGroupMassUnsafe(index, val));
                }
                if (sliderFlex != null)
                {
                    sliderFlex.minValue = 0;
                    sliderFlex.maxValue = maxFlex;
                    sliderFlex.SetValueWithoutNotify(muscleValues.y);

                    if (sliderFlex.onValueChanged == null) sliderFlex.onValueChanged = new Slider.SliderEvent(); else sliderFlex.onValueChanged.RemoveAllListeners();
                    sliderFlex.onValueChanged.AddListener((float val) => character.SetMuscleGroupFlexUnsafe(index, val));
                }
                if (sliderPump != null)
                {
                    sliderPump.minValue = 0;
                    sliderPump.maxValue = maxPump;
                    sliderPump.SetValueWithoutNotify(muscleValues.z);

                    if (sliderPump.onValueChanged == null) sliderPump.onValueChanged = new Slider.SliderEvent(); else sliderPump.onValueChanged.RemoveAllListeners();
                    sliderPump.onValueChanged.AddListener((float val) => character.SetMuscleGroupPumpUnsafe(index, val));
                }

            }
        }

    }
}

#endif